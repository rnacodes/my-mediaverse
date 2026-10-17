using AwesomeAssertions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Configuration;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class ReadwiseSyncServiceTests
    {
        private static readonly DateTime ReaderSince = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime HighlightsSince = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);

        private readonly IReaderService _reader = Substitute.For<IReaderService>();
        private readonly IHighlightService _highlights = Substitute.For<IHighlightService>();
        private readonly ISyncStateService _syncState = Substitute.For<ISyncStateService>();
        private readonly ReadwiseSyncService _service;

        public ReadwiseSyncServiceTests()
        {
            _service = new ReadwiseSyncService(_reader, _highlights, _syncState,
                Options.Create(new ReadwiseSyncOptions()), Substitute.For<ILogger<ReadwiseSyncService>>());

            // Both per-source cursors already exist, so no seeding from the legacy key happens by default.
            _syncState.GetLastSuccessfulSyncAsync(Arg.Any<string>()).Returns((DateTime?)ReaderSince);
            _syncState.GetIncrementalWindowAsync(
                    ISyncStateService.ReadwiseReaderKey, Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
                .Returns(new IncrementalSyncWindow(ReaderSince, SyncWindowSource.Cursor));
            _syncState.GetIncrementalWindowAsync(
                    ISyncStateService.ReadwiseHighlightsKey, Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
                .Returns(new IncrementalSyncWindow(HighlightsSince, SyncWindowSource.Cursor));

            _reader.SyncDocumentsAsync(null, Arg.Any<DateTime?>())
                .Returns(new ReaderSyncResultDto { Success = true, CreatedCount = 1, UpdatedCount = 2 });

            _highlights.SyncHighlightsExportAsync(Arg.Is<DateTime?>(d => d != null), Arg.Any<string?>(), Arg.Any<DateTime?>())
                .Returns(new HighlightSyncResultDto { Success = true, CreatedCount = 3, UpdatedCount = 4, LinkedCount = 5, StubBooksCreatedCount = 6 });
            _highlights.SyncHighlightsExportAsync(Arg.Is<DateTime?>(d => d == null), Arg.Any<string?>(), Arg.Any<DateTime?>())
                .Returns(new HighlightSyncResultDto { Success = true, CreatedCount = 30, UpdatedCount = 40, LinkedCount = 50, StubBooksCreatedCount = 60 });
        }

        [Fact]
        public async Task SyncAll_ReportsStubBooksCreated_AndCountsThemAsMediaItems()
        {
            var incremental = await _service.SyncAllAsync(incremental: true);
            var full = await _service.SyncAllAsync(incremental: false);

            incremental.BooksCreated.Should().Be(6);
            full.BooksCreated.Should().Be(60);
            // Reader created 1 + updated 2 = 3 articles, plus the stub books, all land in the media index
            full.TotalMediaItemsProcessed.Should().Be(63);
        }

        [Fact]
        public async Task Incremental_UsesEachStepsOwnWindow_AndReportsBoth()
        {
            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeTrue();
            result.ReaderSyncedSince.Should().Be(ReaderSince);
            result.HighlightsSyncedSince.Should().Be(HighlightsSince);
            result.SyncedSince.Should().Be(ReaderSince, "the earliest window is reported at the top level");
            result.SyncWindowSource.Should().Be("cursor");
            await _reader.Received(1).SyncDocumentsAsync(null, ReaderSince);
            await _highlights.Received(1).SyncHighlightsExportAsync(HighlightsSince, Arg.Is<string?>(c => c == null), Arg.Any<DateTime?>());
            await _highlights.DidNotReceive().SyncHighlightsExportAsync(Arg.Is<DateTime?>(d => d == null), Arg.Any<string?>(), Arg.Any<DateTime?>());
        }

        [Fact]
        public async Task Incremental_AnyStepWithoutCursor_ReportsDefaultWindow()
        {
            var fallback = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
            _syncState.GetIncrementalWindowAsync(
                    ISyncStateService.ReadwiseHighlightsKey, Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
                .Returns(new IncrementalSyncWindow(fallback, SyncWindowSource.Default));

            var result = await _service.SyncAllAsync(incremental: true);

            result.SyncWindowSource.Should().Be("default");
            result.HighlightsSyncedSince.Should().Be(fallback);
        }

        [Fact]
        public async Task Incremental_NewKeyWithoutCursor_IsSeededFromLegacySharedCursor()
        {
            var legacy = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
            _syncState.GetLastSuccessfulSyncAsync(ISyncStateService.ReadwiseReaderKey).Returns((DateTime?)null);
            _syncState.GetLastSuccessfulSyncAsync(ISyncStateService.ReadwiseKey).Returns((DateTime?)legacy);

            await _service.SyncAllAsync(incremental: true);

            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseReaderKey, legacy);
            // The highlights key already had a cursor, so it is not seeded.
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, legacy);
        }

        [Fact]
        public async Task Incremental_NoCursorAnywhere_DoesNotSeed()
        {
            _syncState.GetLastSuccessfulSyncAsync(Arg.Any<string>()).Returns((DateTime?)null);

            var result = await _service.SyncAllAsync(incremental: true);

            // Only the end-of-run advance is recorded for each key.
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseReaderKey, result.StartedAt);
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, result.StartedAt);
            await _syncState.Received(2).MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task Full_SkipsWindowLookup_AndRunsFullHighlightSync()
        {
            var result = await _service.SyncAllAsync(incremental: false);

            result.Success.Should().BeTrue();
            result.SyncedSince.Should().BeNull();
            result.SyncWindowSource.Should().Be("full");
            result.HighlightsCreated.Should().Be(30);
            await _syncState.DidNotReceive().GetIncrementalWindowAsync(
                Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>());
            await _reader.Received(1).SyncDocumentsAsync(null, null);
            await _highlights.Received(1).SyncHighlightsExportAsync(Arg.Is<DateTime?>(d => d == null), Arg.Is<string?>(c => c == null), Arg.Any<DateTime?>());
        }

        [Fact]
        public async Task CleanRun_AdvancesBothCursorsToRunStart()
        {
            var result = await _service.SyncAllAsync(incremental: true);

            result.CursorAdvanced.Should().BeTrue();
            result.ReaderCursorAdvanced.Should().BeTrue();
            result.HighlightsCursorAdvanced.Should().BeTrue();
            result.ReaderStepSucceeded.Should().BeTrue();
            result.HighlightStepSucceeded.Should().BeTrue();
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseReaderKey, result.StartedAt);
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, result.StartedAt);
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(ISyncStateService.ReadwiseKey, Arg.Any<DateTime>());
        }

        [Fact]
        public async Task ReaderFailure_StillRunsHighlights_AndAdvancesOnlyTheHighlightsCursor()
        {
            _reader.SyncDocumentsAsync(null, Arg.Any<DateTime?>())
                .Returns(new ReaderSyncResultDto { Success = false, ErrorMessage = "boom" });

            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("boom");
            result.ReaderStepSucceeded.Should().BeFalse();
            result.HighlightStepSucceeded.Should().BeTrue();
            result.HighlightsCreated.Should().Be(3);
            result.CursorAdvanced.Should().BeFalse();
            result.ReaderCursorAdvanced.Should().BeFalse();
            result.HighlightsCursorAdvanced.Should().BeTrue();
            await _highlights.Received(1).SyncHighlightsExportAsync(HighlightsSince, Arg.Is<string?>(c => c == null), Arg.Any<DateTime?>());
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(ISyncStateService.ReadwiseReaderKey, Arg.Any<DateTime>());
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, result.StartedAt);
        }

        [Fact]
        public async Task ReaderThrows_IsReportedAsStepFailure_NotPropagated()
        {
            _reader.SyncDocumentsAsync(null, Arg.Any<DateTime?>())
                .Returns<ReaderSyncResultDto>(_ => throw new InvalidOperationException("token missing"));

            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("token missing");
            result.HighlightStepSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task HighlightFailure_AdvancesOnlyTheReaderCursor()
        {
            _highlights.SyncHighlightsExportAsync(Arg.Is<DateTime?>(d => d != null), Arg.Any<string?>(), Arg.Any<DateTime?>())
                .Returns(new HighlightSyncResultDto { Success = false, ErrorMessage = "export failed" });

            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("export failed");
            result.ArticlesCreated.Should().Be(1);
            result.CursorAdvanced.Should().BeFalse();
            result.ReaderCursorAdvanced.Should().BeTrue();
            result.HighlightsCursorAdvanced.Should().BeFalse();
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseReaderKey, result.StartedAt);
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, Arg.Any<DateTime>());
        }

        [Fact]
        public async Task BothStepsFail_ReportsBothErrors()
        {
            _reader.SyncDocumentsAsync(null, Arg.Any<DateTime?>())
                .Returns(new ReaderSyncResultDto { Success = false, ErrorMessage = "reader down" });
            _highlights.SyncHighlightsExportAsync(Arg.Is<DateTime?>(d => d != null), Arg.Any<string?>(), Arg.Any<DateTime?>())
                .Returns(new HighlightSyncResultDto { Success = false, ErrorMessage = "export down" });

            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("reader down").And.Contain("export down");
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task TruncatedReaderRun_SucceedsButHoldsOnlyTheReaderCursor()
        {
            _reader.SyncDocumentsAsync(null, Arg.Any<DateTime?>())
                .Returns(new ReaderSyncResultDto
                {
                    Success = true,
                    CreatedCount = 1,
                    WarningMessage = "Reader sync stopped at the 100-page safety limit"
                });

            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeTrue();
            result.WarningMessage.Should().Contain("100-page");
            result.CursorAdvanced.Should().BeFalse();
            result.ReaderCursorAdvanced.Should().BeFalse();
            result.HighlightsCursorAdvanced.Should().BeTrue();
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(ISyncStateService.ReadwiseReaderKey, Arg.Any<DateTime>());
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, result.StartedAt);
        }

        [Fact]
        public async Task TruncatedHighlightRun_SucceedsButHoldsOnlyTheHighlightsCursor()
        {
            _highlights.SyncHighlightsExportAsync(Arg.Is<DateTime?>(d => d != null), Arg.Any<string?>(), Arg.Any<DateTime?>())
                .Returns(new HighlightSyncResultDto
                {
                    Success = true,
                    CreatedCount = 1,
                    WarningMessage = "Stopped after 100 pages"
                });

            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeTrue();
            result.WarningMessage.Should().Contain("100 pages");
            result.CursorAdvanced.Should().BeFalse();
            result.ReaderCursorAdvanced.Should().BeTrue();
            result.HighlightsCursorAdvanced.Should().BeFalse();
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseReaderKey, result.StartedAt);
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, Arg.Any<DateTime>());
        }

        #region Resumable highlights runs

        private void GivenSavedResume(string pageCursor, DateTime? updatedAfter, DateTime runStartedAt, int pagesDone = 3)
        {
            var json = JsonSerializer.Serialize(
                new ReadwiseSyncService.HighlightsResumeState(pageCursor, updatedAfter, runStartedAt, pagesDone));
            _syncState.GetValueAsync(ISyncStateService.ReadwiseHighlightsResumeKey).Returns(json);
        }

        [Fact]
        public async Task BudgetReached_SavesPlace_ReportsMorePending_AndHoldsTheHighlightsCursor()
        {
            _highlights.SyncHighlightsExportAsync(Arg.Any<DateTime?>(), Arg.Any<string?>(), Arg.Any<DateTime?>())
                .Returns(new HighlightSyncResultDto
                {
                    Success = true,
                    MorePending = true,
                    NextPageCursor = "page-5",
                    PagesProcessed = 4,
                    WarningMessage = "Stopped at the time budget; more highlights are pending. Run again."
                });

            var result = await _service.SyncAllAsync(incremental: false);

            result.Success.Should().BeTrue();
            result.HighlightsMorePending.Should().BeTrue();
            result.HighlightsCursorAdvanced.Should().BeFalse();
            await _highlights.Received(1).SyncHighlightsExportAsync(
                Arg.Is<DateTime?>(d => d == null), Arg.Is<string?>(c => c == null), Arg.Is<DateTime?>(d => d != null));
            await _syncState.Received(1).SetValueAsync(ISyncStateService.ReadwiseHighlightsResumeKey,
                Arg.Is<string?>(json => json!.Contains("page-5") && json.Contains("\"PagesDone\":4")));
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, Arg.Any<DateTime>());
        }

        [Fact]
        public async Task SavedPlace_ContinuesFromIt_SkipsReader_AndIgnoresTheIncrementalFlag()
        {
            var firstCallStart = new DateTime(2026, 10, 17, 1, 0, 0, DateTimeKind.Utc);
            GivenSavedResume("page-5", updatedAfter: null, firstCallStart);
            _highlights.SyncHighlightsExportAsync(Arg.Any<DateTime?>(), Arg.Any<string?>(), Arg.Any<DateTime?>())
                .Returns(new HighlightSyncResultDto { Success = true, MorePending = true, NextPageCursor = "page-9", PagesProcessed = 4, WarningMessage = "more" });

            var result = await _service.SyncAllAsync(incremental: true);

            result.ReaderStepSkipped.Should().BeTrue();
            result.HighlightsResumed.Should().BeTrue();
            result.SyncWindowSource.Should().Be("full");
            result.ReaderCursorAdvanced.Should().BeFalse();
            await _reader.DidNotReceive().SyncDocumentsAsync(Arg.Any<string?>(), Arg.Any<DateTime?>());
            await _highlights.Received(1).SyncHighlightsExportAsync(
                Arg.Is<DateTime?>(d => d == null), "page-5", Arg.Any<DateTime?>());
            await _syncState.Received(1).SetValueAsync(ISyncStateService.ReadwiseHighlightsResumeKey,
                Arg.Is<string?>(json => json!.Contains("page-9") && json.Contains("\"PagesDone\":7")));
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task SavedPlace_FinalCall_ClearsIt_AndAdvancesToTheFirstCallsStart()
        {
            var firstCallStart = new DateTime(2026, 10, 17, 1, 0, 0, DateTimeKind.Utc);
            GivenSavedResume("page-9", HighlightsSince, firstCallStart);
            _highlights.SyncHighlightsExportAsync(Arg.Any<DateTime?>(), Arg.Any<string?>(), Arg.Any<DateTime?>())
                .Returns(new HighlightSyncResultDto { Success = true, PagesProcessed = 2 });

            var result = await _service.SyncAllAsync(incremental: true);

            result.HighlightsMorePending.Should().BeFalse();
            result.HighlightsCursorAdvanced.Should().BeTrue();
            await _highlights.Received(1).SyncHighlightsExportAsync(HighlightsSince, "page-9", Arg.Any<DateTime?>());
            await _syncState.Received(1).SetValueAsync(ISyncStateService.ReadwiseHighlightsResumeKey, null);
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseHighlightsKey, firstCallStart);
        }

        #endregion
    }
}
