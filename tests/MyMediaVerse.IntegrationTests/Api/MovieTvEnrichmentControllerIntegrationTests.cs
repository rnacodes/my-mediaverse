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
    public class MovieTvEnrichmentControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public MovieTvEnrichmentControllerIntegrationTests(ApiFactory factory)
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
            var client = _factory.CreateAnonymousClient();

            var response = await client.GetAsync("/api/movietvenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RunMovieEnrichment_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsJsonAsync("/api/movietvenrichment/run/movies", new { });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RunTvShowEnrichment_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsJsonAsync("/api/movietvenrichment/run/tvshows", new { });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GetStatus

        [Fact]
        public async Task GetStatus_ShouldReturnOk_WithValidToken()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/movietvenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("moviesNeedingEnrichment", out var movieCount).Should().BeTrue();
            result.TryGetProperty("tvShowsNeedingEnrichment", out var tvCount).Should().BeTrue();
            movieCount.GetInt32().Should().BeGreaterThanOrEqualTo(0);
            tvCount.GetInt32().Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion

        #region RunEnrichment

        [Fact]
        public async Task RunMovieEnrichment_ShouldReturnOk_WithEmptyDb()
        {
            await _client.AuthenticateAsync();

            var response = await _client.PostAsync("/api/movietvenrichment/run/movies", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("totalProcessed", out var total).Should().BeTrue();
            total.GetInt32().Should().Be(0);
        }

        [Fact]
        public async Task RunTvShowEnrichment_ShouldReturnOk_WithEmptyDb()
        {
            await _client.AuthenticateAsync();

            var response = await _client.PostAsync("/api/movietvenrichment/run/tvshows", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("totalProcessed", out var total).Should().BeTrue();
            total.GetInt32().Should().Be(0);
        }

        #endregion
    }
}
