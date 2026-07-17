using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Background hosted service that periodically enriches movies and TV shows
    /// from TMDB API.
    /// </summary>
    public class MovieTvEnrichmentHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MovieTvEnrichmentHostedService> _logger;
        private readonly MovieTvEnrichmentOptions _options;

        public MovieTvEnrichmentHostedService(
            IServiceProvider serviceProvider,
            ILogger<MovieTvEnrichmentHostedService> logger,
            IOptions<MovieTvEnrichmentOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Movie/TV TMDB enrichment background service is disabled (ongoing cadence handled by N8N)");
                return;
            }

            _logger.LogInformation(
                "Movie/TV TMDB enrichment background service started. " +
                "Schedule: every {Hours} hours, Batch size: {BatchSize}, Delay between calls: {Delay}ms",
                _options.IntervalHours, _options.BatchSize, _options.DelayBetweenCallsMs);

            // Initial delay to let the application start up
            await Task.Delay(TimeSpan.FromMinutes(_options.InitialDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunEnrichmentAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in Movie/TV TMDB enrichment background service");
                }

                // Wait for the next scheduled run
                var nextRunDelay = TimeSpan.FromHours(_options.IntervalHours);
                _logger.LogInformation("Next Movie/TV TMDB enrichment run scheduled in {Hours} hours", _options.IntervalHours);

                await Task.Delay(nextRunDelay, stoppingToken);
            }
        }

        private async Task RunEnrichmentAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scheduled Movie/TV TMDB enrichment run");

            using var scope = _serviceProvider.CreateScope();
            var enrichmentService = scope.ServiceProvider.GetRequiredService<IMovieTvEnrichmentService>();

            // Process movies first
            var movieCount = await enrichmentService.GetMoviesNeedingEnrichmentCountAsync();
            _logger.LogInformation("Found {Count} movies needing TMDB enrichment", movieCount);

            if (movieCount > 0)
            {
                var totalMoviesEnriched = 0;
                var totalMoviesFailed = 0;

                while (movieCount > 0 && !stoppingToken.IsCancellationRequested)
                {
                    var result = await enrichmentService.EnrichMoviesWithoutTmdbDataAsync(
                        batchSize: _options.BatchSize,
                        delayBetweenCallsMs: _options.DelayBetweenCallsMs,
                        cancellationToken: stoppingToken);

                    totalMoviesEnriched += result.EnrichedCount;
                    totalMoviesFailed += result.FailedCount + result.NotFoundCount;

                    if (result.WasCancelled || result.TotalProcessed == 0)
                        break;

                    movieCount = await enrichmentService.GetMoviesNeedingEnrichmentCountAsync();

                    if (movieCount > 0)
                    {
                        _logger.LogInformation("Pausing before next movie batch. Remaining: {Count} movies", movieCount);
                        await Task.Delay(TimeSpan.FromSeconds(_options.PauseBetweenBatchesSeconds), stoppingToken);
                    }
                }

                _logger.LogInformation(
                    "Movie enrichment phase complete. Total enriched: {Enriched}, Total failed/not found: {Failed}",
                    totalMoviesEnriched, totalMoviesFailed);
            }

            // Then process TV shows
            var tvShowCount = await enrichmentService.GetTvShowsNeedingEnrichmentCountAsync();
            _logger.LogInformation("Found {Count} TV shows needing TMDB enrichment", tvShowCount);

            if (tvShowCount > 0)
            {
                var totalTvShowsEnriched = 0;
                var totalTvShowsFailed = 0;

                while (tvShowCount > 0 && !stoppingToken.IsCancellationRequested)
                {
                    var result = await enrichmentService.EnrichTvShowsWithoutTmdbDataAsync(
                        batchSize: _options.BatchSize,
                        delayBetweenCallsMs: _options.DelayBetweenCallsMs,
                        cancellationToken: stoppingToken);

                    totalTvShowsEnriched += result.EnrichedCount;
                    totalTvShowsFailed += result.FailedCount + result.NotFoundCount;

                    if (result.WasCancelled || result.TotalProcessed == 0)
                        break;

                    tvShowCount = await enrichmentService.GetTvShowsNeedingEnrichmentCountAsync();

                    if (tvShowCount > 0)
                    {
                        _logger.LogInformation("Pausing before next TV show batch. Remaining: {Count} TV shows", tvShowCount);
                        await Task.Delay(TimeSpan.FromSeconds(_options.PauseBetweenBatchesSeconds), stoppingToken);
                    }
                }

                _logger.LogInformation(
                    "TV show enrichment phase complete. Total enriched: {Enriched}, Total failed/not found: {Failed}",
                    totalTvShowsEnriched, totalTvShowsFailed);
            }

            _logger.LogInformation("Scheduled Movie/TV TMDB enrichment run completed");
        }
    }

    /// <summary>
    /// Configuration options for the Movie/TV TMDB enrichment background service.
    /// </summary>
    public class MovieTvEnrichmentOptions
    {
        public const string SectionName = "MovieTvEnrichment";

        /// <summary>
        /// Whether the background enrichment service is enabled. Default: false
        /// Set to true to enable the built-in scheduler, or use external cron instead.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Hours between enrichment runs. Default: 24 (once per day)
        /// </summary>
        public int IntervalHours { get; set; } = 24;

        /// <summary>
        /// Number of items to process per batch. Default: 50
        /// </summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>
        /// Delay in milliseconds between API calls. Default: 500
        /// TMDB has generous rate limits (40 requests per 10 seconds).
        /// </summary>
        public int DelayBetweenCallsMs { get; set; } = 500;

        /// <summary>
        /// Pause in seconds between batches. Default: 30
        /// </summary>
        public int PauseBetweenBatchesSeconds { get; set; } = 30;

        /// <summary>
        /// Initial delay in minutes before the first run. Default: 5
        /// </summary>
        public int InitialDelayMinutes { get; set; } = 5;
    }
}
