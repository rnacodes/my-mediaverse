using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _validUsername;
        private readonly string _validPassword;

        public AuthControllerIntegrationTests(WebApplicationFactory factory)
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

            // Use the same credential resolution chain as AuthController
            _validUsername = Environment.GetEnvironmentVariable("AUTH_USERNAME") ?? "admin";
            _validPassword = Environment.GetEnvironmentVariable("AUTH_PASSWORD") ?? "password123";
        }

        private StringContent CreateJsonContent(object data)
        {
            return new StringContent(
                JsonSerializer.Serialize(data, _jsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var loginData = new { username = _validUsername, password = _validPassword };
            var response = await _client.PostAsync("/api/auth/login", CreateJsonContent(loginData));
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            return loginResponse.GetProperty("token").GetString()!;
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
        {
            // Arrange - use the same credential chain as the controller
            var loginData = new { username = _validUsername, password = _validPassword };

            // Act
            var response = await _client.PostAsync("/api/auth/login", CreateJsonContent(loginData));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            Assert.True(loginResponse.TryGetProperty("token", out var token));
            Assert.False(string.IsNullOrEmpty(token.GetString()));
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginData = new { username = "admin", password = "wrong-password" };

            // Act
            var response = await _client.PostAsync("/api/auth/login", CreateJsonContent(loginData));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithInvalidUsername_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginData = new { username = "nonexistent", password = "password123" };

            // Act
            var response = await _client.PostAsync("/api/auth/login", CreateJsonContent(loginData));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithEmptyBody_ShouldReturnBadRequest()
        {
            // Arrange
            var loginData = new { username = "", password = "" };

            // Act
            var response = await _client.PostAsync("/api/auth/login", CreateJsonContent(loginData));

            // Assert
            // Should either be BadRequest (validation) or Unauthorized (empty creds fail)
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Expected 400 or 401 but got {(int)response.StatusCode}");
        }

        #endregion

        #region Validate Tests

        [Fact]
        public async Task Validate_WithValidToken_ShouldReturnOk()
        {
            // Arrange
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/auth/validate");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var validateResponse = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            Assert.True(validateResponse.GetProperty("valid").GetBoolean());

            // Cleanup
            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Validate_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange - ensure no auth header
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/auth/validate");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Validate_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.GetAsync("/api/auth/validate");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            // Cleanup
            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task Logout_WithValidToken_ShouldReturnOk()
        {
            // Arrange
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.PostAsync("/api/auth/logout", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Cleanup
            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Logout_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.PostAsync("/api/auth/logout", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        #endregion

        #region Cleanup Tokens Tests

        [Fact]
        public async Task CleanupTokens_WithValidToken_ShouldReturnOk()
        {
            // Arrange
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.PostAsync("/api/auth/cleanup-tokens", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            Assert.True(result.TryGetProperty("message", out _));

            // Cleanup
            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task CleanupTokens_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.PostAsync("/api/auth/cleanup-tokens", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        #endregion
    }
}
