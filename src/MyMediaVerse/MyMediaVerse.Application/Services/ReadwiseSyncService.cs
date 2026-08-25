using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    public class ReadwiseSyncService : IReadwiseSyncService
    {
        // Used only until the first fully-successful run records a cursor.
        internal static readonly TimeSpan DefaultIncrementalLookBack = TimeSpan.FromDays(7);
        // Re-covers a little of the previous run so edits that landed mid-run aren't missed.
        internal static readonly TimeSpan IncrementalOverlap = TimeSpan.FromDays(1);

        private readonly IReaderService _readerService;
        private readonly IHighlightService _highlightService;
        private readonly ISyncStateService _syncStateService;
        private readonly ILogger<ReadwiseSyncService> _logger;

        public ReadwiseSyncService(
            IReaderService readerService,
            IHighlightService highlightService,
            ISyncStateService syncStateService,
            ILogger<ReadwiseSyncService> logger)
        {
            _readerService = readerService;
            _highlightService = highlightService;
            _syncStateService = syncStateService;
            _logger = logger;
        }

        public async Task<ReadwiseSyncAllResultDto> SyncAllAsync(bool incremental)
        {
            var result = new ReadwiseSyncAllResultDto
            {
                StartedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Starting unified Readwise sync (incremental: {Incremental})", incremental);

            DateTime? since = null;
            if (incremental)
            {
                var window = await _syncStateService.GetIncrementalWindowAsync(
                    ISyncStateService.ReadwiseKey, result.StartedAt, DefaultIncrementalLookBack, IncrementalOverlap);
                since = window.Since;
                result.SyncedSince = window.Since;
                result.SyncWindowSource = window.Source == SyncWindowSource.Cursor ? "cursor" : "default";
            }

            // Step 1: Reader documents
            _logger.LogInformation("Step 1: Syncing Reader documents...");
            var readerResult = await _readerService.SyncDocumentsAsync(updatedAfter: since);

            if (!readerResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = $"Reader sync failed: {readerResult.ErrorMessage}";
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }

            result.ArticlesCreated = readerResult.CreatedCount;
            result.ArticlesUpdated = readerResult.UpdatedCount;

            // Step 2: Readwise highlights
            _logger.LogInformation("Step 2: Syncing Readwise highlights...");
            var highlightResult = since.HasValue
                ? await _highlightService.SyncHighlightsIncrementalAsync(since.Value)
                : await _highlightService.SyncHighlightsFromReadwiseAsync();

            if (!highlightResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = $"Highlight sync failed: {highlightResult.ErrorMessage}";
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }

            result.HighlightsCreated = highlightResult.CreatedCount;
            result.HighlightsUpdated = highlightResult.UpdatedCount;
            result.HighlightsLinked = highlightResult.LinkedCount;
            result.HighlightsDeleted = highlightResult.DeletedCount;
            result.WarningMessage = highlightResult.WarningMessage;
            result.Success = true;

            // Only a complete, untruncated run may advance the cursor; a warning means part
            // of the window was not covered and must be re-synced next time.
            if (string.IsNullOrEmpty(result.WarningMessage))
            {
                await _syncStateService.MarkSyncSucceededAsync(ISyncStateService.ReadwiseKey, result.StartedAt);
                result.CursorAdvanced = true;
            }
            else
            {
                _logger.LogWarning("Sync completed with a warning; cursor not advanced: {Warning}", result.WarningMessage);
            }

            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Unified sync completed. Articles: {ArticlesCreated} created, {ArticlesUpdated} updated. " +
                "Highlights: {HighlightsCreated} created, {HighlightsUpdated} updated, {HighlightsLinked} linked. " +
                "Window: {Source} since {Since}. Cursor advanced: {CursorAdvanced}.",
                result.ArticlesCreated, result.ArticlesUpdated,
                result.HighlightsCreated, result.HighlightsUpdated, result.HighlightsLinked,
                result.SyncWindowSource, result.SyncedSince?.ToString("u") ?? "n/a", result.CursorAdvanced);

            return result;
        }
    }
}
