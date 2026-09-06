namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Response for GET /api/bookenrichment/status.
    /// </summary>
    public class BookEnrichmentStatusDto
    {
        /// <summary>
        /// Books with no description that have a lookup key (ISBN, or title plus a real author).
        /// </summary>
        public int BooksNeedingEnrichment { get; set; }
    }

    /// <summary>
    /// Request body for POST /api/bookenrichment/run.
    /// </summary>
    public class RunEnrichmentRequest
    {
        /// <summary>
        /// Number of books to process in this run (1-500, default: 50)
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Delay between API calls in milliseconds (100-10000, default: 1000)
        /// </summary>
        public int? DelayBetweenCallsMs { get; set; }
    }

    /// <summary>
    /// Request body for POST /api/bookenrichment/run-all.
    /// </summary>
    public class RunEnrichmentAllRequest
    {
        /// <summary>
        /// Number of books per batch (1-200, default: 50)
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Delay between API calls in milliseconds (default: 1000)
        /// </summary>
        public int? DelayBetweenCallsMs { get; set; }

        /// <summary>
        /// Maximum total books to process (1-10000, default: 1000)
        /// </summary>
        public int? MaxBooks { get; set; }

        /// <summary>
        /// Pause in seconds between batches (default: 30)
        /// </summary>
        public int? PauseBetweenBatchesSeconds { get; set; }
    }

    /// <summary>
    /// Result of POST /api/bookenrichment/run-all: the sum over every batch it ran. Follows the
    /// sync/import reporting contract; <see cref="Success"/> flips only when a batch aborted.
    /// </summary>
    public class BookEnrichmentRunAllResult
    {
        public bool Success { get; set; } = true;
        public string Operation { get; set; } = "book-description-enrichment-all";

        public int TotalProcessed { get; set; }
        public int TotalEnriched { get; set; }
        public int TotalNotFound { get; set; }
        public int TotalFailed { get; set; }
        public int BatchesRun { get; set; }
        public int RemainingBooks { get; set; }
        public bool WasCancelled { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt
            : null;
    }
}
