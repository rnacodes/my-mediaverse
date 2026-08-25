namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Combined result for unified Readwise sync operation
    /// (Reader documents + Readwise highlights)
    /// </summary>
    public class ReadwiseSyncAllResultDto
    {
        public bool Success { get; set; }

        // Article sync results
        public int ArticlesCreated { get; set; }
        public int ArticlesUpdated { get; set; }

        // Highlight sync results
        public int HighlightsCreated { get; set; }
        public int HighlightsUpdated { get; set; }
        public int HighlightsLinked { get; set; }
        public int HighlightsDeleted { get; set; }  // Removed because Readwise reported them deleted/discarded

        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }  // Non-fatal issues surfaced by either sync step
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Window actually used: null for a full sync, otherwise the "updated after" cutoff.
        public DateTime? SyncedSince { get; set; }
        // "cursor" (persisted last-success time), "default" (no cursor yet), or "full".
        public string SyncWindowSource { get; set; } = "full";
        // True when the persisted cursor was advanced to StartedAt (fully successful, untruncated run).
        public bool CursorAdvanced { get; set; }

        public int TotalArticlesProcessed => ArticlesCreated + ArticlesUpdated;
        public int TotalHighlightsProcessed => HighlightsCreated + HighlightsUpdated;

        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}
