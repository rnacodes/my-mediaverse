using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastFeedImportServiceTests : InMemoryDbTestBase
    {
        private const string FeedUrl = "https://feeds.example.com/show.xml";
        private const string AppleId = "1296350485";

        private readonly IPodcastDirectory _directory = Substitute.For<IPodcastDirectory>();
        private readonly IPodcastFeedReader _feedReader = Substitute.For<IPodcastFeedReader>();
        private readonly PodcastFeedImportService _service;

        public PodcastFeedImportServiceTests()
        {
            _service = new PodcastFeedImportService(
                Context, _directory, _feedReader, Substitute.For<ILogger<PodcastFeedImportService>>());
        }

        private static PodcastFeed Feed(string? podcastGuid = "feed-guid-1") => new()
        {
            Series = new FeedSeries
            {
                Title = "Darknet Diaries",
                Description = "True stories from the dark side of the Internet.",
                Publisher = "Jack Rhysider",
                ImageUrl = "https://feeds.example.com/art.jpg",
                Link = "https://darknetdiaries.com/",
                Language = "en-us",
                PodcastGuid = podcastGuid,
                Categories = new[] { "Technology", "True Crime" }
            },
            TotalItemCount = 180
        };

        private static DirectoryPodcast AppleHit(string? feedUrl = FeedUrl) => new()
        {
            Title = "Darknet Diaries (Apple)",
            Publisher = "Jack Rhysider",
            FeedUrl = feedUrl,
            ApplePodcastsId = AppleId,
            ArtworkUrl = "https://apple.example.com/600.jpg",
            Genres = new[] { "Technology", "Tech News" },
            EpisodeCount = 198,
            Source = DirectoryPodcast.AppleSource
        };

        private async Task<PodcastSeries> SeedSeriesAsync(Action<PodcastSeries> configure)
        {
            var series = new PodcastSeries { Title = "Existing Show", MediaType = MediaType.Podcast };
            configure(series);
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return series;
        }

        #region Validation

        [Theory]
        [InlineData(null, null)]
        [InlineData("  ", "")]
        [InlineData("ftp://feeds.example.com/show.xml", null)]
        [InlineData("not a url", null)]
        [InlineData(null, "id123")]
        public async Task ImportSeriesFromFeedAsync_RejectsMissingOrInvalidInput(string? feedUrl, string? appleId)
        {
            var act = () => _service.ImportSeriesFromFeedAsync(feedUrl, appleId);

            await act.Should().ThrowAsync<ArgumentException>();
            _feedReader.ReceivedCalls().Should().BeEmpty();
            _directory.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_UnknownAppleId_ThrowsKeyNotFound()
        {
            _directory.LookupByAppleIdAsync(AppleId, Arg.Any<CancellationToken>()).Returns((DirectoryPodcast?)null);

            var act = () => _service.ImportSeriesFromFeedAsync(null, AppleId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        #endregion

        #region Create from feed

        [Fact]
        public async Task ImportSeriesFromFeedAsync_NewFeed_CreatesSeriesFilledFromTheFeed()
        {
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>()).Returns(Feed());

            var result = await _service.ImportSeriesFromFeedAsync(FeedUrl, null);

            result.Created.Should().BeTrue();
            result.FeedRead.Should().BeTrue();
            result.WarningMessage.Should().BeNull();

            var saved = await Context.PodcastSeries.Include(s => s.Genres).SingleAsync();
            saved.Id.Should().Be(result.Series.Id);
            saved.Title.Should().Be("Darknet Diaries");
            saved.Publisher.Should().Be("Jack Rhysider");
            saved.Thumbnail.Should().Be("https://feeds.example.com/art.jpg");
            saved.Language.Should().Be("en-us");
            saved.FeedGuid.Should().Be("feed-guid-1");
            saved.RssFeedUrl.Should().Be(FeedUrl);
            saved.FeedUrlKey.Should().Be("feeds.example.com/show.xml");
            saved.TotalEpisodes.Should().Be(180);
            saved.MetadataSource.Should().Be(PodcastMetadataSources.Rss);
            saved.IsSubscribed.Should().BeTrue();
            saved.EnrichedAt.Should().NotBeNull();
            saved.LastEnrichmentAttemptAt.Should().NotBeNull();
            saved.Genres.Select(g => g.Name).Should().BeEquivalentTo("technology", "true crime");

            // Only channel metadata is read: no episodes are created here.
            await _feedReader.Received(1).ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>());
            (await Context.PodcastEpisodes.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_AppleIdOnly_ResolvesTheFeedThroughTheDirectory()
        {
            _directory.LookupByAppleIdAsync(AppleId, Arg.Any<CancellationToken>()).Returns(AppleHit());
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>()).Returns(Feed());

            var result = await _service.ImportSeriesFromFeedAsync(null, $" {AppleId} ");

            result.Created.Should().BeTrue();
            var saved = await Context.PodcastSeries.Include(s => s.Genres).SingleAsync();
            saved.ApplePodcastsId.Should().Be(AppleId);
            saved.RssFeedUrl.Should().Be(FeedUrl);
            saved.Title.Should().Be("Darknet Diaries");
            saved.MetadataSource.Should().Be(PodcastMetadataSources.Rss);
            saved.Genres.Select(g => g.Name).Should().BeEquivalentTo("technology", "true crime", "tech news");
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_ReusesExistingGenres()
        {
            Context.Genres.Add(new Genre { Name = "technology" });
            await Context.SaveChangesAsync();
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>()).Returns(Feed());

            await _service.ImportSeriesFromFeedAsync(FeedUrl, null);

            (await Context.Genres.CountAsync(g => g.Name == "technology")).Should().Be(1);
        }

        #endregion

        #region Existing series

        [Theory]
        [InlineData("http://feeds.example.com/show.xml")]
        [InlineData("https://www.feeds.example.com/show.xml/")]
        public async Task ImportSeriesFromFeedAsync_FeedAlreadyInLibrary_ReturnsItWithoutFetching(string variant)
        {
            var existing = await SeedSeriesAsync(s =>
            {
                s.RssFeedUrl = FeedUrl;
                s.FeedUrlKey = "feeds.example.com/show.xml";
            });

            var result = await _service.ImportSeriesFromFeedAsync(variant, null);

            result.Created.Should().BeFalse();
            result.FeedRead.Should().BeFalse();
            result.Series.Id.Should().Be(existing.Id);
            _feedReader.ReceivedCalls().Should().BeEmpty();
            (await Context.PodcastSeries.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_FeedMatch_AbsorbsTheAppleId()
        {
            var existing = await SeedSeriesAsync(s =>
            {
                s.RssFeedUrl = FeedUrl;
                s.FeedUrlKey = "feeds.example.com/show.xml";
            });

            await _service.ImportSeriesFromFeedAsync(FeedUrl, AppleId);

            (await Context.PodcastSeries.SingleAsync(s => s.Id == existing.Id)).ApplePodcastsId.Should().Be(AppleId);
            _feedReader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_AppleIdAlreadyInLibrary_ReturnsItWithoutFetching()
        {
            var existing = await SeedSeriesAsync(s => s.ApplePodcastsId = AppleId);
            _directory.LookupByAppleIdAsync(AppleId, Arg.Any<CancellationToken>()).Returns(AppleHit());

            var result = await _service.ImportSeriesFromFeedAsync(null, AppleId);

            result.Created.Should().BeFalse();
            result.Series.Id.Should().Be(existing.Id);
            _feedReader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_ShowMovedHosts_MatchesByFeedGuid()
        {
            var existing = await SeedSeriesAsync(s =>
            {
                s.RssFeedUrl = "https://old-host.example.com/feed";
                s.FeedUrlKey = "old-host.example.com/feed";
                s.FeedGuid = "feed-guid-1";
            });
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>()).Returns(Feed());

            var result = await _service.ImportSeriesFromFeedAsync(FeedUrl, null);

            result.Created.Should().BeFalse();
            result.FeedRead.Should().BeTrue();
            result.Series.Id.Should().Be(existing.Id);
            var saved = await Context.PodcastSeries.SingleAsync();
            saved.Title.Should().Be("Existing Show");
            saved.Publisher.Should().Be("Jack Rhysider");
        }

        #endregion

        #region Feed URL lookup

        [Fact]
        public async Task ImportSeriesFromFeedAsync_FeedUrlOnly_DirectoryIdsFindAShowSavedByAppleId()
        {
            var existing = await SeedSeriesAsync(s => s.ApplePodcastsId = AppleId);
            _directory.LookupByFeedUrlAsync(FeedUrl, Arg.Any<CancellationToken>())
                .Returns(AppleHit() with { PodcastIndexId = 42, Source = DirectoryPodcast.PodcastIndexSource });

            var result = await _service.ImportSeriesFromFeedAsync(FeedUrl, null);

            result.Created.Should().BeFalse();
            result.Series.Id.Should().Be(existing.Id);
            _feedReader.ReceivedCalls().Should().BeEmpty();
            var saved = await Context.PodcastSeries.SingleAsync();
            saved.RssFeedUrl.Should().Be(FeedUrl);
            saved.PodcastIndexId.Should().Be(42);
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_FeedUrlLookupFailure_DoesNotStopTheImport()
        {
            _directory.LookupByFeedUrlAsync(FeedUrl, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("down"));
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>()).Returns(Feed());

            var result = await _service.ImportSeriesFromFeedAsync(FeedUrl, null);

            result.Created.Should().BeTrue();
            result.FeedRead.Should().BeTrue();
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_FeedUrlGivenWithAppleId_SkipsTheFeedUrlLookup()
        {
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>()).Returns(Feed());

            await _service.ImportSeriesFromFeedAsync(FeedUrl, AppleId);

            await _directory.DidNotReceive().LookupByFeedUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_UnreachableFeedWithPodcastIndexHit_IsAPodcastIndexStub()
        {
            _directory.LookupByFeedUrlAsync(FeedUrl, Arg.Any<CancellationToken>())
                .Returns(AppleHit() with { PodcastIndexId = 42, Source = DirectoryPodcast.PodcastIndexSource });
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.HttpError, "The feed returned HTTP 500."));

            var result = await _service.ImportSeriesFromFeedAsync(FeedUrl, null);

            result.Created.Should().BeTrue();
            var saved = await Context.PodcastSeries.SingleAsync();
            saved.MetadataSource.Should().Be(PodcastMetadataSources.PodcastIndex);
            saved.PodcastIndexId.Should().Be(42);
            saved.ApplePodcastsId.Should().Be(AppleId);
            saved.Title.Should().Be("Darknet Diaries (Apple)");
        }

        #endregion

        #region Unreadable feeds

        [Fact]
        public async Task ImportSeriesFromFeedAsync_UnreachableFeed_SavesAStubWithAWarning()
        {
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.HttpError, "The feed returned HTTP 404."));

            var result = await _service.ImportSeriesFromFeedAsync(FeedUrl, null);

            result.Created.Should().BeTrue();
            result.FeedRead.Should().BeFalse();
            result.WarningMessage.Should().StartWith("The feed returned HTTP 404.");

            var saved = await Context.PodcastSeries.SingleAsync();
            saved.Title.Should().Be("Podcast at feeds.example.com");
            saved.MetadataSource.Should().Be(PodcastMetadataSources.Manual);
            saved.EnrichedAt.Should().BeNull();
            saved.LastEnrichmentAttemptAt.Should().NotBeNull();
            saved.FeedUrlKey.Should().Be("feeds.example.com/show.xml");
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_UnreachableFeedWithAppleHit_UsesDirectoryDetails()
        {
            _directory.LookupByAppleIdAsync(AppleId, Arg.Any<CancellationToken>()).Returns(AppleHit());
            _feedReader.ReadAsync(FeedUrl, 0, Arg.Any<CancellationToken>())
                .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.Timeout, "The feed took too long to respond."));

            var result = await _service.ImportSeriesFromFeedAsync(null, AppleId);

            result.WarningMessage.Should().NotBeNull();
            var saved = await Context.PodcastSeries.Include(s => s.Genres).SingleAsync();
            saved.Title.Should().Be("Darknet Diaries (Apple)");
            saved.Thumbnail.Should().Be("https://apple.example.com/600.jpg");
            saved.TotalEpisodes.Should().Be(198);
            saved.MetadataSource.Should().Be(PodcastMetadataSources.Apple);
            saved.EnrichedAt.Should().BeNull();
            saved.Genres.Select(g => g.Name).Should().BeEquivalentTo("technology", "tech news");
        }

        [Fact]
        public async Task ImportSeriesFromFeedAsync_AppleListsNoFeed_SavesDirectoryStubWithoutFetching()
        {
            _directory.LookupByAppleIdAsync(AppleId, Arg.Any<CancellationToken>()).Returns(AppleHit(feedUrl: null));

            var result = await _service.ImportSeriesFromFeedAsync(null, AppleId);

            result.Created.Should().BeTrue();
            result.WarningMessage.Should().Contain("no feed");
            _feedReader.ReceivedCalls().Should().BeEmpty();
            var saved = await Context.PodcastSeries.SingleAsync();
            saved.RssFeedUrl.Should().BeNull();
            saved.FeedUrlKey.Should().BeNull();
            saved.MetadataSource.Should().Be(PodcastMetadataSources.Apple);
        }

        #endregion

        #region Directory search

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchDirectoryAsync_BlankTerm_Throws(string term)
        {
            var act = () => _service.SearchDirectoryAsync(term, 10);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SearchDirectoryAsync_MarksShowsAlreadyInTheLibrary()
        {
            var byFeed = await SeedSeriesAsync(s =>
            {
                s.RssFeedUrl = FeedUrl;
                s.FeedUrlKey = "feeds.example.com/show.xml";
            });
            var byApple = await SeedSeriesAsync(s => s.ApplePodcastsId = "555");
            _directory.SearchAsync("darknet", 10, Arg.Any<CancellationToken>()).Returns(new[]
            {
                AppleHit(feedUrl: "http://feeds.example.com/show.xml") with { ApplePodcastsId = "1" },
                AppleHit(feedUrl: "https://other.example.com/feed") with { ApplePodcastsId = "555" },
                AppleHit(feedUrl: "https://new.example.com/feed") with { ApplePodcastsId = "2" }
            });

            var hits = await _service.SearchDirectoryAsync(" darknet ", 10);

            hits.Select(h => h.ExistingSeriesId).Should().Equal(byFeed.Id, byApple.Id, null);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(500, PodcastFeedImportService.MaxSearchLimit)]
        public async Task SearchDirectoryAsync_ClampsTheLimit(int requested, int expected)
        {
            _directory.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Array.Empty<DirectoryPodcast>());

            await _service.SearchDirectoryAsync("darknet", requested);

            await _directory.Received(1).SearchAsync("darknet", expected, Arg.Any<CancellationToken>());
        }

        #endregion
    }
}
