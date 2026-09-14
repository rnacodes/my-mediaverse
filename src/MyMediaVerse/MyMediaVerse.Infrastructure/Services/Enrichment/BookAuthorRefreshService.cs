using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <inheritdoc cref="IBookAuthorRefreshService" />
    public class BookAuthorRefreshService : IBookAuthorRefreshService
    {
        private const string UnknownAuthor = "Unknown Author";
        private const int SaveEvery = 25;
        private const int MaxErrors = 20;
        private const int MaxChanges = 50;

        private readonly IApplicationDbContext _context;
        private readonly IGoogleBooksApiClient _googleBooksClient;
        private readonly IOpenLibraryApiClient _openLibraryClient;
        private readonly ILogger<BookAuthorRefreshService> _logger;

        public BookAuthorRefreshService(
            IApplicationDbContext context,
            IGoogleBooksApiClient googleBooksClient,
            IOpenLibraryApiClient openLibraryClient,
            ILogger<BookAuthorRefreshService> logger)
        {
            _context = context;
            _googleBooksClient = googleBooksClient;
            _openLibraryClient = openLibraryClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<BookAuthorRefreshResult> RefreshAuthorsAsync(
            int delayBetweenCallsMs = 500,
            int maxBooks = 5000,
            CancellationToken cancellationToken = default)
        {
            var result = new BookAuthorRefreshResult { StartedAt = DateTime.UtcNow };

            try
            {
                var books = await _context.Books
                    .Where(b => (b.GoogleVolumeId != null && b.GoogleVolumeId != "")
                        || (b.OpenLibraryKey != null && b.OpenLibraryKey != ""))
                    .OrderBy(b => b.Id)
                    .Take(maxBooks)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Refreshing authors for {Count} books with a Google Books or Open Library id", books.Count);

                var pendingSaves = 0;
                foreach (var book in books)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    result.TotalChecked++;

                    try
                    {
                        var sourceAuthors = await LookUpAuthorsAsync(book);
                        if (sourceAuthors == null)
                        {
                            result.NotFoundCount++;
                        }
                        else
                        {
                            var outcome = Decide(book.Author, sourceAuthors, out var refreshed);
                            switch (outcome)
                            {
                                case RefreshOutcome.Update:
                                    if (result.Changes.Count < MaxChanges)
                                    {
                                        result.Changes.Add($"{book.Title}: {book.Author} → {refreshed}");
                                    }
                                    book.Author = refreshed!;
                                    result.UpdatedCount++;
                                    pendingSaves++;
                                    break;
                                case RefreshOutcome.Unchanged:
                                    result.UnchangedCount++;
                                    break;
                                case RefreshOutcome.Skip:
                                    result.SkippedCount++;
                                    break;
                            }
                        }
                    }
                    catch (IncompleteAuthorListException ex)
                    {
                        result.FailedCount++;
                        AddError(result, $"{book.Title}: {ex.Message}");
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning(ex, "Author lookup failed for book {BookId} ({Title})", book.Id, book.Title);
                        result.FailedCount++;
                        AddError(result, $"{book.Title}: lookup failed");
                    }

                    if (pendingSaves >= SaveEvery)
                    {
                        await _context.SaveChangesAsync(CancellationToken.None);
                        pendingSaves = 0;
                    }

                    if (delayBetweenCallsMs > 0)
                    {
                        try
                        {
                            await Task.Delay(delayBetweenCallsMs, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            result.WasCancelled = true;
                            break;
                        }
                    }
                }

                if (pendingSaves > 0)
                {
                    // Keep what was already corrected even when the caller has gone away.
                    await _context.SaveChangesAsync(CancellationToken.None);
                }

                if (result.FailedCount > 0)
                {
                    result.WarningMessage = $"{result.FailedCount} of {result.TotalChecked} author lookups failed";
                }

                _logger.LogInformation(
                    "Author refresh completed. Checked: {Checked}, Updated: {Updated}, Unchanged: {Unchanged}, Skipped: {Skipped}, NotFound: {NotFound}, Failed: {Failed}",
                    result.TotalChecked, result.UpdatedCount, result.UnchangedCount, result.SkippedCount, result.NotFoundCount, result.FailedCount);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                result.WasCancelled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Author refresh run aborted");
                result.Success = false;
                result.ErrorMessage = "Author refresh run failed unexpectedly";
            }

            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// The source's full author list: Google Books when the book has a volume id and Google
        /// lists authors for it, otherwise Open Library. Null when neither source has authors.
        /// </summary>
        private async Task<IReadOnlyList<string>?> LookUpAuthorsAsync(Book book)
        {
            if (!string.IsNullOrWhiteSpace(book.GoogleVolumeId))
            {
                var volume = await _googleBooksClient.GetVolumeByIdAsync(book.GoogleVolumeId);
                var googleAuthors = volume?.VolumeInfo?.Authors?
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .ToList();
                if (googleAuthors is { Count: > 0 })
                {
                    return googleAuthors;
                }
            }

            if (!string.IsNullOrWhiteSpace(book.OpenLibraryKey))
            {
                var work = await _openLibraryClient.GetBookByOpenLibraryIdAsync(book.OpenLibraryKey.Replace("/works/", ""));
                var expected = work.Authors?.Count(a => !string.IsNullOrWhiteSpace(a.Author?.Key)) ?? 0;
                if (expected == 0) return null;

                var names = await OpenLibraryAuthorResolver.ResolveNamesAsync(_openLibraryClient, work.Authors, _logger);
                if (names.Count != expected)
                {
                    // Writing a partial list would drop a credit, which is what this run exists to fix.
                    throw new IncompleteAuthorListException($"resolved {names.Count} of {expected} Open Library authors");
                }

                return names;
            }

            return null;
        }

        internal enum RefreshOutcome
        {
            Update,
            Unchanged,
            Skip
        }

        /// <summary>
        /// Replaces the stored author only when it is the truncated form an earlier import wrote:
        /// the source's first author alone, or the "Unknown Author" placeholder.
        /// </summary>
        internal static RefreshOutcome Decide(string storedAuthor, IReadOnlyList<string> sourceAuthors, out string? refreshed)
        {
            refreshed = BookAuthors.Join(sourceAuthors);
            if (refreshed == null) return RefreshOutcome.Unchanged;

            var stored = storedAuthor.Trim();
            if (string.Equals(stored, refreshed, StringComparison.OrdinalIgnoreCase))
            {
                return RefreshOutcome.Unchanged;
            }

            if (string.Equals(stored, sourceAuthors[0].Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(stored, UnknownAuthor, StringComparison.OrdinalIgnoreCase)
                || stored.Length == 0)
            {
                return RefreshOutcome.Update;
            }

            return RefreshOutcome.Skip;
        }

        private static void AddError(BookAuthorRefreshResult result, string message)
        {
            if (result.Errors.Count < MaxErrors)
            {
                result.Errors.Add(message);
            }
        }

        private sealed class IncompleteAuthorListException : Exception
        {
            public IncompleteAuthorListException(string message) : base(message) { }
        }
    }
}
