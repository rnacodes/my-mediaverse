namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Service for enriching podcast series from ListenNotes API.
    /// Designed for background processing with batch support and rate limiting.
    /// </summary>
    public interface IPodcastEnrichmentService
    {
        /// <summary>
        /// Enriches podcast series that are missing ListenNotes metadata by searching and fetching from the API.
        /// Processes podcasts in batches with delays between API calls to respect rate limits.
        /// </summary>
        /// <param name="batchSize">Number of podcasts to process in this run (default: 25)</param>
        /// <param name="delayBetweenCallsMs">Delay between API calls in milliseconds (default: 1500)</param>
        /// <param name="cancellationToken">Cancellation token for stopping the operation</param>
        /// <returns>Result containing counts of processed, enriched, and failed podcasts</returns>
        Task<PodcastEnrichmentResult> EnrichPodcastsWithoutListenNotesDataAsync(
            int batchSize = 25,
            int delayBetweenCallsMs = 1500,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of podcast series that need ListenNotes enrichment (have no ExternalId).
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
        /// Number of podcasts successfully enriched with ListenNotes data.
        /// </summary>
        public int EnrichedCount { get; set; }

        /// <summary>
        /// Number of podcasts where enrichment failed (API error).
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Number of podcasts where no ListenNotes match was found.
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
