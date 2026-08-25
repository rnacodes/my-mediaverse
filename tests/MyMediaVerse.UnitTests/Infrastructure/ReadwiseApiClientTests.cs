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
    public class ReadwiseApiClientTests
    {
        private readonly IConfiguration _mockConfiguration;
        private readonly ILogger<ReadwiseApiClient> _mockLogger;
        private readonly TestHttpMessageHandler _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly ReadwiseApiClient _client;

        public ReadwiseApiClientTests()
        {
            _mockConfiguration = Substitute.For<IConfiguration>();
            _mockConfiguration["ApiKeys:Readwise"].Returns("test-api-token");

            _mockLogger = Substitute.For<ILogger<ReadwiseApiClient>>();
            _mockHttpMessageHandler = new TestHttpMessageHandler();
            _httpClient = new HttpClient(_mockHttpMessageHandler)
            {
                BaseAddress = new Uri("https://readwise.io/api/v2/")
            };

            _client = new ReadwiseApiClient(_httpClient, _mockLogger, _mockConfiguration);
        }

        [Fact]
        public async Task ValidateTokenAsync_Success_ReturnsTrue()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NoContent, string.Empty);

            // Act
            var result = await _client.ValidateTokenAsync();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTokenAsync_Unauthorized_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            // Act & Assert
            await _client.Invoking(c => c.ValidateTokenAsync())
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Readwise API token is invalid or expired*");
        }


        [Fact]
        public async Task GetExportAsync_Success_ReturnsBooksWithNestedHighlights()
        {
            // Arrange
            var responseJson = @"{
                ""count"": 1,
                ""nextPageCursor"": ""cursor-2"",
                ""results"": [
                    {
                        ""user_book_id"": 123,
                        ""title"": ""Test Book"",
                        ""author"": ""Test Author"",
                        ""category"": ""books"",
                        ""source"": ""kindle"",
                        ""cover_image_url"": ""https://example.com/cover.jpg"",
                        ""highlights"": [
                            {
                                ""id"": 1,
                                ""text"": ""Highlight 1"",
                                ""note"": ""Note 1"",
                                ""location"": 100,
                                ""location_type"": ""location"",
                                ""highlighted_at"": ""2023-01-01T12:00:00Z"",
                                ""url"": ""https://readwise.io/open/1"",
                                ""color"": ""yellow"",
                                ""is_favorite"": false,
                                ""tags"": [{ ""id"": 1, ""name"": ""Important"" }]
                            }
                        ]
                    }
                ]
            }";

            SetupHttpResponse(HttpStatusCode.OK, responseJson);

            // Act
            var result = await _client.GetExportAsync();

            // Assert
            result.Should().NotBeNull();
            result.nextPageCursor.Should().Be("cursor-2");
            result.results.Should().HaveCount(1);
            result.results[0].title.Should().Be("Test Book");
            result.results[0].user_book_id.Should().Be(123);
            result.results[0].highlights.Should().HaveCount(1);
            result.results[0].highlights[0].text.Should().Be("Highlight 1");
            result.results[0].highlights[0].tags.Should().ContainSingle(t => t.name == "Important");
        }

        [Fact]
        public async Task GetExportAsync_WithUpdatedAfterAndCursor_IncludesQueryParameters()
        {
            // Arrange
            var responseJson = @"{""count"": 0, ""results"": []}";

            _mockHttpMessageHandler.OnSend = (req, ct) => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

            // Act
            await _client.GetExportAsync("2023-01-01T00:00:00Z", "abc123");

            // Assert
            var capturedRequest = _mockHttpMessageHandler.Requests.LastOrDefault();
            capturedRequest.Should().NotBeNull();
            capturedRequest.RequestUri.AbsolutePath.Should().EndWith("export/");
            capturedRequest.RequestUri.Query.Should().Contain("updatedAfter=");
            capturedRequest.RequestUri.Query.Should().Contain("pageCursor=abc123");
        }

        [Fact]
        public async Task GetExportAsync_Unauthorized_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            // Act & Assert — failures must propagate so a bad token can't
            // masquerade as an empty (successful) sync
            await _client.Invoking(c => c.GetExportAsync())
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Readwise API token is invalid or expired*");
        }

        [Fact]
        public async Task GetExportAsync_ServerError_ThrowsHttpRequestException()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

            // Act & Assert
            await _client.Invoking(c => c.GetExportAsync())
                .Should().ThrowAsync<HttpRequestException>()
                .WithMessage("*failed with status*");
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

