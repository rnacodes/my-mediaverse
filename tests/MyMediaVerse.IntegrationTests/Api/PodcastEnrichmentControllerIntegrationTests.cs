using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Podcast enrichment over HTTP: the batch run, and the single-series re-fill on the podcast
    /// controller. The directory and feed reader are substituted, so nothing reaches Apple or a feed host.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class PodcastEnrichmentControllerIntegrationTests : IAsyncLifetime
    {
        private const string FeedUrl = "https://feeds.example.com/daily.xml";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public PodcastEnrichmentControllerIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region helpers

        private Task<HttpResponseMessage> PostJson(HttpClient client, string url, object body) =>
            client.PostAsync(url, new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json"));

        private async Task<T> Read<T>(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), _jsonOptions)!;

        private static PodcastFeed Feed() => new()
        {
            Series = new FeedSeries
            {
                Title = "The Daily",
                Description = "The biggest stories of our time.",
                Publisher = "The New York Times",
                ImageUrl = "https://feeds.example.com/art.jpg",
                Language = "en-us",
                PodcastGuid = "b1f2a6d0-0000-4000-8000-000000000001",
                Categories = new[] { "News" }
            },
            TotalItemCount = 2000
        };

        private static DirectoryPodcast Hit() => new()
        {
            Title = "The Daily",
            Publisher = "The New York Times",
            FeedUrl = FeedUrl,
            ApplePodcastsId = "1200361736",
            ArtworkUrl = "https://apple.example.com/600.jpg",
            Genres = new[] { "News" },
            Source = DirectoryPodcast.AppleSource
        };

        /// <summary>A client whose feed reader returns <see cref="Feed"/> for any URL.</summary>
        private (HttpClient Client, IPodcastDirectory Directory, IPodcastFeedReader Reader) CreateClient(
            Action<IPodcastDirectory>? directory = null)
        {
            return _factory.CreateClientWithSubstitutes<IPodcastDirectory, IPodcastFeedReader>(
                directory,
                r => r.ReadAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Feed()));
        }

        private async Task<PodcastSeriesResponseDto> CreateSeries(
            HttpClient client, string title, string? feedUrl = FeedUrl)
        {
            var response = await PostJson(client, "/api/podcast/series",
                new CreatePodcastSeriesDto { Title = title, RssFeedUrl = feedUrl });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return await Read<PodcastSeriesResponseDto>(response);
        }

        private async Task<PodcastSeriesResponseDto> GetSeries(HttpClient client, Guid id) =>
            await Read<PodcastSeriesResponseDto>(await client.GetAsync($"/api/podcast/series/{id}"));

        #endregion

        #region Auth Tests

        [Fact]
        public async Task GetStatus_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.GetAsync("/api/podcastenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RunEnrichment_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsJsonAsync("/api/podcastenrichment/run", new { });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task EnrichSeries_Anonymous_Returns401()
        {
            var seeded = await CreateSeries(_client, "The Daily");

            var response = await _factory.CreateAnonymousClient()
                .PostAsync($"/api/podcast/series/{seeded.Id}/enrich", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GetStatus

        [Fact]
        public async Task GetStatus_ShouldReturnOk_WithValidToken()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/podcastenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("podcastsNeedingEnrichment", out var count).Should().BeTrue();
            count.GetInt32().Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion

        #region RunEnrichment

        [Fact]
        public async Task RunEnrichment_ShouldReturnOk_WithEmptyDb()
        {
            await _client.AuthenticateAsync();

            var response = await _client.PostAsync("/api/podcastenrichment/run", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await Read<PodcastEnrichmentResult>(response);
            result.Success.Should().BeTrue();
            result.Operation.Should().Be(PodcastEnrichmentResult.EnrichmentOperation);
            result.TotalProcessed.Should().Be(0);
            result.PendingCount.Should().Be(0);
            result.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RunEnrichment_FillsASeriesFromItsFeed_AndReportsTheContractBody()
        {
            var (client, _, _) = CreateClient();
            var series = await CreateSeries(client, "Podcast at feeds.example.com");

            var response = await PostJson(client, "/api/podcastenrichment/run",
                new { batchSize = 10, delayBetweenCallsMs = 500 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await Read<PodcastEnrichmentResult>(response);
            result.Success.Should().BeTrue();
            result.TotalProcessed.Should().Be(1);
            result.EnrichedCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            result.PendingCount.Should().Be(0);
            result.ReindexTriggered.Should().BeTrue();

            var stored = await GetSeries(client, series.Id);
            stored.Title.Should().Be("The Daily");
            stored.Publisher.Should().Be("The New York Times");
            stored.MetadataSource.Should().Be("rss");
            stored.EnrichedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RunEnrichment_SeriesWithoutAFeedUrl_IsResolvedThroughTheDirectory()
        {
            var (client, _, reader) = CreateClient(d =>
                d.SearchAsync("The Daily", Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { Hit() }));
            var series = await CreateSeries(client, "The Daily", feedUrl: null);

            var response = await PostJson(client, "/api/podcastenrichment/run",
                new { batchSize = 10, delayBetweenCallsMs = 500 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Read<PodcastEnrichmentResult>(response)).EnrichedCount.Should().Be(1);

            var stored = await GetSeries(client, series.Id);
            stored.RssFeedUrl.Should().Be(FeedUrl);
            stored.ApplePodcastsId.Should().Be("1200361736");
            await reader.Received(1).ReadAsync(FeedUrl, Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region EnrichSeries

        [Fact]
        public async Task EnrichSeries_FillsTheSeries_ThenSkipsItUntilForced()
        {
            var (client, _, _) = CreateClient();
            var series = await CreateSeries(client, "Podcast at feeds.example.com");

            var first = await client.PostAsync($"/api/podcast/series/{series.Id}/enrich", null);

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            var filled = await Read<PodcastEnrichmentResult>(first);
            filled.EnrichedCount.Should().Be(1);
            filled.SkippedCount.Should().Be(0);
            filled.ReindexTriggered.Should().BeTrue();
            (await GetSeries(client, series.Id)).Title.Should().Be("The Daily");

            var again = await client.PostAsync($"/api/podcast/series/{series.Id}/enrich", null);

            again.StatusCode.Should().Be(HttpStatusCode.OK);
            var skipped = await Read<PodcastEnrichmentResult>(again);
            skipped.SkippedCount.Should().Be(1);
            skipped.EnrichedCount.Should().Be(0);
            skipped.WarningMessage.Should().Contain("force");

            var forced = await client.PostAsync($"/api/podcast/series/{series.Id}/enrich?force=true", null);

            forced.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Read<PodcastEnrichmentResult>(forced)).SkippedCount.Should().Be(0);
        }

        [Fact]
        public async Task EnrichSeries_UnknownSeries_Returns404WithErrorObject()
        {
            var (client, _, _) = CreateClient();

            var response = await client.PostAsync($"/api/podcast/series/{Guid.NewGuid()}/enrich", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await Read<JsonElement>(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        #endregion
    }
}
