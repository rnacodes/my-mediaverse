using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The from-feed import and directory search over HTTP, against real Postgres. The directory
    /// resolves an Apple id to a feed and the feed fills the series; both are substituted so no test
    /// reaches Apple or a feed host.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class PodcastFeedImportIntegrationTests : IAsyncLifetime
    {
        private const string FeedUrl = "https://feeds.example.com/darknet.xml";
        private const string AppleId = "1296350485";

        private readonly ApiFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public PodcastFeedImportIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        private Task<HttpResponseMessage> PostJson(HttpClient client, string url, object body) =>
            client.PostAsync(url, new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json"));

        private async Task<T> Read<T>(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), _jsonOptions)!;

        private static PodcastFeed Feed() => new()
        {
            Series = new FeedSeries
            {
                Title = "Darknet Diaries",
                Description = "True stories from the dark side of the Internet.",
                Publisher = "Jack Rhysider",
                ImageUrl = "https://feeds.example.com/art.jpg",
                Link = "https://darknetdiaries.com/",
                Language = "en-us",
                PodcastGuid = "ac631a54-a629-5367-a1a5-c2e2f7c25054",
                Categories = new[] { "Technology", "True Crime" }
            },
            TotalItemCount = 180
        };

        private static DirectoryPodcast AppleHit(string? feedUrl = FeedUrl) => new()
        {
            Title = "Darknet Diaries",
            Publisher = "Jack Rhysider",
            FeedUrl = feedUrl,
            ApplePodcastsId = AppleId,
            ArtworkUrl = "https://apple.example.com/600.jpg",
            Genres = new[] { "Technology" },
            EpisodeCount = 198,
            Source = DirectoryPodcast.AppleSource
        };

        private (HttpClient Client, IPodcastDirectory Directory, IPodcastFeedReader Reader) CreateClient(
            Action<IPodcastDirectory>? directory = null, Action<IPodcastFeedReader>? reader = null)
        {
            var (client, directoryMock, readerMock) = _factory.CreateClientWithSubstitutes(directory, reader);
            return (client, directoryMock, readerMock);
        }

        #region from-feed

        [Fact]
        public async Task FromFeed_NewFeed_Returns201_ThenSchemeAndSlashVariantsReturnTheSameSeriesWith200()
        {
            var (client, _, reader) = CreateClient(reader: r =>
                r.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed()));

            var first = await PostJson(client, "/api/podcast/series/from-feed", new ImportPodcastFromFeedDto { FeedUrl = FeedUrl });

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            first.Headers.Location.Should().NotBeNull();
            var created = await Read<PodcastFeedImportResultDto>(first);
            created.Created.Should().BeTrue();
            created.FeedRead.Should().BeTrue();
            created.WarningMessage.Should().BeNull();
            created.Series.Title.Should().Be("Darknet Diaries");
            created.Series.Publisher.Should().Be("Jack Rhysider");
            created.Series.MetadataSource.Should().Be("rss");
            created.Series.FeedGuid.Should().Be("ac631a54-a629-5367-a1a5-c2e2f7c25054");
            created.Series.TotalEpisodes.Should().Be(180);
            created.Series.IsSubscribed.Should().BeTrue();
            created.Series.Genres.Should().BeEquivalentTo("technology", "true crime");

            foreach (var variant in new[] { "http://feeds.example.com/darknet.xml", "https://www.feeds.example.com/darknet.xml/" })
            {
                var again = await PostJson(client, "/api/podcast/series/from-feed", new ImportPodcastFromFeedDto { FeedUrl = variant });

                again.StatusCode.Should().Be(HttpStatusCode.OK);
                var existing = await Read<PodcastFeedImportResultDto>(again);
                existing.Created.Should().BeFalse();
                existing.FeedRead.Should().BeFalse();
                existing.Series.Id.Should().Be(created.Series.Id);
            }

            // Probe before fetch: only the first import read the feed.
            await reader.Received(1).ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task FromFeed_AppleIdOnly_ResolvesTheFeedThroughTheDirectory()
        {
            var (client, _, reader) = CreateClient(
                d => d.LookupByAppleIdAsync(AppleId, Arg.Any<CancellationToken>()).Returns(AppleHit()),
                r => r.ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed()));

            var response = await PostJson(client, "/api/podcast/series/from-feed", new ImportPodcastFromFeedDto { ApplePodcastsId = AppleId });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await Read<PodcastFeedImportResultDto>(response);
            result.Series.ApplePodcastsId.Should().Be(AppleId);
            result.Series.RssFeedUrl.Should().Be(FeedUrl);
            result.Series.MetadataSource.Should().Be("rss");
            await reader.Received(1).ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task FromFeed_UnreachableFeed_Returns201StubWithWarning()
        {
            var (client, _, _) = CreateClient(reader: r =>
                r.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .ThrowsAsync(new PodcastFeedException(PodcastFeedFailureReason.Unreachable, "The feed's server could not be reached.")));

            var response = await PostJson(client, "/api/podcast/series/from-feed", new ImportPodcastFromFeedDto { FeedUrl = FeedUrl });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await Read<PodcastFeedImportResultDto>(response);
            result.Created.Should().BeTrue();
            result.FeedRead.Should().BeFalse();
            result.WarningMessage.Should().StartWith("The feed's server could not be reached.");
            result.Series.Title.Should().Be("Podcast at feeds.example.com");
            result.Series.MetadataSource.Should().Be("manual");
            result.Series.EnrichedAt.Should().BeNull();
            result.Series.LastEnrichmentAttemptAt.Should().NotBeNull();
        }

        [Fact]
        public async Task FromFeed_UnknownAppleId_Returns404WithErrorObject()
        {
            var (client, _, _) = CreateClient(d =>
                d.LookupByAppleIdAsync(AppleId, Arg.Any<CancellationToken>()).Returns((DirectoryPodcast?)null));

            var response = await PostJson(client, "/api/podcast/series/from-feed", new ImportPodcastFromFeedDto { ApplePodcastsId = AppleId });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task FromFeed_DirectoryUnavailable_Returns502()
        {
            var (client, _, _) = CreateClient(d =>
                d.LookupByAppleIdAsync(AppleId, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("Apple is down")));

            var response = await PostJson(client, "/api/podcast/series/from-feed", new ImportPodcastFromFeedDto { ApplePodcastsId = AppleId });

            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            (await response.Content.ReadAsStringAsync()).Should().NotContain("Apple is down");
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("ftp://feeds.example.com/darknet.xml", null)]
        [InlineData(null, "not-a-number")]
        public async Task FromFeed_MissingOrInvalidInput_Returns400WithErrorObject(string? feedUrl, string? appleId)
        {
            var (client, _, reader) = CreateClient();

            var response = await PostJson(client, "/api/podcast/series/from-feed",
                new ImportPodcastFromFeedDto { FeedUrl = feedUrl, ApplePodcastsId = appleId });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
            reader.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task FromFeed_Anonymous_Returns401()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await PostJson(client, "/api/podcast/series/from-feed", new ImportPodcastFromFeedDto { FeedUrl = FeedUrl });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region directory search

        [Theory]
        [InlineData("/api/podcast/directory/search")]
        [InlineData("/api/podcast/directory/search?term=%20%20")]
        public async Task DirectorySearch_BlankTerm_Returns400(string url)
        {
            var (client, directory, _) = CreateClient();

            var response = await client.GetAsync(url);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            directory.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task DirectorySearch_MarksShowsAlreadyInTheLibrary()
        {
            var (client, _, _) = CreateClient(d =>
                d.SearchAsync("darknet diaries", Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[]
                {
                    AppleHit(feedUrl: "http://feeds.example.com/darknet.xml"),
                    AppleHit(feedUrl: "https://other.example.com/feed") with { ApplePodcastsId = "42", Title = "Another Show" }
                }));

            var seeded = await PostJson(client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = "Darknet Diaries", RssFeedUrl = FeedUrl });
            seeded.StatusCode.Should().Be(HttpStatusCode.Created);
            var series = await Read<PodcastSeriesResponseDto>(seeded);

            var response = await client.GetAsync("/api/podcast/directory/search?term=darknet%20diaries");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await Read<List<PodcastDirectorySearchResultDto>>(response);
            results.Should().HaveCount(2);
            results[0].ExistingSeriesId.Should().Be(series.Id);
            results[0].ApplePodcastsId.Should().Be(AppleId);
            results[0].Source.Should().Be("apple");
            results[1].ExistingSeriesId.Should().BeNull();
        }

        [Fact]
        public async Task DirectorySearch_DirectoryUnavailable_Returns502()
        {
            var (client, _, _) = CreateClient(d =>
                d.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .ThrowsAsync(new HttpRequestException("Apple is down")));

            var response = await client.GetAsync("/api/podcast/directory/search?term=darknet");

            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        }

        #endregion
    }
}
