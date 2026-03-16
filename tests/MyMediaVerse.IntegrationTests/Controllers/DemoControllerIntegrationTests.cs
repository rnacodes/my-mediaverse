using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    public class DemoControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public DemoControllerIntegrationTests(WebApplicationFactory factory)
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

        #region Status Endpoint Tests

        [Fact]
        public async Task GetDemoStatus_InTestingEnvironment_ReturnsNotDemoEnvironment()
        {
            // Act - Testing environment is not "Demo", so isDemoEnvironment should be false
            var response = await _client.GetAsync("/api/demo/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            Assert.False(result.GetProperty("isDemoEnvironment").GetBoolean());
            Assert.True(result.GetProperty("writeAccessEnabled").GetBoolean());
        }

        [Fact]
        public async Task GetDemoStatus_ReturnsValidJson()
        {
            // Act
            var response = await _client.GetAsync("/api/demo/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            // Should have all expected properties
            Assert.True(result.TryGetProperty("isDemoEnvironment", out _));
            Assert.True(result.TryGetProperty("writeAccessEnabled", out _));
            Assert.True(result.TryGetProperty("message", out _));
        }

        #endregion

        #region Unlock Endpoint Tests

        [Fact]
        public async Task UnlockDemoWriteAccess_InNonDemoEnvironment_ReturnsNotFound()
        {
            // Act - Testing environment is not "Demo"
            var response = await _client.GetAsync("/api/demo/unlock?code=123456");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UnlockDemoWriteAccess_WithNoCode_InNonDemoEnvironment_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/demo/unlock");

            // Assert - Returns BadRequest because model binding rejects missing required 'code' parameter
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Lock Endpoint Tests

        [Fact]
        public async Task LockDemoWriteAccess_Post_InNonDemoEnvironment_ReturnsNotFound()
        {
            // Act - Testing environment is not "Demo"
            var response = await _client.PostAsync("/api/demo/lock", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task LockDemoWriteAccess_Get_InNonDemoEnvironment_ReturnsNotFound()
        {
            // Act - Lock also supports GET
            var response = await _client.GetAsync("/api/demo/lock");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Generate Secret Endpoint Tests

        [Fact]
        public async Task GenerateSecret_InTestingEnvironment_ReturnsNotFound()
        {
            // Act - generate-secret is only available in Development environment
            var response = await _client.GetAsync("/api/demo/generate-secret");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region DemoReadOnlyFilter Tests

        [Fact]
        public async Task DemoReadOnlyFilter_InTestingEnvironment_AllowsWriteOperations()
        {
            // In Testing environment (not Demo), the filter should allow all operations
            // Create a simple media item to verify POST is allowed
            var createDto = new
            {
                title = "Test Filter Media",
                description = "Testing that write ops work in non-demo env",
                mediaType = "Article",
                status = "Uncharted"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/media", content);

            // Assert - Should succeed (not get blocked by demo filter)
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DemoReadOnlyFilter_GetRequests_AlwaysAllowed()
        {
            // GET requests should always be allowed regardless of environment
            var response = await _client.GetAsync("/api/media");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        #endregion
    }
}
