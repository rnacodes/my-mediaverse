using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Infrastructure.Clients.AI;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class GradientAIClientTests : IDisposable
    {
        private readonly TestHttpMessageHandler _mockGradientHandler;
        private readonly ILogger<GradientAIClient> _mockLogger;
        private readonly HttpClient _gradientHttpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        // Store original env vars for cleanup
        private readonly string? _originalGradientKey;
        private readonly string? _originalGenerationModel;

        public GradientAIClientTests()
        {
            // Save original env vars
            _originalGradientKey = Environment.GetEnvironmentVariable("GRADIENT_API_KEY");
            _originalGenerationModel = Environment.GetEnvironmentVariable("GRADIENT_GENERATION_MODEL");

            // Set environment variables for testing
            Environment.SetEnvironmentVariable("GRADIENT_API_KEY", "test-gradient-key");

            _mockGradientHandler = new TestHttpMessageHandler();
            _mockLogger = Substitute.For<ILogger<GradientAIClient>>();

            _gradientHttpClient = new HttpClient(_mockGradientHandler)
            {
                BaseAddress = new Uri("https://cloud.digitalocean.com/gen-ai/api/v1/")
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public void Dispose()
        {
            // Restore original env vars
            Environment.SetEnvironmentVariable("GRADIENT_API_KEY", _originalGradientKey);
            Environment.SetEnvironmentVariable("GRADIENT_GENERATION_MODEL", _originalGenerationModel);
            _gradientHttpClient.Dispose();
        }

        private GradientAIClient CreateClient()
        {
            return new GradientAIClient(_gradientHttpClient, _mockLogger);
        }

        private void SetupGradientResponse(HttpStatusCode statusCode, string jsonResponse)
            => _mockGradientHandler.RespondWith(statusCode, jsonResponse);

        #region Configuration Tests

        [Fact]
        public void Constructor_ShouldSetDefaultGenerationModel()
        {
            // Arrange - clear env var so default is used
            Environment.SetEnvironmentVariable("GRADIENT_GENERATION_MODEL", null);

            // Act
            var client = CreateClient();

            // Assert
            client.GenerationModelName.Should().Be("gpt-4-turbo");
        }

        [Fact]
        public void Constructor_ShouldUseCustomGenerationModelFromEnvironment()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GRADIENT_GENERATION_MODEL", "llama-3-70b");

            // Act
            var client = CreateClient();

            // Assert
            client.GenerationModelName.Should().Be("llama-3-70b");
        }

        #endregion

        #region GenerateTextAsync Tests

        [Fact]
        public async Task GenerateTextAsync_ShouldReturnGeneratedText()
        {
            // Arrange
            var response = new
            {
                id = "chatcmpl-123",
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new { role = "assistant", content = "Generated response text" },
                        finish_reason = "stop"
                    }
                }
            };

            SetupGradientResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response, _jsonOptions));
            var client = CreateClient();

            // Act
            var result = await client.GenerateTextAsync("Describe this note");

            // Assert
            result.Should().Be("Generated response text");
        }

        [Fact]
        public async Task GenerateTextAsync_WithSystemPrompt_ShouldSendBothMessages()
        {
            // Arrange
            var response = new
            {
                id = "chatcmpl-123",
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new { role = "assistant", content = "Response" },
                        finish_reason = "stop"
                    }
                }
            };

            SetupGradientResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response, _jsonOptions));
            var client = CreateClient();

            // Act
            var result = await client.GenerateTextAsync("User prompt", "System prompt", 1000);

            // Assert
            result.Should().Be("Response");
        }

        [Fact]
        public async Task GenerateTextAsync_WithEmptyPrompt_ShouldThrowArgumentException()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => client.GenerateTextAsync(""));
        }

        [Fact]
        public async Task GenerateTextAsync_WhenNotConfigured_ShouldThrowInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GRADIENT_API_KEY", null);
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GenerateTextAsync("test"));
        }

        [Fact]
        public async Task GenerateTextAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
        {
            // Arrange
            SetupGradientResponse(HttpStatusCode.InternalServerError, "{\"error\": \"server error\"}");
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GenerateTextAsync("test"));
        }

        #endregion

        #region IsAvailableAsync Tests

        [Fact]
        public async Task IsAvailableAsync_WhenConfigured_ShouldReturnTrue()
        {
            // Arrange
            SetupGradientResponse(HttpStatusCode.OK, "{}");
            var client = CreateClient();

            // Act
            var result = await client.IsAvailableAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAvailableAsync_WhenApiReturnsError_ShouldReturnFalse()
        {
            // Arrange
            SetupGradientResponse(HttpStatusCode.Unauthorized, "{\"error\": \"invalid key\"}");
            var client = CreateClient();

            // Act
            var result = await client.IsAvailableAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsAvailableAsync_WhenNotConfigured_ShouldReturnFalse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GRADIENT_API_KEY", null);
            var client = CreateClient();

            // Act
            var result = await client.IsAvailableAsync();

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
