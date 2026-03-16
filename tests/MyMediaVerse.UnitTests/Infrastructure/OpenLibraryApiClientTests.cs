using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyMediaVerse.Infrastructure.Clients;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.Shared.Interfaces;
using Xunit;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public class OpenLibraryApiClientTests
    {
        private readonly Mock<ILogger<OpenLibraryApiClient>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly IOpenLibraryApiClient _openLibraryApiClient;

        public OpenLibraryApiClientTests()
        {
            _mockLogger = new Mock<ILogger<OpenLibraryApiClient>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://openlibrary.org/")
            };
            _openLibraryApiClient = new OpenLibraryApiClient(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task SearchBooksAsync_WithValidQuery_ReturnsSearchResult()
        {
            // Arrange
            var query = "test book";
            var expectedResponse = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Start = 0,
                NumFoundExact = true,
                Docs = new[]
                {
                    new OpenLibraryBookDto
                    {
                        Key = "/works/OL123W",
                        Title = "Test Book",
                        AuthorName = new[] { "Test Author" },
                        FirstPublishYear = 2020
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
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

            // Act
            var result = await _openLibraryApiClient.SearchBooksAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NumFound);
            Assert.Single(result.Docs);
            Assert.Equal("Test Book", result.Docs[0].Title);
            Assert.Equal("Test Author", result.Docs[0].AuthorName?[0]);
        }

        [Fact]
        public async Task SearchBooksByTitleAsync_WithValidTitle_ReturnsSearchResult()
        {
            // Arrange
            var title = "Test Book";
            var expectedResponse = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Start = 0,
                NumFoundExact = true,
                Docs = new[]
                {
                    new OpenLibraryBookDto
                    {
                        Key = "/works/OL123W",
                        Title = title,
                        AuthorName = new[] { "Test Author" }
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
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

            // Act
            var result = await _openLibraryApiClient.SearchBooksByTitleAsync(title);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NumFound);
            Assert.Single(result.Docs);
            Assert.Equal(title, result.Docs[0].Title);
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_WithValidAuthor_ReturnsSearchResult()
        {
            // Arrange
            var author = "Test Author";
            var expectedResponse = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Start = 0,
                NumFoundExact = true,
                Docs = new[]
                {
                    new OpenLibraryBookDto
                    {
                        Key = "/works/OL123W",
                        Title = "Test Book",
                        AuthorName = new[] { author }
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
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

            // Act
            var result = await _openLibraryApiClient.SearchBooksByAuthorAsync(author);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NumFound);
            Assert.Single(result.Docs);
            Assert.Equal(author, result.Docs[0].AuthorName?[0]);
        }

        [Fact]
        public async Task SearchBooksByISBNAsync_WithValidISBN_ReturnsSearchResult()
        {
            // Arrange
            var isbn = "9780123456789";
            var expectedResponse = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Start = 0,
                NumFoundExact = true,
                Docs = new[]
                {
                    new OpenLibraryBookDto
                    {
                        Key = "/works/OL123W",
                        Title = "Test Book",
                        Isbn = new[] { isbn }
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
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

            // Act
            var result = await _openLibraryApiClient.SearchBooksByISBNAsync(isbn);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.NumFound);
            Assert.Single(result.Docs);
            Assert.Equal(isbn, result.Docs[0].Isbn?[0]);
        }

        [Fact]
        public async Task GetBookByOpenLibraryIdAsync_WithValidId_ReturnsWorkDto()
        {
            // Arrange
            var openLibraryId = "OL123W";
            var expectedResponse = new OpenLibraryWorkDto
            {
                Key = "/works/OL123W",
                Title = "Test Book",
                Authors = new[]
                {
                    new OpenLibraryAuthorReference
                    {
                        Author = new OpenLibraryTypeReference { Key = "/authors/OL456A" }
                    }
                },
                Subjects = new[] { "Fiction", "Adventure" },
                Covers = new[] { 12345 }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
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

            // Act
            var result = await _openLibraryApiClient.GetBookByOpenLibraryIdAsync(openLibraryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("/works/OL123W", result.Key);
            Assert.Equal("Test Book", result.Title);
            Assert.NotNull(result.Authors);
            Assert.Single(result.Authors);
        }

        [Fact]
        public async Task GetBookByISBNAsync_WithValidISBN_ReturnsBookDto()
        {
            // Arrange
            var isbn = "9780123456789";
            var expectedResponse = new OpenLibraryBookDto
            {
                Key = "/books/OL123M",
                Title = "Test Book",
                Isbn = new[] { isbn },
                AuthorName = new[] { "Test Author" }
            };

            var jsonResponse = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
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

            // Act
            var result = await _openLibraryApiClient.GetBookByISBNAsync(isbn);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("/books/OL123M", result.Key);
            Assert.Equal("Test Book", result.Title);
            Assert.Equal(isbn, result.Isbn?[0]);
        }

        [Fact]
        public async Task GetAuthorAsync_WithValidId_ReturnsAuthorDto()
        {
            // Arrange
            var authorId = "OL456A";
            var expectedResponse = new OpenLibraryAuthorDto
            {
                Key = "/authors/OL456A",
                Name = "Test Author",
                BirthDate = "1970",
                Bio = "Test biography"
            };

            var jsonResponse = JsonSerializer.Serialize(expectedResponse, new JsonSerializerOptions
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

            // Act
            var result = await _openLibraryApiClient.GetAuthorAsync(authorId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("/authors/OL456A", result.Key);
            Assert.Equal("Test Author", result.Name);
            Assert.Equal("1970", result.BirthDate);
        }

        [Theory]
        [InlineData(12345, "L", "https://covers.openlibrary.org/b/id/12345-L.jpg")]
        [InlineData(12345, "M", "https://covers.openlibrary.org/b/id/12345-M.jpg")]
        [InlineData(12345, "S", "https://covers.openlibrary.org/b/id/12345-S.jpg")]
        [InlineData(null, "L", "")]
        public void GetCoverImageUrl_WithVariousInputs_ReturnsExpectedUrl(int? coverId, string size, string expectedUrl)
        {
            // Act
            var result = _openLibraryApiClient.GetCoverImageUrl(coverId, size);

            // Assert
            Assert.Equal(expectedUrl, result);
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
            await Assert.ThrowsAsync<HttpRequestException>(() => _openLibraryApiClient.SearchBooksAsync(query));
        }

        [Fact]
        public async Task GetBookByOpenLibraryIdAsync_WithNotFound_ThrowsException()
        {
            // Arrange
            var openLibraryId = "INVALID";

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

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _openLibraryApiClient.GetBookByOpenLibraryIdAsync(openLibraryId));
        }

    }
}
