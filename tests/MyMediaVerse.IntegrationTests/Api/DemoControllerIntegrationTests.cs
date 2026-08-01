using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.IntegrationTests.Fixtures;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class DemoControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public DemoControllerIntegrationTests(ApiFactory factory)
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

        #region Unlock Rate Limiting Tests

        // The rate limiter runs as middleware ahead of the action, so it applies even in the
        // "Testing" environment (where the action itself returns NotFound). Each test uses a
        // unique X-Forwarded-For IP so the shared-host limiter state does not bleed across tests.

        private async Task<HttpResponseMessage> SendUnlockAsync(string clientIp)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/demo/unlock?code=123456");
            request.Headers.Add("X-Forwarded-For", clientIp);
            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task UnlockDemoWriteAccess_ExceedingRateLimit_Returns429()
        {
            const string clientIp = "203.0.113.10";

            // The policy permits 10 requests per minute per IP.
            for (var i = 0; i < 10; i++)
            {
                var allowed = await SendUnlockAsync(clientIp);
                Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
            }

            // The 11th request in the window is rejected by the limiter before reaching the action.
            var throttled = await SendUnlockAsync(clientIp);
            Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        }

        [Fact]
        public async Task UnlockDemoWriteAccess_RateLimit_IsPartitionedByClientIp()
        {
            const string exhaustedIp = "203.0.113.20";
            const string freshIp = "203.0.113.21";

            // Exhaust the window for the first IP.
            for (var i = 0; i < 10; i++)
            {
                await SendUnlockAsync(exhaustedIp);
            }
            var throttled = await SendUnlockAsync(exhaustedIp);
            Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

            // A different IP has its own bucket and is unaffected.
            var otherIp = await SendUnlockAsync(freshIp);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, otherIp.StatusCode);
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

        #region Demo Write Gate Tests

        [Fact]
        public async Task DemoWriteGate_InTestingEnvironment_AllowsWriteOperations()
        {
            // In Testing environment (not Demo), the write gate should allow all operations
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

            // Assert - Should succeed (not get blocked by the demo gate). Asserting success rather
            // than merely "not Forbidden" so an auth failure (401) can't slip through unnoticed.
            Assert.True(response.IsSuccessStatusCode,
                $"Expected a success status code but got {(int)response.StatusCode} {response.StatusCode}.");
        }

        [Fact]
        public async Task DemoWriteGate_GetRequests_AlwaysAllowed()
        {
            // GET requests should always be allowed regardless of environment
            var response = await _client.GetAsync("/api/media");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        #endregion
    }
}
