using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    public class RelatedMediaService : IRelatedMediaService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<RelatedMediaService> _logger;

        public RelatedMediaService(IApplicationDbContext context, ILogger<RelatedMediaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RelatedMediaResult> GetRelatedMediaAsync(Guid mediaItemId, bool includeBidirectional)
        {
            var mediaItemExists = await _context.MediaItems
                .AsNoTracking()
                .AnyAsync(m => m.Id == mediaItemId);

            if (!mediaItemExists)
            {
                return new RelatedMediaResult { MediaItemFound = false };
            }

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

                var existingIds = relatedTo.Select(r => r.RelatedMediaItem?.Id).ToHashSet();
                relatedTo.AddRange(relatedFrom.Where(r => !existingIds.Contains(r.RelatedMediaItem?.Id)));
            }

            return new RelatedMediaResult
            {
                MediaItemFound = true,
                Items = relatedTo.OrderByDescending(r => r.CreatedAt).ToList()
            };
        }

        public async Task<SaveRelatedMediaResult> SaveRelatedMediaAsync(Guid sourceMediaItemId, SaveRelatedMediaDto dto)
        {
            if (sourceMediaItemId == dto.RelatedMediaItemId)
            {
                return new SaveRelatedMediaResult { SelfReference = true };
            }

            var sourceExists = await _context.MediaItems
                .AsNoTracking()
                .AnyAsync(m => m.Id == sourceMediaItemId);

            if (!sourceExists)
            {
                return new SaveRelatedMediaResult { SourceFound = false };
            }

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
                return new SaveRelatedMediaResult { SourceFound = true, RelatedFound = false };
            }

            var existingRelation = await _context.MediaItemRelations
                .AsNoTracking()
                .AnyAsync(r => r.SourceMediaItemId == sourceMediaItemId
                            && r.RelatedMediaItemId == dto.RelatedMediaItemId);

            if (existingRelation)
            {
                return new SaveRelatedMediaResult { SourceFound = true, RelatedFound = true, AlreadyExists = true };
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

            _context.Add(relation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved related media: {SourceId} -> {RelatedId} ({Source})",
                sourceMediaItemId, dto.RelatedMediaItemId, source);

            return new SaveRelatedMediaResult
            {
                SourceFound = true,
                RelatedFound = true,
                Saved = new RelatedMediaResponseDto
                {
                    SourceMediaItemId = sourceMediaItemId,
                    RelatedMediaItemId = dto.RelatedMediaItemId,
                    CreatedAt = relation.CreatedAt,
                    Source = relation.Source.ToString(),
                    SimilarityScore = relation.SimilarityScore,
                    Note = relation.Note,
                    RelatedMediaItem = relatedItem
                }
            };
        }

        public async Task<bool> RemoveRelatedMediaAsync(Guid sourceMediaItemId, Guid relatedMediaItemId)
        {
            var relation = await _context.MediaItemRelations
                .FirstOrDefaultAsync(r => r.SourceMediaItemId == sourceMediaItemId
                                       && r.RelatedMediaItemId == relatedMediaItemId);

            if (relation == null)
            {
                return false;
            }

            _context.Remove(relation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed related media: {SourceId} -> {RelatedId}",
                sourceMediaItemId, relatedMediaItemId);
            return true;
        }

        public async Task<BatchSaveRelatedMediaResult> SaveRelatedMediaBatchAsync(Guid sourceMediaItemId, IReadOnlyList<SaveRelatedMediaDto> dtos)
        {
            var result = new BatchSaveRelatedMediaResult();

            var sourceExists = await _context.MediaItems
                .AsNoTracking()
                .AnyAsync(m => m.Id == sourceMediaItemId);

            if (!sourceExists)
            {
                return result;
            }

            result.SourceFound = true;

            foreach (var dto in dtos)
            {
                try
                {
                    if (sourceMediaItemId == dto.RelatedMediaItemId)
                    {
                        result.Errors.Add($"Skipped self-reference for {dto.RelatedMediaItemId}");
                        continue;
                    }

                    var relatedExists = await _context.MediaItems
                        .AsNoTracking()
                        .AnyAsync(m => m.Id == dto.RelatedMediaItemId);

                    if (!relatedExists)
                    {
                        result.Errors.Add($"Related media item {dto.RelatedMediaItemId} not found");
                        continue;
                    }

                    var existingRelation = await _context.MediaItemRelations
                        .AsNoTracking()
                        .AnyAsync(r => r.SourceMediaItemId == sourceMediaItemId
                                    && r.RelatedMediaItemId == dto.RelatedMediaItemId);

                    if (existingRelation)
                    {
                        result.Errors.Add($"Relationship with {dto.RelatedMediaItemId} already exists");
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

                    _context.Add(relation);
                    result.Saved.Add(new { relatedMediaItemId = dto.RelatedMediaItemId, status = "saved" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving related media batch entry {RelatedId}", dto.RelatedMediaItemId);
                    result.Errors.Add($"Error saving {dto.RelatedMediaItemId}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Batch saved {Count} related media items for {SourceMediaItemId}",
                result.Saved.Count, sourceMediaItemId);

            return result;
        }
    }
}
