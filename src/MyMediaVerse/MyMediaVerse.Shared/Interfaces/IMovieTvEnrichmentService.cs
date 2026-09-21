using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Service for enriching movies and TV shows from TMDB API.
    /// Designed for background processing with batch support and rate limiting.
    /// </summary>
    public interface IMovieTvEnrichmentService
    {
        /// <summary>
        /// Enriches movies that are missing TMDB metadata by searching and fetching from TMDB API.
        /// Processes movies in batches with delays between API calls to respect rate limits.
        /// </summary>
        /// <param name="batchSize">Number of movies to process in this run (default: 50)</param>
        /// <param name="delayBetweenCallsMs">Delay between API calls in milliseconds (default: 500)</param>
        /// <param name="cancellationToken">Cancellation token for stopping the operation</param>
        /// <returns>Result containing counts of processed, enriched, and failed movies</returns>
        Task<MovieTvEnrichmentResult> EnrichMoviesWithoutTmdbDataAsync(
            int batchSize = 50,
            int delayBetweenCallsMs = 500,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Enriches TV shows that are missing TMDB metadata by searching and fetching from TMDB API.
        /// Processes TV shows in batches with delays between API calls to respect rate limits.
        /// </summary>
        /// <param name="batchSize">Number of TV shows to process in this run (default: 50)</param>
        /// <param name="delayBetweenCallsMs">Delay between API calls in milliseconds (default: 500)</param>
        /// <param name="cancellationToken">Cancellation token for stopping the operation</param>
        /// <returns>Result containing counts of processed, enriched, and failed TV shows</returns>
        Task<MovieTvEnrichmentResult> EnrichTvShowsWithoutTmdbDataAsync(
            int batchSize = 50,
            int delayBetweenCallsMs = 500,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes stored movies and TV shows whose TMDB data is missing a refresh timestamp or is
        /// older than <paramref name="olderThanDays"/>, oldest first, movies and shows in one run.
        /// TMDB-owned fields are overwritten, descriptive fields are only filled when empty, and
        /// status, rating, ownership, dates, notes, topics, and mixlists are never touched.
        /// </summary>
        /// <param name="limit">Maximum items to refresh in this run (default: 50)</param>
        /// <param name="olderThanDays">Age at which stored TMDB data counts as stale (default: 180)</param>
        /// <param name="delayBetweenCallsMs">Delay between API calls in milliseconds (default: 250)</param>
        Task<MovieTvRefreshResultDto> RefreshStaleAsync(
            int limit = 50,
            int olderThanDays = 180,
            int delayBetweenCallsMs = 250,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of movies that need TMDB enrichment (have no TmdbId).
        /// </summary>
        Task<int> GetMoviesNeedingEnrichmentCountAsync();

        /// <summary>
        /// Gets the count of TV shows that need TMDB enrichment (have no TmdbId).
        /// </summary>
        Task<int> GetTvShowsNeedingEnrichmentCountAsync();
    }

    /// <summary>
    /// Result of a movie or TV show enrichment run.
    /// </summary>
    public class MovieTvEnrichmentResult
    {
        /// <summary>
        /// Total number of items processed in this run.
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Number of items successfully enriched with TMDB data.
        /// </summary>
        public int EnrichedCount { get; set; }

        /// <summary>
        /// Number of items where enrichment failed (API error).
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Number of items where no TMDB match was found.
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
