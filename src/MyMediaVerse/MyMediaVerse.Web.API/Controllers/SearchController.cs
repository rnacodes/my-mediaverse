using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Controller for handling search operations using Typesense.
    /// Provides a secure proxy between the frontend and Typesense server.
    /// Read operations (search) are public. Write operations (reindex) require authorization.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ITypeSenseService _typeSenseService;
        private readonly IGradientAIClient _gradientClient;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            ITypeSenseService typeSenseService,
            IGradientAIClient gradientClient,
            ILogger<SearchController> logger)
        {
            _typeSenseService = typeSenseService;
            _gradientClient = gradientClient;
            _logger = logger;
        }

        /// <summary>
        /// Searches media items using Typesense full-text search.
        /// GET /api/search?q=searchterm&filter=media_type:=Book&page=1&per_page=20
        /// </summary>
        /// <param name="q">Search query text</param>
        /// <param name="filter">Optional filter string (e.g., "media_type:=Book" or "status:=Completed")</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="per_page">Results per page (default: 20, max: 100)</param>
        /// <returns>Search results from Typesense</returns>
        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20)
        {
            try
            {
                // Validate query parameter
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { error = "Search query 'q' parameter is required." });
                }

                // Limit per_page to prevent abuse
                if (per_page > 100)
                {
                    per_page = 100;
                }

                if (per_page < 1)
                {
                    per_page = 20;
                }

                if (page < 1)
                {
                    page = 1;
                }

                _logger.LogInformation("Search request: query='{Query}', filter='{Filter}', page={Page}, per_page={PerPage}", 
                    q, filter, page, per_page);

                var results = await _typeSenseService.SearchAsync(q, filter, per_page, page);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search for query '{Query}'", q);
                return StatusCode(500, new { error = "An error occurred while searching. Please try again." });
            }
        }

        /// <summary>
        /// Searches media items by media type.
        /// GET /api/search/by-type/Book?q=searchterm
        /// </summary>
        /// <param name="mediaType">The media type to filter by (Article, Book, Movie, TVShow, Video, Podcast, Website, Channel, Playlist)</param>
        /// <param name="q">Search query text</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="per_page">Results per page (default: 20)</param>
        [HttpGet("by-type/{mediaType}")]
        public async Task<IActionResult> SearchByType(
            string mediaType,
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { error = "Search query 'q' parameter is required." });
                }

                // Validate media type (basic validation - Typesense will handle invalid values gracefully)
                var validMediaTypes = new[] { "Article", "Book", "Movie", "TVShow", "Video", "Podcast", "Website", "Channel", "Playlist" };
                if (!validMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = $"Invalid media type. Valid types: {string.Join(", ", validMediaTypes)}" });
                }

                // Create filter for media type
                var filter = $"media_type:={mediaType}";

                var results = await _typeSenseService.SearchAsync(q, filter, per_page, page);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search by type '{MediaType}' for query '{Query}'", mediaType, q);
                return StatusCode(500, new { error = "An error occurred while searching. Please try again." });
            }
        }

        /// <summary>
        /// Searches mixlists using Typesense full-text search.
        /// GET /api/search/mixlists?q=searchterm&filter=topics:=productivity&page=1&per_page=20
        /// </summary>
        /// <param name="q">Search query text</param>
        /// <param name="filter">Optional filter string (e.g., "topics:=productivity" or "genres:=fiction")</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="per_page">Results per page (default: 20, max: 100)</param>
        /// <returns>Search results from Typesense</returns>
        [HttpGet("mixlists")]
        public async Task<IActionResult> SearchMixlists(
            [FromQuery] string q,
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20)
        {
            try
            {
                // Validate query parameter
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { error = "Search query 'q' parameter is required." });
                }

                // Limit per_page to prevent abuse
                if (per_page > 100)
                {
                    per_page = 100;
                }

                if (per_page < 1)
                {
                    per_page = 20;
                }

                if (page < 1)
                {
                    page = 1;
                }

                _logger.LogInformation("Mixlist search request: query='{Query}', filter='{Filter}', page={Page}, per_page={PerPage}", 
                    q, filter, page, per_page);

                var results = await _typeSenseService.SearchMixlistsAsync(q, filter, per_page, page);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing mixlist search for query '{Query}'", q);
                return StatusCode(500, new { error = "An error occurred while searching mixlists. Please try again." });
            }
        }

        /// <summary>
        /// Triggers a full re-index of all media items from PostgreSQL to Typesense.
        /// POST /api/search/reindex
        /// This is an admin operation and should be used sparingly.
        /// </summary>
        [HttpPost("reindex")]
        [Authorize] // Require authorization for admin operations
        public async Task<IActionResult> ReindexAll()
        {
            try
            {
                _logger.LogInformation("Starting full re-index of all media items...");

                var count = await _typeSenseService.BulkReindexAllMediaItemsAsync();

                _logger.LogInformation("Re-index complete. Indexed {Count} media items.", count);

                return Ok(new 
                { 
                    message = "Re-index completed successfully.", 
                    indexed_count = count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during re-index operation.");
                return StatusCode(500, new { error = "An error occurred during re-index. Please check logs." });
            }
        }

        /// <summary>
        /// Triggers a full re-index of all mixlists from PostgreSQL to Typesense.
        /// POST /api/search/reindex-mixlists
        /// This is an admin operation and should be used sparingly.
        /// </summary>
        [HttpPost("reindex-mixlists")]
        [Authorize] // Require authorization for admin operations
        public async Task<IActionResult> ReindexAllMixlists()
        {
            try
            {
                _logger.LogInformation("Starting full re-index of all mixlists...");

                var count = await _typeSenseService.BulkReindexAllMixlistsAsync();

                _logger.LogInformation("Re-index of mixlists complete. Indexed {Count} mixlists.", count);

                return Ok(new 
                { 
                    message = "Mixlist re-index completed successfully.", 
                    indexed_count = count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during mixlist re-index operation.");
                return StatusCode(500, new { error = "An error occurred during mixlist re-index. Please check logs." });
            }
        }

        /// <summary>
        /// Health check endpoint for Typesense integration.
        /// GET /api/search/health
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous] // Allow anonymous access for health checks
        public IActionResult Health()
        {
            try
            {
                // Simple check - if the service is injected, it's configured
                if (_typeSenseService == null)
                {
                    return StatusCode(503, new { status = "unavailable", message = "Typesense service not configured." });
                }

                return Ok(new { status = "healthy", message = "Typesense integration is operational." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Typesense health.");
                return StatusCode(503, new { status = "unhealthy", message = ex.Message });
            }
        }

        /// <summary>
        /// Completely resets the media_items collection by deleting and recreating it.
        /// POST /api/search/reset
        /// WARNING: This will delete all indexed media items from Typesense!
        /// </summary>
        [HttpPost("reset")]
        [Authorize] // Require authorization for admin operations
        public async Task<IActionResult> ResetMediaItemsCollection()
        {
            try
            {
                _logger.LogInformation("Resetting media_items collection...");

                await _typeSenseService.ResetMediaItemsCollectionAsync();

                _logger.LogInformation("Media_items collection reset complete.");

                return Ok(new 
                { 
                    message = "Media items collection reset successfully. All old data has been cleared.",
                    collection = "media_items"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting media_items collection.");
                return StatusCode(500, new { error = "An error occurred while resetting the collection. Please check logs." });
            }
        }

        /// <summary>
        /// Completely resets the mixlists collection by deleting and recreating it.
        /// POST /api/search/reset-mixlists
        /// WARNING: This will delete all indexed mixlists from Typesense!
        /// </summary>
        [HttpPost("reset-mixlists")]
        [Authorize] // Require authorization for admin operations
        public async Task<IActionResult> ResetMixlistsCollection()
        {
            try
            {
                _logger.LogInformation("Resetting mixlists collection...");

                await _typeSenseService.ResetMixlistsCollectionAsync();

                _logger.LogInformation("Mixlists collection reset complete.");

                return Ok(new
                {
                    message = "Mixlists collection reset successfully. All old data has been cleared.",
                    collection = "mixlists"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting mixlists collection.");
                return StatusCode(500, new { error = "An error occurred while resetting the collection. Please check logs." });
            }
        }

        // ============================================
        // Notes Search Endpoints
        // ============================================

        /// <summary>
        /// Searches Obsidian notes using Typesense full-text search.
        /// GET /api/search/notes?q=searchterm&filter=vault_name:=general&page=1&per_page=20
        /// </summary>
        [HttpGet("notes")]
        public async Task<IActionResult> SearchNotes(
            [FromQuery] string q,
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { error = "Search query 'q' parameter is required." });
                }

                if (per_page > 100) per_page = 100;
                if (per_page < 1) per_page = 20;
                if (page < 1) page = 1;

                _logger.LogInformation("Notes search request: query='{Query}', filter='{Filter}', page={Page}, per_page={PerPage}",
                    q, filter, page, per_page);

                var results = await _typeSenseService.SearchNotesAsync(q, filter, per_page, page);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing notes search for query '{Query}'", q);
                return StatusCode(500, new { error = "An error occurred while searching notes. Please try again." });
            }
        }

        /// <summary>
        /// Searches notes by vault name.
        /// GET /api/search/notes/by-vault/general?q=searchterm
        /// </summary>
        [HttpGet("notes/by-vault/{vault}")]
        public async Task<IActionResult> SearchNotesByVault(
            string vault,
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { error = "Search query 'q' parameter is required." });
                }

                var filter = $"vault_name:={vault.ToLower()}";
                var results = await _typeSenseService.SearchNotesAsync(q, filter, per_page, page);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing notes search by vault '{Vault}' for query '{Query}'", vault, q);
                return StatusCode(500, new { error = "An error occurred while searching notes. Please try again." });
            }
        }

        /// <summary>
        /// Performs a multi-search across media items, mixlists, and notes.
        /// GET /api/search/all?q=searchterm&page=1&per_page=20
        /// Returns results from all collections.
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> MultiSearch(
            [FromQuery] string q,
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { error = "Search query 'q' parameter is required." });
                }

                if (per_page > 100) per_page = 100;
                if (per_page < 1) per_page = 20;
                if (page < 1) page = 1;

                _logger.LogInformation("Multi-search request: query='{Query}', filter='{Filter}', page={Page}, per_page={PerPage}",
                    q, filter, page, per_page);

                var results = await _typeSenseService.MultiSearchAsync(q, filter, per_page, page);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing multi-search for query '{Query}'", q);
                return StatusCode(500, new { error = "An error occurred while searching. Please try again." });
            }
        }

        /// <summary>
        /// Triggers a full re-index of all notes from PostgreSQL to Typesense.
        /// POST /api/search/reindex-notes
        /// </summary>
        [HttpPost("reindex-notes")]
        [Authorize]
        public async Task<IActionResult> ReindexAllNotes()
        {
            try
            {
                _logger.LogInformation("Starting full re-index of all notes...");

                var count = await _typeSenseService.BulkReindexAllNotesAsync();

                _logger.LogInformation("Re-index of notes complete. Indexed {Count} notes.", count);

                return Ok(new
                {
                    message = "Notes re-index completed successfully.",
                    indexed_count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during notes re-index operation.");
                return StatusCode(500, new { error = "An error occurred during notes re-index. Please check logs." });
            }
        }

        /// <summary>
        /// Completely resets the obsidian_notes collection by deleting and recreating it.
        /// POST /api/search/reset-notes
        /// WARNING: This will delete all indexed notes from Typesense!
        /// </summary>
        [HttpPost("reset-notes")]
        [Authorize]
        public async Task<IActionResult> ResetNotesCollection()
        {
            try
            {
                _logger.LogInformation("Resetting obsidian_notes collection...");

                await _typeSenseService.ResetNotesCollectionAsync();

                _logger.LogInformation("Obsidian_notes collection reset complete.");

                return Ok(new
                {
                    message = "Notes collection reset successfully. All old data has been cleared.",
                    collection = "obsidian_notes"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting obsidian_notes collection.");
                return StatusCode(500, new { error = "An error occurred while resetting the collection. Please check logs." });
            }
        }

        // ============================================
        // Semantic/Hybrid Search Endpoints
        // ============================================

        /// <summary>
        /// Performs a semantic/hybrid search across media items.
        /// POST /api/search/semantic
        /// Uses AI-generated embeddings for semantic understanding.
        /// </summary>
        /// <param name="request">Search request with query and optional parameters</param>
        [HttpPost("semantic")]
        public async Task<IActionResult> SemanticSearchMedia([FromBody] SemanticSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { error = "Search query is required." });
                }

                var perPage = Math.Clamp(request.PerPage ?? 20, 1, 100);
                var page = Math.Max(request.Page ?? 1, 1);
                var alpha = Math.Clamp(request.Alpha ?? 0.5f, 0f, 1f);

                _logger.LogInformation(
                    "Semantic media search: query='{Query}', alpha={Alpha}, page={Page}, per_page={PerPage}",
                    request.Query, alpha, page, perPage);

                // Generate embedding for the query
                float[]? queryEmbedding = null;
                if (await _gradientClient.IsAvailableAsync())
                {
                    try
                    {
                        queryEmbedding = await _gradientClient.GenerateEmbeddingAsync(request.Query);
                        _logger.LogDebug("Generated embedding with {Dims} dimensions for query", queryEmbedding.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate embedding for query, falling back to keyword search");
                    }
                }
                else
                {
                    _logger.LogWarning("Gradient AI not available, falling back to keyword search");
                }

                var results = await _typeSenseService.HybridSearchMediaAsync(
                    request.Query,
                    queryEmbedding,
                    request.Filter,
                    alpha,
                    perPage,
                    page);

                return Ok(new
                {
                    results,
                    semantic_enabled = queryEmbedding != null,
                    alpha
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing semantic media search for query '{Query}'", request.Query);
                return StatusCode(500, new { error = "An error occurred during semantic search. Please try again." });
            }
        }

        /// <summary>
        /// Performs a semantic/hybrid search across notes.
        /// POST /api/search/semantic/notes
        /// </summary>
        [HttpPost("semantic/notes")]
        public async Task<IActionResult> SemanticSearchNotes([FromBody] SemanticSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { error = "Search query is required." });
                }

                var perPage = Math.Clamp(request.PerPage ?? 20, 1, 100);
                var page = Math.Max(request.Page ?? 1, 1);
                var alpha = Math.Clamp(request.Alpha ?? 0.5f, 0f, 1f);

                _logger.LogInformation(
                    "Semantic notes search: query='{Query}', alpha={Alpha}, page={Page}, per_page={PerPage}",
                    request.Query, alpha, page, perPage);

                float[]? queryEmbedding = null;
                if (await _gradientClient.IsAvailableAsync())
                {
                    try
                    {
                        queryEmbedding = await _gradientClient.GenerateEmbeddingAsync(request.Query);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate embedding for query, falling back to keyword search");
                    }
                }

                var results = await _typeSenseService.HybridSearchNotesAsync(
                    request.Query,
                    queryEmbedding,
                    request.Filter,
                    alpha,
                    perPage,
                    page);

                return Ok(new
                {
                    results,
                    semantic_enabled = queryEmbedding != null,
                    alpha
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing semantic notes search for query '{Query}'", request.Query);
                return StatusCode(500, new { error = "An error occurred during semantic search. Please try again." });
            }
        }

        /// <summary>
        /// Performs a "search by vibe" - pure semantic search using a description.
        /// POST /api/search/by-vibe
        /// Useful for queries like "dark atmospheric sci-fi movies" or "uplifting productivity podcasts".
        /// </summary>
        [HttpPost("by-vibe")]
        public async Task<IActionResult> SearchByVibe([FromBody] VibeSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return BadRequest(new { error = "Description is required for vibe search." });
                }

                var limit = Math.Clamp(request.Limit ?? 20, 1, 100);

                _logger.LogInformation("Vibe search: description='{Description}', limit={Limit}", request.Description, limit);

                if (!await _gradientClient.IsAvailableAsync())
                {
                    return StatusCode(503, new { error = "Semantic search is not available. AI service is not configured." });
                }

                // Generate embedding for the vibe description
                var embedding = await _gradientClient.GenerateEmbeddingAsync(request.Description);

                var results = await _typeSenseService.VectorSearchMediaAsync(
                    embedding,
                    request.Filter,
                    null,
                    limit);

                return Ok(new
                {
                    results,
                    description = request.Description
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing vibe search for description '{Description}'", request.Description);
                return StatusCode(500, new { error = "An error occurred during vibe search. Please try again." });
            }
        }

        // ============================================
        // Highlights Search Endpoints
        // ============================================

        /// <summary>
        /// Search highlights in Typesense.
        /// GET /api/search/highlights?q=query&filter=category:=books&page=1&per_page=20
        /// Searchable fields: text, note, title, author, tags
        /// Filterable fields: category, tags, source_type, is_favorite, linked_media_type
        /// </summary>
        [HttpGet("highlights")]
        public async Task<IActionResult> SearchHighlights(
            [FromQuery] string q = "*",
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 20)
        {
            try
            {
                var perPage = Math.Clamp(per_page, 1, 100);
                var pageNum = Math.Max(page, 1);

                _logger.LogInformation("Searching highlights: query='{Query}', filter='{Filter}', page={Page}", q, filter, pageNum);

                var results = await _typeSenseService.SearchHighlightsAsync(q, filter, perPage, pageNum);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching highlights for query '{Query}'", q);
                return StatusCode(500, new { error = "An error occurred while searching highlights. Please try again." });
            }
        }

        /// <summary>
        /// Triggers a full re-index of all highlights from the database.
        /// POST /api/search/reindex-highlights
        /// </summary>
        [HttpPost("reindex-highlights")]
        [Authorize]
        public async Task<IActionResult> ReindexHighlights()
        {
            try
            {
                _logger.LogInformation("Starting full re-index of highlights...");

                var count = await _typeSenseService.BulkReindexAllHighlightsAsync();

                _logger.LogInformation("Highlights re-index complete. Indexed {Count} highlights.", count);

                return Ok(new
                {
                    message = $"Successfully re-indexed {count} highlights.",
                    indexed_count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-indexing highlights.");
                return StatusCode(500, new { error = "An error occurred while re-indexing highlights. Please check logs." });
            }
        }

        /// <summary>
        /// Completely resets the highlights collection by deleting and recreating it.
        /// POST /api/search/reset-highlights
        /// WARNING: This will delete all indexed highlights from Typesense!
        /// </summary>
        [HttpPost("reset-highlights")]
        [Authorize]
        public async Task<IActionResult> ResetHighlightsCollection()
        {
            try
            {
                _logger.LogInformation("Resetting highlights collection...");

                await _typeSenseService.ResetHighlightsCollectionAsync();

                _logger.LogInformation("Highlights collection reset complete.");

                return Ok(new
                {
                    message = "Highlights collection reset successfully. All old data has been cleared.",
                    collection = "highlights"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting highlights collection.");
                return StatusCode(500, new { error = "An error occurred while resetting the collection. Please check logs." });
            }
        }

    }

    /// <summary>
    /// Request model for semantic search operations.
    /// </summary>
    public class SemanticSearchRequest
    {
        /// <summary>
        /// The search query text.
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Optional filter string (e.g., "media_type:=Book").
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// Balance between keyword (0) and semantic (1) search. Default: 0.5
        /// </summary>
        public float? Alpha { get; set; }

        /// <summary>
        /// Page number (default: 1).
        /// </summary>
        public int? Page { get; set; }

        /// <summary>
        /// Results per page (default: 20, max: 100).
        /// </summary>
        public int? PerPage { get; set; }
    }

    /// <summary>
    /// Request model for vibe-based search.
    /// </summary>
    public class VibeSearchRequest
    {
        /// <summary>
        /// A description of the "vibe" or mood you're looking for.
        /// Examples: "dark atmospheric sci-fi", "uplifting productivity content"
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional filter string.
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// Maximum number of results (default: 20, max: 100).
        /// </summary>
        public int? Limit { get; set; }
    }
}
