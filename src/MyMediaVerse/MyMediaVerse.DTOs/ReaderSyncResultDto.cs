namespace MyMediaVerse.DTOs
{
    public class ReaderSyncResultDto
    {
        public bool Success { get; set; }

        // Stable identifier of the operation for sync-state records and notifications.
        public string Operation { get; set; } = "reader-sync";
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
        public string? ErrorMessage { get; set; }

        // Non-fatal: set when the run completed but did not cover its whole window
        // (e.g. the page-limit safety cap was hit). A warning holds the sync cursor.
        public string? WarningMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // True when the caller kicked off a search reindex after this run.
        public bool ReindexTriggered { get; set; }

        public int TotalProcessed => CreatedCount + UpdatedCount + SkippedCount;
        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}
