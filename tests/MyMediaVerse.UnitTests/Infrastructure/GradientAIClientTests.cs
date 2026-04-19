using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyMediaVerse.Infrastructure.Clients.AI;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class GradientAIClientTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> _mockGradientHandler;
        private readonly Mock<HttpMessageHandler> _mockOpenAIHandler;
        private readonly Mock<ILogger<GradientAIClient>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly HttpClient _gradientHttpClient;
        private readonly HttpClient _openAIHttpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        // Store original env vars for cleanup
        private readonly string? _originalGradientKey;
        private readonly string? _originalOpenAIKey;
        private readonly string? _originalEmbeddingModel;
        private readonly string? _originalDimensions;
        private readonly string? _originalGenerationModel;

        public GradientAIClientTests()
        {
            // Save original env vars
            _originalGradientKey = Environment.GetEnvironmentVariable("GRADIENT_API_KEY");
            _originalOpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _originalEmbeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL");
            _originalDimensions = Environment.GetEnvironmentVariable("OPENAI_DIMENSIONS");
            _originalGenerationModel = Environment.GetEnvironmentVariable("GRADIENT_GENERATION_MODEL");

            // Set environment variables for testing
            Environment.SetEnvironmentVariable("GRADIENT_API_KEY", "test-gradient-key");
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-openai-key");

            _mockGradientHandler = new Mock<HttpMessageHandler>();
            _mockOpenAIHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<GradientAIClient>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            _gradientHttpClient = new HttpClient(_mockGradientHandler.Object)
            {
                BaseAddress = new Uri("https://cloud.digitalocean.com/gen-ai/api/v1/")
            };

            _openAIHttpClient = new HttpClient(_mockOpenAIHandler.Object)
            {
                BaseAddress = new Uri("https://api.openai.com/v1/")
            };

            _mockHttpClientFactory.Setup(f => f.CreateClient("OpenAIEmbeddings"))
                .Returns(_openAIHttpClient);

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
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", _originalOpenAIKey);
            Environment.SetEnvironmentVariable("OPENAI_EMBEDDING_MODEL", _originalEmbeddingModel);
            Environment.SetEnvironmentVariable("OPENAI_DIMENSIONS", _originalDimensions);
            Environment.SetEnvironmentVariable("GRADIENT_GENERATION_MODEL", _originalGenerationModel);
            _gradientHttpClient.Dispose();
            _openAIHttpClient.Dispose();
        }

        private GradientAIClient CreateClient()
        {
            return new GradientAIClient(_gradientHttpClient, _mockHttpClientFactory.Object, _mockLogger.Object);
        }

        private void SetupOpenAIResponse(HttpStatusCode statusCode, string jsonResponse)
        {
            _mockOpenAIHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });
        }

        private void SetupGradientResponse(HttpStatusCode statusCode, string jsonResponse)
        {
            _mockGradientHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });
        }

        #region Configuration Tests

        [Fact]
        public void Constructor_ShouldSetDefaultModelNames()
        {
            // Arrange - clear env vars so defaults are used
            Environment.SetEnvironmentVariable("OPENAI_EMBEDDING_MODEL", null);
            Environment.SetEnvironmentVariable("OPENAI_DIMENSIONS", null);
            Environment.SetEnvironmentVariable("GRADIENT_GENERATION_MODEL", null);

            // Act
            var client = CreateClient();

            // Assert
            client.EmbeddingModelName.Should().Be("text-embedding-3-large");
            client.EmbeddingDimensions.Should().Be(1024);
            client.GenerationModelName.Should().Be("gpt-4-turbo");
        }

        [Fact]
        public void Constructor_ShouldUseCustomModelFromEnvironment()
        {
            // Arrange
            Environment.SetEnvironmentVariable("OPENAI_EMBEDDING_MODEL", "text-embedding-ada-002");
            Environment.SetEnvironmentVariable("OPENAI_DIMENSIONS", "512");
            Environment.SetEnvironmentVariable("GRADIENT_GENERATION_MODEL", "llama-3-70b");

            // Act
            var client = CreateClient();

            // Assert
            client.EmbeddingModelName.Should().Be("text-embedding-ada-002");
            client.EmbeddingDimensions.Should().Be(512);
            client.GenerationModelName.Should().Be("llama-3-70b");
        }

        #endregion

        #region GenerateEmbeddingAsync Tests

        [Fact]
        public async Task GenerateEmbeddingAsync_ShouldReturnEmbeddingArray()
        {
            // Arrange
            var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
            var response = new
            {
                data = new[]
                {
                    new { index = 0, embedding = expectedEmbedding }
                },
                model = "text-embedding-3-large",
                usage = new { prompt_tokens = 5, total_tokens = 5 }
            };

            SetupOpenAIResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response, _jsonOptions));
            var client = CreateClient();

            // Act
            var result = await client.GenerateEmbeddingAsync("test text");

            // Assert
            result.Should().BeEquivalentTo(expectedEmbedding);
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithEmptyText_ShouldThrowArgumentException()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => client.GenerateEmbeddingAsync(""));
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WithWhitespaceText_ShouldThrowArgumentException()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => client.GenerateEmbeddingAsync("   "));
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WhenApiReturnsError_ShouldThrowHttpRequestException()
        {
            // Arrange
            SetupOpenAIResponse(HttpStatusCode.BadRequest, "{\"error\": \"invalid request\"}");
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GenerateEmbeddingAsync("test text"));
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_WhenNotConfigured_ShouldThrowInvalidOperationException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GenerateEmbeddingAsync("test text"));
        }

        #endregion

        #region GenerateEmbeddingsBatchAsync Tests

        [Fact]
        public async Task GenerateEmbeddingsBatchAsync_ShouldReturnMultipleEmbeddings()
        {
            // Arrange
            var embedding1 = new float[] { 0.1f, 0.2f };
            var embedding2 = new float[] { 0.3f, 0.4f };
            var response = new
            {
                data = new[]
                {
                    new { index = 0, embedding = embedding1 },
                    new { index = 1, embedding = embedding2 }
                },
                model = "text-embedding-3-large"
            };

            SetupOpenAIResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response, _jsonOptions));
            var client = CreateClient();

            // Act
            var result = await client.GenerateEmbeddingsBatchAsync(new List<string> { "text 1", "text 2" });

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().BeEquivalentTo(embedding1);
            result[1].Should().BeEquivalentTo(embedding2);
        }

        [Fact]
        public async Task GenerateEmbeddingsBatchAsync_WithNullTexts_ShouldThrowArgumentException()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => client.GenerateEmbeddingsBatchAsync(null!));
        }

        [Fact]
        public async Task GenerateEmbeddingsBatchAsync_WithEmptyList_ShouldThrowArgumentException()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => client.GenerateEmbeddingsBatchAsync(new List<string>()));
        }

        [Fact]
        public async Task GenerateEmbeddingsBatchAsync_WithAllEmptyTexts_ShouldThrowArgumentException()
        {
            // Arrange
            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                client.GenerateEmbeddingsBatchAsync(new List<string> { "", "  ", null! }));
        }

        [Fact]
        public async Task GenerateEmbeddingsBatchAsync_ShouldOrderByIndex()
        {
            // Arrange - return out of order
            var embedding0 = new float[] { 0.1f, 0.2f };
            var embedding1 = new float[] { 0.3f, 0.4f };
            var response = new
            {
                data = new[]
                {
                    new { index = 1, embedding = embedding1 },
                    new { index = 0, embedding = embedding0 }
                },
                model = "text-embedding-3-large"
            };

            SetupOpenAIResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response, _jsonOptions));
            var client = CreateClient();

            // Act
            var result = await client.GenerateEmbeddingsBatchAsync(new List<string> { "text 0", "text 1" });

            // Assert - should be sorted by index
            result[0].Should().BeEquivalentTo(embedding0);
            result[1].Should().BeEquivalentTo(embedding1);
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
        public async Task IsAvailableAsync_WhenNotConfigured_ShouldReturnFalse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GRADIENT_API_KEY", null);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            var client = CreateClient();

            // Act
            var result = await client.IsAvailableAsync();

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
