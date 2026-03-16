using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;

namespace MyMediaVerse.Infrastructure.Services
{
    /// <summary>
    /// Background hosted service that periodically generates AI descriptions
    /// for notes that don't have descriptions.
    /// </summary>
    public class NoteDescriptionGenerationHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NoteDescriptionGenerationHostedService> _logger;
        private readonly NoteDescriptionGenerationOptions _options;

        public NoteDescriptionGenerationHostedService(
            IServiceProvider serviceProvider,
            ILogger<NoteDescriptionGenerationHostedService> logger,
            IOptions<NoteDescriptionGenerationOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Note description generation background service is disabled");
                return;
            }

            _logger.LogInformation(
                "Note description generation background service started. Schedule: every {Hours} hours, batch size: {BatchSize}",
                _options.IntervalHours, _options.BatchSize);

            // Initial delay to let the application start up and note sync to complete
            await Task.Delay(TimeSpan.FromMinutes(_options.InitialDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunDescriptionGenerationAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in note description generation background service");
                }

                // Wait for the next scheduled run
                var nextRunDelay = TimeSpan.FromHours(_options.IntervalHours);
                _logger.LogInformation("Next note description generation scheduled in {Hours} hours", _options.IntervalHours);

                await Task.Delay(nextRunDelay, stoppingToken);
            }
        }

        private async Task RunDescriptionGenerationAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scheduled note description generation");

            using var scope = _serviceProvider.CreateScope();
            var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();

            // Check if AI service is available
            if (!await aiService.IsAvailableAsync())
            {
                _logger.LogWarning("AI service is not available. Skipping description generation.");
                return;
            }

            // Check how many notes need descriptions
            var pendingCount = await aiService.GetNotesNeedingDescriptionCountAsync();
            if (pendingCount == 0)
            {
                _logger.LogInformation("No notes need description generation");
                return;
            }

            _logger.LogInformation("Found {Count} notes needing description generation", pendingCount);

            // Generate descriptions in batches
            var result = await aiService.GenerateNoteDescriptionsBatchAsync(_options.BatchSize, stoppingToken);

            _logger.LogInformation(
                "Note description generation completed: {Success} succeeded, {Failed} failed, {Skipped} skipped in {Duration}ms",
                result.SuccessCount, result.FailedCount, result.SkippedCount, result.DurationMs);

            if (result.Errors.Any())
            {
                foreach (var error in result.Errors.Take(5)) // Log first 5 errors
                {
                    _logger.LogWarning("Description generation error: {Error}", error);
                }

                if (result.Errors.Count > 5)
                {
                    _logger.LogWarning("... and {Count} more errors", result.Errors.Count - 5);
                }
            }
        }
    }

    /// <summary>
    /// Configuration options for the note description generation background service.
    /// </summary>
    public class NoteDescriptionGenerationOptions
    {
        public const string SectionName = "NoteDescriptionGeneration";

        /// <summary>
        /// Whether the background service is enabled. Default: false
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Hours between generation runs. Default: 12
        /// </summary>
        public int IntervalHours { get; set; } = 12;

        /// <summary>
        /// Initial delay in minutes before the first run. Default: 20
        /// (gives time for note sync to complete first)
        /// </summary>
        public int InitialDelayMinutes { get; set; } = 20;

        /// <summary>
        /// Number of notes to process per batch. Default: 20
        /// </summary>
        public int BatchSize { get; set; } = 20;

        /// <summary>
        /// Maximum tokens for generated descriptions. Default: 200
        /// </summary>
        public int MaxTokensPerDescription { get; set; } = 200;
    }
}
