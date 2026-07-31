using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MixlistController : ControllerBase
    {
        private readonly IMixlistService _mixlistService;
        private readonly ILogger<MixlistController> _logger;

        public MixlistController(IMixlistService mixlistService, ILogger<MixlistController> logger)
        {
            _mixlistService = mixlistService;
            _logger = logger;
        }

        // GET: api/mixlist
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MixlistResponseDto>>> GetAllMixlists()
        {
            try
            {
                var mixlists = await _mixlistService.GetAllMixlistsAsync();
                return Ok(mixlists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mixlists");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        // GET: api/mixlist/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MixlistResponseDto>> GetMixlist(Guid id)
        {
            try
            {
                var mixlist = await _mixlistService.GetMixlistAsync(id);
                if (mixlist == null)
                {
                    return NotFound($"Mixlist with ID {id} not found.");
                }
                return Ok(mixlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mixlist {Id}", id);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        // GET: api/mixlist/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<MixlistResponseDto>>> SearchMixlists([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query is required.");
            }

            try
            {
                var mixlists = await _mixlistService.SearchMixlistsAsync(query);
                return Ok(mixlists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching mixlists");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        // POST: api/mixlist
        [HttpPost]
        public async Task<ActionResult<MixlistResponseDto>> CreateMixlist([FromBody] CreateMixlistDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Mixlist data is null.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Mixlist name is required.");
            }

            var response = await _mixlistService.CreateMixlistAsync(dto);
            return CreatedAtAction(nameof(GetMixlist), new { id = response.Id }, response);
        }

        // POST: api/mixlist/{mixlistId}/items/{mediaItemId}
        [HttpPost("{mixlistId:guid}/items/{mediaItemId:guid}")]
        public async Task<IActionResult> AddMediaItemToMixlist(Guid mixlistId, Guid mediaItemId)
        {
            try
            {
                var result = await _mixlistService.AddMediaItemToMixlistAsync(mixlistId, mediaItemId);
                if (!result.MixlistFound)
                {
                    return NotFound($"Mixlist with ID {mixlistId} not found.");
                }
                if (!result.MediaItemFound)
                {
                    return NotFound($"Media item with ID {mediaItemId} not found.");
                }
                if (result.AlreadyInMixlist)
                {
                    return BadRequest($"Media item with ID {mediaItemId} is already in the mixlist.");
                }

                return Ok(new { message = $"Media item '{result.MediaItemTitle}' added to mixlist '{result.MixlistName}'" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding media to mixlist");
                return StatusCode(500, new { error = "Failed to add media item to mixlist", details = ex.Message });
            }
        }

        // DELETE: api/mixlist/{mixlistId}/items/{mediaItemId}
        [HttpDelete("{mixlistId:guid}/items/{mediaItemId:guid}")]
        public async Task<IActionResult> RemoveMediaItemFromMixlist(Guid mixlistId, Guid mediaItemId)
        {
            try
            {
                var result = await _mixlistService.RemoveMediaItemFromMixlistAsync(mixlistId, mediaItemId);
                if (!result.MixlistFound)
                {
                    return NotFound($"Mixlist with ID {mixlistId} not found.");
                }
                if (!result.MediaInMixlist)
                {
                    return NotFound($"Media item with ID {mediaItemId} not found in the mixlist.");
                }

                return Ok(new { message = $"Media item removed from mixlist '{result.MixlistName}'" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing media from mixlist");
                return StatusCode(500, new { error = "Failed to remove media item from mixlist", details = ex.Message });
            }
        }

        // PUT: api/mixlist/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateMixlist(Guid id, [FromBody] UpdateMixlistDto dto)
        {
            try
            {
                var response = await _mixlistService.UpdateMixlistAsync(id, dto);
                if (response == null)
                {
                    return NotFound($"Mixlist with ID {id} not found.");
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mixlist {Id}", id);
                return StatusCode(500, new { error = "Failed to update mixlist", details = ex.Message });
            }
        }

        // DELETE: api/mixlist/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteMixlist(Guid id)
        {
            try
            {
                var deleted = await _mixlistService.DeleteMixlistAsync(id);
                if (!deleted)
                {
                    return NotFound($"Mixlist with ID {id} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mixlist {Id}", id);
                return StatusCode(500, new { error = "Failed to delete mixlist", details = ex.Message });
            }
        }

        // POST: api/mixlist/{mixlistId}/notes
        [HttpPost("{mixlistId:guid}/notes")]
        public async Task<IActionResult> LinkNoteToMixlist(Guid mixlistId, [FromBody] LinkNoteToMixlistDto dto)
        {
            try
            {
                var result = await _mixlistService.LinkNoteToMixlistAsync(mixlistId, dto);
                if (!result.MixlistFound)
                {
                    return NotFound($"Mixlist with ID {mixlistId} not found.");
                }
                if (!result.NoteFound)
                {
                    return NotFound($"Note with ID {dto.NoteId} not found.");
                }
                if (result.AlreadyLinked)
                {
                    return BadRequest($"Note is already linked to this mixlist.");
                }
                return Ok(new { message = "Note linked to mixlist successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking note to mixlist");
                return StatusCode(500, new { error = "Failed to link note to mixlist", details = ex.Message });
            }
        }

        // DELETE: api/mixlist/{mixlistId}/notes/{noteId}
        [HttpDelete("{mixlistId:guid}/notes/{noteId:guid}")]
        public async Task<IActionResult> UnlinkNoteFromMixlist(Guid mixlistId, Guid noteId)
        {
            try
            {
                var unlinked = await _mixlistService.UnlinkNoteFromMixlistAsync(mixlistId, noteId);
                if (!unlinked)
                {
                    return NotFound($"Note link not found for this mixlist.");
                }
                return Ok(new { message = "Note unlinked from mixlist successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking note from mixlist");
                return StatusCode(500, new { error = "Failed to unlink note from mixlist", details = ex.Message });
            }
        }

        // GET: api/mixlist/{mixlistId}/notes
        [HttpGet("{mixlistId:guid}/notes")]
        public async Task<ActionResult<IEnumerable<LinkedNoteDto>>> GetNotesForMixlist(Guid mixlistId)
        {
            try
            {
                var result = await _mixlistService.GetNotesForMixlistAsync(mixlistId);
                if (!result.MixlistFound)
                {
                    return NotFound($"Mixlist with ID {mixlistId} not found.");
                }
                return Ok(result.Notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes for mixlist {MixlistId}", mixlistId);
                return StatusCode(500, new { error = "Failed to get notes for mixlist", details = ex.Message });
            }
        }

        // POST: api/mixlist/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportMixlists([FromBody] List<ImportMixlistDto> importDtos)
        {
            if (importDtos == null || !importDtos.Any())
            {
                return BadRequest("No mixlist data provided.");
            }

            try
            {
                var result = await _mixlistService.ImportMixlistsAsync(importDtos);
                return Ok(new
                {
                    SuccessCount = result.ImportedMixlists.Count,
                    ErrorCount = result.Errors.Count,
                    ImportedMixlists = result.ImportedMixlists,
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing mixlists");
                return StatusCode(500, new { error = "Failed to import mixlists", details = ex.Message });
            }
        }

        // GET: api/mixlist/{id}/export
        [HttpGet("{id:guid}/export")]
        [Authorize] // Whole-mixlist export; not part of anonymous browsing.
        public async Task<IActionResult> ExportMixlist(Guid id)
        {
            try
            {
                var result = await _mixlistService.ExportMixlistAsync(id);
                if (!result.MixlistFound)
                {
                    return NotFound($"Mixlist with ID {id} not found.");
                }
                return File(result.Content, "text/csv", result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting mixlist {Id}", id);
                return StatusCode(500, new { error = "Failed to export mixlist", details = ex.Message });
            }
        }

        // GET: api/mixlist/export
        [HttpGet("export")]
        [Authorize] // Bulk export of all mixlists; not part of anonymous browsing.
        public async Task<IActionResult> ExportAllMixlists()
        {
            try
            {
                var result = await _mixlistService.ExportAllMixlistsAsync();
                return File(result.Content, "text/csv", result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting all mixlists");
                return StatusCode(500, new { error = "Failed to export mixlists", details = ex.Message });
            }
        }
    }
}
