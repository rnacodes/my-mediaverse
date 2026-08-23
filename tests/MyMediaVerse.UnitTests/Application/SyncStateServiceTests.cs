using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class SyncStateServiceTests : InMemoryDbTestBase
    {
        private const string Key = "readwise";
        private static readonly DateTime Now = new(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan LookBack = TimeSpan.FromDays(7);
        private static readonly TimeSpan Overlap = TimeSpan.FromDays(1);

        private readonly SyncStateService _service;

        public SyncStateServiceTests()
        {
            _service = new SyncStateService(Context, Substitute.For<ILogger<SyncStateService>>());
        }

        private async Task SeedCursorAsync(DateTime lastSuccess)
        {
            Context.SyncStates.Add(new SyncState { Key = Key, LastSuccessfulSyncAt = lastSuccess, UpdatedAt = lastSuccess });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        [Fact]
        public async Task GetLastSuccessfulSyncAsync_NoRow_ReturnsNull()
        {
            var result = await _service.GetLastSuccessfulSyncAsync(Key);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetIncrementalWindowAsync_NoCursor_FallsBackToDefaultLookBack()
        {
            var window = await _service.GetIncrementalWindowAsync(Key, Now, LookBack, Overlap);

            window.Source.Should().Be(SyncWindowSource.Default);
            window.Since.Should().Be(Now - LookBack);
        }

        [Fact]
        public async Task GetIncrementalWindowAsync_WithCursor_UsesCursorMinusOverlap()
        {
            var lastSuccess = Now.AddDays(-30);
            await SeedCursorAsync(lastSuccess);

            var window = await _service.GetIncrementalWindowAsync(Key, Now, LookBack, Overlap);

            window.Source.Should().Be(SyncWindowSource.Cursor);
            window.Since.Should().Be(lastSuccess - Overlap);
        }

        [Fact]
        public async Task GetIncrementalWindowAsync_IsolatedByKey()
        {
            await SeedCursorAsync(Now.AddDays(-30));

            var window = await _service.GetIncrementalWindowAsync("youtube", Now, LookBack, Overlap);

            window.Source.Should().Be(SyncWindowSource.Default);
        }

        [Fact]
        public async Task MarkSyncSucceededAsync_NoRow_CreatesRow()
        {
            await _service.MarkSyncSucceededAsync(Key, Now);

            var row = await Context.SyncStates.AsNoTracking().SingleAsync(s => s.Key == Key);
            row.LastSuccessfulSyncAt.Should().Be(Now);
        }

        [Fact]
        public async Task MarkSyncSucceededAsync_ExistingRow_AdvancesCursor()
        {
            await SeedCursorAsync(Now.AddDays(-1));

            await _service.MarkSyncSucceededAsync(Key, Now);

            var row = await Context.SyncStates.AsNoTracking().SingleAsync(s => s.Key == Key);
            row.LastSuccessfulSyncAt.Should().Be(Now);
            (await Context.SyncStates.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task MarkSyncSucceededAsync_OlderTimestamp_DoesNotMoveCursorBackwards()
        {
            await SeedCursorAsync(Now);

            await _service.MarkSyncSucceededAsync(Key, Now.AddHours(-2));

            var row = await Context.SyncStates.AsNoTracking().SingleAsync(s => s.Key == Key);
            row.LastSuccessfulSyncAt.Should().Be(Now);
        }
    }
}
