using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Sync
{
    /// <summary>
    /// Background hosted service that periodically syncs Obsidian notes
    /// from Quartz-published vaults.
    /// </summary>
    public class ObsidianNoteSyncHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ObsidianNoteSyncHostedService> _logger;
        private readonly ObsidianNoteSyncOptions _options;

        public ObsidianNoteSyncHostedService(
            IServiceProvider serviceProvider,
            ILogger<ObsidianNoteSyncHostedService> logger,
            IOptions<ObsidianNoteSyncOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Obsidian note sync background service is disabled (ongoing cadence handled by N8N)");
                return;
            }

            _logger.LogInformation(
                "Obsidian note sync background service started. Schedule: every {Hours} hours",
                _options.IntervalHours);

            // Initial delay to let the application start up
            await Task.Delay(TimeSpan.FromMinutes(_options.InitialDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunSyncAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in Obsidian note sync background service");
                }

                // Wait for the next scheduled run
                var nextRunDelay = TimeSpan.FromHours(_options.IntervalHours);
                _logger.LogInformation("Next Obsidian note sync scheduled in {Hours} hours", _options.IntervalHours);

                await Task.Delay(nextRunDelay, stoppingToken);
            }
        }

        private async Task RunSyncAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scheduled Obsidian note sync");

            using var scope = _serviceProvider.CreateScope();
            var noteService = scope.ServiceProvider.GetRequiredService<INoteService>();

            var results = await noteService.SyncAllVaultsAsync();

            foreach (var result in results)
            {
                _logger.LogInformation(
                    "Sync completed for vault '{VaultName}': {Imported} imported, {Updated} updated, {Unchanged} unchanged, {Failed} failed",
                    result.VaultName, result.Imported, result.Updated, result.Unchanged, result.Failed);

                if (result.Errors.Any())
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning("Sync error for vault '{VaultName}': {Error}", result.VaultName, error);
                    }
                }
            }

            _logger.LogInformation("Scheduled Obsidian note sync completed");
        }
    }

    /// <summary>
    /// Configuration options for the Obsidian note sync background service.
    /// </summary>
    public class ObsidianNoteSyncOptions
    {
        public const string SectionName = "ObsidianNoteSync";

        /// <summary>
        /// Whether the background sync service is enabled. Default: false
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Hours between sync runs. Default: 6
        /// </summary>
        public int IntervalHours { get; set; } = 6;

        /// <summary>
        /// Initial delay in minutes before the first run. Default: 10
        /// </summary>
        public int InitialDelayMinutes { get; set; } = 10;

        /// <summary>
        /// URL for the general vault (e.g., "https://garden.mymediaverseuniverse.com")
        /// </summary>
        public string? GeneralVaultUrl { get; set; }

        /// <summary>
        /// Authentication token for the general vault (if protected)
        /// </summary>
        public string? GeneralVaultAuthToken { get; set; }

        /// <summary>
        /// URL for the programming vault (e.g., "https://hackerman.mymediaverseuniverse.com")
        /// </summary>
        public string? ProgrammingVaultUrl { get; set; }

        /// <summary>
        /// Authentication token for the programming vault (if protected)
        /// </summary>
        public string? ProgrammingVaultAuthToken { get; set; }
    }
}
