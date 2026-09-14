namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Fills gaps in podcast series that have not been enriched yet (<c>EnrichedAt</c> is null).
    /// Designed for background processing with batch support and rate limiting.
    /// </summary>
    public interface IPodcastEnrichmentService
    {
        /// <summary>
        /// Enriches podcast series that have not been enriched yet, filling only empty fields.
        /// Processes podcasts in batches with delays between external calls.
        /// </summary>
        /// <param name="batchSize">Number of podcasts to process in this run (default: 25)</param>
        /// <param name="delayBetweenCallsMs">Delay between API calls in milliseconds (default: 1500)</param>
        /// <param name="cancellationToken">Cancellation token for stopping the operation</param>
        /// <returns>Result containing counts of processed, enriched, and failed podcasts</returns>
        Task<PodcastEnrichmentResult> EnrichPendingPodcastsAsync(
            int batchSize = 25,
            int delayBetweenCallsMs = 1500,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of podcast series that have not been enriched yet.
        /// </summary>
        Task<int> GetPodcastsNeedingEnrichmentCountAsync();
    }

    /// <summary>
    /// Result of a podcast enrichment run.
    /// </summary>
    public class PodcastEnrichmentResult
    {
        /// <summary>
        /// Total number of podcasts processed in this run.
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Number of podcasts where at least one empty field was filled.
        /// </summary>
        public int EnrichedCount { get; set; }

        /// <summary>
        /// Number of podcasts where enrichment failed (API error).
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Number of podcasts where no source had anything to add.
        /// </summary>
        public int NotFoundCount { get; set; }

        /// <summary>
        /// List of error messages for failed enrichments.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Whether the operation was cancelled before completion.
        /// </summary>
        public bool WasCancelled { get; set; }
    }
}
