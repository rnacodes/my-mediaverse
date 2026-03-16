using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Controller for AI-powered operations including description generation and embeddings.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(IAIService aiService, ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        // ============================================
        // Status Endpoints
        // ============================================

        /// <summary>
        /// Gets the current status of AI services.
        /// GET /api/ai/status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var status = await _aiService.GetStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI status");
                return StatusCode(500, new { message = "Error getting AI status", error = ex.Message });
            }
        }

        // ============================================
        // Note Description Generation
        // ============================================

        /// <summary>
        /// Generates an AI description for a specific note.
        /// POST /api/ai/notes/{id}/generate-description
        /// </summary>
        [HttpPost("notes/{id:guid}/generate-description")]
        public async Task<IActionResult> GenerateNoteDescription(Guid id)
        {
            try
            {
                var description = await _aiService.GenerateNoteDescriptionAsync(id);

                if (description == null)
                {
                    return NotFound(new { message = "Note not found or has no content for description generation" });
                }

                return Ok(new NoteDescriptionResultDto
                {
                    NoteId = id,
                    GeneratedDescription = description,
                    GeneratedAt = DateTime.UtcNow,
                    Success = true
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI service not available for note description generation");
                return ServiceUnavailable(new { message = "AI service is not configured or unavailable", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating description for note {NoteId}", id);
                return StatusCode(500, new { message = "Error generating description", error = ex.Message });
            }
        }

        /// <summary>
        /// Generates AI descriptions for a batch of notes that don't have descriptions.
        /// POST /api/ai/notes/generate-descriptions-batch
        /// </summary>
        [HttpPost("notes/generate-descriptions-batch")]
        public async Task<IActionResult> GenerateNoteDescriptionsBatch([FromQuery] int batchSize = 20)
        {
            try
            {
                if (batchSize < 1 || batchSize > 100)
                {
                    return BadRequest(new { message = "Batch size must be between 1 and 100" });
                }

                var result = await _aiService.GenerateNoteDescriptionsBatchAsync(batchSize);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI service not available for batch description generation");
                return ServiceUnavailable(new { message = "AI service is not configured or unavailable", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch description generation");
                return StatusCode(500, new { message = "Error in batch description generation", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the count of notes that need AI description generation.
        /// GET /api/ai/notes/pending-descriptions
        /// </summary>
        [HttpGet("notes/pending-descriptions")]
        public async Task<IActionResult> GetPendingNoteDescriptionsCount()
        {
            try
            {
                var count = await _aiService.GetNotesNeedingDescriptionCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending note descriptions count");
                return StatusCode(500, new { message = "Error getting pending count", error = ex.Message });
            }
        }

        // ============================================
        // Embedding Generation
        // ============================================

        /// <summary>
        /// Generates an embedding for a specific media item.
        /// POST /api/ai/media/{id}/generate-embedding
        /// </summary>
        [HttpPost("media/{id:guid}/generate-embedding")]
        public async Task<IActionResult> GenerateMediaItemEmbedding(Guid id)
        {
            try
            {
                var success = await _aiService.GenerateMediaItemEmbeddingAsync(id);

                if (!success)
                {
                    return NotFound(new { message = "Media item not found" });
                }

                return Ok(new { message = "Embedding generated successfully", mediaItemId = id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI service not available for embedding generation");
                return ServiceUnavailable(new { message = "AI service is not configured or unavailable", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for media item {MediaItemId}", id);
                return StatusCode(500, new { message = "Error generating embedding", error = ex.Message });
            }
        }

        /// <summary>
        /// Generates an embedding for a specific note.
        /// POST /api/ai/notes/{id}/generate-embedding
        /// </summary>
        [HttpPost("notes/{id:guid}/generate-embedding")]
        public async Task<IActionResult> GenerateNoteEmbedding(Guid id)
        {
            try
            {
                var success = await _aiService.GenerateNoteEmbeddingAsync(id);

                if (!success)
                {
                    return NotFound(new { message = "Note not found" });
                }

                return Ok(new { message = "Embedding generated successfully", noteId = id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI service not available for embedding generation");
                return ServiceUnavailable(new { message = "AI service is not configured or unavailable", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for note {NoteId}", id);
                return StatusCode(500, new { message = "Error generating embedding", error = ex.Message });
            }
        }

        /// <summary>
        /// Generates embeddings for a batch of media items that don't have embeddings.
        /// POST /api/ai/media/generate-embeddings-batch
        /// </summary>
        [HttpPost("media/generate-embeddings-batch")]
        public async Task<IActionResult> GenerateMediaItemEmbeddingsBatch([FromQuery] int batchSize = 50)
        {
            try
            {
                if (batchSize < 1 || batchSize > 200)
                {
                    return BadRequest(new { message = "Batch size must be between 1 and 200" });
                }

                var result = await _aiService.GenerateMediaItemEmbeddingsBatchAsync(batchSize);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI service not available for batch embedding generation");
                return ServiceUnavailable(new { message = "AI service is not configured or unavailable", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch media item embedding generation");
                return StatusCode(500, new { message = "Error in batch embedding generation", error = ex.Message });
            }
        }

        /// <summary>
        /// Generates embeddings for a batch of notes that don't have embeddings.
        /// POST /api/ai/notes/generate-embeddings-batch
        /// </summary>
        [HttpPost("notes/generate-embeddings-batch")]
        public async Task<IActionResult> GenerateNoteEmbeddingsBatch([FromQuery] int batchSize = 50)
        {
            try
            {
                if (batchSize < 1 || batchSize > 200)
                {
                    return BadRequest(new { message = "Batch size must be between 1 and 200" });
                }

                var result = await _aiService.GenerateNoteEmbeddingsBatchAsync(batchSize);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI service not available for batch embedding generation");
                return ServiceUnavailable(new { message = "AI service is not configured or unavailable", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch note embedding generation");
                return StatusCode(500, new { message = "Error in batch embedding generation", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the count of media items that need embedding generation.
        /// GET /api/ai/media/pending-embeddings
        /// </summary>
        [HttpGet("media/pending-embeddings")]
        public async Task<IActionResult> GetPendingMediaEmbeddingsCount()
        {
            try
            {
                var count = await _aiService.GetMediaItemsNeedingEmbeddingCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending media embeddings count");
                return StatusCode(500, new { message = "Error getting pending count", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the count of notes that need embedding generation.
        /// GET /api/ai/notes/pending-embeddings
        /// </summary>
        [HttpGet("notes/pending-embeddings")]
        public async Task<IActionResult> GetPendingNoteEmbeddingsCount()
        {
            try
            {
                var count = await _aiService.GetNotesNeedingEmbeddingCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending note embeddings count");
                return StatusCode(500, new { message = "Error getting pending count", error = ex.Message });
            }
        }

        // ============================================
        // Helper Methods
        // ============================================

        private ObjectResult ServiceUnavailable(object value)
        {
            return StatusCode(503, value);
        }
    }
}
