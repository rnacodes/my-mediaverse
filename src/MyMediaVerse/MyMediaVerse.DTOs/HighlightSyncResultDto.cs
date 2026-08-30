namespace MyMediaVerse.DTOs
{
    public class HighlightSyncResultDto
    {
        public bool Success { get; set; }

        // Stable identifier of the operation for sync-state records and notifications.
        public string Operation { get; set; } = "highlight-sync";
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
        public int LinkedCount { get; set; }  // Number of highlights auto-linked to articles/books
        public int DeletedCount { get; set; }  // Highlights removed because Readwise reported them deleted/discarded
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }  // Non-fatal issues, e.g. pagination stopped at the safety limit
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int TotalProcessed => CreatedCount + UpdatedCount + SkippedCount;
        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}

