using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RelatedMediaController : ControllerBase
    {
        private readonly IRelatedMediaService _relatedMediaService;
        private readonly ILogger<RelatedMediaController> _logger;

        public RelatedMediaController(
            IRelatedMediaService relatedMediaService,
            ILogger<RelatedMediaController> logger)
        {
            _relatedMediaService = relatedMediaService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all saved related media items for a specific media item.
        /// GET /api/relatedmedia/{mediaItemId}
        /// </summary>
        [HttpGet("{mediaItemId:guid}")]
        public async Task<ActionResult<IEnumerable<RelatedMediaResponseDto>>> GetRelatedMedia(
            Guid mediaItemId,
            [FromQuery] bool includeBidirectional = true)
        {
            try
            {
                var result = await _relatedMediaService.GetRelatedMediaAsync(mediaItemId, includeBidirectional);
                if (!result.MediaItemFound)
                {
                    return NotFound($"Media item with ID {mediaItemId} not found.");
                }
                return Ok(result.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related media for {MediaItemId}", mediaItemId);
                return StatusCode(500, new { error = "Failed to get related media", details = ex.Message });
            }
        }

        /// <summary>
        /// Saves a related media item.
        /// POST /api/relatedmedia/{sourceMediaItemId}
        /// </summary>
        [HttpPost("{sourceMediaItemId:guid}")]
        public async Task<ActionResult<RelatedMediaResponseDto>> SaveRelatedMedia(
            Guid sourceMediaItemId,
            [FromBody] SaveRelatedMediaDto dto)
        {
            try
            {
                var result = await _relatedMediaService.SaveRelatedMediaAsync(sourceMediaItemId, dto);

                if (result.SelfReference)
                {
                    return BadRequest("A media item cannot be related to itself.");
                }
                if (!result.SourceFound)
                {
                    return NotFound($"Source media item with ID {sourceMediaItemId} not found.");
                }
                if (!result.RelatedFound)
                {
                    return NotFound($"Related media item with ID {dto.RelatedMediaItemId} not found.");
                }
                if (result.AlreadyExists)
                {
                    return BadRequest("This relationship already exists.");
                }

                return CreatedAtAction(nameof(GetRelatedMedia),
                    new { mediaItemId = sourceMediaItemId },
                    result.Saved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving related media for {SourceMediaItemId}", sourceMediaItemId);
                return StatusCode(500, new { error = "Failed to save related media", details = ex.Message });
            }
        }

        /// <summary>
        /// Removes a related media item.
        /// DELETE /api/relatedmedia/{sourceMediaItemId}/{relatedMediaItemId}
        /// </summary>
        [HttpDelete("{sourceMediaItemId:guid}/{relatedMediaItemId:guid}")]
        public async Task<IActionResult> RemoveRelatedMedia(Guid sourceMediaItemId, Guid relatedMediaItemId)
        {
            try
            {
                var removed = await _relatedMediaService.RemoveRelatedMediaAsync(sourceMediaItemId, relatedMediaItemId);
                if (!removed)
                {
                    return NotFound($"Relationship between {sourceMediaItemId} and {relatedMediaItemId} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing related media for {SourceMediaItemId} -> {RelatedMediaItemId}",
                    sourceMediaItemId, relatedMediaItemId);
                return StatusCode(500, new { error = "Failed to remove related media", details = ex.Message });
            }
        }

        /// <summary>
        /// Batch save multiple related items (useful for saving multiple AI recommendations at once).
        /// POST /api/relatedmedia/{sourceMediaItemId}/batch
        /// </summary>
        [HttpPost("{sourceMediaItemId:guid}/batch")]
        public async Task<ActionResult> SaveRelatedMediaBatch(
            Guid sourceMediaItemId,
            [FromBody] List<SaveRelatedMediaDto> dtos)
        {
            try
            {
                if (dtos == null || !dtos.Any())
                {
                    return BadRequest("No related items provided.");
                }

                var result = await _relatedMediaService.SaveRelatedMediaBatchAsync(sourceMediaItemId, dtos);
                if (!result.SourceFound)
                {
                    return NotFound($"Source media item with ID {sourceMediaItemId} not found.");
                }

                return Ok(new
                {
                    savedCount = result.Saved.Count,
                    errorCount = result.Errors.Count,
                    saved = result.Saved,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch save related media for {SourceMediaItemId}", sourceMediaItemId);
                return StatusCode(500, new { error = "Failed to batch save related media", details = ex.Message });
            }
        }
    }
}
