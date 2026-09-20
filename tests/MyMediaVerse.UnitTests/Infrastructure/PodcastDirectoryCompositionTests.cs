using System.Net;
using System.Threading.RateLimiting;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Infrastructure.Clients.Itunes;
using MyMediaVerse.Infrastructure.Services.Podcasts;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.DTOs.PodcastIndex;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// The pieces that make up the application's podcast directory: the Podcast Index directory, the
    /// composite that puts Apple first with Podcast Index as fallback, Apple's cache, and the Apple rate limit.
    /// </summary>
    [Trait("Category", "Unit")]
    public class PodcastDirectoryCompositionTests
    {
        private static DirectoryPodcast Apple(string? feedUrl = "https://feeds.example.com/show") => new()
        {
            Title = "Show", ApplePodcastsId = "123", FeedUrl = feedUrl, Source = DirectoryPodcast.AppleSource
        };

        private static DirectoryPodcast Index(string feedUrl = "https://index.example.com/show") => new()
        {
            Title = "Show", ApplePodcastsId = "123", FeedUrl = feedUrl, PodcastIndexId = 42, Source = DirectoryPodcast.PodcastIndexSource
        };

        #region PodcastIndexDirectory

        [Fact]
        public void PodcastIndexDirectory_Map_CopiesTheFeed()
        {
            var podcast = PodcastIndexDirectory.Map(new PodcastIndexFeedDto
            {
                Id = 42, Title = "Show", Url = "https://feeds.example.com/show", ItunesId = 123,
                OwnerName = "Owner", Image = "https://example.com/i.jpg", Categories = new[] { "Technology" },
                EpisodeCount = 10, NewestItemPublishedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
            })!;

            podcast.PodcastIndexId.Should().Be(42);
            podcast.ApplePodcastsId.Should().Be("123");
            podcast.FeedUrl.Should().Be("https://feeds.example.com/show");
            podcast.Publisher.Should().Be("Owner");
            podcast.ArtworkUrl.Should().Be("https://example.com/i.jpg");
            podcast.Genres.Should().Equal("Technology");
            podcast.EpisodeCount.Should().Be(10);
            podcast.LatestReleaseDate.Should().Be(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
            podcast.Source.Should().Be(DirectoryPodcast.PodcastIndexSource);

            PodcastIndexDirectory.Map(new PodcastIndexFeedDto { Id = 0, Title = "No id" }).Should().BeNull();
        }

        [Fact]
        public async Task PodcastIndexDirectory_SearchSkipsDeadFeeds_AndLookupRejectsNonNumericAppleIds()
        {
            var client = Substitute.For<IPodcastIndexClient>();
            client.SearchByTermAsync("show", 5, Arg.Any<CancellationToken>()).Returns(new[]
            {
                new PodcastIndexFeedDto { Id = 1, Title = "Alive" },
                new PodcastIndexFeedDto { Id = 2, Title = "Dead", Dead = true }
            });
            var directory = new PodcastIndexDirectory(client);

            (await directory.SearchAsync("show", 5)).Should().ContainSingle().Which.Title.Should().Be("Alive");
            (await directory.LookupByAppleIdAsync("abc")).Should().BeNull();
            await client.DidNotReceive().GetByItunesIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region CompositePodcastDirectory

        private readonly IPodcastDirectory _apple = Substitute.For<IPodcastDirectory>();
        private readonly IPodcastDirectory _index = Substitute.For<IPodcastDirectory>();

        private CompositePodcastDirectory Composite(bool withFallback = true) =>
            new(_apple, withFallback ? _index : null, Substitute.For<ILogger<CompositePodcastDirectory>>());

        [Fact]
        public async Task Search_UsesApple_AndFallsBackOnlyWhenAppleFails()
        {
            _apple.SearchAsync("show", 5, Arg.Any<CancellationToken>()).Returns(new[] { Apple() });
            (await Composite().SearchAsync("show", 5)).Should().ContainSingle().Which.Source.Should().Be(DirectoryPodcast.AppleSource);
            await _index.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

            _apple.SearchAsync("down", 5, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("Apple down"));
            _index.SearchAsync("down", 5, Arg.Any<CancellationToken>()).Returns(new[] { Index() });
            (await Composite().SearchAsync("down", 5)).Should().ContainSingle().Which.Source.Should().Be(DirectoryPodcast.PodcastIndexSource);
        }

        [Fact]
        public async Task Search_WithoutFallback_RethrowsAppleFailure()
        {
            _apple.SearchAsync("down", 5, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("Apple down"));

            var act = () => Composite(withFallback: false).SearchAsync("down", 5);

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task LookupByAppleId_AppleHitWithAFeed_DoesNotAskPodcastIndex()
        {
            _apple.LookupByAppleIdAsync("123", Arg.Any<CancellationToken>()).Returns(Apple());

            (await Composite().LookupByAppleIdAsync("123"))!.FeedUrl.Should().Be("https://feeds.example.com/show");
            await _index.DidNotReceive().LookupByAppleIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task LookupByAppleId_AppleHitWithoutAFeed_TakesTheFeedFromPodcastIndex()
        {
            _apple.LookupByAppleIdAsync("123", Arg.Any<CancellationToken>()).Returns(Apple(feedUrl: null));
            _index.LookupByAppleIdAsync("123", Arg.Any<CancellationToken>()).Returns(Index());

            var hit = (await Composite().LookupByAppleIdAsync("123"))!;

            hit.Source.Should().Be(DirectoryPodcast.AppleSource);
            hit.FeedUrl.Should().Be("https://index.example.com/show");
            hit.PodcastIndexId.Should().Be(42);
        }

        [Fact]
        public async Task LookupByAppleId_AppleMissOrFailure_UsesPodcastIndex()
        {
            _apple.LookupByAppleIdAsync("123", Arg.Any<CancellationToken>()).Returns((DirectoryPodcast?)null);
            _index.LookupByAppleIdAsync("123", Arg.Any<CancellationToken>()).Returns(Index());
            (await Composite().LookupByAppleIdAsync("123"))!.Source.Should().Be(DirectoryPodcast.PodcastIndexSource);

            _apple.LookupByAppleIdAsync("456", Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("Apple down"));
            _index.LookupByAppleIdAsync("456", Arg.Any<CancellationToken>()).Returns(Index());
            (await Composite().LookupByAppleIdAsync("456"))!.PodcastIndexId.Should().Be(42);
        }

        [Fact]
        public async Task LookupByAppleId_BothFail_RethrowsTheAppleError_ButAFallbackErrorNeverHidesAnAppleHit()
        {
            _apple.LookupByAppleIdAsync("123", Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("Apple down"));
            _index.LookupByAppleIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("Index down"));
            (await ((Func<Task>)(() => Composite().LookupByAppleIdAsync("123"))).Should().ThrowAsync<HttpRequestException>())
                .WithMessage("Apple down");

            _apple.LookupByAppleIdAsync("789", Arg.Any<CancellationToken>()).Returns(Apple(feedUrl: null));
            (await Composite().LookupByAppleIdAsync("789"))!.FeedUrl.Should().BeNull();
        }

        [Fact]
        public async Task LookupByFeedUrl_AsksPodcastIndex_BestEffort()
        {
            _apple.LookupByFeedUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DirectoryPodcast?)null);
            _index.LookupByFeedUrlAsync("https://index.example.com/show", Arg.Any<CancellationToken>()).Returns(Index());
            _index.LookupByFeedUrlAsync("https://broken.example.com", Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("down"));

            (await Composite().LookupByFeedUrlAsync("https://index.example.com/show"))!.PodcastIndexId.Should().Be(42);
            (await Composite().LookupByFeedUrlAsync("https://broken.example.com")).Should().BeNull();
            (await Composite(withFallback: false).LookupByFeedUrlAsync("https://index.example.com/show")).Should().BeNull();
        }

        #endregion

        #region Apple cache and rate limit

        private static ItunesPodcastDto ItunesShow() => new()
        {
            Kind = "podcast", CollectionId = 123, CollectionName = "Show", FeedUrl = "https://feeds.example.com/show"
        };

        [Fact]
        public async Task AppleDirectory_CachesSearchesAndLookups_IncludingMisses()
        {
            var client = Substitute.For<IItunesLookupClient>();
            client.SearchPodcastsAsync(Arg.Any<string>(), 5, Arg.Any<CancellationToken>()).Returns(new[] { ItunesShow() });
            client.GetPodcastByCollectionIdAsync("123", Arg.Any<CancellationToken>()).Returns(ItunesShow());
            client.GetPodcastByCollectionIdAsync("999", Arg.Any<CancellationToken>()).Returns((ItunesPodcastDto?)null);
            var directory = new ApplePodcastDirectory(client, new MemoryCache(new MemoryCacheOptions()), Options.Create(new PodcastDirectoryOptions()));

            await directory.SearchAsync("Show ", 5);
            await directory.SearchAsync("show", 5);
            await directory.LookupByAppleIdAsync("123");
            await directory.LookupByAppleIdAsync("123");
            (await directory.LookupByAppleIdAsync("999")).Should().BeNull();
            (await directory.LookupByAppleIdAsync("999")).Should().BeNull();

            await client.Received(1).SearchPodcastsAsync(Arg.Any<string>(), 5, Arg.Any<CancellationToken>());
            await client.Received(1).GetPodcastByCollectionIdAsync("123", Arg.Any<CancellationToken>());
            await client.Received(1).GetPodcastByCollectionIdAsync("999", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ItunesRateLimitHandler_RefusesCallsBeyondTheBucketAsBusy()
        {
            using var limiter = new ItunesRateLimiter(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = false
            }));
            var inner = new TestHttpMessageHandler();
            inner.RespondWith(HttpStatusCode.OK, "{}");
            var client = new HttpClient(new ItunesRateLimitHandler(limiter) { InnerHandler = inner })
            {
                BaseAddress = new Uri("https://itunes.apple.com/")
            };

            (await client.GetAsync("search?term=a")).StatusCode.Should().Be(HttpStatusCode.OK);
            var act = () => client.GetAsync("search?term=b");

            (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            inner.Requests.Should().ContainSingle();
        }

        #endregion
    }
}
