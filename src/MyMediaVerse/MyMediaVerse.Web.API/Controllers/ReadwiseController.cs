using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Web.API.Conventions;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Controller for Readwise and Reader sync operations.
    /// Provides a unified sync endpoint that syncs both Reader documents and Readwise highlights.
    /// </summary>
    // Proxies the owner's personal Readwise/Reader library via an app-wide API token,
    // so these endpoints exist only on hosts the owner uses.
    [Environments("Production", "Development", "Testing")]
    [ApiController]
    [Route("api/[controller]")]
    public class ReadwiseController : ControllerBase
    {
        private readonly IReaderService _readerService;
        private readonly IHighlightService _highlightService;
        private readonly IReadwiseService _readwiseService;
        private readonly ILogger<ReadwiseController> _logger;

        public ReadwiseController(
            IReaderService readerService,
            IHighlightService highlightService,
            IReadwiseService readwiseService,
            ILogger<ReadwiseController> logger)
        {
            _readerService = readerService;
            _highlightService = highlightService;
            _readwiseService = readwiseService;
            _logger = logger;
        }

        /// <summary>
        /// Validates Readwise API connection
        /// </summary>
        [HttpGet("validate")]
        public async Task<ActionResult<object>> ValidateConnection()
        {
            try
            {
                var isValid = await _readwiseService.ValidateConnectionAsync();
                return Ok(new
                {
                    connected = isValid,
                    message = isValid
                        ? "Readwise API connection is valid"
                        : "Readwise API connection failed"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new
                {
                    connected = false,
                    message = "Readwise API not configured",
                    details = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ok(new
                {
                    connected = false,
                    message = "Invalid API token",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Readwise connection");
                return Ok(new
                {
                    connected = false,
                    message = "Connection validation failed",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Unified sync operation: syncs Reader documents and Readwise highlights.
        /// Auto-links highlights to articles during import.
        /// </summary>
        /// <param name="incremental">If true (default), only syncs items from the last 7 days</param>
        [HttpPost("sync")]
        public async Task<ActionResult<ReadwiseSyncAllResultDto>> SyncAll([FromQuery] bool incremental = true)
        {
            var result = new ReadwiseSyncAllResultDto
            {
                StartedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting unified Readwise sync (incremental: {Incremental})", incremental);

                // Calculate the date for incremental sync (7 days ago)
                DateTime? lastSync = incremental ? DateTime.UtcNow.AddDays(-7) : null;

                // Step 1: Sync Reader documents (pass lastSync for incremental filtering)
                _logger.LogInformation("Step 1: Syncing Reader documents...");
                var readerResult = await _readerService.SyncDocumentsAsync(updatedAfter: lastSync);

                if (!readerResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Reader sync failed: {readerResult.ErrorMessage}";
                    result.CompletedAt = DateTime.UtcNow;
                    return Ok(result);
                }

                result.ArticlesCreated = readerResult.CreatedCount;
                result.ArticlesUpdated = readerResult.UpdatedCount;

                // Step 2: Sync Readwise highlights
                _logger.LogInformation("Step 2: Syncing Readwise highlights...");
                HighlightSyncResultDto highlightResult;

                if (lastSync.HasValue)
                {
                    highlightResult = await _highlightService.SyncHighlightsIncrementalAsync(lastSync.Value);
                }
                else
                {
                    highlightResult = await _highlightService.SyncHighlightsFromReadwiseAsync();
                }

                if (!highlightResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Highlight sync failed: {highlightResult.ErrorMessage}";
                    result.CompletedAt = DateTime.UtcNow;
                    return Ok(result);
                }

                result.HighlightsCreated = highlightResult.CreatedCount;
                result.HighlightsUpdated = highlightResult.UpdatedCount;
                result.HighlightsLinked = highlightResult.LinkedCount;
                result.WarningMessage = highlightResult.WarningMessage;

                result.Success = true;
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Unified sync completed. Articles: {ArticlesCreated} created, {ArticlesUpdated} updated. " +
                    "Highlights: {HighlightsCreated} created, {HighlightsUpdated} updated, {HighlightsLinked} linked.",
                    result.ArticlesCreated, result.ArticlesUpdated,
                    result.HighlightsCreated, result.HighlightsUpdated, result.HighlightsLinked);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in unified Readwise sync");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
                return StatusCode(500, result);
            }
        }

        /// <summary>
        /// Fetch full HTML content for archived articles.
        /// Only fetches content for articles with Status = Completed.
        /// </summary>
        /// <param name="batchSize">Number of articles to fetch (default 50)</param>
        /// <param name="recentOnly">If true, only fetch articles synced in the last 7 days</param>
        [HttpPost("fetch-content")]
        public async Task<ActionResult<object>> FetchArticleContent(
            [FromQuery] int batchSize = 50,
            [FromQuery] bool recentOnly = false)
        {
            try
            {
                _logger.LogInformation("Starting article content fetch (batchSize: {BatchSize}, recentOnly: {RecentOnly})",
                    batchSize, recentOnly);

                DateTime? updatedAfter = recentOnly ? DateTime.UtcNow.AddDays(-7) : null;
                var fetchedCount = await _readerService.BulkFetchArticleContentsAsync(batchSize, updatedAfter);

                return Ok(new
                {
                    fetchedCount,
                    message = fetchedCount > 0
                        ? $"Successfully fetched content for {fetchedCount} archived article(s)"
                        : "No archived articles without content found to fetch"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching article content");
                return StatusCode(500, new { error = "Failed to fetch article content", details = ex.Message });
            }
        }

        /// <summary>
        /// Test endpoint: Fetches a document directly from Reader API by its document ID.
        /// Returns detailed information about what the API returns, useful for debugging.
        /// </summary>
        /// <param name="documentId">The Readwise Reader document ID (found in article.ReadwiseDocumentId)</param>
        /// <param name="includeHtml">Whether to request HTML content (default true)</param>
        [HttpGet("test-fetch/{documentId}")]
        public async Task<ActionResult<ReaderDocumentTestResultDto>> TestFetchDocument(
            string documentId,
            [FromQuery] bool includeHtml = true)
        {
            try
            {
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
        /// Fetch and store content for a specific article using its Reader document ID.
        /// Bypasses status checks, allowing you to fetch content for any article.
        /// </summary>
        /// <param name="documentId">The Readwise Reader document ID</param>
        [HttpPost("fetch-by-document-id/{documentId}")]
        public async Task<ActionResult<object>> FetchByDocumentId(string documentId)
        {
            try
            {
                _logger.LogInformation("Fetching content by Reader document ID: {DocumentId}", documentId);
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
        /// Lists articles that have Reader document IDs. Useful for finding articles to test with.
        /// Queries the LOCAL DATABASE.
        /// </summary>
        /// <param name="limit">Maximum number of articles to return (default 20)</param>
        /// <param name="onlyWithoutContent">If true, only returns articles without stored content</param>
        /// <param name="status">Filter by article status (e.g., "Completed" for archived, "Uncharted" for unread)</param>
        [HttpGet("articles-with-document-ids")]
        public async Task<ActionResult<IEnumerable<ReaderArticleSummaryDto>>> GetArticlesWithDocumentIds(
            [FromQuery] int limit = 20,
            [FromQuery] bool onlyWithoutContent = false,
            [FromQuery] string? status = null)
        {
            try
            {
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
        /// Fetches documents directly from the Readwise Reader API (NOT the local database).
        /// Useful for seeing what's in your Reader library.
        /// </summary>
        /// <param name="location">Filter by Reader location: "new", "later", "archive", "feed" (default: all)</param>
        /// <param name="limit">Maximum number of documents to return (default 50)</param>
        [HttpGet("reader-api/documents")]
        public async Task<ActionResult<IEnumerable<ReaderArticleSummaryDto>>> GetDocumentsFromReaderApi(
            [FromQuery] string? location = null,
            [FromQuery] int limit = 50)
        {
            try
            {
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

        /// <summary>
        /// Imports documents from the Reader API filtered by location and saves them to the database.
        /// Use this to selectively sync only archived articles, new articles, etc.
        /// </summary>
        /// <param name="location">Filter by Reader location: "new", "later", "archive", "feed" (required)</param>
        /// <param name="limit">Maximum number of documents to import (default 50)</param>
        [HttpPost("import-by-location")]
        public async Task<ActionResult<ReaderSyncResultDto>> ImportByLocation(
            [FromQuery] string location,
            [FromQuery] int limit = 50)
        {
            if (string.IsNullOrEmpty(location))
            {
                return BadRequest(new { error = "Location parameter is required. Use: new, later, archive, or feed" });
            }

            try
            {
                _logger.LogInformation("Importing {Limit} documents from Reader with location: {Location}", limit, location);
                var result = await _readerService.SyncDocumentsByLocationAsync(location, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing documents by location");
                return StatusCode(500, new { error = "Failed to import documents", details = ex.Message });
            }
        }

    }
}
