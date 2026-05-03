using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;

namespace MyMediaVerse.IntegrationTests.Controllers
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

        #region Search (Typesense mocked - returns NoContent for empty results)

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
    }
}
