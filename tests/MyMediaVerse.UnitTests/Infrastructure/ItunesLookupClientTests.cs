using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Clients.Itunes;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class ItunesLookupClientTests
    {
        private readonly TestHttpMessageHandler _mockHttpMessageHandler;
        private readonly ItunesLookupClient _client;

        public ItunesLookupClientTests()
        {
            _mockHttpMessageHandler = new TestHttpMessageHandler();
            var httpClient = new HttpClient(_mockHttpMessageHandler)
            {
                BaseAddress = new Uri("https://itunes.apple.com/")
            };
            var logger = Substitute.For<ILogger<ItunesLookupClient>>();
            _client = new ItunesLookupClient(httpClient, logger);
        }

        [Fact]
        public async Task GetPodcastByCollectionIdAsync_ShouldRequestLookupUrl_AndMapPodcastFields()
        {
            // Arrange — a representative iTunes Lookup response (Apple's external casing).
            const string json = """
            {
              "resultCount": 1,
              "results": [
                {
                  "kind": "podcast",
                  "collectionId": 1200361736,
                  "collectionName": "The Daily",
                  "artistName": "The New York Times",
                  "feedUrl": "https://feeds.simplecast.com/Sl5CSM3S",
                  "artworkUrl600": "https://example.com/600x600bb.jpg",
                  "trackCount": 2652,
                  "collectionViewUrl": "https://podcasts.apple.com/us/podcast/the-daily/id1200361736",
                  "primaryGenreName": "Daily News"
                }
              ]
            }
            """;
            _mockHttpMessageHandler.RespondWith(HttpStatusCode.OK, json);

            // Act
            var result = await _client.GetPodcastByCollectionIdAsync("1200361736");

            // Assert
            result.Should().NotBeNull();
            result!.FeedUrl.Should().Be("https://feeds.simplecast.com/Sl5CSM3S");
            result.ArtistName.Should().Be("The New York Times");
            result.CollectionName.Should().Be("The Daily");
            result.TrackCount.Should().Be(2652);

            var request = _mockHttpMessageHandler.Requests.Should().ContainSingle().Subject;
            request.Method.Should().Be(HttpMethod.Get);
            var uri = request.RequestUri!.ToString();
            uri.Should().Contain("lookup?id=1200361736");
            uri.Should().Contain("entity=podcast");
        }

        [Fact]
        public async Task GetPodcastByCollectionIdAsync_ShouldReturnNull_WhenNoResults()
        {
            // Arrange
            _mockHttpMessageHandler.RespondWith(HttpStatusCode.OK, """{ "resultCount": 0, "results": [] }""");

            // Act
            var result = await _client.GetPodcastByCollectionIdAsync("0");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPodcastByCollectionIdAsync_ShouldThrow_WhenHttpRequestFails()
        {
            // Arrange
            _mockHttpMessageHandler.RespondWith(HttpStatusCode.InternalServerError, "boom");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetPodcastByCollectionIdAsync("1200361736"));
        }

        [Fact]
        public async Task SearchPodcastsAsync_ShouldRequestSearchUrl_AndMapGenresAndReleaseDate()
        {
            // Arrange — trimmed from a real Search API response.
            const string json = """
            {
              "resultCount": 2,
              "results": [
                {
                  "wrapperType": "track",
                  "kind": "podcast",
                  "collectionId": 1296350485,
                  "collectionName": "Darknet Diaries",
                  "artistName": "Jack Rhysider",
                  "feedUrl": "https://podcast.darknetdiaries.com",
                  "artworkUrl600": "https://example.com/600x600bb.jpg",
                  "trackCount": 198,
                  "releaseDate": "2026-09-01T07:00:00Z",
                  "primaryGenreName": "Technology",
                  "genreIds": ["1318", "26"],
                  "genres": ["Technology", "Podcasts"]
                },
                {
                  "wrapperType": "track",
                  "kind": "podcast-episode",
                  "collectionId": 1,
                  "collectionName": "Not a show"
                }
              ]
            }
            """;
            _mockHttpMessageHandler.RespondWith(HttpStatusCode.OK, json);

            // Act
            var results = await _client.SearchPodcastsAsync("darknet diaries & more", 10);

            // Assert
            var podcast = results.Should().ContainSingle().Subject;
            podcast.CollectionName.Should().Be("Darknet Diaries");
            podcast.Genres.Should().Equal("Technology", "Podcasts");
            podcast.GenreIds.Should().Equal("1318", "26");
            podcast.ReleaseDate.Should().Be(new DateTime(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc));

            var uri = _mockHttpMessageHandler.Requests.Should().ContainSingle().Subject.RequestUri!;
            uri.AbsolutePath.Should().Be("/search");
            uri.Query.Should().Contain("media=podcast")
                .And.Contain("entity=podcast")
                .And.Contain("term=darknet%20diaries%20%26%20more")
                .And.Contain("limit=10");
        }

        [Fact]
        public async Task SearchPodcastsAsync_ShouldReturnEmpty_WhenNoResults()
        {
            _mockHttpMessageHandler.RespondWith(HttpStatusCode.OK, """{ "resultCount": 0, "results": [] }""");

            var results = await _client.SearchPodcastsAsync("nothing", 5);

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchPodcastsAsync_ShouldThrow_WhenHttpRequestFails()
        {
            _mockHttpMessageHandler.RespondWith(HttpStatusCode.ServiceUnavailable, "down");

            await Assert.ThrowsAsync<HttpRequestException>(() => _client.SearchPodcastsAsync("darknet", 5));
        }
    }
}
