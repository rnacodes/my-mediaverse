using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class ReadwiseSyncServiceTests
    {
        private static readonly DateTime CursorSince = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly IReaderService _reader = Substitute.For<IReaderService>();
        private readonly IHighlightService _highlights = Substitute.For<IHighlightService>();
        private readonly ISyncStateService _syncState = Substitute.For<ISyncStateService>();
        private readonly ReadwiseSyncService _service;

        public ReadwiseSyncServiceTests()
        {
            _service = new ReadwiseSyncService(_reader, _highlights, _syncState, Substitute.For<ILogger<ReadwiseSyncService>>());

            _syncState.GetIncrementalWindowAsync(
                    ISyncStateService.ReadwiseKey, Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
                .Returns(new IncrementalSyncWindow(CursorSince, SyncWindowSource.Cursor));

            _reader.SyncDocumentsAsync(null, Arg.Any<DateTime?>())
                .Returns(new ReaderSyncResultDto { Success = true, CreatedCount = 1, UpdatedCount = 2 });

            _highlights.SyncHighlightsIncrementalAsync(Arg.Any<DateTime>())
                .Returns(new HighlightSyncResultDto { Success = true, CreatedCount = 3, UpdatedCount = 4, LinkedCount = 5 });
            _highlights.SyncHighlightsFromReadwiseAsync()
                .Returns(new HighlightSyncResultDto { Success = true, CreatedCount = 30, UpdatedCount = 40, LinkedCount = 50 });
        }

        [Fact]
        public async Task Incremental_UsesResolvedWindowForBothSteps_AndReportsIt()
        {
            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeTrue();
            result.SyncedSince.Should().Be(CursorSince);
            result.SyncWindowSource.Should().Be("cursor");
            await _reader.Received(1).SyncDocumentsAsync(null, CursorSince);
            await _highlights.Received(1).SyncHighlightsIncrementalAsync(CursorSince);
            await _highlights.DidNotReceive().SyncHighlightsFromReadwiseAsync();
        }

        [Fact]
        public async Task Incremental_NoCursor_ReportsDefaultWindow()
        {
            var fallback = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
            _syncState.GetIncrementalWindowAsync(
                    ISyncStateService.ReadwiseKey, Arg.Any<DateTime>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
                .Returns(new IncrementalSyncWindow(fallback, SyncWindowSource.Default));

            var result = await _service.SyncAllAsync(incremental: true);

            result.SyncedSince.Should().Be(fallback);
            result.SyncWindowSource.Should().Be("default");
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
            await _highlights.Received(1).SyncHighlightsFromReadwiseAsync();
        }

        [Fact]
        public async Task CleanRun_AdvancesCursorToRunStart()
        {
            var result = await _service.SyncAllAsync(incremental: true);

            result.CursorAdvanced.Should().BeTrue();
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseKey, result.StartedAt);
        }

        [Fact]
        public async Task FullCleanRun_AlsoAdvancesCursor()
        {
            var result = await _service.SyncAllAsync(incremental: false);

            result.CursorAdvanced.Should().BeTrue();
            await _syncState.Received(1).MarkSyncSucceededAsync(ISyncStateService.ReadwiseKey, result.StartedAt);
        }

        [Fact]
        public async Task ReaderFailure_DoesNotAdvanceCursor_AndSkipsHighlights()
        {
            _reader.SyncDocumentsAsync(null, Arg.Any<DateTime?>())
                .Returns(new ReaderSyncResultDto { Success = false, ErrorMessage = "boom" });

            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("boom");
            result.CursorAdvanced.Should().BeFalse();
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
            await _highlights.DidNotReceive().SyncHighlightsIncrementalAsync(Arg.Any<DateTime>());
        }

        [Fact]
        public async Task HighlightFailure_DoesNotAdvanceCursor()
        {
            _highlights.SyncHighlightsIncrementalAsync(Arg.Any<DateTime>())
                .Returns(new HighlightSyncResultDto { Success = false, ErrorMessage = "export failed" });

            var result = await _service.SyncAllAsync(incremental: true);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("export failed");
            result.CursorAdvanced.Should().BeFalse();
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task TruncatedRun_SucceedsButDoesNotAdvanceCursor()
        {
            _highlights.SyncHighlightsIncrementalAsync(Arg.Any<DateTime>())
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
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
        }
    }
}
