using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Enriches books from the Google Books API. Looks a book up by ISBN when it has one, otherwise
    /// by title plus author (accepting only a volume whose author list contains the book's author,
    /// so a loose title search cannot attach the wrong book). Every write is fill-only: existing
    /// values are never overwritten, and the external ids the lookup returns are stored so the
    /// book acquires a stable identity for future dedup.
    /// Processes books in batches with rate limiting to respect API guidelines.
    /// </summary>
    public class BookDescriptionEnrichmentService : IBookDescriptionEnrichmentService
    {
        // Placeholder author written by stub creation and imports when the source has no author.
        // A title-only Google lookup has no author guard, so these books are not candidates.
        private const string UnknownAuthor = "Unknown Author";

        private const int TitleAuthorSearchResults = 5;

        /// <summary>
        /// The single definition of "needs enrichment", shared by the count and the batch query:
        /// no description, and either an ISBN or a title plus a real author to look up.
        /// </summary>
        private static readonly Expression<Func<Book, bool>> NeedsEnrichment = b =>
            (b.Description == null || b.Description == "")
            && ((b.ISBN != null && b.ISBN != "")
                || (b.Title != "" && b.Author != "" && b.Author != UnknownAuthor));

        private readonly IApplicationDbContext _context;
        private readonly IGoogleBooksApiClient _googleBooksClient;
        private readonly ILogger<BookDescriptionEnrichmentService> _logger;

        public BookDescriptionEnrichmentService(
            IApplicationDbContext context,
            IGoogleBooksApiClient googleBooksClient,
            ILogger<BookDescriptionEnrichmentService> logger)
        {
            _context = context;
            _googleBooksClient = googleBooksClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<int> GetBooksNeedingEnrichmentCountAsync()
        {
            return await _context.Books.Where(NeedsEnrichment).CountAsync();
        }

        /// <inheritdoc />
        public async Task<BookDescriptionEnrichmentResult> EnrichBooksWithoutDescriptionsAsync(
            int batchSize = 50,
            int delayBetweenCallsMs = 1000,
            CancellationToken cancellationToken = default)
        {
            var result = new BookDescriptionEnrichmentResult { StartedAt = DateTime.UtcNow };

            try
            {
                var booksToEnrich = await _context.Books
                    .Where(NeedsEnrichment)
                    .OrderBy(b => b.DateAdded) // Process oldest first
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = booksToEnrich.Count;

                if (booksToEnrich.Count == 0)
                {
                    _logger.LogInformation("No books found needing description enrichment");
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }

                _logger.LogInformation("Starting book description enrichment for {Count} books", booksToEnrich.Count);

                foreach (var book in booksToEnrich)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Book description enrichment canceled");
                        result.WasCancelled = true;
                        break;
                    }

                    try
                    {
                        var outcome = await TryEnrichAsync(book, cancellationToken);

                        switch (outcome.Status)
                        {
                            case EnrichStatus.Enriched:
                                result.EnrichedCount++;
                                _logger.LogDebug("Enriched {Title}: filled {Fields}", book.Title, string.Join(", ", outcome.FilledFields));
                                break;
                            case EnrichStatus.NotFound:
                                // Left null on purpose so the book is retried if Google Books gains the data later.
                                result.NotFoundCount++;
                                _logger.LogDebug("No description found for: {Title}", book.Title);
                                break;
                            case EnrichStatus.NoLookupKey:
                                result.SkippedCount++;
                                break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to enrich '{book.Title}': {ex.Message}");
                        _logger.LogWarning(ex, "Failed to enrich description for book: {Title}", book.Title);
                    }

                    // Rate limiting: delay between API calls
                    if (delayBetweenCallsMs > 0)
                    {
                        await Task.Delay(delayBetweenCallsMs, cancellationToken);
                    }
                }

                // Save all changes, including ids filled on books that still have no description.
                await _context.SaveChangesAsync(cancellationToken);

                if (result.FailedCount > 0)
                {
                    result.WarningMessage = $"{result.FailedCount} of {result.TotalProcessed} book lookups failed";
                }

                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Book description enrichment complete. Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}, Skipped: {Skipped}",
                    result.EnrichedCount, result.NotFoundCount, result.FailedCount, result.SkippedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                result.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Book description enrichment was canceled");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Enrichment run failed: {ex.Message}";
                _logger.LogError(ex, "Book description enrichment run failed");
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<SingleBookEnrichmentResult> EnrichBookByIdAsync(Guid bookId, CancellationToken cancellationToken = default)
        {
            var result = new SingleBookEnrichmentResult();

            try
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken);

                if (book == null)
                {
                    result.NotFound = true;
                    result.ErrorMessage = "Book not found";
                    return result;
                }

                result.BookTitle = book.Title;

                if (!string.IsNullOrWhiteSpace(book.Description))
                {
                    result.AlreadyHasDescription = true;
                    result.Description = book.Description;
                    result.Success = true;
                    return result;
                }

                var outcome = await TryEnrichAsync(book, cancellationToken);
                result.FilledFields = outcome.FilledFields.ToList();

                switch (outcome.Status)
                {
                    case EnrichStatus.NoLookupKey:
                        result.NoLookupKey = true;
                        result.ErrorMessage = "Book has no ISBN or title and author to look up";
                        return result;

                    case EnrichStatus.NotFound:
                        // Ids may still have been filled from a volume without a description.
                        if (outcome.FilledFields.Count > 0)
                        {
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                        result.ErrorMessage = "No description found in Google Books for this book";
                        _logger.LogInformation("No description found for: {Title}", book.Title);
                        return result;

                    case EnrichStatus.Enriched:
                        await _context.SaveChangesAsync(cancellationToken);
                        result.Success = true;
                        result.Description = book.Description;
                        _logger.LogInformation("Successfully enriched {Title}: filled {Fields}", book.Title, string.Join(", ", outcome.FilledFields));
                        return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to enrich book: {ex.Message}";
                _logger.LogError(ex, "Failed to enrich description for book ID: {BookId}", bookId);
            }

            return result;
        }

        private enum EnrichStatus { Enriched, NotFound, NoLookupKey }

        private sealed record EnrichOutcome(EnrichStatus Status, IReadOnlyList<string> FilledFields);

        /// <summary>
        /// Looks the book up (ISBN first, then title plus author) and copies every missing field from
        /// the matched volume onto the entity. Does not save; callers decide when to flush.
        /// </summary>
        private async Task<EnrichOutcome> TryEnrichAsync(Book book, CancellationToken cancellationToken)
        {
            var lookupIsbn = IsbnNormalizer.Normalize(book.ISBN);
            var hasTitleAndAuthor = HasTitleAndRealAuthor(book);

            if (lookupIsbn == null && !hasTitleAndAuthor)
            {
                return new EnrichOutcome(EnrichStatus.NoLookupKey, Array.Empty<string>());
            }

            GoogleBooksVolumeDto? volume = null;

            if (lookupIsbn != null)
            {
                _logger.LogDebug("Looking up {Title} by ISBN {ISBN}", book.Title, lookupIsbn);
                var byIsbn = await _googleBooksClient.SearchBooksByISBNAsync(lookupIsbn);
                volume = byIsbn?.Items?.FirstOrDefault(v => v.VolumeInfo != null);
            }

            if (volume == null && hasTitleAndAuthor)
            {
                _logger.LogDebug("Looking up {Title} by title and author {Author}", book.Title, book.Author);
                var byTitleAuthor = await _googleBooksClient.SearchBooksAsync(
                    $"intitle:{book.Title} inauthor:{book.Author}", maxResults: TitleAuthorSearchResults);
                volume = byTitleAuthor?.Items?.FirstOrDefault(v => AuthorMatches(v, book.Author));
            }

            if (volume?.VolumeInfo == null)
            {
                return new EnrichOutcome(EnrichStatus.NotFound, Array.Empty<string>());
            }

            var filled = await FillFromVolumeAsync(book, volume, cancellationToken);

            if (filled.Count > 0)
            {
                book.EnrichedAt = DateTime.UtcNow;
                _context.Update(book);
            }

            var status = filled.Contains("description") ? EnrichStatus.Enriched : EnrichStatus.NotFound;
            return new EnrichOutcome(status, filled);
        }

        /// <summary>
        /// Fill-only copy of the volume's data onto the book. Returns the names of the fields written.
        /// </summary>
        private async Task<List<string>> FillFromVolumeAsync(Book book, GoogleBooksVolumeDto volume, CancellationToken cancellationToken)
        {
            var info = volume.VolumeInfo!;
            var filled = new List<string>();

            var description = HtmlText.Strip(info.Description);
            if (string.IsNullOrWhiteSpace(book.Description) && description != null)
            {
                book.Description = description;
                filled.Add("description");
            }

            if (string.IsNullOrWhiteSpace(book.ISBN))
            {
                var isbn = IsbnNormalizer.Normalize(info.GetBestIsbn());
                if (isbn != null)
                {
                    book.ISBN = isbn;
                    filled.Add("isbn");
                }
            }

            if (string.IsNullOrWhiteSpace(book.GoogleVolumeId) && !string.IsNullOrWhiteSpace(volume.Id))
            {
                // GoogleVolumeId is unique per book; if another row already carries this id the two
                // are probably duplicates, which is a merge decision for a person, not this job.
                var takenBy = await _context.Books
                    .Where(b => b.GoogleVolumeId == volume.Id && b.Id != book.Id)
                    .Select(b => b.Title)
                    .FirstOrDefaultAsync(cancellationToken);

                if (takenBy == null)
                {
                    book.GoogleVolumeId = volume.Id;
                    filled.Add("googleVolumeId");
                }
                else
                {
                    _logger.LogWarning(
                        "Not storing Google volume id {VolumeId} on '{Title}': already held by '{OtherTitle}' (possible duplicate)",
                        volume.Id, book.Title, takenBy);
                }
            }

            if (string.IsNullOrWhiteSpace(book.Publisher) && !string.IsNullOrWhiteSpace(info.Publisher))
            {
                book.Publisher = info.Publisher;
                filled.Add("publisher");
            }

            if (book.YearPublished == null)
            {
                var year = info.GetPublishedYear();
                if (year != null)
                {
                    book.YearPublished = year;
                    filled.Add("yearPublished");
                }
            }

            if (string.IsNullOrWhiteSpace(book.Thumbnail))
            {
                var thumbnail = info.ImageLinks?.GetBestThumbnail();
                if (!string.IsNullOrWhiteSpace(thumbnail))
                {
                    book.Thumbnail = thumbnail;
                    filled.Add("thumbnail");
                }
            }

            return filled;
        }

        private static bool HasTitleAndRealAuthor(Book book) =>
            !string.IsNullOrWhiteSpace(book.Title)
            && !string.IsNullOrWhiteSpace(book.Author)
            && !string.Equals(book.Author.Trim(), UnknownAuthor, StringComparison.OrdinalIgnoreCase);

        private static bool AuthorMatches(GoogleBooksVolumeDto volume, string author)
        {
            var authors = volume.VolumeInfo?.Authors;
            if (authors == null || authors.Length == 0) return false;

            var wanted = author.Trim();
            return authors.Any(a => string.Equals(a?.Trim(), wanted, StringComparison.OrdinalIgnoreCase));
        }
    }
}
