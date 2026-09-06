using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class ReadwiseService : IReadwiseService
    {
        private readonly IApplicationDbContext _context;
        private readonly IReadwiseApiClient _readwiseClient;
        private readonly ILogger<ReadwiseService> _logger;

        public ReadwiseService(
            IApplicationDbContext context,
            IReadwiseApiClient readwiseClient,
            ILogger<ReadwiseService> logger)
        {
            _context = context;
            _readwiseClient = readwiseClient;
            _logger = logger;
        }

        public async Task<bool> ValidateConnectionAsync()
        {
            _logger.LogInformation("Validating Readwise API connection");
            return await _readwiseClient.ValidateTokenAsync();
        }

        // Failures propagate to the caller: the returned count must only ever describe
        // links that were actually persisted.
        public async Task<int> LinkHighlightsToMediaAsync()
        {
            _logger.LogInformation("Starting to link highlights to media items");

            var linkedCount = 0;

            // Get all highlights that don't have ArticleId or BookId set yet
            var unlinkedHighlights = await _context.Highlights
                .Where(h => h.ArticleId == null && h.BookId == null)
                .ToListAsync();

            _logger.LogInformation("Found {Count} unlinked highlights", unlinkedHighlights.Count);

            foreach (var highlight in unlinkedHighlights)
            {
                var match = await HighlightLinkMatcher.ResolveAsync(
                    _context,
                    new[] { highlight.SourceUrl },
                    highlight.Title,
                    highlight.Author,
                    highlight.Category,
                    highlight.ReadwiseBookId);

                if (match.Article != null)
                {
                    highlight.ArticleId = match.Article.Id;
                    linkedCount++;
                    _logger.LogDebug("Linked highlight {HighlightId} to article {ArticleId}",
                        highlight.Id, match.Article.Id);
                }
                else if (match.Book != null)
                {
                    highlight.BookId = match.Book.Id;
                    linkedCount++;
                    _logger.LogDebug("Linked highlight {HighlightId} to book {BookId}",
                        highlight.Id, match.Book.Id);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully linked {Count} highlights to media items", linkedCount);

            return linkedCount;
        }

        public async Task<bool> ExportHighlightToReadwiseAsync(Guid highlightId)
        {
            try
            {
                var highlight = await _context.Highlights
                    .Include(h => h.Article)
                    .Include(h => h.Book)
                    .FirstOrDefaultAsync(h => h.Id == highlightId);

                if (highlight == null)
                {
                    _logger.LogWarning("Highlight {HighlightId} not found for export", highlightId);
                    return false;
                }

                var dto = new Shared.DTOs.Readwise.CreateReadwiseHighlightDto
                {
                    text = highlight.Text,
                    title = highlight.Title ?? highlight.Article?.Title ?? highlight.Book?.Title,
                    author = highlight.Author ?? highlight.Article?.Author ?? highlight.Book?.Author,
                    source_url = highlight.SourceUrl ?? highlight.Article?.Link,
                    note = highlight.Note,
                    category = highlight.Category,
                    highlighted_at = highlight.HighlightedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    location = highlight.Location,
                    location_type = highlight.LocationType
                };

                var success = await _readwiseClient.CreateHighlightsAsync(new List<Shared.DTOs.Readwise.CreateReadwiseHighlightDto> { dto });

                if (success)
                {
                    _logger.LogInformation("Successfully exported highlight {HighlightId} to Readwise", highlightId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting highlight {HighlightId} to Readwise", highlightId);
                return false;
            }
        }
    }
}

