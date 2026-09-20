using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Clients.PodcastIndex;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class PodcastIndexClientTests
    {
        private const string FeedJson = """
        {
          "id": 920666,
          "podcastGuid": "ac631a54-a629-5367-a1a5-c2e2f7c25054",
          "title": "Darknet Diaries",
          "url": "https://podcast.darknetdiaries.com",
          "link": "https://darknetdiaries.com/",
          "description": "True stories from the dark side of the Internet.",
          "author": "Jack Rhysider",
          "ownerName": "Jack Rhysider",
          "image": "https://example.com/image.jpg",
          "artwork": "https://example.com/artwork.jpg",
          "itunesId": 1296350485,
          "language": "en-us",
          "categories": { "102": "Technology", "55": "News" },
          "episodeCount": "180",
          "dead": 0,
          "newestItemPublishTime": 1788246000
        }
        """;

        private readonly TestHttpMessageHandler _handler = new();

        private PodcastIndexClient CreateClient(string? key = "KEY", string? secret = "SECRET") =>
            new(new HttpClient(_handler) { BaseAddress = new Uri(PodcastIndexClient.BaseUrl) },
                new PodcastIndexCredentials(key, secret),
                Substitute.For<ILogger<PodcastIndexClient>>());

        [Fact]
        public async Task SearchByTermAsync_RequestsTheSearchEndpoint_AndParsesFeeds()
        {
            _handler.RespondWith(HttpStatusCode.OK, $$"""{ "status": "true", "feeds": [ {{FeedJson}} ], "count": 1 }""");

            var feeds = await CreateClient().SearchByTermAsync("darknet diaries", 5);

            var feed = feeds.Should().ContainSingle().Subject;
            feed.Id.Should().Be(920666);
            feed.PodcastGuid.Should().Be("ac631a54-a629-5367-a1a5-c2e2f7c25054");
            feed.Url.Should().Be("https://podcast.darknetdiaries.com");
            feed.Author.Should().Be("Jack Rhysider");
            feed.Artwork.Should().Be("https://example.com/artwork.jpg");
            feed.ItunesId.Should().Be(1296350485);
            feed.Categories.Should().BeEquivalentTo("Technology", "News");
            feed.EpisodeCount.Should().Be(180);
            feed.Dead.Should().BeFalse();
            feed.NewestItemPublishedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1788246000).UtcDateTime);

            var uri = _handler.Requests.Should().ContainSingle().Subject.RequestUri!;
            uri.AbsoluteUri.Should().StartWith("https://api.podcastindex.org/api/1.0/search/byterm?");
            uri.Query.Should().Contain("q=darknet%20diaries").And.Contain("max=5");
        }

        [Fact]
        public async Task GetByFeedUrlAsync_ParsesTheFeedObject()
        {
            _handler.RespondWith(HttpStatusCode.OK, $$"""{ "status": "true", "feed": {{FeedJson}} }""");

            var feed = await CreateClient().GetByFeedUrlAsync("https://podcast.darknetdiaries.com");

            feed!.Title.Should().Be("Darknet Diaries");
            _handler.Requests.Single().RequestUri!.Query
                .Should().Contain("url=https%3A%2F%2Fpodcast.darknetdiaries.com");
        }

        [Theory]
        [InlineData("""{ "status": "true", "feed": [], "description": "No feeds match this url." }""")]
        [InlineData("""{ "status": "false", "description": "Not found" }""")]
        public async Task GetByItunesIdAsync_NotFoundShapes_ReturnNull(string body)
        {
            _handler.RespondWith(HttpStatusCode.OK, body);

            var feed = await CreateClient().GetByItunesIdAsync(1);

            feed.Should().BeNull();
            _handler.Requests.Single().RequestUri!.Query.Should().Contain("id=1");
        }

        [Fact]
        public async Task Http404_ReturnsNull()
        {
            _handler.RespondWith(HttpStatusCode.NotFound, "{}");

            (await CreateClient().GetByFeedUrlAsync("https://example.com/feed")).Should().BeNull();
        }

        [Fact]
        public async Task Unauthorized_Throws()
        {
            _handler.RespondWith(HttpStatusCode.Unauthorized, "{}");

            var act = () => CreateClient().GetByFeedUrlAsync("https://example.com/feed");

            (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task NotConfigured_MakesNoRequests()
        {
            var client = CreateClient(key: null);

            client.IsConfigured.Should().BeFalse();
            (await client.SearchByTermAsync("darknet", 5)).Should().BeEmpty();
            (await client.GetByFeedUrlAsync("https://example.com/feed")).Should().BeNull();
            (await client.GetByItunesIdAsync(1)).Should().BeNull();
            _handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task AuthHandler_SignsEachRequest()
        {
            var credentials = new PodcastIndexCredentials("KEY", "SECRET");
            var client = new HttpClient(new PodcastIndexAuthHandler(credentials) { InnerHandler = _handler })
            {
                BaseAddress = new Uri(PodcastIndexClient.BaseUrl)
            };

            await client.GetAsync("search/byterm?q=x");

            var request = _handler.Requests.Single();
            request.Headers.GetValues("X-Auth-Key").Should().Equal("KEY");
            var date = long.Parse(request.Headers.GetValues("X-Auth-Date").Single());
            date.Should().BeCloseTo(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 5);
            request.Headers.GetValues("Authorization").Single()
                .Should().Be(PodcastIndexAuthHandler.BuildAuthorization("KEY", "SECRET", date));
        }

        [Fact]
        public void BuildAuthorization_IsLowercaseHexSha1OfKeySecretAndDate()
        {
            // Example key and secret from the Podcast Index API documentation.
            PodcastIndexAuthHandler.BuildAuthorization("UXKCGDSYGUUEVQJSYDZH", "yzJe2eE7XV-3eY576dyRZ6wXyAbndh6LUrCZ8KN|", 1613713388)
                .Should().Be("73a1fffed61c1d30d858beb1fc48f355386449d2");
        }

        [Fact]
        public async Task AuthHandler_WithoutCredentials_AddsNoAuthHeaders()
        {
            var client = new HttpClient(new PodcastIndexAuthHandler(new PodcastIndexCredentials(null, null)) { InnerHandler = _handler })
            {
                BaseAddress = new Uri(PodcastIndexClient.BaseUrl)
            };

            await client.GetAsync("search/byterm?q=x");

            _handler.Requests.Single().Headers.Contains("X-Auth-Key").Should().BeFalse();
        }
    }
}
