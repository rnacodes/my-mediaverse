namespace MyMediaVerse.DTOs
{
    public class DeduplicationResultDto
    {
        public bool Success { get; set; }
        public int MergedCount { get; set; }
        public int GroupCount { get; set; }
        public List<MergeGroupDto> MergedGroups { get; set; } = new List<MergeGroupDto>();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    }

    public class MergeGroupDto
    {
        public Guid PrimaryId { get; set; }
        public List<Guid> DuplicateIds { get; set; } = new List<Guid>();
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class DuplicateGroupDto
    {
        public string NormalizedUrl { get; set; } = string.Empty;
        public List<ArticleSummaryDto> Articles { get; set; } = new List<ArticleSummaryDto>();
    }

    public class ArticleSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Link { get; set; }
        public DateTime DateAdded { get; set; }
        public bool HasReaderData { get; set; }
        public bool HasContent { get; set; }
    }
}
