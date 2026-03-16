using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class RelatedMediaController : ControllerBase
    {
        private readonly MediaLibraryDbContext _context;
        private readonly ILogger<RelatedMediaController> _logger;

        public RelatedMediaController(
            MediaLibraryDbContext context,
            ILogger<RelatedMediaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all saved related media items for a specific media item.
        /// GET /api/relatedmedia/{mediaItemId}
        /// </summary>
        /// <param name="mediaItemId">The ID of the media item to get related items for.</param>
        /// <param name="includeBidirectional">If true, includes items that link TO this item (default true).</param>
        [HttpGet("{mediaItemId:guid}")]
        public async Task<ActionResult<IEnumerable<RelatedMediaResponseDto>>> GetRelatedMedia(
            Guid mediaItemId,
            [FromQuery] bool includeBidirectional = true)
        {
            try
            {
                // Check if media item exists
                var mediaItemExists = await _context.MediaItems
                    .AsNoTracking()
                    .AnyAsync(m => m.Id == mediaItemId);

                if (!mediaItemExists)
                {
                    return NotFound($"Media item with ID {mediaItemId} not found.");
                }

                // Get items this media relates TO (outgoing relationships)
                var relatedTo = await _context.MediaItemRelations
                    .AsNoTracking()
                    .Where(r => r.SourceMediaItemId == mediaItemId)
                    .Select(r => new RelatedMediaResponseDto
                    {
                        SourceMediaItemId = r.SourceMediaItemId,
                        RelatedMediaItemId = r.RelatedMediaItemId,
                        CreatedAt = r.CreatedAt,
                        Source = r.Source.ToString(),
                        SimilarityScore = r.SimilarityScore,
                        Note = r.Note,
                        RelatedMediaItem = new RelatedMediaItemSummaryDto
                        {
                            Id = r.RelatedMediaItem.Id,
                            Title = r.RelatedMediaItem.Title,
                            MediaType = r.RelatedMediaItem.MediaType.ToString(),
                            Description = r.RelatedMediaItem.Description,
                            Thumbnail = r.RelatedMediaItem.Thumbnail,
                            Status = r.RelatedMediaItem.Status.ToString(),
                            Rating = r.RelatedMediaItem.Rating != null ? r.RelatedMediaItem.Rating.ToString() : null
                        }
                    })
                    .ToListAsync();

                if (includeBidirectional)
                {
                    // Also get items that relate TO this media (incoming relationships)
                    var relatedFrom = await _context.MediaItemRelations
                        .AsNoTracking()
                        .Where(r => r.RelatedMediaItemId == mediaItemId)
                        .Select(r => new RelatedMediaResponseDto
                        {
                            SourceMediaItemId = r.SourceMediaItemId,
                            RelatedMediaItemId = r.RelatedMediaItemId,
                            CreatedAt = r.CreatedAt,
                            Source = r.Source.ToString(),
                            SimilarityScore = r.SimilarityScore,
                            Note = r.Note,
                            RelatedMediaItem = new RelatedMediaItemSummaryDto
                            {
                                Id = r.SourceMediaItem.Id,
                                Title = r.SourceMediaItem.Title,
                                MediaType = r.SourceMediaItem.MediaType.ToString(),
                                Description = r.SourceMediaItem.Description,
                                Thumbnail = r.SourceMediaItem.Thumbnail,
                                Status = r.SourceMediaItem.Status.ToString(),
                                Rating = r.SourceMediaItem.Rating != null ? r.SourceMediaItem.Rating.ToString() : null
                            }
                        })
                        .ToListAsync();

                    // Combine and deduplicate (avoid showing same item twice if linked bidirectionally)
                    var existingIds = relatedTo.Select(r => r.RelatedMediaItem?.Id).ToHashSet();
                    var uniqueRelatedFrom = relatedFrom.Where(r => !existingIds.Contains(r.RelatedMediaItem?.Id));
                    relatedTo.AddRange(uniqueRelatedFrom);
                }

                return Ok(relatedTo.OrderByDescending(r => r.CreatedAt));
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
                if (sourceMediaItemId == dto.RelatedMediaItemId)
                {
                    return BadRequest("A media item cannot be related to itself.");
                }

                // Check if source media item exists
                var sourceExists = await _context.MediaItems
                    .AsNoTracking()
                    .AnyAsync(m => m.Id == sourceMediaItemId);

                if (!sourceExists)
                {
                    return NotFound($"Source media item with ID {sourceMediaItemId} not found.");
                }

                // Check if related media item exists and get its details
                var relatedItem = await _context.MediaItems
                    .AsNoTracking()
                    .Where(m => m.Id == dto.RelatedMediaItemId)
                    .Select(m => new RelatedMediaItemSummaryDto
                    {
                        Id = m.Id,
                        Title = m.Title,
                        MediaType = m.MediaType.ToString(),
                        Description = m.Description,
                        Thumbnail = m.Thumbnail,
                        Status = m.Status.ToString(),
                        Rating = m.Rating != null ? m.Rating.ToString() : null
                    })
                    .FirstOrDefaultAsync();

                if (relatedItem == null)
                {
                    return NotFound($"Related media item with ID {dto.RelatedMediaItemId} not found.");
                }

                // Check if relationship already exists
                var existingRelation = await _context.MediaItemRelations
                    .AsNoTracking()
                    .AnyAsync(r => r.SourceMediaItemId == sourceMediaItemId
                                && r.RelatedMediaItemId == dto.RelatedMediaItemId);

                if (existingRelation)
                {
                    return BadRequest("This relationship already exists.");
                }

                // Parse the source enum
                if (!Enum.TryParse<RelationSource>(dto.Source, true, out var source))
                {
                    source = RelationSource.ManuallyAdded;
                }

                // Create the relationship
                var relation = new MediaItemRelation
                {
                    SourceMediaItemId = sourceMediaItemId,
                    RelatedMediaItemId = dto.RelatedMediaItemId,
                    Source = source,
                    SimilarityScore = dto.SimilarityScore,
                    Note = dto.Note,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MediaItemRelations.Add(relation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved related media: {SourceId} -> {RelatedId} ({Source})",
                    sourceMediaItemId, dto.RelatedMediaItemId, source);

                return CreatedAtAction(nameof(GetRelatedMedia), new { mediaItemId = sourceMediaItemId },
                    new RelatedMediaResponseDto
                    {
                        SourceMediaItemId = sourceMediaItemId,
                        RelatedMediaItemId = dto.RelatedMediaItemId,
                        CreatedAt = relation.CreatedAt,
                        Source = relation.Source.ToString(),
                        SimilarityScore = relation.SimilarityScore,
                        Note = relation.Note,
                        RelatedMediaItem = relatedItem
                    });
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
                var relation = await _context.MediaItemRelations
                    .FirstOrDefaultAsync(r => r.SourceMediaItemId == sourceMediaItemId
                                           && r.RelatedMediaItemId == relatedMediaItemId);

                if (relation == null)
                {
                    return NotFound($"Relationship between {sourceMediaItemId} and {relatedMediaItemId} not found.");
                }

                _context.MediaItemRelations.Remove(relation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Removed related media: {SourceId} -> {RelatedId}",
                    sourceMediaItemId, relatedMediaItemId);

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

                var sourceExists = await _context.MediaItems
                    .AsNoTracking()
                    .AnyAsync(m => m.Id == sourceMediaItemId);

                if (!sourceExists)
                {
                    return NotFound($"Source media item with ID {sourceMediaItemId} not found.");
                }

                var results = new List<object>();
                var errors = new List<string>();

                foreach (var dto in dtos)
                {
                    try
                    {
                        if (sourceMediaItemId == dto.RelatedMediaItemId)
                        {
                            errors.Add($"Skipped self-reference for {dto.RelatedMediaItemId}");
                            continue;
                        }

                        var relatedExists = await _context.MediaItems
                            .AsNoTracking()
                            .AnyAsync(m => m.Id == dto.RelatedMediaItemId);

                        if (!relatedExists)
                        {
                            errors.Add($"Related media item {dto.RelatedMediaItemId} not found");
                            continue;
                        }

                        var existingRelation = await _context.MediaItemRelations
                            .AsNoTracking()
                            .AnyAsync(r => r.SourceMediaItemId == sourceMediaItemId
                                        && r.RelatedMediaItemId == dto.RelatedMediaItemId);

                        if (existingRelation)
                        {
                            errors.Add($"Relationship with {dto.RelatedMediaItemId} already exists");
                            continue;
                        }

                        if (!Enum.TryParse<RelationSource>(dto.Source, true, out var source))
                        {
                            source = RelationSource.ManuallyAdded;
                        }

                        var relation = new MediaItemRelation
                        {
                            SourceMediaItemId = sourceMediaItemId,
                            RelatedMediaItemId = dto.RelatedMediaItemId,
                            Source = source,
                            SimilarityScore = dto.SimilarityScore,
                            Note = dto.Note,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.MediaItemRelations.Add(relation);
                        results.Add(new { relatedMediaItemId = dto.RelatedMediaItemId, status = "saved" });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error saving {dto.RelatedMediaItemId}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Batch saved {Count} related media items for {SourceMediaItemId}",
                    results.Count, sourceMediaItemId);

                return Ok(new
                {
                    savedCount = results.Count,
                    errorCount = errors.Count,
                    saved = results,
                    errors = errors
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
