using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Configuration;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Runs the Reader (documents) and Readwise (highlights) syncs back to back.
    /// The two steps are independent: each has its own cursor, and a failure in one
    /// never prevents the other from running.
    /// The highlights step has a per-call time budget. A step that runs out of time saves its
    /// place, and the next call continues that run (highlights only) until the export is done.
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
        private readonly ReadwiseSyncOptions _options;
        private readonly ILogger<ReadwiseSyncService> _logger;

        /// <summary>Where an unfinished highlights run stopped. UpdatedAfter null = full sync.</summary>
        internal record HighlightsResumeState(string PageCursor, DateTime? UpdatedAfter, DateTime RunStartedAt, int PagesDone);

        public ReadwiseSyncService(
            IReaderService readerService,
            IHighlightService highlightService,
            ISyncStateService syncStateService,
            IOptions<ReadwiseSyncOptions> options,
            ILogger<ReadwiseSyncService> logger)
        {
            _readerService = readerService;
            _highlightService = highlightService;
            _syncStateService = syncStateService;
            _options = options.Value;
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

            // An unfinished highlights run wins over the incremental flag, so a full run stays full.
            var resume = await LoadResumeStateAsync();

            if (resume != null)
            {
                result.HighlightsSyncedSince = resume.UpdatedAfter;
                result.SyncedSince = resume.UpdatedAfter;
                result.SyncWindowSource = resume.UpdatedAfter.HasValue ? "cursor" : "full";
            }
            else if (incremental)
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

            // Step 1: Reader documents (skipped while continuing an unfinished highlights run)
            ReaderSyncResultDto readerResult;
            if (resume != null)
            {
                _logger.LogInformation("Step 1: Skipping Reader; continuing the highlights run started {RunStartedAt:u}", resume.RunStartedAt);
                result.ReaderStepSkipped = true;
                readerResult = new ReaderSyncResultDto { Success = true };
            }
            else
            {
                _logger.LogInformation("Step 1: Syncing Reader documents...");
                try
                {
                    readerResult = await _readerService.SyncDocumentsAsync(updatedAfter: readerWindow?.Since);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reader sync step threw");
                    readerResult = new ReaderSyncResultDto { Success = false, ErrorMessage = ex.Message };
                }
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
            var highlightsUpdatedAfter = resume != null ? resume.UpdatedAfter : highlightWindow?.Since;
            var highlightsRunStartedAt = resume?.RunStartedAt ?? result.StartedAt;
            DateTime? deadline = _options.HighlightsTimeBudgetSeconds > 0
                ? DateTime.UtcNow.AddSeconds(_options.HighlightsTimeBudgetSeconds)
                : null;

            HighlightSyncResultDto highlightResult;
            try
            {
                highlightResult = await _highlightService.SyncHighlightsExportAsync(
                    highlightsUpdatedAfter, resume?.PageCursor, deadline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Highlight sync step threw");
                highlightResult = new HighlightSyncResultDto { Success = false, ErrorMessage = ex.Message };
            }

            await SaveResumeStateAsync(highlightResult, highlightsUpdatedAfter, highlightsRunStartedAt, resume);

            result.HighlightStepSucceeded = highlightResult.Success;
            result.HighlightsCreated = highlightResult.CreatedCount;
            result.HighlightsUpdated = highlightResult.UpdatedCount;
            result.HighlightsLinked = highlightResult.LinkedCount;
            result.HighlightsDeleted = highlightResult.DeletedCount;
            result.BooksCreated = highlightResult.StubBooksCreatedCount;
            result.HighlightsMorePending = highlightResult.MorePending;
            result.HighlightsResumed = resume != null;

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
            // A multi-call highlights run advances to the time its FIRST call started, so edits made
            // while it ran are picked up next time.
            result.ReaderCursorAdvanced = !result.ReaderStepSkipped && await TryAdvanceCursorAsync(
                ISyncStateService.ReadwiseReaderKey, readerResult.Success, readerResult.WarningMessage, result.StartedAt);
            result.HighlightsCursorAdvanced = await TryAdvanceCursorAsync(
                ISyncStateService.ReadwiseHighlightsKey, highlightResult.Success, highlightResult.WarningMessage, highlightsRunStartedAt);
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

        private async Task<HighlightsResumeState?> LoadResumeStateAsync()
        {
            var json = await _syncStateService.GetValueAsync(ISyncStateService.ReadwiseHighlightsResumeKey);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<HighlightsResumeState>(json);
                if (state != null && !string.IsNullOrEmpty(state.PageCursor))
                {
                    return state;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Ignoring unreadable highlights resume state");
            }

            await _syncStateService.SetValueAsync(ISyncStateService.ReadwiseHighlightsResumeKey, null);
            return null;
        }

        /// <summary>
        /// Saves where the highlights step stopped, or clears the saved place once the export is done.
        /// A failed step with no cursor to report leaves any saved place as it was.
        /// </summary>
        private async Task SaveResumeStateAsync(
            HighlightSyncResultDto highlightResult, DateTime? updatedAfter, DateTime runStartedAt, HighlightsResumeState? previous)
        {
            if (!string.IsNullOrEmpty(highlightResult.NextPageCursor))
            {
                var state = new HighlightsResumeState(
                    highlightResult.NextPageCursor, updatedAfter, runStartedAt,
                    (previous?.PagesDone ?? 0) + highlightResult.PagesProcessed);
                await _syncStateService.SetValueAsync(
                    ISyncStateService.ReadwiseHighlightsResumeKey, JsonSerializer.Serialize(state));
                _logger.LogInformation("Highlights run paused after {PagesDone} pages in total; the next call continues it", state.PagesDone);
            }
            else if (highlightResult.Success && previous != null)
            {
                await _syncStateService.SetValueAsync(ISyncStateService.ReadwiseHighlightsResumeKey, null);
                _logger.LogInformation("Highlights run started {RunStartedAt:u} finished", runStartedAt);
            }
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
