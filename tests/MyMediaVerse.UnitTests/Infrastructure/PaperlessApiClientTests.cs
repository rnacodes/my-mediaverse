using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyMediaVerse.Infrastructure.Clients;
using MyMediaVerse.Shared.DTOs.Paperless;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class PaperlessApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<PaperlessApiClient>> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly PaperlessApiClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public PaperlessApiClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<PaperlessApiClient>>();

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://paperless.example.com/api/")
            };

            _client = new PaperlessApiClient(_httpClient, _mockLogger.Object);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string jsonResponse)
        {
            _mockHttpMessageHandler.Protected()
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

        private void SetupHttpByteResponse(HttpStatusCode statusCode, byte[] content)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new ByteArrayContent(content)
                });
        }

        #region GetDocumentsAsync Tests

        [Fact]
        public async Task GetDocumentsAsync_ShouldReturnDocumentList()
        {
            // Arrange
            var response = new PaperlessDocumentListResponseDto
            {
                Count = 2,
                Results = new List<PaperlessDocumentDto>
                {
                    new() { Id = 1, Title = "Invoice 1" },
                    new() { Id = 2, Title = "Receipt 2" }
                }
            };

            SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response, _jsonOptions));

            // Act
            var result = await _client.GetDocumentsAsync(1, 25);

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().HaveCount(2);
            result.Count.Should().Be(2);
        }

        [Fact]
        public async Task GetDocumentsAsync_WhenApiError_ShouldThrow()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetDocumentsAsync());
        }

        #endregion

        #region GetDocumentByIdAsync Tests

        [Fact]
        public async Task GetDocumentByIdAsync_WhenDocumentExists_ShouldReturnDocument()
        {
            // Arrange
            var document = new PaperlessDocumentDto { Id = 42, Title = "Tax Return" };
            SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(document, _jsonOptions));

            // Act
            var result = await _client.GetDocumentByIdAsync(42);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(42);
            result.Title.Should().Be("Tax Return");
        }

        [Fact]
        public async Task GetDocumentByIdAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound, "");

            // Act
            var result = await _client.GetDocumentByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region SearchDocumentsAsync Tests

        [Fact]
        public async Task SearchDocumentsAsync_ShouldReturnResults()
        {
            // Arrange
            var response = new PaperlessDocumentListResponseDto
            {
                Count = 1,
                Results = new List<PaperlessDocumentDto>
                {
                    new() { Id = 1, Title = "Matching Document" }
                }
            };
            SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response, _jsonOptions));

            // Act
            var result = await _client.SearchDocumentsAsync("matching");

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().HaveCount(1);
        }

        #endregion

        #region GetDocumentContentAsync Tests

        [Fact]
        public async Task GetDocumentContentAsync_ShouldReturnByteArray()
        {
            // Arrange
            var expectedContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
            SetupHttpByteResponse(HttpStatusCode.OK, expectedContent);

            // Act
            var result = await _client.GetDocumentContentAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedContent);
        }

        #endregion

        #region GetDocumentThumbnailAsync Tests

        [Fact]
        public async Task GetDocumentThumbnailAsync_ShouldReturnByteArray()
        {
            // Arrange
            var expectedContent = new byte[] { 0xFF, 0xD8, 0xFF }; // JPEG magic bytes
            SetupHttpByteResponse(HttpStatusCode.OK, expectedContent);

            // Act
            var result = await _client.GetDocumentThumbnailAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedContent);
        }

        #endregion

        #region URL Construction Tests

        [Fact]
        public void GetDocumentPreviewUrl_ShouldReturnCorrectUrl()
        {
            // Act
            var url = _client.GetDocumentPreviewUrl(42);

            // Assert
            url.Should().Be("https://paperless.example.com/api/documents/42/preview/");
        }

        [Fact]
        public void GetDocumentDownloadUrl_ShouldReturnCorrectUrl()
        {
            // Act
            var url = _client.GetDocumentDownloadUrl(42);

            // Assert
            url.Should().Be("https://paperless.example.com/api/documents/42/download/");
        }

        [Fact]
        public void GetBaseUrl_ShouldReturnBaseAddress()
        {
            // Act
            var url = _client.GetBaseUrl();

            // Assert
            url.Should().Be("https://paperless.example.com/api/");
        }

        #endregion

        #region DeleteDocumentAsync Tests

        [Fact]
        public async Task DeleteDocumentAsync_WhenExists_ShouldReturnTrue()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NoContent, "");

            // Act
            var result = await _client.DeleteDocumentAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteDocumentAsync_WhenNotFound_ShouldReturnFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound, "");

            // Act
            var result = await _client.DeleteDocumentAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetTagsAsync Tests

        [Fact]
        public async Task GetTagsAsync_ShouldReturnTagList()
        {
            // Arrange
            var response = new
            {
                count = 2,
                results = new[]
                {
                    new { id = 1, name = "invoice", color = "#ff0000" },
                    new { id = 2, name = "receipt", color = "#00ff00" }
                }
            };
            SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response, _jsonOptions));

            // Act
            var result = await _client.GetTagsAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        #endregion

        #region IsAvailableAsync Tests

        [Fact]
        public async Task IsAvailableAsync_WhenReachable_ShouldReturnTrue()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{}");

            // Act
            var result = await _client.IsAvailableAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAvailableAsync_WhenUnreachable_ShouldReturnFalse()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            // Act
            var result = await _client.IsAvailableAsync();

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
