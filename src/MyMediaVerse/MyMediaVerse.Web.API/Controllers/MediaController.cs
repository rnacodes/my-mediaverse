using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly ILogger<MediaController> _logger;

        public MediaController(IMediaService mediaService, ILogger<MediaController> logger)
        {
            _mediaService = mediaService;
            _logger = logger;
        }

        // GET: api/media
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MediaItemResponseDto>>> GetAllMedia()
        {
            try
            {
                var result = await _mediaService.GetAllMediaAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve media items");
                return StatusCode(500, new { error = "Failed to retrieve media items", details = ex.Message, type = ex.GetType().Name });
            }
        }

        // GET: api/media/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MediaItemResponseDto>> GetMediaItem(Guid id)
        {
            try
            {
                var response = await _mediaService.GetMediaItemAsync(id);

                if (response == null)
                {
                    return NotFound($"Media item with ID {id} not found.");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve media item", details = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // PUT: api/media/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMediaItem(Guid id, [FromBody] CreateMediaItemDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Media item data is null.");
            }

            try
            {
                var response = await _mediaService.UpdateMediaItemAsync(id, dto);
                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Media item with ID {id} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update media item", details = ex.Message });
            }
        }

        // DELETE: api/media/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMediaItem(Guid id)
        {
            try
            {
                var deleted = await _mediaService.DeleteMediaItemAsync(id);

                if (!deleted)
                {
                    return NotFound($"Media item with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete media item", details = ex.Message });
            }
        }

        // DELETE: api/media/bulk
        [HttpDelete("bulk")]
        public async Task<IActionResult> BulkDeleteMediaItems([FromBody] BulkDeleteRequest request)
        {
            try
            {
                if (request.Ids == null || !request.Ids.Any())
                {
                    return BadRequest("No media IDs provided for deletion.");
                }

                var (deletedCount, thumbnailErrors) = await _mediaService.BulkDeleteMediaItemsAsync(request.Ids);

                if (deletedCount == 0)
                {
                    return NotFound("No media items found with the provided IDs.");
                }

                var response = new
                {
                    message = $"Successfully deleted {deletedCount} media item{(deletedCount != 1 ? "s" : "")}",
                    deletedCount = deletedCount,
                    thumbnailsDeletionErrors = thumbnailErrors.Any() ? thumbnailErrors : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to bulk delete media items", details = ex.Message });
            }
        }

        // GET: api/media/{id}/export
        [HttpGet("{id:guid}/export")]
        [Authorize] // Whole-item export of a personal library; not part of anonymous browsing.
        public async Task<IActionResult> ExportMediaItem(Guid id)
        {
            try
            {
                var result = await _mediaService.ExportMediaItemAsync(id);

                if (result == null)
                {
                    return NotFound($"Media item with ID {id} not found.");
                }

                return File(result.Value.content, "text/csv", result.Value.fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export media item", details = ex.Message });
            }
        }

        // GET: api/media/export
        [HttpGet("export")]
        [Authorize] // Whole-library export; not part of anonymous browsing.
        public async Task<IActionResult> ExportAllMedia()
        {
            try
            {
                var (content, fileName) = await _mediaService.ExportAllMediaAsync();
                return File(content, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export media items", details = ex.Message });
            }
        }
    }
}
