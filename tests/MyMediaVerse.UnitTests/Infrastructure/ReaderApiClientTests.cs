using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyMediaVerse.Infrastructure.Clients;
using Xunit;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class ReaderApiClientTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<ReaderApiClient>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly ReaderApiClient _client;

        public ReaderApiClientTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["ApiKeys:Readwise"]).Returns("test-api-token");

            _mockLogger = new Mock<ILogger<ReaderApiClient>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://readwise.io/api/v3/")
            };

            _client = new ReaderApiClient(_httpClient, _mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task GetDocumentsAsync_Success_ReturnsDocuments()
        {
            // Arrange
            var responseJson = @"{
                ""count"": 2,
                ""next"": null,
                ""previous"": null,
                ""results"": [
                    {
                        ""id"": ""doc-123"",
                        ""url"": ""https://example.com/article1"",
                        ""title"": ""Test Article 1"",
                        ""author"": ""Author 1"",
                        ""source"": ""web"",
                        ""category"": ""article"",
                        ""location"": ""new"",
                        ""tags"": {""tech"": {}},
                        ""site_name"": ""Example Site"",
                        ""word_count"": 1000,
                        ""created_at"": ""2023-01-01T12:00:00Z"",
                        ""updated_at"": ""2023-01-01T12:00:00Z"",
                        ""notes"": ""Test note"",
                        ""summary"": ""Test summary"",
                        ""image_url"": ""https://example.com/image.jpg"",
                        ""content"": ""<html><body>Content</body></html>"",
                        ""source_url"": ""https://example.com/article1"",
                        ""published_date"": ""2023-01-01"",
                        ""reading_progress"": 0.5,
                        ""parent_id"": null
                    },
                    {
                        ""id"": ""doc-456"",
                        ""url"": ""https://example.com/article2"",
                        ""title"": ""Test Article 2"",
                        ""author"": ""Author 2"",
                        ""source"": ""web"",
                        ""category"": ""article"",
                        ""location"": ""archive"",
                        ""tags"": {},
                        ""site_name"": ""Example Site"",
                        ""word_count"": 2000,
                        ""created_at"": ""2023-01-02T12:00:00Z"",
                        ""updated_at"": ""2023-01-02T12:00:00Z"",
                        ""notes"": null,
                        ""summary"": null,
                        ""image_url"": null,
                        ""content"": null,
                        ""source_url"": ""https://example.com/article2"",
                        ""published_date"": null,
                        ""reading_progress"": 0.0,
                        ""parent_id"": null
                    }
                ]
            }";

            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            // Act
            var result = await _client.GetDocumentsAsync();

            // Assert
            result.Should().NotBeNull();
            result.count.Should().Be(2);
            result.results.Should().HaveCount(2);
            result.results[0].title.Should().Be("Test Article 1");
            result.results[1].title.Should().Be("Test Article 2");
        }

        [Fact]
        public async Task GetDocumentsAsync_WithLocation_IncludesQueryParameter()
        {
            // Arrange
            var location = "new";
            var responseJson = @"{""count"": 0, ""results"": []}";

            HttpRequestMessage capturedRequest = null;
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            await _client.GetDocumentsAsync(location: location);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest.RequestUri.Query.Should().Contain("location=new");
        }

        [Fact]
        public async Task GetDocumentsAsync_WithUpdatedAfter_IncludesQueryParameter()
        {
            // Arrange
            var updatedAfter = "2023-01-01";
            var responseJson = @"{""count"": 0, ""results"": []}";

            HttpRequestMessage capturedRequest = null;
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            await _client.GetDocumentsAsync(updatedAfter: updatedAfter);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest.RequestUri.Query.Should().Contain("updatedAfter=2023-01-01");
        }

        [Fact]
        public async Task GetDocumentByIdAsync_Success_ReturnsDocument()
        {
            // Arrange
            var documentId = "doc-123";
            var responseJson = @"{
                ""count"": 1,
                ""results"": [
                    {
                        ""id"": ""doc-123"",
                        ""url"": ""https://example.com/article"",
                        ""title"": ""Test Article"",
                        ""author"": ""Test Author"",
                        ""source"": ""web"",
                        ""category"": ""article"",
                        ""location"": ""new"",
                        ""tags"": {""tech"": {}},
                        ""site_name"": ""Example Site"",
                        ""word_count"": 1000,
                        ""created_at"": ""2023-01-01T12:00:00Z"",
                        ""updated_at"": ""2023-01-01T12:00:00Z"",
                        ""notes"": ""Test note"",
                        ""summary"": ""Test summary"",
                        ""image_url"": ""https://example.com/image.jpg"",
                        ""content"": ""<html><body>Full content here</body></html>"",
                        ""source_url"": ""https://example.com/article"",
                        ""published_date"": ""2023-01-01"",
                        ""reading_progress"": 0.5,
                        ""parent_id"": null
                    }
                ]
            }";

            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            // Act
            var result = await _client.GetDocumentByIdAsync(documentId);

            // Assert
            result.Should().NotBeNull();
            result.id.Should().Be("doc-123");
            result.title.Should().Be("Test Article");
            result.content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetDocumentsAsync_Unauthorized_ReturnsEmptyResponse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            // Act
            var result = await _client.GetDocumentsAsync();

            // Assert
            result.Should().NotBeNull();
            result.results.Should().BeEmpty();
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }
    }
}

