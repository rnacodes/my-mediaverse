using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class PodcastEnrichmentControllerIntegrationTests : IAsyncLifetime
    {
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

        #region Auth Tests

        [Fact]
        public async Task GetStatus_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/podcastenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RunEnrichment_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PostAsJsonAsync("/api/podcastenrichment/run", new { });

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
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("totalProcessed", out var total).Should().BeTrue();
            total.GetInt32().Should().Be(0);
        }

        #endregion
    }
}
