using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Derives the MMV <c>Rating</c> enum from the raw <c>GoodreadsRating</c> stored at import time.
    /// Goodreads CSV import stores only the raw 1-5 rating (no conversion during upload); this step
    /// performs the conversion. Pure local operation — no external API, no rate limiting. Candidates
    /// are walked in pages so a large library is never materialized in memory at once.
    /// </summary>
    public class BookRatingEnrichmentService : IBookRatingEnrichmentService
    {
        // Page size for walking candidates. The candidate predicate does not change when a row is
        // converted, so plain offset paging over a stable order is safe here.
        internal const int PageSize = 200;

        private readonly IApplicationDbContext _context;
        private readonly ILogger<BookRatingEnrichmentService> _logger;

        public BookRatingEnrichmentService(
            IApplicationDbContext context,
            ILogger<BookRatingEnrichmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<int> GetBooksNeedingRatingConversionCountAsync(CancellationToken cancellationToken = default)
        {
            // "Needs conversion" = has a real 1-5 Goodreads rating but no derived MMV Rating yet.
            return await _context.Books
                .Where(b => b.GoodreadsRating >= 1 && b.GoodreadsRating <= 5 && b.Rating == null)
                .CountAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<BookRatingConversionResult> ConvertGoodreadsRatingsAsync(CancellationToken cancellationToken = default)
        {
            var result = new BookRatingConversionResult { StartedAt = DateTime.UtcNow };

            try
            {
                // Every book carrying a real 1-5 Goodreads rating is a candidate; the derived value is
                // Goodreads-primary, so a candidate whose current Rating differs is overwritten.
                var candidates = _context.Books
                    .Where(b => b.GoodreadsRating >= 1 && b.GoodreadsRating <= 5)
                    .OrderBy(b => b.Id);

                var offset = 0;
                while (true)
                {
                    var page = await candidates.Skip(offset).Take(PageSize).ToListAsync(cancellationToken);
                    if (page.Count == 0)
                    {
                        break;
                    }

                    var convertedInPage = 0;
                    foreach (var book in page)
                    {
                        var derived = RatingConverter.ConvertGoodreadsRatingToPLBRating(book.GoodreadsRating);
                        if (derived == null || book.Rating == derived)
                        {
                            result.Unchanged++;
                            continue;
                        }

                        book.Rating = derived;
                        convertedInPage++;
                    }

                    result.TotalCandidates += page.Count;
                    result.Converted += convertedInPage;

                    if (convertedInPage > 0)
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    offset += page.Count;
                }

                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Goodreads rating conversion complete. Candidates: {Candidates}, Converted: {Converted}, Unchanged: {Unchanged}",
                    result.TotalCandidates, result.Converted, result.Unchanged);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Rating conversion failed: {ex.Message}";
                _logger.LogError(ex, "Goodreads rating conversion run failed");
            }

            return result;
        }
    }
}
