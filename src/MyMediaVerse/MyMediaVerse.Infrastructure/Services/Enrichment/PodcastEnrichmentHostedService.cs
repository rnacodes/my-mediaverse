using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Background hosted service that periodically enriches podcast series that have not been
    /// enriched yet.
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
                _logger.LogInformation("Podcast enrichment background service is disabled (ongoing cadence handled by N8N)");
                return;
            }

            _logger.LogInformation(
                "Podcast enrichment background service started. " +
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
                    _logger.LogError(ex, "Error in podcast enrichment background service");
                }

                // Wait for the next scheduled run
                var nextRunDelay = TimeSpan.FromHours(_options.IntervalHours);
                _logger.LogInformation("Next podcast enrichment run scheduled in {Hours} hours", _options.IntervalHours);

                await Task.Delay(nextRunDelay, stoppingToken);
            }
        }

        private async Task RunEnrichmentAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scheduled podcast enrichment run");

            using var scope = _serviceProvider.CreateScope();
            var enrichmentService = scope.ServiceProvider.GetRequiredService<IPodcastEnrichmentService>();

            // Get count of podcasts needing enrichment
            var pendingCount = await enrichmentService.GetPodcastsNeedingEnrichmentCountAsync();
            _logger.LogInformation("Found {Count} podcasts needing enrichment", pendingCount);

            if (pendingCount == 0)
            {
                return;
            }

            // Process in batches until done or cancelled
            var totalEnriched = 0;
            var totalFailed = 0;

            while (pendingCount > 0 && !stoppingToken.IsCancellationRequested)
            {
                var result = await enrichmentService.EnrichPendingPodcastsAsync(
                    batchSize: _options.BatchSize,
                    delayBetweenCallsMs: _options.DelayBetweenCallsMs,
                    cancellationToken: stoppingToken);

                totalEnriched += result.EnrichedCount;
                totalFailed += result.FailedCount + result.NotFoundCount;

                if (result.WasCancelled)
                {
                    break;
                }

                // Done when nothing was processed, or nothing in the batch could be enriched.
                if (result.TotalProcessed == 0 || result.EnrichedCount == 0)
                {
                    break;
                }

                // Get updated count for next iteration
                pendingCount = await enrichmentService.GetPodcastsNeedingEnrichmentCountAsync();

                // Pause between batches to go easy on external services
                if (pendingCount > 0)
                {
                    _logger.LogInformation("Pausing before next batch. Remaining: {Count} podcasts", pendingCount);
                    await Task.Delay(TimeSpan.FromSeconds(_options.PauseBetweenBatchesSeconds), stoppingToken);
                }
            }

            _logger.LogInformation(
                "Scheduled podcast enrichment run completed. Total enriched: {Enriched}, Total failed/not found: {Failed}",
                totalEnriched, totalFailed);
        }
    }

    /// <summary>
    /// Configuration options for the podcast enrichment background service.
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
        /// </summary>
        public int IntervalHours { get; set; } = 72;

        /// <summary>
        /// Number of podcasts to process per batch. Default: 25
        /// </summary>
        public int BatchSize { get; set; } = 25;

        /// <summary>
        /// Delay in milliseconds between external calls. Default: 1500
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
