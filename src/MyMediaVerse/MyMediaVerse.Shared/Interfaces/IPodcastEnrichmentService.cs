namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Fills podcast series from their RSS feed, resolving a missing feed URL through the podcast
    /// directory first. The feed is the source of stored series metadata.
    /// </summary>
    public interface IPodcastEnrichmentService
    {
        /// <summary>
        /// Fills a batch of series that have never been filled, oldest attempt first. Pass null for
        /// either argument to use the configured default.
        /// </summary>
        Task<PodcastEnrichmentResult> EnrichPendingPodcastsAsync(
            int? batchSize = null,
            int? delayBetweenCallsMs = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fills one series. Without <paramref name="force"/> a series that has already been filled is
        /// left alone and reported as skipped; with it, the feed's values replace what is stored.
        /// Throws <see cref="KeyNotFoundException"/> when the series does not exist.
        /// </summary>
        Task<PodcastEnrichmentResult> EnrichSeriesAsync(
            Guid seriesId,
            bool force = false,
            CancellationToken cancellationToken = default);

        /// <summary>How many series are eligible for filling right now.</summary>
        Task<int> GetPodcastsNeedingEnrichmentCountAsync();
    }

    public class PodcastEnrichmentResult
    {
        public const string EnrichmentOperation = "podcast-enrichment";

        public bool Success { get; set; } = true;

        public string Operation { get; set; } = EnrichmentOperation;

        /// <summary>Series this run looked at.</summary>
        public int TotalProcessed { get; set; }

        /// <summary>Series whose stored metadata changed.</summary>
        public int EnrichedCount { get; set; }

        /// <summary>Series whose feed was read but held nothing the series did not already have.</summary>
        public int UnchangedCount { get; set; }

        /// <summary>Series left alone because they were already filled and the run was not forced.</summary>
        public int SkippedCount { get; set; }

        /// <summary>Series with no feed URL that the directory could not resolve either.</summary>
        public int NotFoundCount { get; set; }

        public int FailedCount { get; set; }

        /// <summary>Series still eligible after this run.</summary>
        public int PendingCount { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public bool WasCancelled { get; set; }

        public string? ErrorMessage { get; set; }

        public string? WarningMessage { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

        public bool ReindexTriggered { get; set; }
    }
}
