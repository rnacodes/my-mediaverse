using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Background hosted service that periodically enriches book descriptions
    /// from Open Library API.
    /// </summary>
    public class BookDescriptionEnrichmentHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookDescriptionEnrichmentHostedService> _logger;
        private readonly BookDescriptionEnrichmentOptions _options;

        public BookDescriptionEnrichmentHostedService(
            IServiceProvider serviceProvider,
            ILogger<BookDescriptionEnrichmentHostedService> logger,
            IOptions<BookDescriptionEnrichmentOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Book description enrichment background service is disabled (ongoing cadence handled by N8N)");
                return;
            }

            _logger.LogInformation(
                "Book description enrichment background service started. " +
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
                    _logger.LogError(ex, "Error in book description enrichment background service");
                }

                // Wait for the next scheduled run
                var nextRunDelay = TimeSpan.FromHours(_options.IntervalHours);
                _logger.LogInformation("Next book description enrichment run scheduled in {Hours} hours", _options.IntervalHours);

                await Task.Delay(nextRunDelay, stoppingToken);
            }
        }

        private async Task RunEnrichmentAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scheduled book description enrichment run");

            using var scope = _serviceProvider.CreateScope();
            var enrichmentService = scope.ServiceProvider.GetRequiredService<IBookDescriptionEnrichmentService>();

            // Get count of books needing enrichment
            var pendingCount = await enrichmentService.GetBooksNeedingEnrichmentCountAsync();
            _logger.LogInformation("Found {Count} books needing description enrichment", pendingCount);

            if (pendingCount == 0)
            {
                return;
            }

            // Process in batches until done or cancelled
            var totalEnriched = 0;
            var totalFailed = 0;

            while (pendingCount > 0 && !stoppingToken.IsCancellationRequested)
            {
                var result = await enrichmentService.EnrichBooksWithoutDescriptionsAsync(
                    batchSize: _options.BatchSize,
                    delayBetweenCallsMs: _options.DelayBetweenCallsMs,
                    cancellationToken: stoppingToken);

                totalEnriched += result.EnrichedCount;
                totalFailed += result.FailedCount;

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
                pendingCount = await enrichmentService.GetBooksNeedingEnrichmentCountAsync();

                // Add a pause between batches to be nice to the API
                if (pendingCount > 0)
                {
                    _logger.LogInformation("Pausing before next batch. Remaining: {Count} books", pendingCount);
                    await Task.Delay(TimeSpan.FromSeconds(_options.PauseBetweenBatchesSeconds), stoppingToken);
                }
            }

            _logger.LogInformation(
                "Scheduled book description enrichment run completed. Total enriched: {Enriched}, Total failed: {Failed}",
                totalEnriched, totalFailed);
        }
    }

    /// <summary>
    /// Configuration options for the book description enrichment background service.
    /// </summary>
    public class BookDescriptionEnrichmentOptions
    {
        public const string SectionName = "BookDescriptionEnrichment";

        /// <summary>
        /// Whether the background enrichment service is enabled. Default: false
        /// Set to true to enable the built-in scheduler, or use external cron instead.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Hours between enrichment runs. Default: 48 (every 2 days)
        /// </summary>
        public int IntervalHours { get; set; } = 48;

        /// <summary>
        /// Number of books to process per batch. Default: 50
        /// </summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>
        /// Delay in milliseconds between API calls. Default: 1000 (1 second)
        /// </summary>
        public int DelayBetweenCallsMs { get; set; } = 1000;

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
