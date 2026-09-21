using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MovieTvEnrichmentController : ControllerBase
    {
        private readonly IMovieTvEnrichmentService _enrichmentService;
        private readonly IImportReindexService _importReindexService;
        private readonly ILogger<MovieTvEnrichmentController> _logger;

        public MovieTvEnrichmentController(
            IMovieTvEnrichmentService enrichmentService,
            IImportReindexService importReindexService,
            ILogger<MovieTvEnrichmentController> logger)
        {
            _enrichmentService = enrichmentService;
            _importReindexService = importReindexService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the count of movies and TV shows that need TMDB enrichment (have no TmdbId).
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<MovieTvEnrichmentStatusDto>> GetStatus()
        {
            try
            {
                var moviesCount = await _enrichmentService.GetMoviesNeedingEnrichmentCountAsync();
                var tvShowsCount = await _enrichmentService.GetTvShowsNeedingEnrichmentCountAsync();

                return Ok(new MovieTvEnrichmentStatusDto
                {
                    MoviesNeedingEnrichment = moviesCount,
                    TvShowsNeedingEnrichment = tvShowsCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Movie/TV enrichment status");
                return StatusCode(500, new { error = "Failed to get enrichment status", details = ex.Message });
            }
        }

        /// <summary>
        /// Triggers an on-demand movie TMDB enrichment run.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run/movies")]
        public async Task<ActionResult<MovieTvEnrichmentResult>> RunMovieEnrichment(
            [FromBody] RunMovieTvEnrichmentRequest? request = null)
        {
            try
            {
                var batchSize = request?.BatchSize ?? 50;
                var delayMs = request?.DelayBetweenCallsMs ?? 500;

                // Validate parameters
                if (batchSize < 1 || batchSize > 500)
                {
                    return BadRequest(new { error = "BatchSize must be between 1 and 500" });
                }

                if (delayMs < 100 || delayMs > 10000)
                {
                    return BadRequest(new { error = "DelayBetweenCallsMs must be between 100 and 10000" });
                }

                _logger.LogInformation(
                    "Starting on-demand movie TMDB enrichment. BatchSize: {BatchSize}, Delay: {Delay}ms",
                    batchSize, delayMs);

                var result = await _enrichmentService.EnrichMoviesWithoutTmdbDataAsync(
                    batchSize: batchSize,
                    delayBetweenCallsMs: delayMs);

                _logger.LogInformation(
                    "On-demand movie enrichment completed. Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}",
                    result.EnrichedCount, result.NotFoundCount, result.FailedCount);

                await ReindexAsync(result.EnrichedCount, "movie enrichment");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running on-demand movie enrichment");
                return StatusCode(500, new MovieTvEnrichmentResult { Errors = { $"Movie enrichment run failed: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Triggers an on-demand TV show TMDB enrichment run.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run/tvshows")]
        public async Task<ActionResult<MovieTvEnrichmentResult>> RunTvShowEnrichment(
            [FromBody] RunMovieTvEnrichmentRequest? request = null)
        {
            try
            {
                var batchSize = request?.BatchSize ?? 50;
                var delayMs = request?.DelayBetweenCallsMs ?? 500;

                // Validate parameters
                if (batchSize < 1 || batchSize > 500)
                {
                    return BadRequest(new { error = "BatchSize must be between 1 and 500" });
                }

                if (delayMs < 100 || delayMs > 10000)
                {
                    return BadRequest(new { error = "DelayBetweenCallsMs must be between 100 and 10000" });
                }

                _logger.LogInformation(
                    "Starting on-demand TV show TMDB enrichment. BatchSize: {BatchSize}, Delay: {Delay}ms",
                    batchSize, delayMs);

                var result = await _enrichmentService.EnrichTvShowsWithoutTmdbDataAsync(
                    batchSize: batchSize,
                    delayBetweenCallsMs: delayMs);

                _logger.LogInformation(
                    "On-demand TV show enrichment completed. Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}",
                    result.EnrichedCount, result.NotFoundCount, result.FailedCount);

                await ReindexAsync(result.EnrichedCount, "TV show enrichment");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running on-demand TV show enrichment");
                return StatusCode(500, new MovieTvEnrichmentResult { Errors = { $"TV show enrichment run failed: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Runs enrichment for all movies and TV shows until complete or limit reached.
        /// Use with caution for large libraries - this can take a long time.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run-all")]
        public async Task<ActionResult<MovieTvEnrichmentRunAllResult>> RunAllEnrichment(
            [FromBody] RunMovieTvEnrichmentAllRequest? request = null)
        {
            try
            {
                var batchSize = request?.BatchSize ?? 50;
                var delayMs = request?.DelayBetweenCallsMs ?? 500;
                var maxMovies = request?.MaxMovies ?? 500;
                var maxTvShows = request?.MaxTvShows ?? 500;
                var pauseBetweenBatchesSeconds = request?.PauseBetweenBatchesSeconds ?? 30;

                // Validate parameters
                if (batchSize < 1 || batchSize > 200)
                {
                    return BadRequest(new { error = "BatchSize must be between 1 and 200" });
                }

                if (maxMovies < 0 || maxMovies > 5000)
                {
                    return BadRequest(new { error = "MaxMovies must be between 0 and 5000" });
                }

                if (maxTvShows < 0 || maxTvShows > 5000)
                {
                    return BadRequest(new { error = "MaxTvShows must be between 0 and 5000" });
                }

                _logger.LogInformation(
                    "Starting full Movie/TV TMDB enrichment. BatchSize: {BatchSize}, MaxMovies: {MaxMovies}, MaxTvShows: {MaxTvShows}",
                    batchSize, maxMovies, maxTvShows);

                var totalMoviesEnriched = 0;
                var totalMoviesNotFound = 0;
                var totalMoviesFailed = 0;
                var totalMoviesProcessed = 0;
                var totalTvShowsEnriched = 0;
                var totalTvShowsNotFound = 0;
                var totalTvShowsFailed = 0;
                var totalTvShowsProcessed = 0;
                var allErrors = new List<string>();

                // Process movies
                if (maxMovies > 0)
                {
                    var moviesPending = await _enrichmentService.GetMoviesNeedingEnrichmentCountAsync();

                    while (moviesPending > 0 && totalMoviesProcessed < maxMovies)
                    {
                        var result = await _enrichmentService.EnrichMoviesWithoutTmdbDataAsync(
                            batchSize: Math.Min(batchSize, maxMovies - totalMoviesProcessed),
                            delayBetweenCallsMs: delayMs);

                        totalMoviesEnriched += result.EnrichedCount;
                        totalMoviesNotFound += result.NotFoundCount;
                        totalMoviesFailed += result.FailedCount;
                        totalMoviesProcessed += result.TotalProcessed;
                        allErrors.AddRange(result.Errors.Take(5));

                        if (result.TotalProcessed == 0)
                            break;

                        moviesPending = await _enrichmentService.GetMoviesNeedingEnrichmentCountAsync();

                        if (moviesPending > 0 && totalMoviesProcessed < maxMovies)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(pauseBetweenBatchesSeconds));
                        }
                    }
                }

                // Process TV shows
                if (maxTvShows > 0)
                {
                    var tvShowsPending = await _enrichmentService.GetTvShowsNeedingEnrichmentCountAsync();

                    while (tvShowsPending > 0 && totalTvShowsProcessed < maxTvShows)
                    {
                        var result = await _enrichmentService.EnrichTvShowsWithoutTmdbDataAsync(
                            batchSize: Math.Min(batchSize, maxTvShows - totalTvShowsProcessed),
                            delayBetweenCallsMs: delayMs);

                        totalTvShowsEnriched += result.EnrichedCount;
                        totalTvShowsNotFound += result.NotFoundCount;
                        totalTvShowsFailed += result.FailedCount;
                        totalTvShowsProcessed += result.TotalProcessed;
                        allErrors.AddRange(result.Errors.Take(5));

                        if (result.TotalProcessed == 0)
                            break;

                        tvShowsPending = await _enrichmentService.GetTvShowsNeedingEnrichmentCountAsync();

                        if (tvShowsPending > 0 && totalTvShowsProcessed < maxTvShows)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(pauseBetweenBatchesSeconds));
                        }
                    }
                }

                var remainingMovies = await _enrichmentService.GetMoviesNeedingEnrichmentCountAsync();
                var remainingTvShows = await _enrichmentService.GetTvShowsNeedingEnrichmentCountAsync();

                _logger.LogInformation(
                    "Full enrichment completed. Movies: {MoviesEnriched}/{MoviesProcessed}, TV Shows: {TvShowsEnriched}/{TvShowsProcessed}",
                    totalMoviesEnriched, totalMoviesProcessed, totalTvShowsEnriched, totalTvShowsProcessed);

                // One reindex for the whole run rather than one per batch.
                await ReindexAsync(totalMoviesEnriched + totalTvShowsEnriched, "movie and TV show enrichment");

                return Ok(new MovieTvEnrichmentRunAllResult
                {
                    TotalMoviesProcessed = totalMoviesProcessed,
                    TotalMoviesEnriched = totalMoviesEnriched,
                    TotalMoviesNotFound = totalMoviesNotFound,
                    TotalMoviesFailed = totalMoviesFailed,
                    TotalTvShowsProcessed = totalTvShowsProcessed,
                    TotalTvShowsEnriched = totalTvShowsEnriched,
                    TotalTvShowsNotFound = totalTvShowsNotFound,
                    TotalTvShowsFailed = totalTvShowsFailed,
                    RemainingMovies = remainingMovies,
                    RemainingTvShows = remainingTvShows,
                    Errors = allErrors.Take(20).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running full Movie/TV enrichment");
                return StatusCode(500, new MovieTvEnrichmentRunAllResult { Errors = { $"Full enrichment run failed: {ex.Message}" } });
            }
        }

        /// <summary>
        /// Refreshes stored movies and TV shows whose TMDB data is older than the given age
        /// (TMDB asks that stored data be no older than six months). Safe to call on a schedule:
        /// a run with nothing stale reports zero processed.
        /// </summary>
        /// <param name="limit">Maximum items to refresh in this run (1-500, default: 50)</param>
        /// <param name="olderThanDays">Age at which stored TMDB data counts as stale (0-3650, default: 180)</param>
        [HttpPost("refresh-stale")]
        public async Task<ActionResult<MovieTvRefreshResultDto>> RefreshStale(
            [FromQuery] int limit = 50,
            [FromQuery] int olderThanDays = 180)
        {
            if (limit < 1 || limit > 500)
            {
                return BadRequest(new { error = "limit must be between 1 and 500" });
            }

            if (olderThanDays < 0 || olderThanDays > 3650)
            {
                return BadRequest(new { error = "olderThanDays must be between 0 and 3650" });
            }

            MovieTvRefreshResultDto result;
            try
            {
                result = await _enrichmentService.RefreshStaleAsync(
                    limit: limit,
                    olderThanDays: olderThanDays,
                    cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running TMDB refresh");
                result = new MovieTvRefreshResultDto
                {
                    Success = false,
                    StartedAt = DateTime.UtcNow,
                    ErrorMessage = $"TMDB refresh run failed: {ex.Message}"
                };
            }

            if (!result.Success)
            {
                return StatusCode(500, result);
            }

            // Search reindex comes last, and only when stored data actually changed.
            if (result.UpdatedCount > 0)
            {
                try
                {
                    await _importReindexService.ReindexAfterImportAsync(result.UpdatedCount, "TMDB refresh");
                    result.ReindexTriggered = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Search reindex after TMDB refresh failed");
                    result.Warnings.Add("The refresh was saved, but the search reindex failed; search results may lag until the next reindex.");
                }
            }

            return Ok(result);
        }

        private async Task ReindexAsync(int changedCount, string label)
        {
            if (changedCount <= 0)
            {
                return;
            }

            try
            {
                await _importReindexService.ReindexAfterImportAsync(changedCount, label);
            }
            catch (Exception ex)
            {
                // The enrichment itself is already saved; a reindex failure must not turn it into a 500.
                _logger.LogError(ex, "Search reindex after {Label} failed", label);
            }
        }
    }

    public class MovieTvEnrichmentStatusDto
    {
        public int MoviesNeedingEnrichment { get; set; }
        public int TvShowsNeedingEnrichment { get; set; }
    }

    public class RunMovieTvEnrichmentRequest
    {
        /// <summary>
        /// Number of items to process in this run (1-500, default: 50)
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Delay between API calls in milliseconds (100-10000, default: 500)
        /// </summary>
        public int? DelayBetweenCallsMs { get; set; }
    }

    public class RunMovieTvEnrichmentAllRequest
    {
        /// <summary>
        /// Number of items per batch (1-200, default: 50)
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Delay between API calls in milliseconds (default: 500)
        /// </summary>
        public int? DelayBetweenCallsMs { get; set; }

        /// <summary>
        /// Maximum movies to process (0-5000, default: 500)
        /// </summary>
        public int? MaxMovies { get; set; }

        /// <summary>
        /// Maximum TV shows to process (0-5000, default: 500)
        /// </summary>
        public int? MaxTvShows { get; set; }

        /// <summary>
        /// Pause in seconds between batches (default: 30)
        /// </summary>
        public int? PauseBetweenBatchesSeconds { get; set; }
    }

    public class MovieTvEnrichmentRunAllResult
    {
        public int TotalMoviesProcessed { get; set; }
        public int TotalMoviesEnriched { get; set; }
        public int TotalMoviesNotFound { get; set; }
        public int TotalMoviesFailed { get; set; }
        public int TotalTvShowsProcessed { get; set; }
        public int TotalTvShowsEnriched { get; set; }
        public int TotalTvShowsNotFound { get; set; }
        public int TotalTvShowsFailed { get; set; }
        public int RemainingMovies { get; set; }
        public int RemainingTvShows { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
