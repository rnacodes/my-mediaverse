using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Infrastructure.Services.Podcasts;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastEpisodeSyncServiceTests : InMemoryDbTestBase
    {
        private const string FeedUrl = "https://feeds.example.com/show.xml";
        private static readonly DateTime Newest = new(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc);

        private readonly IPodcastFeedReader _reader = Substitute.For<IPodcastFeedReader>();
        private readonly ISyncStateService _syncState = Substitute.For<ISyncStateService>();
        private readonly PodcastSyncOptions _options = new() { MaxEpisodesPerSync = 50, HostDelayMs = 0, RunTimeBudgetSeconds = 0 };
        private readonly PodcastEpisodeSyncService _service;

        public PodcastEpisodeSyncServiceTests()
        {
            _service = new PodcastEpisodeSyncService(
                Context, _reader, _syncState, Options.Create(_options), Substitute.For<ILogger<PodcastEpisodeSyncService>>());
        }

        /// <summary>A feed of <paramref name="count"/> items, newest first, one day apart, guids g0..gN.</summary>
        private static PodcastFeed Feed(int count, DateTime? newest = null, Func<int, FeedEpisode, FeedEpisode>? tweak = null)
        {
            var start = newest ?? Newest;
            var items = Enumerable.Range(0, count).Select(i =>
            {
                var item = new FeedEpisode
                {
                    Title = $"Episode {count - i}",
                    Guid = $"g{i}",
                    EnclosureUrl = $"https://cdn.example.com/{i}.mp3",
                    EnclosureType = "audio/mpeg",
                    PublishedAt = start.AddDays(-i),
                    DurationSeconds = 1800
                };
                return tweak?.Invoke(i, item) ?? item;
            }).ToList();

            return new PodcastFeed
            {
                Series = new FeedSeries { Title = "Show", PodcastGuid = "feed-guid" },
                Episodes = items,
                TotalItemCount = count
            };
        }

        private async Task<PodcastSeries> SeedSeriesAsync(
            string title = "Show", string? feedUrl = FeedUrl, bool subscribed = true, DateTime? lastSync = null)
        {
            var topic = await Context.Topics.FirstOrDefaultAsync(t => t.Name == "security") ?? new Topic { Name = "security" };
            var genre = await Context.Genres.FirstOrDefaultAsync(g => g.Name == "technology") ?? new Genre { Name = "technology" };
            var series = new PodcastSeries
            {
                Title = title,
                MediaType = MediaType.Podcast,
                RssFeedUrl = feedUrl,
                IsSubscribed = subscribed,
                LastSyncDate = lastSync,
                Publisher = "Host",
                Topics = new List<Topic> { topic },
                Genres = new List<Genre> { genre }
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return series;
        }

        private async Task SeedEpisodeAsync(Guid seriesId, string? guid, string? audio, DateTime? released, string title = "Stored")
        {
            Context.PodcastEpisodes.Add(new PodcastEpisode
            {
                Title = title, MediaType = MediaType.Podcast, SeriesId = seriesId, RssGuid = guid, AudioLink = audio, ReleaseDate = released
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private void FeedReturns(PodcastFeed feed, string url = FeedUrl) =>
            _reader.ReadAsync(url, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(feed);

        #region SyncSeriesAsync

        [Fact]
        public async Task FirstSync_ImportsTheNewestEpisodesUpToTheLimit_AndLeavesTheRestAsBacklog()
        {
            var series = await SeedSeriesAsync();
            FeedReturns(Feed(60));

            var result = await _service.SyncSeriesAsync(series.Id);

            result.Success.Should().BeTrue();
            result.Operation.Should().Be(PodcastEpisodeSyncResultDto.SyncOperation);
            result.CreatedCount.Should().Be(50);
            result.BacklogCount.Should().Be(10);
            result.SkippedCount.Should().Be(0);
            result.FeedItemCount.Should().Be(60);
            result.WarningMessage.Should().BeNull();
            result.CompletedAt.Should().NotBeNull();

            var stored = await Context.PodcastEpisodes.Include(e => e.Topics).Include(e => e.Genres).ToListAsync();
            stored.Should().HaveCount(50);
            stored.Select(e => e.RssGuid).Should().BeEquivalentTo(Enumerable.Range(0, 50).Select(i => $"g{i}"));
            stored.Should().OnlyContain(e => e.Topics.Any(t => t.Name == "security") && e.Genres.Any(g => g.Name == "technology"));
            stored.Should().OnlyContain(e => e.Publisher == "Host" && e.Status == Status.Uncharted);

            var saved = await Context.PodcastSeries.SingleAsync();
            saved.LastSyncDate.Should().NotBeNull();
            saved.TotalEpisodes.Should().Be(60);
            saved.FeedGuid.Should().Be("feed-guid");
        }

        [Fact]
        public async Task SecondSync_WithNothingNew_CreatesNothing()
        {
            var series = await SeedSeriesAsync();
            FeedReturns(Feed(60));
            await _service.SyncSeriesAsync(series.Id);
            Context.ChangeTracker.Clear();

            var result = await _service.SyncSeriesAsync(series.Id);

            result.CreatedCount.Should().Be(0);
            result.SkippedCount.Should().Be(50);
            result.BacklogCount.Should().Be(10);
            (await Context.PodcastEpisodes.CountAsync()).Should().Be(50);
        }

        [Fact]
        public async Task LaterSync_ImportsOnlyEpisodesNewerThanTheNewestStored()
        {
            var series = await SeedSeriesAsync();
            await SeedEpisodeAsync(series.Id, "g1", "https://cdn.example.com/1.mp3", Newest.AddDays(-1));
            // g0 is newer than the stored g1; g2..g4 are older back catalog.
            FeedReturns(Feed(5));

            var result = await _service.SyncSeriesAsync(series.Id);

            result.CreatedCount.Should().Be(1);
            result.SkippedCount.Should().Be(1);
            result.BacklogCount.Should().Be(3);
            (await Context.PodcastEpisodes.Select(e => e.RssGuid).ToListAsync()).Should().BeEquivalentTo("g0", "g1");
        }

        [Fact]
        public async Task LaterSync_WithMoreNewEpisodesThanTheLimit_WarnsAndImportsTheNewest()
        {
            _options.MaxEpisodesPerSync = 3;
            var series = await SeedSeriesAsync();
            await SeedEpisodeAsync(series.Id, "old", "https://cdn.example.com/old.mp3", Newest.AddDays(-30));
            FeedReturns(Feed(5));

            var result = await _service.SyncSeriesAsync(series.Id);

            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(3);
            result.BacklogCount.Should().Be(2);
            result.WarningMessage.Should().Contain("5 new episodes").And.Contain("at most 3");
            (await Context.PodcastEpisodes.Select(e => e.RssGuid).ToListAsync()).Should().BeEquivalentTo("old", "g0", "g1", "g2");
        }

        [Fact]
        public async Task StoredEpisodeWithoutGuid_MatchedByAudioUrl_AdoptsTheFeedGuid()
        {
            var series = await SeedSeriesAsync();
            await SeedEpisodeAsync(series.Id, null, "http://www.cdn.example.com/0.mp3", Newest);
            FeedReturns(Feed(1));

            var result = await _service.SyncSeriesAsync(series.Id);

            result.CreatedCount.Should().Be(0);
            result.SkippedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(1);
            (await Context.PodcastEpisodes.SingleAsync()).RssGuid.Should().Be("g0");
        }

        [Fact]
        public async Task DuplicateItemsInOneFeed_CreateOneEpisode()
        {
            var series = await SeedSeriesAsync();
            var feed = Feed(2, tweak: (_, item) => item with { Guid = "same", EnclosureUrl = "https://cdn.example.com/same.mp3" });
            FeedReturns(feed);

            var result = await _service.SyncSeriesAsync(series.Id);

            result.CreatedCount.Should().Be(1);
            result.SkippedCount.Should().Be(1);
            (await Context.PodcastEpisodes.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task ItemsWithoutAnEnclosure_AreIgnored()
        {
            var series = await SeedSeriesAsync();
            FeedReturns(Feed(3, tweak: (i, item) => i == 1 ? item with { EnclosureUrl = null } : item));

            var result = await _service.SyncSeriesAsync(series.Id);

            result.CreatedCount.Should().Be(2);
            result.IgnoredCount.Should().Be(1);
        }

        [Fact]
        public async Task RealFeedFixture_ImportsEpisodesWithTheirFields()
        {
            var series = await SeedSeriesAsync(title: "Darknet Diaries");
            await using (var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Feeds", "darknet-diaries.xml")))
            {
                FeedReturns(RssPodcastFeedReader.Parse(stream));
            }

            var result = await _service.SyncSeriesAsync(series.Id);

            result.CreatedCount.Should().Be(5);
            var latest = await Context.PodcastEpisodes.SingleAsync(e => e.RssGuid == "prx_7057_82aaba05-80c9-4d2e-8069-3c20f2b7dbc8");
            latest.Title.Should().Be("179: The Courthouse - Revisited");
            latest.DurationInSeconds.Should().Be(5702);
            latest.EpisodeNumber.Should().Be(179);
            latest.ReleaseDate.Should().Be(new DateTime(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc));
            latest.AudioLink.Should().EndWith("darknet-diaries-ep179-the-courthouse-revisited.mp3");
        }

        [Fact]
        public async Task UnreadableFeed_ReturnsAFailedResult_AndChangesNothing()
        {
            var series = await SeedSeriesAsync();
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.HttpError, "The feed returned HTTP 404."));

            var result = await _service.SyncSeriesAsync(series.Id);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("The feed returned HTTP 404.");
            result.CompletedAt.Should().BeNull();
            (await Context.PodcastSeries.SingleAsync()).LastSyncDate.Should().BeNull();
        }

        [Fact]
        public async Task UnknownSeries_Throws()
        {
            var act = () => _service.SyncSeriesAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task SeriesWithoutAFeed_Throws()
        {
            var series = await SeedSeriesAsync(feedUrl: null);

            var act = () => _service.SyncSeriesAsync(series.Id);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _reader.ReceivedCalls().Should().BeEmpty();
        }

        #endregion

        #region SyncSubscribedAsync

        [Fact]
        public async Task SyncAll_SyncsOnlySubscribedSeriesWithAFeed_AndRecordsACleanRun()
        {
            var subscribed = await SeedSeriesAsync(title: "Subscribed");
            await SeedSeriesAsync(title: "Unsubscribed", feedUrl: "https://feeds.example.com/other.xml", subscribed: false);
            await SeedSeriesAsync(title: "No feed", feedUrl: null);
            FeedReturns(Feed(2));

            var result = await _service.SyncSubscribedAsync();

            result.Success.Should().BeTrue();
            result.Operation.Should().Be(PodcastSyncAllResultDto.SyncAllOperation);
            result.SeriesChecked.Should().Be(1);
            result.SeriesSucceeded.Should().Be(1);
            result.CreatedCount.Should().Be(2);
            result.Series.Should().ContainSingle().Which.SeriesId.Should().Be(subscribed.Id);
            result.WarningMessage.Should().BeNull();
            await _syncState.Received(1).MarkSyncSucceededAsync(PodcastSyncAllResultDto.SyncAllOperation, result.StartedAt);
            await _reader.DidNotReceive().ReadAsync("https://feeds.example.com/other.xml", Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SyncAll_OneFailingSeries_DoesNotStopTheOthers_AndSkipsTheSyncRecord()
        {
            await SeedSeriesAsync(title: "Broken", feedUrl: "https://broken.example.com/feed", lastSync: null);
            await SeedSeriesAsync(title: "Works", lastSync: DateTime.UtcNow.AddDays(-1));
            _reader.ReadAsync("https://broken.example.com/feed", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.Unreachable, "The feed's server could not be reached."));
            FeedReturns(Feed(2));

            var result = await _service.SyncSubscribedAsync();

            result.Success.Should().BeTrue();
            result.SeriesChecked.Should().Be(2);
            result.SeriesFailed.Should().Be(1);
            result.SeriesSucceeded.Should().Be(1);
            result.CreatedCount.Should().Be(2);
            result.Errors.Should().ContainSingle().Which.Should().Be("Broken: The feed's server could not be reached.");
            result.WarningMessage.Should().Contain("1 of 2 series could not be synced");
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task SyncAll_UnexpectedErrorInOneSeries_IsReportedWithoutItsDetails()
        {
            await SeedSeriesAsync(title: "Explodes");
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidCastException("internal detail"));

            var result = await _service.SyncSubscribedAsync();

            result.Success.Should().BeTrue();
            result.SeriesFailed.Should().Be(1);
            result.Errors.Should().ContainSingle().Which.Should().Be("Explodes: Sync failed.");
        }

        [Fact]
        public async Task SyncAll_CapsTheErrorList()
        {
            for (var i = 0; i < PodcastSyncAllResultDto.MaxErrors + 5; i++)
                await SeedSeriesAsync(title: $"Broken {i}", feedUrl: $"https://broken{i}.example.com/feed");
            _reader.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.Timeout, "The feed took too long to respond."));

            var result = await _service.SyncSubscribedAsync();

            result.SeriesFailed.Should().Be(PodcastSyncAllResultDto.MaxErrors + 5);
            result.Errors.Should().HaveCount(PodcastSyncAllResultDto.MaxErrors);
        }

        [Fact]
        public async Task SyncAll_Cancelled_StopsAndReportsPendingSeries()
        {
            await SeedSeriesAsync(title: "First", feedUrl: "https://a.example.com/feed");
            await SeedSeriesAsync(title: "Second", feedUrl: "https://b.example.com/feed");
            await SeedSeriesAsync(title: "Third", feedUrl: "https://c.example.com/feed");
            using var cancellation = new CancellationTokenSource();
            _reader.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((CallInfo _) => { cancellation.Cancel(); return Task.FromException<PodcastFeed>(new OperationCanceledException(cancellation.Token)); });

            var result = await _service.SyncSubscribedAsync(cancellation.Token);

            result.WasCancelled.Should().BeTrue();
            result.PendingSeriesCount.Should().Be(3);
            result.WarningMessage.Should().Contain("cancelled");
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task SyncAll_StopsAtTheTimeBudget_LeavingTheRestForTheNextRun()
        {
            _options.RunTimeBudgetSeconds = 1;
            await SeedSeriesAsync(title: "Slow", feedUrl: "https://slow.example.com/feed", lastSync: null);
            await SeedSeriesAsync(title: "Later", feedUrl: "https://later.example.com/feed", lastSync: DateTime.UtcNow);
            _reader.ReadAsync("https://slow.example.com/feed", Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(async (CallInfo _) => { await Task.Delay(1100); return Feed(1); });

            var result = await _service.SyncSubscribedAsync();

            result.TimeBudgetReached.Should().BeTrue();
            result.SeriesChecked.Should().Be(1);
            result.PendingSeriesCount.Should().Be(1);
            result.WarningMessage.Should().Contain("time limit");
            await _reader.DidNotReceive().ReadAsync("https://later.example.com/feed", Arg.Any<int>(), Arg.Any<CancellationToken>());
            await _syncState.DidNotReceive().MarkSyncSucceededAsync(Arg.Any<string>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task SyncAll_WaitsBetweenFeedsOnTheSameHost()
        {
            _options.HostDelayMs = 300;
            await SeedSeriesAsync(title: "One", feedUrl: "https://feeds.example.com/one.xml");
            await SeedSeriesAsync(title: "Two", feedUrl: "https://feeds.example.com/two.xml");
            var readTimes = new List<DateTime>();
            _reader.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((CallInfo _) => { readTimes.Add(DateTime.UtcNow); return Task.FromResult(Feed(0)); });

            await _service.SyncSubscribedAsync();

            readTimes.Should().HaveCount(2);
            (readTimes[1] - readTimes[0]).Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(250));
        }

        #endregion
    }
}
