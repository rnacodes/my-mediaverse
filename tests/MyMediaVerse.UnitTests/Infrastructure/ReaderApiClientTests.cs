using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Infrastructure.Clients.Readwise;
using MyMediaVerse.UnitTests.TestHelpers;
using Xunit;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class ReaderApiClientTests
    {
        private readonly IConfiguration _mockConfiguration;
        private readonly ILogger<ReaderApiClient> _mockLogger;
        private readonly TestHttpMessageHandler _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly ReaderApiClient _client;

        public ReaderApiClientTests()
        {
            _mockConfiguration = Substitute.For<IConfiguration>();
            _mockConfiguration["ApiKeys:Readwise"].Returns("test-api-token");

            _mockLogger = Substitute.For<ILogger<ReaderApiClient>>();
            _mockHttpMessageHandler = new TestHttpMessageHandler();
            _httpClient = new HttpClient(_mockHttpMessageHandler)
            {
                BaseAddress = new Uri("https://readwise.io/api/v3/")
            };

            _client = new ReaderApiClient(_httpClient, _mockLogger, _mockConfiguration);
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
            result.Count.Should().Be(2);
            result.Results.Should().HaveCount(2);
            result.Results[0].Title.Should().Be("Test Article 1");
            result.Results[0].SourceUrl.Should().Be("https://example.com/article1");
            result.Results[0].SiteName.Should().Be("Example Site");
            result.Results[0].WordCount.Should().Be(1000);
            result.Results[0].ReadingProgress.Should().Be(0.5);
            result.Results[0].Tags.Should().ContainKey("tech");
            result.Results[1].Title.Should().Be("Test Article 2");
            result.Results[1].Tags.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDocumentsAsync_WithLocation_IncludesQueryParameter()
        {
            // Arrange
            var location = "new";
            var responseJson = @"{""count"": 0, ""results"": []}";

            _mockHttpMessageHandler.OnSend = (req, ct) => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

            // Act
            await _client.GetDocumentsAsync(location: location);

            // Assert
            var capturedRequest = _mockHttpMessageHandler.Requests.LastOrDefault();
            capturedRequest.Should().NotBeNull();
            capturedRequest.RequestUri.Query.Should().Contain("location=new");
        }

        [Fact]
        public async Task GetDocumentsAsync_WithUpdatedAfter_IncludesQueryParameter()
        {
            // Arrange
            var updatedAfter = "2023-01-01";
            var responseJson = @"{""count"": 0, ""results"": []}";

            _mockHttpMessageHandler.OnSend = (req, ct) => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

            // Act
            await _client.GetDocumentsAsync(updatedAfter: updatedAfter);

            // Assert
            var capturedRequest = _mockHttpMessageHandler.Requests.LastOrDefault();
            capturedRequest.Should().NotBeNull();
            capturedRequest.RequestUri.Query.Should().Contain("updatedAfter=2023-01-01");
        }

        [Fact]
        public async Task GetDocumentsAsync_PagedResponse_ExposesNextPageCursor()
        {
            var responseJson = @"{""count"": 0, ""nextPageCursor"": ""abc123"", ""results"": []}";
            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            var result = await _client.GetDocumentsAsync();

            result.NextPageCursor.Should().Be("abc123");
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
            result.Id.Should().Be("doc-123");
            result.Title.Should().Be("Test Article");
            result.Content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetDocumentByIdAsync_NotFound_ReturnsNull()
        {
            SetupHttpResponse(HttpStatusCode.NotFound, "Not found");

            var result = await _client.GetDocumentByIdAsync("missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetDocumentsAsync_Unauthorized_Throws()
        {
            // An API failure must surface as an error, never as an empty page.
            SetupHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            Func<Task> act = () => _client.GetDocumentsAsync();

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            _mockHttpMessageHandler.RespondWith(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
        }
    }
}
