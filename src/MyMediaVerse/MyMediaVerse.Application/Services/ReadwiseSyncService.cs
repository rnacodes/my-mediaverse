using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Runs the Reader (documents) and Readwise (highlights) syncs back to back.
    /// The two steps are independent: each has its own cursor, and a failure in one
    /// never prevents the other from running.
    /// </summary>
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

            IncrementalSyncWindow? readerWindow = null;
            IncrementalSyncWindow? highlightWindow = null;

            if (incremental)
            {
                readerWindow = await ResolveWindowAsync(ISyncStateService.ReadwiseReaderKey, result.StartedAt);
                highlightWindow = await ResolveWindowAsync(ISyncStateService.ReadwiseHighlightsKey, result.StartedAt);

                result.ReaderSyncedSince = readerWindow.Since;
                result.HighlightsSyncedSince = highlightWindow.Since;
                result.SyncedSince = readerWindow.Since < highlightWindow.Since ? readerWindow.Since : highlightWindow.Since;
                result.SyncWindowSource =
                    readerWindow.Source == SyncWindowSource.Cursor && highlightWindow.Source == SyncWindowSource.Cursor
                        ? "cursor"
                        : "default";
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            // Step 1: Reader documents
            _logger.LogInformation("Step 1: Syncing Reader documents...");
            ReaderSyncResultDto readerResult;
            try
            {
                readerResult = await _readerService.SyncDocumentsAsync(updatedAfter: readerWindow?.Since);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reader sync step threw");
                readerResult = new ReaderSyncResultDto { Success = false, ErrorMessage = ex.Message };
            }

            result.ReaderStepSucceeded = readerResult.Success;
            result.ArticlesCreated = readerResult.CreatedCount;
            result.ArticlesUpdated = readerResult.UpdatedCount;

            if (!readerResult.Success)
            {
                errors.Add($"Reader sync failed: {readerResult.ErrorMessage}");
            }
            else if (!string.IsNullOrEmpty(readerResult.WarningMessage))
            {
                warnings.Add($"Reader: {readerResult.WarningMessage}");
            }

            // Step 2: Readwise highlights (runs regardless of step 1's outcome)
            _logger.LogInformation("Step 2: Syncing Readwise highlights...");
            HighlightSyncResultDto highlightResult;
            try
            {
                highlightResult = highlightWindow != null
                    ? await _highlightService.SyncHighlightsIncrementalAsync(highlightWindow.Since)
                    : await _highlightService.SyncHighlightsFromReadwiseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Highlight sync step threw");
                highlightResult = new HighlightSyncResultDto { Success = false, ErrorMessage = ex.Message };
            }

            result.HighlightStepSucceeded = highlightResult.Success;
            result.HighlightsCreated = highlightResult.CreatedCount;
            result.HighlightsUpdated = highlightResult.UpdatedCount;
            result.HighlightsLinked = highlightResult.LinkedCount;
            result.HighlightsDeleted = highlightResult.DeletedCount;
            result.BooksCreated = highlightResult.StubBooksCreatedCount;

            if (!highlightResult.Success)
            {
                errors.Add($"Highlight sync failed: {highlightResult.ErrorMessage}");
            }
            else if (!string.IsNullOrEmpty(highlightResult.WarningMessage))
            {
                warnings.Add($"Highlights: {highlightResult.WarningMessage}");
            }

            result.Success = errors.Count == 0;
            result.ErrorMessage = errors.Count > 0 ? string.Join(" ", errors) : null;
            result.WarningMessage = warnings.Count > 0 ? string.Join(" ", warnings) : null;

            // Each cursor advances on its own step's outcome. Only a complete, untruncated
            // step may advance; a warning means part of the window was not covered and
            // must be re-synced next time.
            result.ReaderCursorAdvanced = await TryAdvanceCursorAsync(
                ISyncStateService.ReadwiseReaderKey, readerResult.Success, readerResult.WarningMessage, result.StartedAt);
            result.HighlightsCursorAdvanced = await TryAdvanceCursorAsync(
                ISyncStateService.ReadwiseHighlightsKey, highlightResult.Success, highlightResult.WarningMessage, result.StartedAt);
            result.CursorAdvanced = result.ReaderCursorAdvanced && result.HighlightsCursorAdvanced;

            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Unified sync completed (success: {Success}). Articles: {ArticlesCreated} created, {ArticlesUpdated} updated. " +
                "Highlights: {HighlightsCreated} created, {HighlightsUpdated} updated, {HighlightsLinked} linked. " +
                "Stub books created: {BooksCreated}. " +
                "Window: {Source} since {Since}. Cursors advanced: reader={ReaderAdvanced}, highlights={HighlightsAdvanced}.",
                result.Success,
                result.ArticlesCreated, result.ArticlesUpdated,
                result.HighlightsCreated, result.HighlightsUpdated, result.HighlightsLinked,
                result.BooksCreated,
                result.SyncWindowSource, result.SyncedSince?.ToString("u") ?? "n/a",
                result.ReaderCursorAdvanced, result.HighlightsCursorAdvanced);

            return result;
        }

        /// <summary>
        /// Resolves a step's incremental window. A key that has never recorded a run
        /// inherits the legacy shared cursor once, so splitting the cursors did not
        /// reset anyone's sync window.
        /// </summary>
        private async Task<IncrementalSyncWindow> ResolveWindowAsync(string key, DateTime now)
        {
            if (await _syncStateService.GetLastSuccessfulSyncAsync(key) == null)
            {
                var legacyCursor = await _syncStateService.GetLastSuccessfulSyncAsync(ISyncStateService.ReadwiseKey);
                if (legacyCursor.HasValue)
                {
                    _logger.LogInformation("Seeding sync cursor {Key} from legacy shared cursor ({Cursor})", key, legacyCursor.Value);
                    await _syncStateService.MarkSyncSucceededAsync(key, legacyCursor.Value);
                }
            }

            return await _syncStateService.GetIncrementalWindowAsync(key, now, DefaultIncrementalLookBack, IncrementalOverlap);
        }

        private async Task<bool> TryAdvanceCursorAsync(string key, bool stepSucceeded, string? warning, DateTime runStartedAt)
        {
            if (!stepSucceeded)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(warning))
            {
                _logger.LogWarning("Step {Key} completed with a warning; cursor not advanced: {Warning}", key, warning);
                return false;
            }

            await _syncStateService.MarkSyncSucceededAsync(key, runStartedAt);
            return true;
        }
    }
}
