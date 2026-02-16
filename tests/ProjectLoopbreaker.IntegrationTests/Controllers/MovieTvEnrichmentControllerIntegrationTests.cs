using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace ProjectLoopbreaker.IntegrationTests.Controllers
{
    public class MovieTvEnrichmentControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _validUsername;
        private readonly string _validPassword;

        public MovieTvEnrichmentControllerIntegrationTests(WebApplicationFactory factory)
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
            _validUsername = Environment.GetEnvironmentVariable("AUTH_USERNAME") ?? "admin";
            _validPassword = Environment.GetEnvironmentVariable("AUTH_PASSWORD") ?? "password123";
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var loginData = new { username = _validUsername, password = _validPassword };
            var content = new StringContent(JsonSerializer.Serialize(loginData, _jsonOptions), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/auth/login", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            return loginResponse.GetProperty("token").GetString()!;
        }

        #region Auth Tests

        [Fact]
        public async Task GetStatus_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/movietvenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RunMovieEnrichment_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PostAsJsonAsync("/api/movietvenrichment/run/movies", new { });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RunTvShowEnrichment_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PostAsJsonAsync("/api/movietvenrichment/run/tvshows", new { });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GetStatus

        [Fact]
        public async Task GetStatus_ShouldReturnOk_WithValidToken()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/movietvenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("moviesNeedingEnrichment", out var movieCount).Should().BeTrue();
            result.TryGetProperty("tvShowsNeedingEnrichment", out var tvCount).Should().BeTrue();
            movieCount.GetInt32().Should().BeGreaterThanOrEqualTo(0);
            tvCount.GetInt32().Should().BeGreaterThanOrEqualTo(0);

            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region RunEnrichment

        [Fact]
        public async Task RunMovieEnrichment_ShouldReturnOk_WithEmptyDb()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("/api/movietvenrichment/run/movies", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("totalProcessed", out var total).Should().BeTrue();
            total.GetInt32().Should().Be(0);

            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task RunTvShowEnrichment_ShouldReturnOk_WithEmptyDb()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("/api/movietvenrichment/run/tvshows", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("totalProcessed", out var total).Should().BeTrue();
            total.GetInt32().Should().Be(0);

            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion
    }
}
