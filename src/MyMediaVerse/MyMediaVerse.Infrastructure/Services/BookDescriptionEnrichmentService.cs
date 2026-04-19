using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services
{
    /// <summary>
    /// Service for enriching book descriptions from Google Books API.
    /// Processes books in batches with rate limiting to respect API guidelines.
    /// </summary>
    public class BookDescriptionEnrichmentService : IBookDescriptionEnrichmentService
    {
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
            return await _context.Books
                .Where(b => b.ISBN != null && b.ISBN != "" && b.Description == null)
                .CountAsync();
        }

        /// <inheritdoc />
        public async Task<BookDescriptionEnrichmentResult> EnrichBooksWithoutDescriptionsAsync(
            int batchSize = 50,
            int delayBetweenCallsMs = 1000,
            CancellationToken cancellationToken = default)
        {
            var result = new BookDescriptionEnrichmentResult();

            try
            {
                // Get books that need enrichment: have ISBN but no description
                var booksToEnrich = await _context.Books
                    .Where(b => b.ISBN != null && b.ISBN != "" && b.Description == null)
                    .OrderBy(b => b.DateAdded) // Process oldest first
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = booksToEnrich.Count;

                if (booksToEnrich.Count == 0)
                {
                    _logger.LogInformation("No books found needing description enrichment");
                    return result;
                }

                _logger.LogInformation("Starting book description enrichment for {Count} books", booksToEnrich.Count);

                foreach (var book in booksToEnrich)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Book description enrichment cancelled");
                        result.WasCancelled = true;
                        break;
                    }

                    try
                    {
                        if (string.IsNullOrWhiteSpace(book.ISBN))
                        {
                            result.SkippedCount++;
                            continue;
                        }

                        // Clean ISBN for API call
                        var cleanIsbn = book.ISBN.Replace("-", "").Replace(" ", "").Trim();

                        _logger.LogDebug("Fetching description for book: {Title} (ISBN: {ISBN})", book.Title, cleanIsbn);

                        var description = await _googleBooksClient.GetBookDescriptionByISBNAsync(cleanIsbn);

                        if (!string.IsNullOrWhiteSpace(description))
                        {
                            book.Description = description;
                            _context.Update(book);
                            result.EnrichedCount++;
                            _logger.LogDebug("Successfully enriched description for: {Title}", book.Title);
                        }
                        else
                        {
                            // Mark as attempted by setting empty description to avoid re-processing
                            // Note: We don't set an empty string here - we leave it null
                            // so it can potentially be enriched later if Google Books gets updated
                            result.FailedCount++;
                            _logger.LogDebug("No description found for: {Title} (ISBN: {ISBN})", book.Title, cleanIsbn);
                        }
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

                // Save all changes
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Book description enrichment complete. Enriched: {Enriched}, Failed: {Failed}, Skipped: {Skipped}",
                    result.EnrichedCount, result.FailedCount, result.SkippedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                _logger.LogInformation("Book description enrichment was cancelled");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Enrichment run failed: {ex.Message}");
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

                if (string.IsNullOrWhiteSpace(book.ISBN))
                {
                    result.NoIsbn = true;
                    result.ErrorMessage = "Book has no ISBN to look up";
                    return result;
                }

                var cleanIsbn = book.ISBN.Replace("-", "").Replace(" ", "").Trim();

                _logger.LogInformation("Fetching description for book: {Title} (ISBN: {ISBN})", book.Title, cleanIsbn);

                var description = await _googleBooksClient.GetBookDescriptionByISBNAsync(cleanIsbn);

                if (!string.IsNullOrWhiteSpace(description))
                {
                    book.Description = description;
                    _context.Update(book);
                    await _context.SaveChangesAsync(cancellationToken);

                    result.Success = true;
                    result.Description = description;
                    _logger.LogInformation("Successfully enriched description for: {Title}", book.Title);
                }
                else
                {
                    result.ErrorMessage = "No description found in Google Books for this ISBN";
                    _logger.LogInformation("No description found for: {Title} (ISBN: {ISBN})", book.Title, cleanIsbn);
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to enrich book: {ex.Message}";
                _logger.LogError(ex, "Failed to enrich description for book ID: {BookId}", bookId);
            }

            return result;
        }
    }
}
