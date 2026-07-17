using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Search
{
    /// <summary>
    /// Background hosted service that periodically re-indexes all collections (media, mixlists,
    /// notes, highlights) from PostgreSQL into Typesense. The bulk reindex upserts in place, so a
    /// steady-state run only re-embeds items whose text actually changed — keeping the index fresh
    /// after the initial on-command sync without the per-write cost of synchronous indexing.
    /// Disabled by default; enable via the <c>SearchIndexSync</c> configuration section.
    /// </summary>
    public class SearchIndexSyncHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SearchIndexSyncHostedService> _logger;
        private readonly SearchIndexSyncOptions _options;

        public SearchIndexSyncHostedService(
            IServiceProvider serviceProvider,
            ILogger<SearchIndexSyncHostedService> logger,
            IOptions<SearchIndexSyncOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Search index sync background service is disabled (ongoing cadence handled by N8N)");
                return;
            }

            _logger.LogInformation(
                "Search index sync background service started. Schedule: every {Hours} hours",
                _options.IntervalHours);

            // Initial delay so the app (and any startup sync) settles before the first reindex.
            await Task.Delay(TimeSpan.FromMinutes(_options.InitialDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunIndexSyncAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in search index sync background service");
                }

                _logger.LogInformation("Next search index sync scheduled in {Hours} hours", _options.IntervalHours);
                await Task.Delay(TimeSpan.FromHours(_options.IntervalHours), stoppingToken);
            }
        }

        private async Task RunIndexSyncAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scheduled search index sync");

            using var scope = _serviceProvider.CreateScope();
            var typesense = scope.ServiceProvider.GetRequiredService<ITypesenseService>();

            // Reindex each collection independently so a Typesense/OpenAI hiccup on one collection
            // leaves the others (and the source rows in Postgres) intact, to be retried next run.
            var mediaCount = await ReindexCollectionAsync("media items", typesense.BulkReindexAllMediaItemsAsync);
            stoppingToken.ThrowIfCancellationRequested();

            var mixlistCount = await ReindexCollectionAsync("mixlists", typesense.BulkReindexAllMixlistsAsync);
            stoppingToken.ThrowIfCancellationRequested();

            var noteCount = await ReindexCollectionAsync("notes", typesense.BulkReindexAllNotesAsync);
            stoppingToken.ThrowIfCancellationRequested();

            var highlightCount = await ReindexCollectionAsync("highlights", typesense.BulkReindexAllHighlightsAsync);

            _logger.LogInformation(
                "Search index sync completed. Indexed media={Media}, mixlists={Mixlists}, notes={Notes}, highlights={Highlights}",
                mediaCount, mixlistCount, noteCount, highlightCount);
        }

        /// <summary>
        /// Runs one collection's bulk reindex, logging the indexed count and swallowing failures so
        /// the remaining collections still run. Returns -1 when the reindex failed.
        /// </summary>
        private async Task<int> ReindexCollectionAsync(string collectionLabel, Func<Task<int>> reindex)
        {
            try
            {
                var count = await reindex();
                _logger.LogInformation("Search index sync: reindexed {Count} {Collection}", count, collectionLabel);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search index sync: failed to reindex {Collection}; will retry next run", collectionLabel);
                return -1;
            }
        }
    }

    /// <summary>
    /// Configuration options for the scheduled search index sync background service.
    /// </summary>
    public class SearchIndexSyncOptions
    {
        public const string SectionName = "SearchIndexSync";

        /// <summary>
        /// Whether the background service is enabled. Default: false
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Hours between reindex runs. Default: 6
        /// </summary>
        public int IntervalHours { get; set; } = 6;

        /// <summary>
        /// Initial delay in minutes before the first run. Default: 10
        /// (gives the app and any startup sync time to settle first)
        /// </summary>
        public int InitialDelayMinutes { get; set; } = 10;
    }
}
