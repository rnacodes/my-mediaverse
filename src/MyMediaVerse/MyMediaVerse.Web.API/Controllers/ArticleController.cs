using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Extensions;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// All article operations, including the Readwise Reader import/sync surface.
    /// Reader-backed actions are operator-only: they act on the owner's Reader library
    /// through the app-wide API token.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public partial class ArticleController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly IArticleMappingService _articleMappingService;
        private readonly IImportReindexService _importReindexService;
        private readonly IReaderService? _readerService;
        private readonly IArticleDeduplicationService? _deduplicationService;
        private readonly IWebsiteScraperService? _websiteScraperService;
        private readonly ILogger<ArticleController> _logger;

        public ArticleController(
            IArticleService articleService,
            IArticleMappingService articleMappingService,
            IImportReindexService importReindexService,
            ILogger<ArticleController> logger,
            IReaderService? readerService = null,
            IArticleDeduplicationService? deduplicationService = null,
            IWebsiteScraperService? websiteScraperService = null)
        {
            _articleService = articleService;
            _articleMappingService = articleMappingService;
            _importReindexService = importReindexService;
            _logger = logger;
            _readerService = readerService;
            _deduplicationService = deduplicationService;
            _websiteScraperService = websiteScraperService;
        }

        // GET: api/article
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArticleResponseDto>>> GetAllArticles()
        {
            try
            {
                var articles = await _articleService.GetAllArticlesAsync();
                var response = await _articleMappingService.MapToResponseDtoAsync(articles);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all articles");
                return StatusCode(500, new { error = "Failed to retrieve articles", details = ex.Message });
            }
        }

        // GET: api/article/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleResponseDto>> GetArticle(Guid id)
        {
            try
            {
                var article = await _articleService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    return NotFound($"Article with ID {id} not found.");
                }

                var response = await _articleMappingService.MapToResponseDtoAsync(article);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve article", details = ex.Message });
            }
        }

        // GET: api/article/by-author/{author}
        [HttpGet("by-author/{author}")]
        public async Task<ActionResult<IEnumerable<ArticleResponseDto>>> GetArticlesByAuthor(string author)
        {
            try
            {
                var articles = await _articleService.GetArticlesByAuthorAsync(author);
                var response = await _articleMappingService.MapToResponseDtoAsync(articles);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving articles by author: {Author}", author);
                return StatusCode(500, new { error = "Failed to retrieve articles by author", details = ex.Message });
            }
        }

        // GET: api/article/archived
        [HttpGet("archived")]
        public async Task<ActionResult<IEnumerable<ArticleResponseDto>>> GetArchivedArticles()
        {
            try
            {
                var articles = await _articleService.GetArchivedArticlesAsync();
                var response = await _articleMappingService.MapToResponseDtoAsync(articles);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving archived articles");
                return StatusCode(500, new { error = "Failed to retrieve archived articles", details = ex.Message });
            }
        }

        // GET: api/article/starred
        [HttpGet("starred")]
        public async Task<ActionResult<IEnumerable<ArticleResponseDto>>> GetStarredArticles()
        {
            try
            {
                var articles = await _articleService.GetStarredArticlesAsync();
                var response = await _articleMappingService.MapToResponseDtoAsync(articles);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving starred articles");
                return StatusCode(500, new { error = "Failed to retrieve starred articles", details = ex.Message });
            }
        }

        // POST: api/article
        [HttpPost]
        public async Task<IActionResult> CreateArticle([FromBody] CreateArticleDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Article data is required");
                }

                var article = await _articleService.CreateArticleAsync(dto);
                var response = await _articleMappingService.MapToResponseDtoAsync(article);

                return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating article");
                return StatusCode(500, new { error = "Failed to create article", details = ex.Message });
            }
        }

        // PUT: api/article/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticle(Guid id, [FromBody] CreateArticleDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Article data is required");
                }

                var article = await _articleService.UpdateArticleAsync(id, dto);
                var response = await _articleMappingService.MapToResponseDtoAsync(article);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Article with ID {Id} not found for update", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update article", details = ex.Message });
            }
        }

        // DELETE: api/article/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticle(Guid id)
        {
            try
            {
                var deleted = await _articleService.DeleteArticleAsync(id);
                if (!deleted)
                {
                    return NotFound($"Article with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete article", details = ex.Message });
            }
        }

        // GET: api/article/{id}/content
        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetArticleContent(Guid id)
        {
            try
            {
                var content = await _articleService.GetArticleContentAsync(id);
                if (content == null)
                {
                    return NotFound($"Content for article with ID {id} not found.");
                }

                return Ok(new { content });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving content for article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve article content", details = ex.Message });
            }
        }

        // POST: api/article/{id}/content
        [HttpPost("{id}/content")]
        public async Task<IActionResult> UpdateArticleContent(Guid id, [FromBody] ArticleContentUpdateDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.HtmlContent))
                {
                    return BadRequest("HTML content is required");
                }

                var success = await _articleService.UpdateArticleContentAsync(id, dto.HtmlContent);
                if (!success)
                {
                    return NotFound($"Article with ID {id} not found or S3 storage not configured.");
                }

                return Ok(new { message = "Article content updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating content for article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update article content", details = ex.Message });
            }
        }

        // PUT: api/article/{id}/sync-status
        [HttpPut("{id}/sync-status")]
        public async Task<IActionResult> UpdateArticleSyncStatus(Guid id, [FromBody] ArticleSyncStatusUpdateDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Sync status data is required");
                }

                var article = await _articleService.UpdateArticleSyncStatusAsync(id, dto.IsArchived, dto.IsStarred);
                var response = await _articleMappingService.MapToResponseDtoAsync(article);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Article with ID {Id} not found for sync status update", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating sync status for article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update article sync status", details = ex.Message });
            }
        }

        // POST: api/article/scrape-preview
        // Fetches an arbitrary URL server-side; operator-only to keep the app from acting as an open proxy.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("scrape-preview")]
        public async Task<IActionResult> ScrapePreview([FromBody] ArticleScrapeRequestDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Url))
                {
                    return BadRequest(new { error = "URL is required" });
                }

                if (_websiteScraperService == null)
                {
                    return StatusCode(500, new { error = "Website scraper service not configured" });
                }

                _logger.LogInformation("Scraping article metadata from URL: {Url}", dto.Url);
                var scrapedData = await _websiteScraperService.ScrapeWebsiteAsync(dto.Url);
                return Ok(scrapedData);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid URL for scrape preview: {Url}", dto?.Url);
                return BadRequest(new { error = "Invalid URL", details = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch URL for scrape preview: {Url}", dto?.Url);
                return BadRequest(new { error = "Failed to fetch URL", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping article metadata from URL: {Url}", dto?.Url);
                return StatusCode(500, new { error = "Failed to scrape article metadata", details = ex.Message });
            }
        }
    }

    // Helper DTOs for specific endpoints
    public class ArticleScrapeRequestDto
    {
        public required string Url { get; set; }
    }
    public class ArticleContentUpdateDto
    {
        public required string HtmlContent { get; set; }
    }

    public class ArticleSyncStatusUpdateDto
    {
        public bool IsArchived { get; set; }
        public bool IsStarred { get; set; }
    }

    // Readwise Reader import/sync surface
    public partial class ArticleController
    {
        /// <summary>
        /// Syncs documents from Readwise Reader into the library.
        /// Completed runs (including runs with a warning) return 200; aborted runs return 500
        /// with the same result shape.
        /// </summary>
        /// <param name="location">Optional Reader location filter: "new", "later", "archive", "feed"</param>
        /// <param name="limit">Optional cap on documents to sync (requires <paramref name="location"/>)</param>
        // Writes to the library from the owner's Reader account via the app-wide token; never a visitor action.
        [Authorize]
        [HttpPost("sync-reader")]
        public async Task<ActionResult<ReaderSyncResultDto>> SyncFromReader(
            [FromQuery] string? location = null,
            [FromQuery] int? limit = null)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                if (limit.HasValue && string.IsNullOrEmpty(location))
                {
                    return BadRequest(new { error = "A location is required when limit is specified. Use: new, later, archive, or feed" });
                }

                _logger.LogInformation("Starting Reader document sync (location: {Location}, limit: {Limit})",
                    location ?? "all", limit?.ToString() ?? "none");

                var result = limit.HasValue
                    ? await _readerService.SyncDocumentsByLocationAsync(location!, limit.Value)
                    : await _readerService.SyncDocumentsAsync(location);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                await _importReindexService.ReindexAfterImportAsync(result.TotalProcessed, "Reader sync");
                result.ReindexTriggered = result.TotalProcessed > 0;

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing from Reader");
                return StatusCode(500, new ReaderSyncResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        // POST: api/article/{id}/fetch-content
        // Pulls full text from the owner's Reader account for one article.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("{id}/fetch-content")]
        public async Task<IActionResult> FetchArticleContent(Guid id)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                var success = await _readerService.FetchAndStoreArticleContentAsync(id);
                if (!success)
                {
                    return NotFound(new { error = "Article not found or content unavailable" });
                }
                return Ok(new { message = "Content fetched and stored successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching content for article {Id}", id);
                return StatusCode(500, new { error = "Failed to fetch content", details = ex.Message });
            }
        }

        /// <summary>
        /// Fetches full HTML content for archived articles that don't have it yet.
        /// Content is not indexed for search, so this never triggers a reindex.
        /// </summary>
        /// <param name="batchSize">Number of articles to fetch (default 50)</param>
        /// <param name="recentOnly">If true, only fetch articles synced in the last 7 days</param>
        // Bulk pull from the owner's Reader account; operator-only.
        [Authorize]
        [HttpPost("bulk-fetch-content")]
        public async Task<ActionResult<ReaderSyncResultDto>> BulkFetchContent(
            [FromQuery] int batchSize = 50,
            [FromQuery] bool recentOnly = false)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                _logger.LogInformation("Starting article content fetch (batchSize: {BatchSize}, recentOnly: {RecentOnly})",
                    batchSize, recentOnly);

                DateTime? updatedAfter = recentOnly ? DateTime.UtcNow.AddDays(-7) : null;
                var result = await _readerService.BulkFetchArticleContentsAsync(batchSize, updatedAfter);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk content fetch");
                return StatusCode(500, new ReaderSyncResultDto
                {
                    Operation = "reader-bulk-fetch-content",
                    Success = false,
                    ErrorMessage = ex.Message,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Fetches and stores content for the article matching a Reader document ID,
        /// regardless of the article's status.
        /// </summary>
        // Writes content pulled from the owner's Reader account; operator-only.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("reader/fetch-by-document-id/{documentId}")]
        public async Task<ActionResult<object>> FetchByReaderDocumentId(string documentId)
        {
            try
            {
                _logger.LogInformation("Fetching content by Reader document ID: {DocumentId}", documentId);

                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                var (success, message, contentLength) = await _readerService.FetchContentByReaderDocumentIdAsync(documentId);

                return Ok(new
                {
                    success,
                    message,
                    contentLength
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching content by document ID {DocumentId}", documentId);
                return StatusCode(500, new { error = "Fetch failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Diagnostic: fetches a document directly from the Reader API and reports what came back.
        /// </summary>
        // Reads the owner's Reader account; diagnostic, operator-only.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpGet("reader/test-fetch/{documentId}")]
        public async Task<ActionResult<ReaderDocumentTestResultDto>> TestFetchReaderDocument(
            string documentId,
            [FromQuery] bool includeHtml = true)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                _logger.LogInformation("Testing fetch for document {DocumentId}", documentId);
                var result = await _readerService.TestFetchDocumentByIdAsync(documentId, includeHtml);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing fetch for document {DocumentId}", documentId);
                return StatusCode(500, new { error = "Test fetch failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Lists articles in the library that carry a Reader document ID.
        /// </summary>
        // Admin-shaped listing used to pick test targets; operator-only.
        [Authorize]
        [HttpGet("with-reader-document-ids")]
        public async Task<ActionResult<IEnumerable<ReaderArticleSummaryDto>>> GetArticlesWithReaderDocumentIds(
            [FromQuery] int limit = 20,
            [FromQuery] bool onlyWithoutContent = false,
            [FromQuery] string? status = null)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                var articles = await _readerService.GetArticlesWithReaderDocumentIdsAsync(limit, onlyWithoutContent, status);
                return Ok(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving articles with document IDs");
                return StatusCode(500, new { error = "Failed to retrieve articles", details = ex.Message });
            }
        }

        /// <summary>
        /// Lists documents straight from the Reader API (not the library).
        /// </summary>
        // Proxies the owner's Reader library; operator-only.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpGet("reader/documents")]
        public async Task<ActionResult<IEnumerable<ReaderArticleSummaryDto>>> GetReaderDocuments(
            [FromQuery] string? location = null,
            [FromQuery] int limit = 50)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                _logger.LogInformation("Fetching documents from Reader API (location: {Location}, limit: {Limit})",
                    location ?? "all", limit);
                var documents = await _readerService.FetchDocumentsFromReaderApiAsync(location, limit);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documents from Reader API");
                return StatusCode(500, new { error = "Failed to fetch from Reader API", details = ex.Message });
            }
        }
    }

    // Deduplication endpoints
    public partial class ArticleController
    {
        // POST: api/article/deduplicate
        // Library-wide merge that deletes rows; operator-only maintenance.
        [Authorize]
        [HttpPost("deduplicate")]
        public async Task<ActionResult<DeduplicationResultDto>> DeduplicateArticles()
        {
            try
            {
                if (_deduplicationService == null)
                {
                    return StatusCode(500, new { error = "Deduplication service not configured" });
                }

                _logger.LogInformation("Starting article deduplication");
                var result = await _deduplicationService.FindAndMergeDuplicatesAsync();

                if (result.Success)
                {
                    _logger.LogInformation("Deduplication completed successfully. Merged {Count} articles",
                        result.MergedCount);
                    return Ok(result);
                }
                else
                {
                    _logger.LogError("Deduplication failed: {Error}", result.ErrorMessage);
                    return StatusCode(500, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during article deduplication");
                return StatusCode(500, new {
                    error = "Failed to deduplicate articles",
                    details = ex.Message
                });
            }
        }

        // GET: api/article/duplicates
        // Library-wide scan; operator-only maintenance.
        [Authorize]
        [HttpGet("duplicates")]
        public async Task<ActionResult<FindDuplicatesResultDto>> FindDuplicates()
        {
            try
            {
                if (_deduplicationService == null)
                {
                    return StatusCode(500, new { error = "Deduplication service not configured" });
                }

                _logger.LogInformation("Finding duplicate articles");
                var duplicates = await _deduplicationService.FindDuplicatesAsync();

                return Ok(new FindDuplicatesResultDto
                {
                    Count = duplicates.Count,
                    TotalDuplicates = duplicates.Sum(g => g.Articles.Count - 1),
                    Groups = duplicates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding duplicate articles");
                return StatusCode(500, new {
                    error = "Failed to find duplicates",
                    details = ex.Message
                });
            }
        }
    }
}
