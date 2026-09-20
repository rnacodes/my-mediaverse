using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// A podcast series' identity is its feed. These tests pin the HTTP contract that follows: a
    /// second save of the same show returns the first row (200, not 201), an edit that would take
    /// another series' feed is a 409, the OPML import requires a token, the generic media endpoint
    /// refuses podcasts, and deleting a series removes its episodes completely — against real
    /// Postgres, where a partial delete used to leave rows that broke every all-media query.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class PodcastIdentityIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public PodcastIdentityIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
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

        [Fact]
        public async Task CreateSeries_SameFeedInAnyVariant_ReturnsExistingRowWith200()
        {
            var first = await PostJson(_client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = "Darknet Diaries", RssFeedUrl = "https://feeds.megaphone.fm/darknetdiaries" });
            first.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await Read<PodcastSeriesResponseDto>(first);

            var second = await PostJson(_client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = "Darknet Diaries", RssFeedUrl = "http://www.feeds.megaphone.fm/darknetdiaries/", ApplePodcastsId = "1296350485" });

            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var existing = await Read<PodcastSeriesResponseDto>(second);
            existing.Id.Should().Be(created.Id);
            existing.ApplePodcastsId.Should().Be("1296350485");
            existing.MetadataSource.Should().Be("manual");
        }

        [Fact]
        public async Task CreateSeries_InvalidFeedUrl_Returns400WithErrorObject()
        {
            var response = await PostJson(_client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = "Bad", RssFeedUrl = "not a url" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task UpdateSeries_TakingAnotherSeriesFeed_Returns409()
        {
            await PostJson(_client, "/api/podcast/series", new CreatePodcastSeriesDto { Title = "A", RssFeedUrl = "https://a.example.com/feed" });
            var b = await Read<PodcastSeriesResponseDto>(await PostJson(_client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = "B", RssFeedUrl = "https://b.example.com/feed" }));

            var response = await _client.PutAsync($"/api/podcast/series/{b.Id}",
                new StringContent(JsonSerializer.Serialize(
                    new CreatePodcastSeriesDto { Title = "B", RssFeedUrl = "https://a.example.com/feed/" }, _jsonOptions),
                    Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await Read<JsonElement>(response)).GetProperty("error").GetString().Should().Contain("feed URL");
        }

        [Fact]
        public async Task CreateEpisode_SameGuidInSeries_ReturnsExistingEpisodeWith200()
        {
            var series = await Read<PodcastSeriesResponseDto>(await PostJson(_client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = "Show" }));
            var dto = new CreatePodcastEpisodeDto { Title = "Episode 1", SeriesId = series.Id, RssGuid = "item-1" };

            var first = await PostJson(_client, "/api/podcast/episodes", dto);
            var second = await PostJson(_client, "/api/podcast/episodes", dto);

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Read<PodcastEpisodeResponseDto>(second)).Id.Should().Be((await Read<PodcastEpisodeResponseDto>(first)).Id);
        }

        [Fact]
        public async Task DeleteSeries_WithEpisodes_LeavesNoOrphanedMediaItems_AndCleansTheSearchIndex()
        {
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();
            var series = await Read<PodcastSeriesResponseDto>(await PostJson(client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = "Show With Episodes" }));
            var ep1 = await Read<PodcastEpisodeResponseDto>(await PostJson(client, "/api/podcast/episodes",
                new CreatePodcastEpisodeDto { Title = "One", SeriesId = series.Id }));
            var ep2 = await Read<PodcastEpisodeResponseDto>(await PostJson(client, "/api/podcast/episodes",
                new CreatePodcastEpisodeDto { Title = "Two", SeriesId = series.Id }));

            var delete = await client.DeleteAsync($"/api/podcast/series/{series.Id}");

            delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Every all-media query materializes each row by its type; an orphaned base row made
            // this endpoint return 500.
            var allMedia = await client.GetAsync("/api/media");
            allMedia.StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.GetAsync($"/api/podcast/episodes/{ep1.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            await typesense.Received(1).DeleteMediaItemAsync(series.Id);
            await typesense.Received(1).DeleteMediaItemAsync(ep1.Id);
            await typesense.Received(1).DeleteMediaItemAsync(ep2.Id);
        }

        [Fact]
        public async Task DeleteSeriesThroughTheGenericMediaEndpoint_LeavesNoOrphanedMediaItems()
        {
            // The Edit Media form deletes through /api/media/{id}, not the podcast endpoint.
            var series = await Read<PodcastSeriesResponseDto>(await PostJson(_client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = "Deleted From Edit Form" }));
            await PostJson(_client, "/api/podcast/episodes", new CreatePodcastEpisodeDto { Title = "One", SeriesId = series.Id });

            var delete = await _client.DeleteAsync($"/api/media/{series.Id}");

            delete.IsSuccessStatusCode.Should().BeTrue();
            (await _client.GetAsync("/api/media")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateMediaItem_WithPodcastType_Returns400PointingToThePodcastEndpoint()
        {
            var response = await PostJson(_client, "/api/media",
                new CreateMediaItemDto { Title = "Generic Podcast", MediaType = MediaType.Podcast, Status = Status.Uncharted });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await Read<JsonElement>(response)).GetProperty("error").GetString().Should().Contain("/api/podcast/series");
        }

        [Theory]
        [InlineData("/api/podcast/series/from-opml")]
        [InlineData("/api/podcast/series/00000000-0000-0000-0000-000000000001/sync")]
        public async Task ImportAndSyncEndpoints_WithoutToken_Return401(string url)
        {
            var response = await _factory.CreateAnonymousClient().PostAsync(url, new StringContent(""));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetSeries_NotFound_ReturnsErrorObject()
        {
            var response = await _client.GetAsync($"/api/podcast/series/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
        }
    }
}
