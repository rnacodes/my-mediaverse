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
    /// performs the conversion. Pure local operation — no external API, no rate limiting.
    /// </summary>
    public class BookRatingEnrichmentService : IBookRatingEnrichmentService
    {
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
            var result = new BookRatingConversionResult();

            // Every book carrying a real 1-5 Goodreads rating is a candidate; the derived value is
            // Goodreads-primary, so a candidate whose current Rating differs is overwritten.
            var candidates = await _context.Books
                .Where(b => b.GoodreadsRating >= 1 && b.GoodreadsRating <= 5)
                .ToListAsync(cancellationToken);

            result.TotalCandidates = candidates.Count;

            foreach (var book in candidates)
            {
                var derived = RatingConverter.ConvertGoodreadsRatingToPLBRating(book.GoodreadsRating);
                if (derived == null || book.Rating == derived)
                {
                    result.Unchanged++;
                    continue;
                }

                book.Rating = derived;
                result.Converted++;
            }

            if (result.Converted > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Goodreads rating conversion complete. Candidates: {Candidates}, Converted: {Converted}, Unchanged: {Unchanged}",
                result.TotalCandidates, result.Converted, result.Unchanged);

            return result;
        }
    }
}
