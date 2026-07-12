using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Background hosted service that periodically enriches podcast series
    /// from ListenNotes API.
    /// </summary>
    public class PodcastEnrichmentHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PodcastEnrichmentHostedService> _logger;
        private readonly PodcastEnrichmentOptions _options;

        public PodcastEnrichmentHostedService(
            IServiceProvider serviceProvider,
            ILogger<PodcastEnrichmentHostedService> logger,
            IOptions<PodcastEnrichmentOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Podcast ListenNotes enrichment background service is disabled (ongoing cadence handled by N8N)");
                return;
            }

            _logger.LogInformation(
                "Podcast ListenNotes enrichment background service started. " +
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
                    _logger.LogError(ex, "Error in Podcast ListenNotes enrichment background service");
                }

                // Wait for the next scheduled run
                var nextRunDelay = TimeSpan.FromHours(_options.IntervalHours);
                _logger.LogInformation("Next Podcast ListenNotes enrichment run scheduled in {Hours} hours", _options.IntervalHours);

                await Task.Delay(nextRunDelay, stoppingToken);
            }
        }

        private async Task RunEnrichmentAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scheduled Podcast ListenNotes enrichment run");

            using var scope = _serviceProvider.CreateScope();
            var enrichmentService = scope.ServiceProvider.GetRequiredService<IPodcastEnrichmentService>();

            // Get count of podcasts needing enrichment
            var pendingCount = await enrichmentService.GetPodcastsNeedingEnrichmentCountAsync();
            _logger.LogInformation("Found {Count} podcasts needing ListenNotes enrichment", pendingCount);

            if (pendingCount == 0)
            {
                return;
            }

            // Process in batches until done or cancelled
            var totalEnriched = 0;
            var totalFailed = 0;

            while (pendingCount > 0 && !stoppingToken.IsCancellationRequested)
            {
                var result = await enrichmentService.EnrichPodcastsWithoutListenNotesDataAsync(
                    batchSize: _options.BatchSize,
                    delayBetweenCallsMs: _options.DelayBetweenCallsMs,
                    cancellationToken: stoppingToken);

                totalEnriched += result.EnrichedCount;
                totalFailed += result.FailedCount + result.NotFoundCount;

                if (result.WasCancelled)
                {
                    break;
                }

                // If nothing was processed, we're done
                if (result.TotalProcessed == 0)
                {
                    break;
                }

                // Get updated count for next iteration
                pendingCount = await enrichmentService.GetPodcastsNeedingEnrichmentCountAsync();

                // Add a pause between batches to be nice to the API
                if (pendingCount > 0)
                {
                    _logger.LogInformation("Pausing before next batch. Remaining: {Count} podcasts", pendingCount);
                    await Task.Delay(TimeSpan.FromSeconds(_options.PauseBetweenBatchesSeconds), stoppingToken);
                }
            }

            _logger.LogInformation(
                "Scheduled Podcast ListenNotes enrichment run completed. Total enriched: {Enriched}, Total failed/not found: {Failed}",
                totalEnriched, totalFailed);
        }
    }

    /// <summary>
    /// Configuration options for the Podcast ListenNotes enrichment background service.
    /// </summary>
    public class PodcastEnrichmentOptions
    {
        public const string SectionName = "PodcastEnrichment";

        /// <summary>
        /// Whether the background enrichment service is enabled. Default: false
        /// Set to true to enable the built-in scheduler, or use external cron instead.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Hours between enrichment runs. Default: 72 (every 3 days)
        /// More conservative due to ListenNotes API limits.
        /// </summary>
        public int IntervalHours { get; set; } = 72;

        /// <summary>
        /// Number of podcasts to process per batch. Default: 25
        /// Smaller batches due to ListenNotes API limits.
        /// </summary>
        public int BatchSize { get; set; } = 25;

        /// <summary>
        /// Delay in milliseconds between API calls. Default: 1500
        /// ListenNotes has stricter rate limits (5 requests/second, 500/month on free tier).
        /// </summary>
        public int DelayBetweenCallsMs { get; set; } = 1500;

        /// <summary>
        /// Pause in seconds between batches. Default: 60
        /// </summary>
        public int PauseBetweenBatchesSeconds { get; set; } = 60;

        /// <summary>
        /// Initial delay in minutes before the first run. Default: 10
        /// </summary>
        public int InitialDelayMinutes { get; set; } = 10;
    }
}
