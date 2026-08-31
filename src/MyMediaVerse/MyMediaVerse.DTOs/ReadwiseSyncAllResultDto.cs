namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Combined result for unified Readwise sync operation
    /// (Reader documents + Readwise highlights). The two steps run independently
    /// with their own incremental cursors; this DTO reports both.
    /// </summary>
    public class ReadwiseSyncAllResultDto
    {
        // Fatal-only: false when either step aborted. Per-step detail is in the fields below.
        public bool Success { get; set; }

        // Stable identifier of the operation for sync-state records and notifications.
        public string Operation { get; set; } = "readwise-sync";

        // Article sync results
        public bool ReaderStepSucceeded { get; set; }
        public int ArticlesCreated { get; set; }
        public int ArticlesUpdated { get; set; }

        // Highlight sync results
        public bool HighlightStepSucceeded { get; set; }
        public int HighlightsCreated { get; set; }
        public int HighlightsUpdated { get; set; }
        public int HighlightsLinked { get; set; }
        public int HighlightsDeleted { get; set; }  // Removed because Readwise reported them deleted/discarded

        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }  // Non-fatal issues surfaced by either sync step
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Window actually used: null for a full sync, otherwise the earliest "updated after"
        // cutoff across the two steps (each step has its own cursor; see the per-step fields).
        public DateTime? SyncedSince { get; set; }
        // "cursor" (both steps used a persisted cursor), "default" (at least one step had no cursor yet), or "full".
        public string SyncWindowSource { get; set; } = "full";
        // True when BOTH cursors were advanced to StartedAt (fully successful, untruncated run).
        public bool CursorAdvanced { get; set; }

        // Per-step cursor detail
        public DateTime? ReaderSyncedSince { get; set; }
        public bool ReaderCursorAdvanced { get; set; }
        public DateTime? HighlightsSyncedSince { get; set; }
        public bool HighlightsCursorAdvanced { get; set; }

        // True when the caller kicked off a search reindex after this run.
        public bool ReindexTriggered { get; set; }

        public int TotalArticlesProcessed => ArticlesCreated + ArticlesUpdated;
        public int TotalHighlightsProcessed => HighlightsCreated + HighlightsUpdated;

        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}
