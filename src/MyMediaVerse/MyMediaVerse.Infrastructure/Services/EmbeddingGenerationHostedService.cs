using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;

namespace MyMediaVerse.Infrastructure.Services
{
    /// <summary>
    /// Background hosted service that periodically generates vector embeddings
    /// for media items and notes that don't have embeddings.
    /// </summary>
    public class EmbeddingGenerationHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmbeddingGenerationHostedService> _logger;
        private readonly EmbeddingGenerationOptions _options;

        public EmbeddingGenerationHostedService(
            IServiceProvider serviceProvider,
            ILogger<EmbeddingGenerationHostedService> logger,
            IOptions<EmbeddingGenerationOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Embedding generation background service is disabled");
                return;
            }

            _logger.LogInformation(
                "Embedding generation background service started. Schedule: every {Hours} hours, batch size: {BatchSize}",
                _options.IntervalHours, _options.BatchSize);

            // Initial delay to let the application start up and other services initialize
            await Task.Delay(TimeSpan.FromMinutes(_options.InitialDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunEmbeddingGenerationAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in embedding generation background service");
                }

                // Wait for the next scheduled run
                var nextRunDelay = TimeSpan.FromHours(_options.IntervalHours);
                _logger.LogInformation("Next embedding generation scheduled in {Hours} hours", _options.IntervalHours);

                await Task.Delay(nextRunDelay, stoppingToken);
            }
        }

        private async Task RunEmbeddingGenerationAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting scheduled embedding generation");

            using var scope = _serviceProvider.CreateScope();
            var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();

            // Check if AI service is available
            if (!await aiService.IsAvailableAsync())
            {
                _logger.LogWarning("AI service is not available. Skipping embedding generation.");
                return;
            }

            // Process media items
            await ProcessMediaItemEmbeddingsAsync(aiService, stoppingToken);

            // Process notes
            await ProcessNoteEmbeddingsAsync(aiService, stoppingToken);
        }

        private async Task ProcessMediaItemEmbeddingsAsync(IAIService aiService, CancellationToken stoppingToken)
        {
            var pendingCount = await aiService.GetMediaItemsNeedingEmbeddingCountAsync();

            if (pendingCount == 0)
            {
                _logger.LogInformation("No media items need embedding generation");
                return;
            }

            _logger.LogInformation("Found {Count} media items needing embedding generation", pendingCount);

            var result = await aiService.GenerateMediaItemEmbeddingsBatchAsync(_options.BatchSize, stoppingToken);

            _logger.LogInformation(
                "Media item embedding generation completed: {Success} succeeded, {Failed} failed, {Skipped} skipped in {Duration}ms",
                result.SuccessCount, result.FailedCount, result.SkippedCount, result.DurationMs);

            LogErrors(result.Errors, "media item embedding");
        }

        private async Task ProcessNoteEmbeddingsAsync(IAIService aiService, CancellationToken stoppingToken)
        {
            var pendingCount = await aiService.GetNotesNeedingEmbeddingCountAsync();

            if (pendingCount == 0)
            {
                _logger.LogInformation("No notes need embedding generation");
                return;
            }

            _logger.LogInformation("Found {Count} notes needing embedding generation", pendingCount);

            var result = await aiService.GenerateNoteEmbeddingsBatchAsync(_options.BatchSize, stoppingToken);

            _logger.LogInformation(
                "Note embedding generation completed: {Success} succeeded, {Failed} failed, {Skipped} skipped in {Duration}ms",
                result.SuccessCount, result.FailedCount, result.SkippedCount, result.DurationMs);

            LogErrors(result.Errors, "note embedding");
        }

        private void LogErrors(List<string> errors, string context)
        {
            if (errors.Any())
            {
                foreach (var error in errors.Take(5))
                {
                    _logger.LogWarning("{Context} error: {Error}", context, error);
                }

                if (errors.Count > 5)
                {
                    _logger.LogWarning("... and {Count} more {Context} errors", errors.Count - 5, context);
                }
            }
        }
    }

    /// <summary>
    /// Configuration options for the embedding generation background service.
    /// </summary>
    public class EmbeddingGenerationOptions
    {
        public const string SectionName = "EmbeddingGeneration";

        /// <summary>
        /// Whether the background service is enabled. Default: false
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Hours between generation runs. Default: 24
        /// </summary>
        public int IntervalHours { get; set; } = 24;

        /// <summary>
        /// Initial delay in minutes before the first run. Default: 30
        /// (gives time for other startup tasks to complete)
        /// </summary>
        public int InitialDelayMinutes { get; set; } = 30;

        /// <summary>
        /// Number of items to process per batch. Default: 50
        /// </summary>
        public int BatchSize { get; set; } = 50;
    }
}
