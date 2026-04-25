using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public class RelatedMediaResult
    {
        public bool MediaItemFound { get; set; }
        public IReadOnlyList<RelatedMediaResponseDto> Items { get; set; } = Array.Empty<RelatedMediaResponseDto>();
    }

    public class SaveRelatedMediaResult
    {
        public bool SourceFound { get; set; }
        public bool RelatedFound { get; set; }
        public bool AlreadyExists { get; set; }
        public bool SelfReference { get; set; }
        public RelatedMediaResponseDto? Saved { get; set; }
    }

    public class BatchSaveRelatedMediaResult
    {
        public bool SourceFound { get; set; }
        public List<object> Saved { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public interface IRelatedMediaService
    {
        Task<RelatedMediaResult> GetRelatedMediaAsync(Guid mediaItemId, bool includeBidirectional);

        Task<SaveRelatedMediaResult> SaveRelatedMediaAsync(Guid sourceMediaItemId, SaveRelatedMediaDto dto);

        // Returns true when a relation row was deleted, false when none existed.
        Task<bool> RemoveRelatedMediaAsync(Guid sourceMediaItemId, Guid relatedMediaItemId);

        Task<BatchSaveRelatedMediaResult> SaveRelatedMediaBatchAsync(Guid sourceMediaItemId, IReadOnlyList<SaveRelatedMediaDto> dtos);
    }
}
