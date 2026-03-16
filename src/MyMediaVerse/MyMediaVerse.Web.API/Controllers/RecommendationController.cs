using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Controller for AI-powered content recommendations using vector similarity.
    /// Provides endpoints for finding similar items, vibe-based search, and personalized recommendations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            IRecommendationService recommendationService,
            ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        /// <summary>
        /// Checks if recommendation features are available.
        /// GET /api/recommendation/status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var isAvailable = await _recommendationService.IsAvailableAsync();
                return Ok(new
                {
                    available = isAvailable,
                    message = isAvailable
                        ? "Recommendation service is operational"
                        : "Recommendation service is limited - AI service or embeddings may not be available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking recommendation service status");
                return StatusCode(500, new { error = "Error checking recommendation service status" });
            }
        }

        /// <summary>
        /// Gets media items similar to the specified item.
        /// GET /api/recommendation/similar/media/{id}?count=10&mediaType=Book
        /// </summary>
        /// <param name="id">The ID of the source media item</param>
        /// <param name="count">Maximum number of recommendations (default: 10, max: 50)</param>
        /// <param name="mediaType">Optional filter by media type</param>
        [HttpGet("similar/media/{id:guid}")]
        public async Task<IActionResult> GetSimilarMedia(
            Guid id,
            [FromQuery] int count = 10,
            [FromQuery] string? mediaType = null)
        {
            try
            {
                count = Math.Clamp(count, 1, 50);

                var results = await _recommendationService.GetSimilarMediaItemsAsync(id, count, mediaType);

                return Ok(new
                {
                    sourceId = id,
                    count = results.Count,
                    items = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar media for {MediaItemId}", id);
                return StatusCode(500, new { error = "Error finding similar items" });
            }
        }

        /// <summary>
        /// Gets notes similar to the specified note.
        /// GET /api/recommendation/similar/note/{id}?count=10&vault=general
        /// </summary>
        /// <param name="id">The ID of the source note</param>
        /// <param name="count">Maximum number of recommendations (default: 10, max: 50)</param>
        /// <param name="vault">Optional filter by vault name</param>
        [HttpGet("similar/note/{id:guid}")]
        public async Task<IActionResult> GetSimilarNotes(
            Guid id,
            [FromQuery] int count = 10,
            [FromQuery] string? vault = null)
        {
            try
            {
                count = Math.Clamp(count, 1, 50);

                var results = await _recommendationService.GetSimilarNotesAsync(id, count, vault);

                return Ok(new
                {
                    sourceId = id,
                    count = results.Count,
                    notes = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar notes for {NoteId}", id);
                return StatusCode(500, new { error = "Error finding similar notes" });
            }
        }

        /// <summary>
        /// Searches for media matching a "vibe" description.
        /// POST /api/recommendation/by-vibe
        /// </summary>
        /// <param name="request">The vibe search request</param>
        [HttpPost("by-vibe")]
        public async Task<IActionResult> SearchByVibe([FromBody] RecommendationVibeSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return BadRequest(new { error = "Description is required" });
                }

                var count = Math.Clamp(request.Count ?? 20, 1, 100);

                var results = await _recommendationService.SearchByVibeAsync(
                    request.Description,
                    count,
                    request.MediaType);

                return Ok(new
                {
                    description = request.Description,
                    count = results.Count,
                    items = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in vibe search for '{Description}'", request.Description);
                return StatusCode(500, new { error = "Error performing vibe search" });
            }
        }

        /// <summary>
        /// Gets personalized recommendations based on liked items.
        /// GET /api/recommendation/for-you?count=20&excludeExplored=true
        /// Requires authorization.
        /// </summary>
        /// <param name="count">Maximum number of recommendations (default: 20, max: 100)</param>
        /// <param name="excludeExplored">Whether to exclude already explored items (default: true)</param>
        [HttpGet("for-you")]
        [Authorize]
        public async Task<IActionResult> GetPersonalizedRecommendations(
            [FromQuery] int count = 20,
            [FromQuery] bool excludeExplored = true)
        {
            try
            {
                count = Math.Clamp(count, 1, 100);

                var results = await _recommendationService.GetPersonalizedRecommendationsAsync(count, excludeExplored);

                return Ok(new
                {
                    count = results.Count,
                    excludedExplored = excludeExplored,
                    items = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations");
                return StatusCode(500, new { error = "Error generating personalized recommendations" });
            }
        }

        /// <summary>
        /// Gets media items related to a specific note.
        /// GET /api/recommendation/media-for-note/{noteId}
        /// </summary>
        /// <param name="noteId">The ID of the note</param>
        /// <param name="count">Maximum number of results (default: 10)</param>
        [HttpGet("media-for-note/{noteId:guid}")]
        public async Task<IActionResult> GetMediaRelatedToNote(
            Guid noteId,
            [FromQuery] int count = 10)
        {
            try
            {
                count = Math.Clamp(count, 1, 50);

                var results = await _recommendationService.GetMediaRelatedToNoteAsync(noteId, count);

                return Ok(new
                {
                    noteId,
                    count = results.Count,
                    items = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media related to note {NoteId}", noteId);
                return StatusCode(500, new { error = "Error finding related media" });
            }
        }

        /// <summary>
        /// Gets notes related to a specific media item.
        /// GET /api/recommendation/notes-for-media/{mediaItemId}
        /// </summary>
        /// <param name="mediaItemId">The ID of the media item</param>
        /// <param name="count">Maximum number of results (default: 10)</param>
        [HttpGet("notes-for-media/{mediaItemId:guid}")]
        public async Task<IActionResult> GetNotesRelatedToMedia(
            Guid mediaItemId,
            [FromQuery] int count = 10)
        {
            try
            {
                count = Math.Clamp(count, 1, 50);

                var results = await _recommendationService.GetNotesRelatedToMediaAsync(mediaItemId, count);

                return Ok(new
                {
                    mediaItemId,
                    count = results.Count,
                    notes = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes related to media item {MediaItemId}", mediaItemId);
                return StatusCode(500, new { error = "Error finding related notes" });
            }
        }
    }

    /// <summary>
    /// Request model for vibe-based search in recommendation context.
    /// </summary>
    public class RecommendationVibeSearchRequest
    {
        /// <summary>
        /// A description of the "vibe" or mood you're looking for.
        /// Examples: "dark atmospheric sci-fi", "uplifting productivity content", "cozy mystery novels"
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional filter by media type (Book, Movie, Article, etc.)
        /// </summary>
        public string? MediaType { get; set; }

        /// <summary>
        /// Maximum number of results (default: 20, max: 100)
        /// </summary>
        public int? Count { get; set; }
    }
}
