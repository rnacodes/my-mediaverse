using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Infrastructure.Data;
using ProjectLoopbreaker.DTOs;
using ProjectLoopbreaker.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace ProjectLoopbreaker.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Allow anonymous access to all mixlist endpoints
    public class MixlistController : ControllerBase
    {
        private readonly MediaLibraryDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITypeSenseService _typeSenseService;
        private readonly ILogger<MixlistController> _logger;

        public MixlistController(
            MediaLibraryDbContext context, 
            IHttpClientFactory httpClientFactory,
            ITypeSenseService typeSenseService,
            ILogger<MixlistController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _typeSenseService = typeSenseService;
            _logger = logger;
        }

        // GET: api/mixlist
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MixlistResponseDto>>> GetAllMixlists()
        {
            try
            {
                // Use AsNoTracking and AsSplitQuery to avoid circular reference issues
                var mixlists = await _context.Mixlists
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Select(m => new MixlistResponseDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Description = m.Description,
                        DateCreated = m.DateCreated,
                        Thumbnail = m.Thumbnail,
                        MediaItemIds = m.MediaItems.Select(mi => mi.Id).ToArray(),
                        MediaItems = m.MediaItems.Select(mi => new MediaItemSummary
                        {
                            Id = mi.Id,
                            Title = mi.Title,
                            Description = mi.Description,
                            MediaType = mi.MediaType,
                            Thumbnail = mi.Thumbnail
                        }).ToArray()
                    })
                    .ToListAsync();

                return Ok(mixlists);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        // GET: api/mixlist/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MixlistResponseDto>> GetMixlist(Guid id)
        {
            try
            {
                // Use AsNoTracking and projection to avoid circular reference issues
                var mixlist = await _context.Mixlists
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(m => m.Id == id)
                    .Select(m => new MixlistResponseDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Description = m.Description,
                        DateCreated = m.DateCreated,
                        Thumbnail = m.Thumbnail,
                        MediaItemIds = m.MediaItems.Select(mi => mi.Id).ToArray(),
                        MediaItems = m.MediaItems.Select(mi => new MediaItemSummary
                        {
                            Id = mi.Id,
                            Title = mi.Title,
                            Description = mi.Description,
                            MediaType = mi.MediaType,
                            Thumbnail = mi.Thumbnail
                        }).ToArray(),
                        LinkedNotes = m.MixlistNotes.Select(mn => new LinkedNoteDto
                        {
                            Id = mn.Note.Id,
                            Slug = mn.Note.Slug,
                            Title = mn.Note.Title,
                            Description = mn.Note.Description,
                            VaultName = mn.Note.VaultName,
                            SourceUrl = mn.Note.SourceUrl,
                            Tags = mn.Note.Tags,
                            LinkedAt = mn.LinkedAt,
                            LinkDescription = mn.LinkDescription
                        }).ToArray()
                    })
                    .FirstOrDefaultAsync();

                if (mixlist == null)
                {
                    return NotFound($"Mixlist with ID {id} not found.");
                }

                return Ok(mixlist);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        // GET: api/mixlist/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<MixlistResponseDto>>> SearchMixlists([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query is required.");
                }

                var searchQuery = query.ToLower();
                
                // Use AsNoTracking and projection to avoid circular reference issues
                var mixlists = await _context.Mixlists
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(m => 
                        m.Name.ToLower().Contains(searchQuery) ||
                        (m.Description != null && m.Description.ToLower().Contains(searchQuery))
                    )
                    .Select(m => new MixlistResponseDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Description = m.Description,
                        DateCreated = m.DateCreated,
                        Thumbnail = m.Thumbnail,
                        MediaItemIds = m.MediaItems.Select(mi => mi.Id).ToArray(),
                        MediaItems = m.MediaItems.Select(mi => new MediaItemSummary
                        {
                            Id = mi.Id,
                            Title = mi.Title,
                            Description = mi.Description,
                            MediaType = mi.MediaType,
                            Thumbnail = mi.Thumbnail
                        }).ToArray()
                    })
                    .ToListAsync();

                return Ok(mixlists);
            }
            catch (Exception ex)
            {
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

            var mixlist = new Mixlist
            {
                Name = dto.Name,
                Description = dto.Description,
                Thumbnail = dto.Thumbnail,
                DateCreated = DateTime.UtcNow
            };

            _context.Mixlists.Add(mixlist);
            await _context.SaveChangesAsync();

            // Index in Typesense
            try
            {
                await _typeSenseService.IndexMixlistAsync(
                    mixlist.Id,
                    mixlist.Name,
                    mixlist.Description,
                    mixlist.Thumbnail,
                    mixlist.DateCreated,
                    new List<string>(), // Empty initially
                    new List<string>(), // Empty initially
                    new List<string>()  // Empty initially
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index new mixlist {MixlistId} in Typesense", mixlist.Id);
                // Don't fail the request if Typesense indexing fails
            }

            var response = new MixlistResponseDto
            {
                Id = mixlist.Id,
                Name = mixlist.Name,
                Description = mixlist.Description,
                DateCreated = mixlist.DateCreated,
                Thumbnail = mixlist.Thumbnail,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItems = Array.Empty<MediaItemSummary>()
            };

            return CreatedAtAction(nameof(GetMixlist), new { id = mixlist.Id }, response);
        }

        // POST: api/mixlist/{mixlistId}/items/{mediaItemId}
        [HttpPost("{mixlistId:guid}/items/{mediaItemId:guid}")]
        public async Task<IActionResult> AddMediaItemToMixlist(Guid mixlistId, Guid mediaItemId)
        {
            try
            {
                // Lightweight check: only fetch the fields we need
                var mixlistInfo = await _context.Mixlists
                    .AsNoTracking()
                    .Where(m => m.Id == mixlistId)
                    .Select(m => new { m.Id, m.Name, m.Description, m.Thumbnail, m.DateCreated })
                    .FirstOrDefaultAsync();

                if (mixlistInfo == null)
                {
                    return NotFound($"Mixlist with ID {mixlistId} not found.");
                }

                // Lightweight check: only fetch title to verify existence
                var mediaItemTitle = await _context.MediaItems
                    .AsNoTracking()
                    .Where(m => m.Id == mediaItemId)
                    .Select(m => m.Title)
                    .FirstOrDefaultAsync();

                if (mediaItemTitle == null)
                {
                    return NotFound($"Media item with ID {mediaItemId} not found.");
                }

                // Check if already in mixlist - query only the IDs we need
                var alreadyInMixlist = await _context.Mixlists
                    .AsNoTracking()
                    .Where(m => m.Id == mixlistId)
                    .SelectMany(m => m.MediaItems.Select(mi => mi.Id))
                    .AnyAsync(id => id == mediaItemId);

                if (alreadyInMixlist)
                {
                    return BadRequest($"Media item with ID {mediaItemId} is already in the mixlist.");
                }

                // Add the relationship by loading minimal entities for EF tracking
                var mixlist = await _context.Mixlists.FindAsync(mixlistId);
                var mediaItem = await _context.MediaItems.FindAsync(mediaItemId);

                mixlist!.MediaItems.Add(mediaItem!);
                await _context.SaveChangesAsync();

                // Re-index in Typesense with lightweight queries for the data we need
                try
                {
                    // Get just the titles of media items in this mixlist
                    var mediaItemTitles = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == mixlistId)
                        .SelectMany(m => m.MediaItems.Select(mi => mi.Title))
                        .ToListAsync();

                    // Get distinct topics from media items in this mixlist
                    var topics = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == mixlistId)
                        .SelectMany(m => m.MediaItems.SelectMany(mi => mi.Topics.Select(t => t.Name)))
                        .Distinct()
                        .ToListAsync();

                    // Get distinct genres from media items in this mixlist
                    var genres = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == mixlistId)
                        .SelectMany(m => m.MediaItems.SelectMany(mi => mi.Genres.Select(g => g.Name)))
                        .Distinct()
                        .ToListAsync();

                    await _typeSenseService.IndexMixlistAsync(
                        mixlistInfo.Id,
                        mixlistInfo.Name,
                        mixlistInfo.Description,
                        mixlistInfo.Thumbnail,
                        mixlistInfo.DateCreated,
                        mediaItemTitles,
                        topics,
                        genres
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-index mixlist {MixlistId} in Typesense after adding media item", mixlistId);
                    // Don't fail the request if Typesense indexing fails
                }

                return Ok(new { message = $"Media item '{mediaItemTitle}' added to mixlist '{mixlistInfo.Name}'" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to add media item to mixlist", details = ex.Message });
            }
        }

        // DELETE: api/mixlist/{mixlistId}/items/{mediaItemId}
        [HttpDelete("{mixlistId:guid}/items/{mediaItemId:guid}")]
        public async Task<IActionResult> RemoveMediaItemFromMixlist(Guid mixlistId, Guid mediaItemId)
        {
            try
            {
                // Lightweight check: only fetch the fields we need
                var mixlistInfo = await _context.Mixlists
                    .AsNoTracking()
                    .Where(m => m.Id == mixlistId)
                    .Select(m => new { m.Id, m.Name, m.Description, m.Thumbnail, m.DateCreated })
                    .FirstOrDefaultAsync();

                if (mixlistInfo == null)
                {
                    return NotFound($"Mixlist with ID {mixlistId} not found.");
                }

                // Check if media item is in the mixlist
                var isInMixlist = await _context.Mixlists
                    .AsNoTracking()
                    .Where(m => m.Id == mixlistId)
                    .SelectMany(m => m.MediaItems.Select(mi => mi.Id))
                    .AnyAsync(id => id == mediaItemId);

                if (!isInMixlist)
                {
                    return NotFound($"Media item with ID {mediaItemId} not found in the mixlist.");
                }

                // Load minimal entities for EF tracking to perform the removal
                var mixlist = await _context.Mixlists
                    .Include(m => m.MediaItems.Where(mi => mi.Id == mediaItemId))
                    .FirstAsync(m => m.Id == mixlistId);

                var mediaItem = mixlist.MediaItems.First();
                mixlist.MediaItems.Remove(mediaItem);
                await _context.SaveChangesAsync();

                // Re-index in Typesense with lightweight queries
                try
                {
                    var mediaItemTitles = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == mixlistId)
                        .SelectMany(m => m.MediaItems.Select(mi => mi.Title))
                        .ToListAsync();

                    var topics = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == mixlistId)
                        .SelectMany(m => m.MediaItems.SelectMany(mi => mi.Topics.Select(t => t.Name)))
                        .Distinct()
                        .ToListAsync();

                    var genres = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == mixlistId)
                        .SelectMany(m => m.MediaItems.SelectMany(mi => mi.Genres.Select(g => g.Name)))
                        .Distinct()
                        .ToListAsync();

                    await _typeSenseService.IndexMixlistAsync(
                        mixlistInfo.Id,
                        mixlistInfo.Name,
                        mixlistInfo.Description,
                        mixlistInfo.Thumbnail,
                        mixlistInfo.DateCreated,
                        mediaItemTitles,
                        topics,
                        genres
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-index mixlist {MixlistId} in Typesense after removing media item", mixlistId);
                    // Don't fail the request if Typesense indexing fails
                }

                return Ok(new { message = $"Media item removed from mixlist '{mixlistInfo.Name}'" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to remove media item from mixlist", details = ex.Message });
            }
        }

        // PUT: api/mixlist/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateMixlist(Guid id, [FromBody] UpdateMixlistDto dto)
        {
            try
            {
                // Load only the mixlist entity without related data
                var mixlist = await _context.Mixlists.FindAsync(id);

                if (mixlist == null)
                {
                    return NotFound($"Mixlist with ID {id} not found.");
                }

                // Update properties
                if (!string.IsNullOrWhiteSpace(dto.Name))
                    mixlist.Name = dto.Name;

                if (dto.Description != null)
                    mixlist.Description = dto.Description;

                if (dto.Thumbnail != null)
                    mixlist.Thumbnail = dto.Thumbnail;

                await _context.SaveChangesAsync();

                // Re-index in Typesense with lightweight queries
                try
                {
                    var mediaItemTitles = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == id)
                        .SelectMany(m => m.MediaItems.Select(mi => mi.Title))
                        .ToListAsync();

                    var topics = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == id)
                        .SelectMany(m => m.MediaItems.SelectMany(mi => mi.Topics.Select(t => t.Name)))
                        .Distinct()
                        .ToListAsync();

                    var genres = await _context.Mixlists
                        .AsNoTracking()
                        .Where(m => m.Id == id)
                        .SelectMany(m => m.MediaItems.SelectMany(mi => mi.Genres.Select(g => g.Name)))
                        .Distinct()
                        .ToListAsync();

                    await _typeSenseService.IndexMixlistAsync(
                        mixlist.Id,
                        mixlist.Name,
                        mixlist.Description,
                        mixlist.Thumbnail,
                        mixlist.DateCreated,
                        mediaItemTitles,
                        topics,
                        genres
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-index mixlist {MixlistId} in Typesense after update", id);
                    // Don't fail the request if Typesense indexing fails
                }

                return Ok(new MixlistResponseDto
                {
                    Id = mixlist.Id,
                    Name = mixlist.Name,
                    Description = mixlist.Description,
                    DateCreated = mixlist.DateCreated,
                    Thumbnail = mixlist.Thumbnail,
                    MediaItemIds = Array.Empty<Guid>(),
                    MediaItems = Array.Empty<MediaItemSummary>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update mixlist", details = ex.Message });
            }
        }

        // DELETE: api/mixlist/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteMixlist(Guid id)
        {
            try
            {
                var mixlist = await _context.Mixlists
                    .Include(m => m.MediaItems)
                    .Include(m => m.MixlistNotes)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mixlist == null)
                {
                    return NotFound($"Mixlist with ID {id} not found.");
                }

                // Delete thumbnail from S3 if it exists
                if (!string.IsNullOrEmpty(mixlist.Thumbnail))
                {
                    await DeleteThumbnailFromS3(mixlist.Thumbnail);
                }

                // Clear media items and notes (just removes associations, doesn't delete them)
                mixlist.MediaItems.Clear();
                mixlist.MixlistNotes.Clear();

                _context.Mixlists.Remove(mixlist);
                await _context.SaveChangesAsync();

                // Delete from Typesense
                try
                {
                    await _typeSenseService.DeleteMixlistAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete mixlist {MixlistId} from Typesense", id);
                    // Don't fail the request if Typesense deletion fails
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete mixlist", details = ex.Message });
            }
        }

        // POST: api/mixlist/{mixlistId}/notes
        [HttpPost("{mixlistId:guid}/notes")]
        public async Task<IActionResult> LinkNoteToMixlist(Guid mixlistId, [FromBody] LinkNoteToMixlistDto dto)
        {
            try
            {
                var mixlistExists = await _context.Mixlists
                    .AsNoTracking()
                    .AnyAsync(m => m.Id == mixlistId);

                if (!mixlistExists)
                {
                    return NotFound($"Mixlist with ID {mixlistId} not found.");
                }

                var noteExists = await _context.Notes
                    .AsNoTracking()
                    .AnyAsync(n => n.Id == dto.NoteId);

                if (!noteExists)
                {
                    return NotFound($"Note with ID {dto.NoteId} not found.");
                }

                var alreadyLinked = await _context.MixlistNotes
                    .AsNoTracking()
                    .AnyAsync(mn => mn.MixlistId == mixlistId && mn.NoteId == dto.NoteId);

                if (alreadyLinked)
                {
                    return BadRequest($"Note is already linked to this mixlist.");
                }

                var mixlistNote = new MixlistNote
                {
                    MixlistId = mixlistId,
                    NoteId = dto.NoteId,
                    LinkedAt = DateTime.UtcNow,
                    LinkDescription = dto.LinkDescription
                };

                _context.MixlistNotes.Add(mixlistNote);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Note linked to mixlist successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to link note to mixlist", details = ex.Message });
            }
        }

        // DELETE: api/mixlist/{mixlistId}/notes/{noteId}
        [HttpDelete("{mixlistId:guid}/notes/{noteId:guid}")]
        public async Task<IActionResult> UnlinkNoteFromMixlist(Guid mixlistId, Guid noteId)
        {
            try
            {
                var mixlistNote = await _context.MixlistNotes
                    .FirstOrDefaultAsync(mn => mn.MixlistId == mixlistId && mn.NoteId == noteId);

                if (mixlistNote == null)
                {
                    return NotFound($"Note link not found for this mixlist.");
                }

                _context.MixlistNotes.Remove(mixlistNote);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Note unlinked from mixlist successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to unlink note from mixlist", details = ex.Message });
            }
        }

        // GET: api/mixlist/{mixlistId}/notes
        [HttpGet("{mixlistId:guid}/notes")]
        public async Task<ActionResult<IEnumerable<LinkedNoteDto>>> GetNotesForMixlist(Guid mixlistId)
        {
            try
            {
                var mixlistExists = await _context.Mixlists
                    .AsNoTracking()
                    .AnyAsync(m => m.Id == mixlistId);

                if (!mixlistExists)
                {
                    return NotFound($"Mixlist with ID {mixlistId} not found.");
                }

                var notes = await _context.MixlistNotes
                    .AsNoTracking()
                    .Where(mn => mn.MixlistId == mixlistId)
                    .OrderByDescending(mn => mn.LinkedAt)
                    .Select(mn => new LinkedNoteDto
                    {
                        Id = mn.Note.Id,
                        Slug = mn.Note.Slug,
                        Title = mn.Note.Title,
                        Description = mn.Note.Description,
                        VaultName = mn.Note.VaultName,
                        SourceUrl = mn.Note.SourceUrl,
                        Tags = mn.Note.Tags,
                        LinkedAt = mn.LinkedAt,
                        LinkDescription = mn.LinkDescription
                    })
                    .ToListAsync();

                return Ok(notes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get notes for mixlist", details = ex.Message });
            }
        }

        private async Task DeleteThumbnailFromS3(string thumbnailUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(thumbnailUrl))
                    return;

                // Call the UploadController's delete endpoint
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(Request.Scheme + "://" + Request.Host);
                
                var response = await httpClient.DeleteAsync($"/api/upload/thumbnail?url={Uri.EscapeDataString(thumbnailUrl)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to delete thumbnail: {thumbnailUrl}. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the entire operation if thumbnail deletion fails
                Console.WriteLine($"Error deleting thumbnail {thumbnailUrl}: {ex.Message}");
            }
        }

        // POST: api/mixlist/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportMixlists([FromBody] List<ImportMixlistDto> importDtos)
        {
            try
            {
                if (importDtos == null || !importDtos.Any())
                {
                    return BadRequest("No mixlist data provided.");
                }

                var importedMixlists = new List<object>();
                var errors = new List<string>();

                foreach (var dto in importDtos)
                {
                    try
                    {
                        // Parse media item IDs from semicolon-separated string
                        var mediaItemIds = !string.IsNullOrEmpty(dto.MediaItemIds) 
                            ? dto.MediaItemIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                .Where(id => Guid.TryParse(id.Trim(), out _))
                                .Select(id => Guid.Parse(id.Trim()))
                                .ToArray()
                            : Array.Empty<Guid>();

                        // Create the mixlist
                        var mixlist = new Mixlist
                        {
                            Name = dto.Name,
                            Description = dto.Description,
                            Thumbnail = dto.Thumbnail,
                            DateCreated = DateTime.UtcNow
                        };

                        _context.Mixlists.Add(mixlist);
                        await _context.SaveChangesAsync();

                        // Add media items to the mixlist if IDs were provided
                        if (mediaItemIds.Any())
                        {
                            var mediaItems = await _context.MediaItems
                                .Where(m => mediaItemIds.Contains(m.Id))
                                .ToListAsync();

                            foreach (var mediaItem in mediaItems)
                            {
                                mixlist.MediaItems.Add(mediaItem);
                            }
                            
                            await _context.SaveChangesAsync();
                        }

                        importedMixlists.Add(new
                        {
                            Id = mixlist.Id,
                            Name = mixlist.Name,
                            MediaItemCount = mixlist.MediaItems.Count,
                            Message = "Mixlist imported successfully"
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to import mixlist '{dto.Name}': {ex.Message}");
                    }
                }

                var successCount = importedMixlists.Count;
                var errorCount = errors.Count;

                return Ok(new
                {
                    SuccessCount = successCount,
                    ErrorCount = errorCount,
                    ImportedMixlists = importedMixlists,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to import mixlists", details = ex.Message });
            }
        }

        // GET: api/mixlist/{id}/export
        [HttpGet("{id:guid}/export")]
        public async Task<IActionResult> ExportMixlist(Guid id)
        {
            try
            {
                var mixlist = await _context.Mixlists
                    .Include(m => m.MediaItems)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mixlist == null)
                {
                    return NotFound($"Mixlist with ID {id} not found.");
                }

                var csvData = new List<object>
                {
                    new
                    {
                        Id = mixlist.Id,
                        Name = mixlist.Name,
                        Description = mixlist.Description ?? "",
                        DateCreated = mixlist.DateCreated.ToString("yyyy-MM-dd"),
                        Thumbnail = mixlist.Thumbnail ?? "",
                        MediaItemIds = string.Join(";", mixlist.MediaItems.Select(mi => mi.Id)),
                        MediaItemTitles = string.Join(";", mixlist.MediaItems.Select(mi => mi.Title)),
                        MediaItemTypes = string.Join(";", mixlist.MediaItems.Select(mi => mi.MediaType.ToString()))
                    }
                };

                using var writer = new StringWriter();
                using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
                
                csv.WriteRecords(csvData);
                
                var csvContent = writer.ToString();
                var fileName = $"mixlist-{mixlist.Name.Replace(" ", "-")}-{DateTime.Now:yyyyMMdd}.csv";
                
                return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export mixlist", details = ex.Message });
            }
        }

        // GET: api/mixlist/export
        [HttpGet("export")]
        public async Task<IActionResult> ExportAllMixlists()
        {
            try
            {
                var mixlists = await _context.Mixlists
                    .Include(m => m.MediaItems)
                    .ToListAsync();

                var csvData = mixlists.Select(mixlist => new
                {
                    Id = mixlist.Id,
                    Name = mixlist.Name,
                    Description = mixlist.Description ?? "",
                    DateCreated = mixlist.DateCreated.ToString("yyyy-MM-dd"),
                    Thumbnail = mixlist.Thumbnail ?? "",
                    MediaItemIds = string.Join(";", mixlist.MediaItems.Select(mi => mi.Id)),
                    MediaItemTitles = string.Join(";", mixlist.MediaItems.Select(mi => mi.Title)),
                    MediaItemTypes = string.Join(";", mixlist.MediaItems.Select(mi => mi.MediaType.ToString()))
                }).ToList();

                using var writer = new StringWriter();
                using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
                
                csv.WriteRecords(csvData);
                
                var csvContent = writer.ToString();
                var fileName = $"all-mixlists-{DateTime.Now:yyyyMMdd}.csv";
                
                return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export mixlists", details = ex.Message });
            }
        }
    }


}
