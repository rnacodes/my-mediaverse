using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HighlightController : ControllerBase
    {
        private readonly IHighlightService _highlightService;
        private readonly IReadwiseService _readwiseService;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<HighlightController> _logger;

        public HighlightController(
            IHighlightService highlightService,
            IReadwiseService readwiseService,
            IApplicationDbContext context,
            ILogger<HighlightController> logger)
        {
            _highlightService = highlightService;
            _readwiseService = readwiseService;
            _context = context;
            _logger = logger;
        }

        // GET: api/highlight
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HighlightResponseDto>>> GetAllHighlights()
        {
            try
            {
                var highlights = await _highlightService.GetAllHighlightsAsync();
                var response = highlights.Select(MapToResponseDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving highlights");
                return StatusCode(500, new { error = "Failed to retrieve highlights", details = ex.Message });
            }
        }

        // GET: api/highlight/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<HighlightResponseDto>> GetHighlight(Guid id)
        {
            try
            {
                var highlight = await _highlightService.GetHighlightByIdAsync(id);
                if (highlight == null)
                {
                    return NotFound(new { error = $"Highlight with ID {id} not found" });
                }
                return Ok(MapToResponseDto(highlight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving highlight {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve highlight", details = ex.Message });
            }
        }

        // GET: api/highlight/article/{articleId}
        [HttpGet("article/{articleId}")]
        public async Task<ActionResult<IEnumerable<HighlightResponseDto>>> GetHighlightsByArticle(Guid articleId)
        {
            try
            {
                var highlights = await _highlightService.GetHighlightsByArticleIdAsync(articleId);
                var response = highlights.Select(MapToResponseDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving highlights for article {ArticleId}", articleId);
                return StatusCode(500, new { error = "Failed to retrieve highlights", details = ex.Message });
            }
        }

        // GET: api/highlight/book/{bookId}
        [HttpGet("book/{bookId}")]
        public async Task<ActionResult<IEnumerable<HighlightResponseDto>>> GetHighlightsByBook(Guid bookId)
        {
            try
            {
                var highlights = await _highlightService.GetHighlightsByBookIdAsync(bookId);
                var response = highlights.Select(MapToResponseDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving highlights for book {BookId}", bookId);
                return StatusCode(500, new { error = "Failed to retrieve highlights", details = ex.Message });
            }
        }

        // GET: api/highlight/unlinked
        [HttpGet("unlinked")]
        public async Task<ActionResult<IEnumerable<HighlightResponseDto>>> GetUnlinkedHighlights()
        {
            try
            {
                var highlights = await _highlightService.GetUnlinkedHighlightsAsync();
                var response = highlights.Select(MapToResponseDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unlinked highlights");
                return StatusCode(500, new { error = "Failed to retrieve unlinked highlights", details = ex.Message });
            }
        }

        // GET: api/highlight/tag/{tag}
        [HttpGet("tag/{tag}")]
        public async Task<ActionResult<IEnumerable<HighlightResponseDto>>> GetHighlightsByTag(string tag)
        {
            try
            {
                var highlights = await _highlightService.GetHighlightsByTagAsync(tag);
                var response = highlights.Select(MapToResponseDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving highlights for tag {Tag}", tag);
                return StatusCode(500, new { error = "Failed to retrieve highlights", details = ex.Message });
            }
        }

        // POST: api/highlight
        [HttpPost]
        public async Task<ActionResult<HighlightResponseDto>> CreateHighlight([FromBody] CreateHighlightDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { error = "Highlight data is required" });
                }

                var highlight = await _highlightService.CreateHighlightAsync(dto);
                var response = MapToResponseDto(highlight);
                return CreatedAtAction(nameof(GetHighlight), new { id = highlight.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating highlight");
                return StatusCode(500, new { error = "Failed to create highlight", details = ex.Message });
            }
        }

        // POST: api/highlight/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<BulkHighlightResultDto>> BulkCreateHighlights([FromBody] List<CreateHighlightDto> dtos)
        {
            try
            {
                if (dtos == null || dtos.Count == 0)
                {
                    return BadRequest(new { error = "At least one highlight is required" });
                }

                _logger.LogInformation("Bulk creating {Count} highlights", dtos.Count);
                var result = await _highlightService.BulkCreateHighlightsAsync(dtos);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating highlights");
                return StatusCode(500, new { error = "Failed to bulk create highlights", details = ex.Message });
            }
        }

        // POST: api/highlight/sync
        [HttpPost("sync")]
        public async Task<ActionResult<HighlightSyncResultDto>> SyncHighlights([FromQuery] DateTime? lastSync = null)
        {
            try
            {
                _logger.LogInformation("Starting highlight sync from Readwise (lastSync: {LastSync})", 
                    lastSync?.ToString() ?? "full");
                
                HighlightSyncResultDto result;
                if (lastSync.HasValue)
                {
                    result = await _highlightService.SyncHighlightsIncrementalAsync(lastSync.Value);
                }
                else
                {
                    result = await _highlightService.SyncHighlightsFromReadwiseAsync();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing highlights");
                return StatusCode(500, new { error = "Failed to sync highlights", details = ex.Message });
            }
        }

        // POST: api/highlight/link
        [HttpPost("link")]
        public async Task<ActionResult<object>> LinkHighlightsToMedia()
        {
            try
            {
                _logger.LogInformation("Starting to link highlights to media items");
                var linkedCount = await _readwiseService.LinkHighlightsToMediaAsync();
                return Ok(new { linkedCount, message = $"Successfully linked {linkedCount} highlights" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking highlights");
                return StatusCode(500, new { error = "Failed to link highlights", details = ex.Message });
            }
        }

        // POST: api/highlight/{id}/export
        [HttpPost("{id}/export")]
        public async Task<ActionResult<object>> ExportHighlight(Guid id)
        {
            try
            {
                var success = await _readwiseService.ExportHighlightToReadwiseAsync(id);
                if (!success)
                {
                    return NotFound(new { error = "Highlight not found or export failed" });
                }
                return Ok(new { message = "Highlight exported to Readwise successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting highlight {Id}", id);
                return StatusCode(500, new { error = "Failed to export highlight", details = ex.Message });
            }
        }

        // PUT: api/highlight/{id}
        // Partial update: null fields are left unchanged, empty strings clear
        // optional fields, an empty tag list clears tags. Links are managed via
        // PUT {id}/link.
        [HttpPut("{id}")]
        public async Task<ActionResult<HighlightResponseDto>> UpdateHighlight(Guid id, [FromBody] UpdateHighlightDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { error = "Highlight data is required" });
                }

                var highlight = await _highlightService.UpdateHighlightAsync(id, dto);
                return Ok(MapToResponseDto(highlight));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Highlight {Id} not found for update", id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating highlight {Id}", id);
                return StatusCode(500, new { error = "Failed to update highlight", details = ex.Message });
            }
        }

        // PUT: api/highlight/{id}/link
        // Sets the highlight's media link: article, book, or neither (unlink).
        [HttpPut("{id}/link")]
        public async Task<ActionResult<HighlightResponseDto>> SetHighlightLink(Guid id, [FromBody] HighlightLinkDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { error = "Link data is required" });
                }

                var highlight = await _highlightService.SetHighlightLinkAsync(id, dto.ArticleId, dto.BookId);
                return Ok(MapToResponseDto(highlight));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Highlight {Id} or link target not found", id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting link for highlight {Id}", id);
                return StatusCode(500, new { error = "Failed to set highlight link", details = ex.Message });
            }
        }

        // DELETE: api/highlight/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHighlight(Guid id)
        {
            try
            {
                var deleted = await _highlightService.DeleteHighlightAsync(id);
                if (!deleted)
                {
                    return NotFound(new { error = $"Highlight with ID {id} not found" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting highlight {Id}", id);
                return StatusCode(500, new { error = "Failed to delete highlight", details = ex.Message });
            }
        }

        // POST: api/highlight/clean-text
        [HttpPost("clean-text")]
        public async Task<ActionResult<object>> CleanHighlightText()
        {
            try
            {
                _logger.LogInformation("Starting highlight text cleanup (removing HTML/CSS)");
                var cleanedCount = await _highlightService.CleanAllHighlightTextAsync();
                return Ok(new
                {
                    cleanedCount,
                    message = $"Successfully cleaned HTML/CSS from {cleanedCount} highlights"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning highlight text");
                return StatusCode(500, new { error = "Failed to clean highlight text", details = ex.Message });
            }
        }

        // GET: api/highlight/validate-connection
        [HttpGet("validate-connection")]
        public async Task<ActionResult<object>> ValidateConnection()
        {
            try
            {
                var isValid = await _readwiseService.ValidateConnectionAsync();
                return Ok(new { 
                    connected = isValid,
                    message = isValid 
                        ? "Readwise API connection is valid ✓" 
                        : "Readwise API connection failed"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Readwise API not configured");
                return Ok(new { 
                    connected = false,
                    message = "Readwise API not configured",
                    details = ex.Message 
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Readwise API token invalid");
                return Ok(new { 
                    connected = false,
                    message = "Invalid API token",
                    details = ex.Message 
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error validating Readwise connection");
                return Ok(new { 
                    connected = false,
                    message = "Network error",
                    details = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Readwise connection");
                return Ok(new { 
                    connected = false,
                    message = "Connection validation failed",
                    details = ex.Message 
                });
            }
        }

        // GET: api/highlight/diagnose-linking
        /// <summary>
        /// Diagnoses why highlights aren't linking to articles.
        /// Shows unlinked highlights with their source URLs and potential matches.
        /// </summary>
        [HttpGet("diagnose-linking")]
        public async Task<ActionResult<object>> DiagnoseLinking([FromQuery] int limit = 10)
        {
            try
            {
                var unlinkedHighlights = (await _highlightService.GetUnlinkedHighlightsAsync())
                    .Where(h => !string.IsNullOrEmpty(h.SourceUrl) && h.Category?.ToLowerInvariant() == "articles")
                    .Take(limit)
                    .ToList();

                var diagnostics = new List<object>();

                // Get unique titles from highlights to search for matching articles
                var uniqueTitles = unlinkedHighlights
                    .Select(h => h.Title)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .ToList();

                foreach (var highlight in unlinkedHighlights)
                {
                    var normalizedUrl = MyMediaVerse.Application.Utilities.UrlNormalizer.Normalize(highlight.SourceUrl);
                    var urlWithoutProtocol = normalizedUrl
                        .Replace("https://", "")
                        .Replace("http://", "");

                    // Search for potential matching articles by URL
                    var potentialMatch = await _context.Articles
                        .Where(a => a.Link != null && (
                            EF.Functions.ILike(a.Link, normalizedUrl) ||
                            EF.Functions.ILike(a.Link, $"%{urlWithoutProtocol}") ||
                            EF.Functions.ILike(a.Link, $"%{urlWithoutProtocol}/")))
                        .Select(a => new { a.Id, a.Title, a.Link })
                        .FirstOrDefaultAsync();

                    // Also try to find by title if no URL match
                    object? titleMatch = null;
                    if (potentialMatch == null && !string.IsNullOrEmpty(highlight.Title))
                    {
                        titleMatch = await _context.Articles
                            .Where(a => EF.Functions.ILike(a.Title, highlight.Title))
                            .Select(a => new { a.Id, a.Title, a.Link })
                            .FirstOrDefaultAsync();
                    }

                    diagnostics.Add(new
                    {
                        highlightId = highlight.Id,
                        highlightTitle = highlight.Title,
                        originalSourceUrl = highlight.SourceUrl,
                        normalizedSourceUrl = normalizedUrl,
                        category = highlight.Category,
                        matchingArticleByUrl = potentialMatch,
                        matchingArticleByTitle = titleMatch,
                        reason = potentialMatch == null && titleMatch == null
                            ? "No matching article found in database - article may not have been imported from Reader"
                            : (potentialMatch != null ? "URL match found but linking failed" : "Title match found but URLs don't match")
                    });
                }

                // Get total article count for context
                var totalArticles = await _context.Articles.CountAsync();

                // Get sample article Links for comparison
                var sampleArticleLinks = await _context.Articles
                    .Where(a => a.Link != null)
                    .Take(5)
                    .Select(a => new { a.Title, a.Link })
                    .ToListAsync();

                return Ok(new
                {
                    totalUnlinkedArticleHighlights = (await _highlightService.GetUnlinkedHighlightsAsync())
                        .Count(h => h.Category?.ToLowerInvariant() == "articles"),
                    totalArticlesInDatabase = totalArticles,
                    sampleDiagnostics = diagnostics,
                    sampleArticleLinks,
                    suggestion = "If 'matchingArticleByUrl' is null for all highlights, the articles haven't been imported from Reader. " +
                                 "Run 'POST /api/readwise/import-by-location?location=archive' to import archived articles, then run 'POST /api/highlight/link' to link them."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error diagnosing highlight linking");
                return StatusCode(500, new { error = "Failed to diagnose linking", details = ex.Message });
            }
        }

        private static HighlightResponseDto MapToResponseDto(Domain.Entities.Highlight highlight)
        {
            return new HighlightResponseDto
            {
                id = highlight.Id,
                text = highlight.Text,
                note = highlight.Note,
                title = highlight.Title,
                author = highlight.Author,
                category = highlight.Category,
                sourceUrl = highlight.SourceUrl,
                imageUrl = highlight.ImageUrl,
                articleId = highlight.ArticleId,
                articleTitle = highlight.Article?.Title,
                bookId = highlight.BookId,
                bookTitle = highlight.Book?.Title,
                tags = highlight.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                location = highlight.Location,
                locationType = highlight.LocationType,
                highlightedAt = highlight.HighlightedAt,
                createdAt = highlight.CreatedAt,
                updatedAt = highlight.UpdatedAt,
                color = highlight.Color,
                isFavorite = highlight.IsFavorite
            };
        }
    }
}

