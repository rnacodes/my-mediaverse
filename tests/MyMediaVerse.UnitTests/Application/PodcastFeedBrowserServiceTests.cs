using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastFeedBrowserServiceTests : InMemoryDbTestBase
    {
        private const string FeedUrl = "https://feeds.example.com/show.xml";
        private static readonly DateTime Newest = new(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc);

        private readonly IPodcastFeedReader _reader = Substitute.For<IPodcastFeedReader>();
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private readonly PodcastFeedBrowserService _service;

        public PodcastFeedBrowserServiceTests()
        {
            var podcastService = new PodcastService(
                Context, Substitute.For<ITypesenseService>(), Substitute.For<ILogger<PodcastService>>());
            _service = new PodcastFeedBrowserService(
                Context, _reader, podcastService, _cache,
                Options.Create(new PodcastSyncOptions { FeedCacheMinutes = 15 }),
                Substitute.For<ILogger<PodcastFeedBrowserService>>());
        }

        private static PodcastFeed Feed(int count) => new()
        {
            Series = new FeedSeries { Title = "Show" },
            Episodes = Enumerable.Range(0, count).Select(i => new FeedEpisode
            {
                Title = $"Episode {count - i}",
                Guid = $"g{i}",
                EnclosureUrl = i == 2 ? null : $"https://cdn.example.com/{i}.mp3",
                PublishedAt = Newest.AddDays(-i),
                DurationSeconds = 600,
                Description = new string('d', 800)
            }).ToList(),
            TotalItemCount = count
        };

        private async Task<PodcastSeries> SeedSeriesAsync(string? feedUrl = FeedUrl)
        {
            var series = new PodcastSeries
            {
                Title = "Show", MediaType = MediaType.Podcast, RssFeedUrl = feedUrl,
                FeedUrlKey = feedUrl == null ? null : "feeds.example.com/show.xml"
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return series;
        }

        [Fact]
        public async Task GetFeedEpisodesAsync_ReturnsTheRequestedPage_MarkingStoredAndUnimportableItems()
        {
            var series = await SeedSeriesAsync();
            Context.PodcastEpisodes.Add(new PodcastEpisode
            {
                Title = "Stored", MediaType = MediaType.Podcast, SeriesId = series.Id, RssGuid = "g1"
            });
            await Context.SaveChangesAsync();
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed(10));

            var page = await _service.GetFeedEpisodesAsync(series.Id, offset: 1, limit: 3);

            page.FeedItemCount.Should().Be(10);
            page.Offset.Should().Be(1);
            page.Limit.Should().Be(3);
            page.Items.Select(i => i.Guid).Should().Equal("g1", "g2", "g3");
            page.Items[0].ExistingEpisodeId.Should().NotBeNull();
            page.Items[1].ExistingEpisodeId.Should().BeNull();
            page.Items[1].Importable.Should().BeFalse();
            page.Items[2].Importable.Should().BeTrue();
            page.Items[2].Description!.Length.Should().Be(PodcastFeedBrowserService.DescriptionPreviewLength + 1);
        }

        [Theory]
        [InlineData(-5, 0, 0, 1)]
        [InlineData(0, 1000, 0, PodcastFeedBrowserService.MaxPageSize)]
        public async Task GetFeedEpisodesAsync_ClampsOffsetAndLimit(int offset, int limit, int expectedOffset, int expectedLimit)
        {
            var series = await SeedSeriesAsync();
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed(3));

            var page = await _service.GetFeedEpisodesAsync(series.Id, offset, limit);

            page.Offset.Should().Be(expectedOffset);
            page.Limit.Should().Be(expectedLimit);
        }

        [Fact]
        public async Task GetFeedEpisodesAsync_ReusesTheCachedFeed_UnlessRefreshIsAsked()
        {
            var series = await SeedSeriesAsync();
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed(5));

            var first = await _service.GetFeedEpisodesAsync(series.Id, 0, 2);
            var second = await _service.GetFeedEpisodesAsync(series.Id, 2, 2);
            await _reader.Received(1).ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>());
            second.FetchedAt.Should().Be(first.FetchedAt);

            await _service.GetFeedEpisodesAsync(series.Id, 0, 2, refresh: true);
            await _reader.Received(2).ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetFeedEpisodesAsync_UnknownSeries_OrNoFeed_Throws()
        {
            var noFeed = await SeedSeriesAsync(feedUrl: null);

            await ((Func<Task>)(() => _service.GetFeedEpisodesAsync(Guid.NewGuid(), 0, 10))).Should().ThrowAsync<KeyNotFoundException>();
            await ((Func<Task>)(() => _service.GetFeedEpisodesAsync(noFeed.Id, 0, 10))).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GetFeedEpisodesAsync_FeedFailure_Propagates_AndIsNotCached()
        {
            var series = await SeedSeriesAsync();
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.Timeout, "The feed took too long to respond."));

            var act = () => _service.GetFeedEpisodesAsync(series.Id, 0, 10);

            await act.Should().ThrowAsync<PodcastFeedException>();
            _cache.Count.Should().Be(0);
        }

        [Fact]
        public async Task ImportEpisodeFromFeedAsync_ByGuid_CreatesTheEpisode_ThenReturnsItAgain()
        {
            var series = await SeedSeriesAsync();
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed(5));

            var first = await _service.ImportEpisodeFromFeedAsync(series.Id, "g4", null);
            var second = await _service.ImportEpisodeFromFeedAsync(series.Id, " g4 ", null);

            first.Created.Should().BeTrue();
            first.Episode.Title.Should().Be("Episode 1");
            first.Episode.RssGuid.Should().Be("g4");
            first.Episode.AudioLink.Should().Be("https://cdn.example.com/4.mp3");
            second.Created.Should().BeFalse();
            second.Episode.Id.Should().Be(first.Episode.Id);
            (await Context.PodcastEpisodes.CountAsync()).Should().Be(1);
            await _reader.Received(1).ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportEpisodeFromFeedAsync_ByAudioUrl_WhenNoGuidIsGiven()
        {
            var series = await SeedSeriesAsync();
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed(5));

            var result = await _service.ImportEpisodeFromFeedAsync(series.Id, null, "http://www.cdn.example.com/3.mp3");

            result.Created.Should().BeTrue();
            result.Episode.RssGuid.Should().Be("g3");
        }

        [Fact]
        public async Task ImportEpisodeFromFeedAsync_RejectsMissingIdentity_UnknownItems_AndUnimportableItems()
        {
            var series = await SeedSeriesAsync();
            _reader.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed(5));

            await ((Func<Task>)(() => _service.ImportEpisodeFromFeedAsync(series.Id, " ", null))).Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => _service.ImportEpisodeFromFeedAsync(series.Id, "missing", null))).Should().ThrowAsync<KeyNotFoundException>();
            await ((Func<Task>)(() => _service.ImportEpisodeFromFeedAsync(series.Id, "g2", null))).Should().ThrowAsync<KeyNotFoundException>();
            (await Context.PodcastEpisodes.CountAsync()).Should().Be(0);
        }
    }
}
