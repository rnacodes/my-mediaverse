using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyMediaVerse.Infrastructure.Clients;
using MyMediaVerse.Shared.DTOs.Obsidian;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class QuartzApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<QuartzApiClient>> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly QuartzApiClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public QuartzApiClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<QuartzApiClient>>();

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            _client = new QuartzApiClient(_httpClient, _mockLogger.Object);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                });
        }

        #region GetContentIndexAsync Tests

        [Fact]
        public async Task GetContentIndexAsync_ShouldReturnParsedNotes()
        {
            // Arrange
            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Description = "Notes on stoic philosophy",
                    Tags = new List<string> { "philosophy", "stoicism" }
                },
                ["programming/csharp"] = new QuartzNoteDto
                {
                    Title = "C# Notes",
                    Description = "Learning C#",
                    Tags = new List<string> { "programming" }
                }
            };

            SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(contentIndex, _jsonOptions));

            // Act
            var result = await _client.GetContentIndexAsync("https://vault.example.com");

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainKey("philosophy/stoicism");
            result.Should().ContainKey("programming/csharp");
            result["philosophy/stoicism"].Title.Should().Be("Stoicism");
            result["philosophy/stoicism"].Tags.Should().Contain("stoicism");
        }

        [Fact]
        public async Task GetContentIndexAsync_ShouldTrimTrailingSlashFromUrl()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{}");

            // Act
            var result = await _client.GetContentIndexAsync("https://vault.example.com/");

            // Assert
            result.Should().BeEmpty();

            // Verify the URL was properly constructed (no double slashes)
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString() == "https://vault.example.com/static/contentIndex.json"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetContentIndexAsync_WhenNotFound_ShouldReturnEmptyDictionary()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound, "");

            // Act
            var result = await _client.GetContentIndexAsync("https://vault.example.com");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetContentIndexAsync_WhenUnauthorized_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Unauthorized, "");

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _client.GetContentIndexAsync("https://vault.example.com"));
        }

        [Fact]
        public async Task GetContentIndexAsync_WhenJsonInvalid_ShouldThrowJsonException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "not valid json {{{");

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(
                () => _client.GetContentIndexAsync("https://vault.example.com"));
        }

        [Fact]
        public async Task GetContentIndexAsync_WhenHttpError_ShouldThrowHttpRequestException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _client.GetContentIndexAsync("https://vault.example.com"));
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task GetContentIndexAsync_WithBasicAuthCredentials_ShouldSetAuthHeader()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{}");

            // Act
            await _client.GetContentIndexAsync("https://vault.example.com", "user:password");

            // Assert - verify Basic auth header was set
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Basic"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetContentIndexAsync_WithBearerToken_ShouldSetAuthHeader()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{}");

            // Act
            await _client.GetContentIndexAsync("https://vault.example.com", "Bearer my-token");

            // Assert - verify Bearer auth header was set
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "my-token"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetContentIndexAsync_WithPlainToken_ShouldDefaultToBearerAuth()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{}");

            // Act
            await _client.GetContentIndexAsync("https://vault.example.com", "plain-api-token");

            // Assert - plain token defaults to Bearer
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "plain-api-token"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetContentIndexAsync_WithNoToken_ShouldNotSetAuthHeader()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{}");

            // Act
            await _client.GetContentIndexAsync("https://vault.example.com", null);

            // Assert - no auth header
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization == null),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetContentIndexAsync_ShouldSetUserAgentHeader()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{}");

            // Act
            await _client.GetContentIndexAsync("https://vault.example.com");

            // Assert
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Contains("User-Agent")),
                ItExpr.IsAny<CancellationToken>());
        }

        #endregion

        #region Empty/Null Response Tests

        [Fact]
        public async Task GetContentIndexAsync_WithEmptyObject_ShouldReturnEmptyDictionary()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{}");

            // Act
            var result = await _client.GetContentIndexAsync("https://vault.example.com");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetContentIndexAsync_WithNullResult_ShouldReturnEmptyDictionary()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "null");

            // Act
            var result = await _client.GetContentIndexAsync("https://vault.example.com");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }
}
