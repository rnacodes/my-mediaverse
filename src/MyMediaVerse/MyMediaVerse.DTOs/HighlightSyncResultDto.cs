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
        public int StubBooksCreatedCount { get; set; }  // Books created so book-category highlights have a local source
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }  // Non-fatal issues, e.g. stopped at the time budget with pages left

        // Resumable runs: a call can stop between export pages and the next call continues from there.
        public bool Resumed { get; set; }  // Started from a saved page cursor instead of the first page
        public bool MorePending { get; set; }  // Stopped before the end of the export; run again to continue
        public int PagesProcessed { get; set; }
        public string? NextPageCursor { get; set; }  // First page not yet processed; null once the export is done
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int TotalProcessed => CreatedCount + UpdatedCount + SkippedCount;
        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}

