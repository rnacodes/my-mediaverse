using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using ProjectLoopbreaker.Application.Helpers;

namespace ProjectLoopbreaker.IntegrationTests.Controllers
{
    public class SearchControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _validUsername;
        private readonly string _validPassword;

        public SearchControllerIntegrationTests(WebApplicationFactory factory)
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

        #region Health

        [Fact]
        public async Task Health_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/search/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Search (TypeSense mocked - returns NoContent for empty results)

        [Fact]
        public async Task Search_ShouldReturnSuccessfully_WhenQueryProvided()
        {
            var response = await _client.GetAsync("/api/search?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchByType_ShouldReturnSuccessfully()
        {
            var response = await _client.GetAsync("/api/search/by-type/book?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchMixlists_ShouldReturnSuccessfully()
        {
            var response = await _client.GetAsync("/api/search/mixlists?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchNotes_ShouldReturnSuccessfully()
        {
            var response = await _client.GetAsync("/api/search/notes?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchHighlights_ShouldReturnSuccessfully()
        {
            var response = await _client.GetAsync("/api/search/highlights?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        #endregion

        #region Auth-Required Endpoints

        [Fact]
        public async Task Reindex_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PostAsync("/api/search/reindex", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReindexMixlists_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PostAsync("/api/search/reindex-mixlists", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Reset_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PostAsync("/api/search/reset", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Reindex_ShouldReturnOk_WithValidToken()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("/api/search/reindex", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region Real-Time Indexing Toggle

        [Fact]
        public async Task GetRealTimeIndexingStatus_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/search/realtime-indexing");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task SetRealTimeIndexingStatus_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var content = new StringContent(
                JsonSerializer.Serialize(new { enabled = false }, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/search/realtime-indexing", content);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetRealTimeIndexingStatus_ShouldReturnEnabled_ByDefault()
        {
            // Ensure default state
            TypesenseIndexingHelper.IsRealTimeIndexingEnabled = true;

            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/search/realtime-indexing");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            result.GetProperty("enabled").GetBoolean().Should().BeTrue();

            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task SetRealTimeIndexingStatus_ShouldToggleSuccessfully()
        {
            // Ensure default state
            TypesenseIndexingHelper.IsRealTimeIndexingEnabled = true;

            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Disable
            var disableContent = new StringContent(
                JsonSerializer.Serialize(new { enabled = false }, _jsonOptions),
                Encoding.UTF8,
                "application/json");
            var disableResponse = await _client.PostAsync("/api/search/realtime-indexing", disableContent);
            disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var disableResult = JsonSerializer.Deserialize<JsonElement>(
                await disableResponse.Content.ReadAsStringAsync(), _jsonOptions);
            disableResult.GetProperty("enabled").GetBoolean().Should().BeFalse();

            // Verify the static flag was updated
            TypesenseIndexingHelper.IsRealTimeIndexingEnabled.Should().BeFalse();

            // Re-enable
            var enableContent = new StringContent(
                JsonSerializer.Serialize(new { enabled = true }, _jsonOptions),
                Encoding.UTF8,
                "application/json");
            var enableResponse = await _client.PostAsync("/api/search/realtime-indexing", enableContent);
            enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var enableResult = JsonSerializer.Deserialize<JsonElement>(
                await enableResponse.Content.ReadAsStringAsync(), _jsonOptions);
            enableResult.GetProperty("enabled").GetBoolean().Should().BeTrue();

            // Reset state
            TypesenseIndexingHelper.IsRealTimeIndexingEnabled = true;
            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion
    }
}
