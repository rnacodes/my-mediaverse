using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Controller for managing Obsidian notes from Quartz vaults.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NoteController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NoteController> _logger;
        private readonly IConfiguration _configuration;

        public NoteController(INoteService noteService, ILogger<NoteController> logger, IConfiguration configuration)
        {
            _noteService = noteService;
            _logger = logger;
            _configuration = configuration;
        }

        // ============================================
        // CRUD Operations
        // ============================================

        /// <summary>
        /// Gets all notes, optionally filtered by vault name.
        /// GET /api/note?vault=general
        /// </summary>
        [HttpGet]        public async Task<IActionResult> GetAll([FromQuery] string? vault = null)
        {
            try
            {
                var notes = await _noteService.GetAllAsync(vault);
                var response = notes.Select(n => MapToResponseDto(n)).ToList();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all notes");
                return StatusCode(500, new { message = "Error retrieving notes", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a note by its ID.
        /// GET /api/note/{id}
        /// </summary>
        [HttpGet("{id:guid}")]        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var note = await _noteService.GetByIdAsync(id);
                if (note == null)
                {
                    return NotFound(new { message = $"Note with ID {id} not found" });
                }

                return Ok(MapToResponseDto(note));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {Id}", id);
                return StatusCode(500, new { message = "Error retrieving note", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a note by its vault name and slug.
        /// GET /api/note/slug/{vault}/{slug}
        /// </summary>
        [HttpGet("slug/{vault}/{slug}")]        public async Task<IActionResult> GetBySlug(string vault, string slug)
        {
            try
            {
                var note = await _noteService.GetBySlugAndVaultAsync(slug, vault);
                if (note == null)
                {
                    return NotFound(new { message = $"Note '{slug}' not found in vault '{vault}'" });
                }

                return Ok(MapToResponseDto(note));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {Slug} from vault {Vault}", slug, vault);
                return StatusCode(500, new { message = "Error retrieving note", error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new note manually.
        /// POST /api/note
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNoteDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Slug) || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.VaultName))
                {
                    return BadRequest(new { message = "Slug, title, and vaultName are required" });
                }

                var note = await _noteService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = note.Id }, MapToResponseDto(note));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note");
                return StatusCode(500, new { message = "Error creating note", error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing note.
        /// PUT /api/note/{id}
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNoteDto dto)
        {
            try
            {
                var note = await _noteService.UpdateAsync(id, dto);
                return Ok(MapToResponseDto(note));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note {Id}", id);
                return StatusCode(500, new { message = "Error updating note", error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a note.
        /// DELETE /api/note/{id}
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _noteService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {Id}", id);
                return StatusCode(500, new { message = "Error deleting note", error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes every note whose ID is in the request; unknown IDs are skipped.
        /// DELETE /api/note/bulk
        /// </summary>
        [HttpDelete("bulk")]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
        {
            try
            {
                if (request.Ids == null || !request.Ids.Any())
                {
                    return BadRequest(new { message = "No note IDs provided for deletion." });
                }

                var deletedCount = await _noteService.BulkDeleteAsync(request.Ids);

                if (deletedCount == 0)
                {
                    return NotFound(new { message = "No notes found with the provided IDs." });
                }

                return Ok(new
                {
                    message = $"Successfully deleted {deletedCount} note{(deletedCount != 1 ? "s" : "")}",
                    deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting notes");
                return StatusCode(500, new { message = "Error bulk deleting notes", error = ex.Message });
            }
        }

        // ============================================
        // Linking Operations
        // ============================================

        /// <summary>
        /// Links a note to a media item.
        /// POST /api/note/{id}/link
        /// </summary>
        [HttpPost("{id:guid}/link")]
        public async Task<IActionResult> LinkToMedia(Guid id, [FromBody] LinkNoteToMediaDto dto)
        {
            try
            {
                await _noteService.LinkToMediaItemAsync(id, dto.MediaItemId, dto.LinkDescription);
                return Ok(new { message = "Note linked to media item successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking note {NoteId} to media item {MediaItemId}", id, dto.MediaItemId);
                return StatusCode(500, new { message = "Error linking note to media item", error = ex.Message });
            }
        }

        /// <summary>
        /// Unlinks a note from a media item.
        /// DELETE /api/note/{noteId}/link/{mediaItemId}
        /// </summary>
        [HttpDelete("{noteId:guid}/link/{mediaItemId:guid}")]
        public async Task<IActionResult> UnlinkFromMedia(Guid noteId, Guid mediaItemId)
        {
            try
            {
                await _noteService.UnlinkFromMediaItemAsync(noteId, mediaItemId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking note {NoteId} from media item {MediaItemId}", noteId, mediaItemId);
                return StatusCode(500, new { message = "Error unlinking note from media item", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all media items linked to a note.
        /// GET /api/note/{id}/media
        /// </summary>
        [HttpGet("{id:guid}/media")]        public async Task<IActionResult> GetMediaForNote(Guid id)
        {
            try
            {
                var mediaItems = await _noteService.GetMediaItemsForNoteAsync(id);
                return Ok(mediaItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media items for note {NoteId}", id);
                return StatusCode(500, new { message = "Error retrieving media items", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all notes linked to a media item.
        /// GET /api/note/for-media/{mediaItemId}
        /// </summary>
        [HttpGet("for-media/{mediaItemId:guid}")]        public async Task<IActionResult> GetNotesForMedia(Guid mediaItemId)
        {
            try
            {
                var notes = await _noteService.GetNotesForMediaItemAsync(mediaItemId);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes for media item {MediaItemId}", mediaItemId);
                return StatusCode(500, new { message = "Error retrieving notes", error = ex.Message });
            }
        }

        // ============================================
        // Sync Operations
        // ============================================

        /// <summary>
        /// Manually triggers a sync for a specific vault.
        /// POST /api/note/sync/{vault}
        /// </summary>
        [HttpPost("sync/{vault}")]
        public async Task<IActionResult> SyncVault(string vault, [FromQuery] string? url = null, [FromQuery] string? authToken = null, [FromQuery] bool removeOrphans = false)
        {
            try
            {
                // Get vault URL from configuration if not provided
                var vaultUrl = url;
                if (string.IsNullOrEmpty(vaultUrl))
                {
                    vaultUrl = vault.ToLower() switch
                    {
                        "general" => Environment.GetEnvironmentVariable("OBSIDIAN_GENERAL_VAULT_URL")
                            ?? _configuration["ObsidianNoteSync:GeneralVaultUrl"],
                        "programming" => Environment.GetEnvironmentVariable("OBSIDIAN_PROGRAMMING_VAULT_URL")
                            ?? _configuration["ObsidianNoteSync:ProgrammingVaultUrl"],
                        _ => null
                    };
                }

                if (string.IsNullOrEmpty(vaultUrl))
                {
                    return BadRequest(new { message = $"Vault URL not configured for '{vault}'. Provide URL as query parameter or set environment variable." });
                }

                // Get auth token from configuration if not provided
                var token = authToken;
                if (string.IsNullOrEmpty(token))
                {
                    token = vault.ToLower() switch
                    {
                        "general" => Environment.GetEnvironmentVariable("OBSIDIAN_GENERAL_VAULT_AUTH_TOKEN")
                            ?? _configuration["ObsidianNoteSync:GeneralVaultAuthToken"],
                        "programming" => Environment.GetEnvironmentVariable("OBSIDIAN_PROGRAMMING_VAULT_AUTH_TOKEN")
                            ?? _configuration["ObsidianNoteSync:ProgrammingVaultAuthToken"],
                        _ => null
                    };
                }

                var result = await _noteService.SyncFromQuartzVaultAsync(vault, vaultUrl, token, removeOrphans);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Vault authentication failed while syncing vault {Vault}", vault);
                return StatusCode(502, new { message = "Vault authentication failed", error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to reach vault {Vault}", vault);
                return StatusCode(502, new { message = "Failed to reach the vault", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing vault {Vault}", vault);
                return StatusCode(500, new { message = "Error syncing vault", error = ex.Message });
            }
        }

        /// <summary>
        /// Manually triggers a sync for all configured vaults.
        /// POST /api/note/sync
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> SyncAll([FromQuery] bool removeOrphans = false)
        {
            try
            {
                var results = await _noteService.SyncAllVaultsAsync(removeOrphans);
                return Ok(new { results });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Vault authentication failed while syncing all vaults");
                return StatusCode(502, new { message = "Vault authentication failed", error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to reach a vault while syncing all vaults");
                return StatusCode(502, new { message = "Failed to reach a vault", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing all vaults");
                return StatusCode(500, new { message = "Error syncing vaults", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the current sync configuration status.
        /// GET /api/note/sync/status
        /// </summary>
        [HttpGet("sync/status")]
        public async Task<IActionResult> GetSyncStatus()
        {
            try
            {
                var status = await _noteService.GetSyncStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync status");
                return StatusCode(500, new { message = "Error retrieving sync status", error = ex.Message });
            }
        }

        // ============================================
        // Helper Methods
        // ============================================

        private static NoteResponseDto MapToResponseDto(Domain.Entities.Note note)
        {
            return new NoteResponseDto
            {
                Id = note.Id,
                Slug = note.Slug,
                Title = note.Title,
                Content = note.Content,
                Description = note.Description,
                VaultName = note.VaultName,
                SourceUrl = note.SourceUrl,
                Tags = note.Tags ?? new List<string>(),
                NoteDate = note.NoteDate,
                DateImported = note.DateImported,
                LastSyncedAt = note.LastSyncedAt,
                AiDescription = note.AiDescription,
                AiDescriptionGeneratedAt = note.AiDescriptionGeneratedAt,
                IsDescriptionManual = note.IsDescriptionManual,
                LinkedMediaItems = note.MediaItemNotes?.Select(min => new LinkedMediaItemDto
                {
                    Id = min.MediaItem.Id,
                    Title = min.MediaItem.Title,
                    MediaType = min.MediaItem.MediaType.ToString(),
                    Thumbnail = min.MediaItem.Thumbnail,
                    LinkedAt = min.LinkedAt,
                    LinkDescription = min.LinkDescription
                }).ToList() ?? new List<LinkedMediaItemDto>()
            };
        }
    }
}
