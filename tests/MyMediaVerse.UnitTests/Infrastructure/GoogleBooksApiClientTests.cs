using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyMediaVerse.Infrastructure.Clients;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.Interfaces;
using Xunit;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class GoogleBooksApiClientTests
    {
        private readonly Mock<ILogger<GoogleBooksApiClient>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly IGoogleBooksApiClient _googleBooksApiClient;

        public GoogleBooksApiClientTests()
        {
            _mockLogger = new Mock<ILogger<GoogleBooksApiClient>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            // Setup configuration to return a test API key
            _mockConfiguration.Setup(x => x["GoogleBooks:ApiKey"]).Returns("test-api-key");

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://www.googleapis.com/books/v1/")
            };

            _googleBooksApiClient = new GoogleBooksApiClient(_httpClient, _mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task SearchBooksAsync_WithValidQuery_ReturnsSearchResult()
        {
            // Arrange
            var query = "test book";
            var expectedResponse = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    new GoogleBooksVolumeDto
                    {
                        Id = "abc123",
                        VolumeInfo = new GoogleBooksVolumeInfoDto
                        {
                            Title = "Test Book",
                            Authors = new[] { "Test Author" },
                            PublishedDate = "2020"
                        }
                    }
                }
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.SearchBooksAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems);
            Assert.Single(result.Items!);
            Assert.Equal("Test Book", result.Items![0].VolumeInfo?.Title);
            Assert.Equal("Test Author", result.Items[0].VolumeInfo?.Authors?[0]);
        }

        [Fact]
        public async Task SearchBooksByTitleAsync_WithValidTitle_ReturnsSearchResult()
        {
            // Arrange
            var title = "Test Book";
            var expectedResponse = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    new GoogleBooksVolumeDto
                    {
                        Id = "abc123",
                        VolumeInfo = new GoogleBooksVolumeInfoDto
                        {
                            Title = title,
                            Authors = new[] { "Test Author" }
                        }
                    }
                }
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.SearchBooksByTitleAsync(title);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems);
            Assert.Single(result.Items!);
            Assert.Equal(title, result.Items![0].VolumeInfo?.Title);
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_WithValidAuthor_ReturnsSearchResult()
        {
            // Arrange
            var author = "Test Author";
            var expectedResponse = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    new GoogleBooksVolumeDto
                    {
                        Id = "abc123",
                        VolumeInfo = new GoogleBooksVolumeInfoDto
                        {
                            Title = "Test Book",
                            Authors = new[] { author }
                        }
                    }
                }
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.SearchBooksByAuthorAsync(author);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems);
            Assert.Single(result.Items!);
            Assert.Equal(author, result.Items![0].VolumeInfo?.Authors?[0]);
        }

        [Fact]
        public async Task SearchBooksByISBNAsync_WithValidISBN_ReturnsSearchResult()
        {
            // Arrange
            var isbn = "9780123456789";
            var expectedResponse = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    new GoogleBooksVolumeDto
                    {
                        Id = "abc123",
                        VolumeInfo = new GoogleBooksVolumeInfoDto
                        {
                            Title = "Test Book",
                            IndustryIdentifiers = new[]
                            {
                                new GoogleBooksIndustryIdentifierDto
                                {
                                    Type = "ISBN_13",
                                    Identifier = isbn
                                }
                            }
                        }
                    }
                }
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.SearchBooksByISBNAsync(isbn);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems);
            Assert.Single(result.Items!);
            Assert.Equal(isbn, result.Items![0].VolumeInfo?.IndustryIdentifiers?[0].Identifier);
        }

        [Fact]
        public async Task GetVolumeByIdAsync_WithValidId_ReturnsVolumeDto()
        {
            // Arrange
            var volumeId = "abc123";
            var expectedResponse = new GoogleBooksVolumeDto
            {
                Id = volumeId,
                VolumeInfo = new GoogleBooksVolumeInfoDto
                {
                    Title = "Test Book",
                    Authors = new[] { "Test Author" },
                    Description = "A test book description",
                    Publisher = "Test Publisher",
                    PublishedDate = "2020-01-01"
                }
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.GetVolumeByIdAsync(volumeId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(volumeId, result.Id);
            Assert.Equal("Test Book", result.VolumeInfo?.Title);
            Assert.Equal("A test book description", result.VolumeInfo?.Description);
        }

        [Fact]
        public async Task GetBookDescriptionByISBNAsync_WithValidISBN_ReturnsDescription()
        {
            // Arrange
            var isbn = "9780123456789";
            var description = "A fascinating test book about testing.";
            var expectedResponse = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    new GoogleBooksVolumeDto
                    {
                        Id = "abc123",
                        VolumeInfo = new GoogleBooksVolumeInfoDto
                        {
                            Title = "Test Book",
                            Description = description
                        }
                    }
                }
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.GetBookDescriptionByISBNAsync(isbn);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(description, result);
        }

        [Fact]
        public async Task GetBookDescriptionByISBNAsync_WithNoResults_ReturnsNull()
        {
            // Arrange
            var isbn = "9780123456789";
            var expectedResponse = new GoogleBooksSearchResultDto
            {
                TotalItems = 0,
                Items = null
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.GetBookDescriptionByISBNAsync(isbn);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBookDescriptionByISBNAsync_WithHtmlDescription_ReturnsStrippedHtml()
        {
            // Arrange
            var isbn = "9780123456789";
            var htmlDescription = "<p>A <b>fascinating</b> test book about testing.</p>";
            var expectedResponse = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    new GoogleBooksVolumeDto
                    {
                        Id = "abc123",
                        VolumeInfo = new GoogleBooksVolumeInfoDto
                        {
                            Title = "Test Book",
                            Description = htmlDescription
                        }
                    }
                }
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.GetBookDescriptionByISBNAsync(isbn);

            // Assert
            Assert.NotNull(result);
            // HTML tags should be stripped
            Assert.DoesNotContain("<p>", result);
            Assert.DoesNotContain("<b>", result);
            Assert.Contains("fascinating", result);
        }

        [Fact]
        public async Task SearchBooksAsync_WithPaginationParameters_IncludesInRequest()
        {
            // Arrange
            var query = "test";
            var startIndex = 10;
            var maxResults = 20;
            var expectedResponse = new GoogleBooksSearchResultDto
            {
                TotalItems = 100,
                Items = new[] { new GoogleBooksVolumeDto { Id = "abc123" } }
            };

            SetupMockHttpResponse(expectedResponse);

            // Act
            var result = await _googleBooksApiClient.SearchBooksAsync(query, startIndex, maxResults);

            // Assert
            Assert.NotNull(result);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains($"startIndex={startIndex}") &&
                    req.RequestUri.ToString().Contains($"maxResults={maxResults}")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task SearchBooksAsync_WithHttpError_ThrowsException()
        {
            // Arrange
            var query = "test book";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal Server Error")
                });

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _googleBooksApiClient.SearchBooksAsync(query));
        }

        [Fact]
        public async Task GetVolumeByIdAsync_WithNotFound_ReturnsNull()
        {
            // Arrange
            var volumeId = "invalid-id";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("Not Found")
                });

            // Act
            var result = await _googleBooksApiClient.GetVolumeByIdAsync(volumeId);

            // Assert
            Assert.Null(result);
        }

        private void SetupMockHttpResponse<T>(T responseObject)
        {
            var jsonResponse = JsonSerializer.Serialize(responseObject, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });
        }
    }
}
