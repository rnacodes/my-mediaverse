using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.Shared.Interfaces;
using Xunit;

namespace MyMediaVerse.UnitTests.Application
{
    public class OpenLibraryServiceTests
    {
        private readonly Mock<IOpenLibraryApiClient> _mockApiClient;
        private readonly Mock<IBookService> _mockBookService;
        private readonly Mock<IBookMappingService> _mockMappingService;
        private readonly Mock<ILogger<OpenLibraryService>> _mockLogger;
        private readonly IOpenLibraryService _openLibraryService;

        public OpenLibraryServiceTests()
        {
            _mockApiClient = new Mock<IOpenLibraryApiClient>();
            _mockBookService = new Mock<IBookService>();
            _mockMappingService = new Mock<IBookMappingService>();
            _mockLogger = new Mock<ILogger<OpenLibraryService>>();

            _openLibraryService = new OpenLibraryService(
                _mockApiClient.Object,
                _mockBookService.Object,
                _mockMappingService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task SearchBooksAsync_CallsApiClient_ReturnsResult()
        {
            // Arrange
            var query = "test book";
            var offset = 0;
            var limit = 10;
            var expectedResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Start = 0,
                Docs = new[]
                {
                    new OpenLibraryBookDto { Title = "Test Book" }
                }
            };

            _mockApiClient
                .Setup(x => x.SearchBooksAsync(query, offset, limit))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _openLibraryService.SearchBooksAsync(query, offset, limit);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Verify(x => x.SearchBooksAsync(query, offset, limit), Times.Once);
        }

        [Fact]
        public async Task SearchBooksByTitleAsync_CallsApiClient_ReturnsResult()
        {
            // Arrange
            var title = "Test Book";
            var expectedResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Start = 0,
                Docs = new[]
                {
                    new OpenLibraryBookDto { Title = title }
                }
            };

            _mockApiClient
                .Setup(x => x.SearchBooksByTitleAsync(title, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _openLibraryService.SearchBooksByTitleAsync(title);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Verify(x => x.SearchBooksByTitleAsync(title, null, null), Times.Once);
        }

        [Fact]
        public async Task ImportBookFromOpenLibraryKeyAsync_WithNewBook_CreatesAndReturnsBook()
        {
            // Arrange
            var openLibraryKey = "/works/OL123W";
            var workDto = new OpenLibraryWorkDto
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
                Subjects = new[] { "Fiction" },
                Covers = new[] { 12345 },
            };

            var mappedBook = new Book
            {
                Title = "Test Book",
                Author = "Test Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            var createdBook = new Book
            {
                Id = Guid.NewGuid(),
                Title = "Test Book",
                Author = "Test Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            _mockApiClient
                .Setup(x => x.GetBookByOpenLibraryIdAsync("OL123W"))
                .ReturnsAsync(workDto);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()))
                .ReturnsAsync((Book?)null);

            _mockMappingService
                .Setup(x => x.MapFromOpenLibraryAsync(It.IsAny<OpenLibraryBookDto>()))
                .ReturnsAsync(mappedBook);

            _mockBookService
                .Setup(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()))
                .ReturnsAsync(createdBook);

            // Act
            var result = await _openLibraryService.ImportBookFromOpenLibraryKeyAsync(openLibraryKey);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Verify(x => x.GetBookByOpenLibraryIdAsync("OL123W"), Times.Once);
            _mockBookService.Verify(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()), Times.Once);
            _mockMappingService.Verify(x => x.MapFromOpenLibraryAsync(It.IsAny<OpenLibraryBookDto>()), Times.Once);
            _mockBookService.Verify(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()), Times.Once);
        }

        [Fact]
        public async Task ImportBookFromOpenLibraryKeyAsync_WithExistingBook_ReturnsExistingBook()
        {
            // Arrange
            var openLibraryKey = "/works/OL123W";
            var workDto = new OpenLibraryWorkDto
            {
                Key = "/works/OL123W",
                Title = "Test Book",
            };

            var existingBook = new Book
            {
                Id = Guid.NewGuid(),
                Title = "Test Book",
                Author = "Test Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            _mockApiClient
                .Setup(x => x.GetBookByOpenLibraryIdAsync("OL123W"))
                .ReturnsAsync(workDto);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()))
                .ReturnsAsync(existingBook);

            // Act
            var result = await _openLibraryService.ImportBookFromOpenLibraryKeyAsync(openLibraryKey);

            // Assert
            Assert.Equal(existingBook, result);
            _mockApiClient.Verify(x => x.GetBookByOpenLibraryIdAsync("OL123W"), Times.Once);
            _mockBookService.Verify(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()), Times.Once);
            _mockMappingService.Verify(x => x.MapFromOpenLibraryAsync(It.IsAny<OpenLibraryBookDto>()), Times.Never);
            _mockBookService.Verify(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()), Times.Never);
        }

        [Fact]
        public async Task ImportBookFromISBNAsync_WithValidISBN_CreatesAndReturnsBook()
        {
            // Arrange
            var isbn = "9780123456789";
            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Docs = new[]
                {
                    new OpenLibraryBookDto
                    {
                        Title = "Test Book",
                        AuthorName = new[] { "Test Author" },
                        Isbn = new[] { isbn },
                        FirstPublishYear = 2020
                    }
                }
            };

            var mappedBook = new Book
            {
                Title = "Test Book",
                Author = "Test Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            var createdBook = new Book
            {
                Id = Guid.NewGuid(),
                Title = "Test Book",
                Author = "Test Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            _mockApiClient
                .Setup(x => x.SearchBooksByISBNAsync(isbn))
                .ReturnsAsync(searchResult);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()))
                .ReturnsAsync((Book?)null);

            _mockMappingService
                .Setup(x => x.MapFromOpenLibraryAsync(It.IsAny<OpenLibraryBookDto>()))
                .ReturnsAsync(mappedBook);

            _mockBookService
                .Setup(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()))
                .ReturnsAsync(createdBook);

            // Act
            var result = await _openLibraryService.ImportBookFromISBNAsync(isbn);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Verify(x => x.SearchBooksByISBNAsync(isbn), Times.Once);
            _mockBookService.Verify(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()), Times.Once);
            _mockMappingService.Verify(x => x.MapFromOpenLibraryAsync(It.IsAny<OpenLibraryBookDto>()), Times.Once);
            _mockBookService.Verify(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()), Times.Once);
        }

        [Fact]
        public async Task ImportBookFromISBNAsync_WithNotFoundISBN_ThrowsException()
        {
            // Arrange
            var isbn = "9780123456789";
            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 0,
                Docs = Array.Empty<OpenLibraryBookDto>()
            };

            _mockApiClient
                .Setup(x => x.SearchBooksByISBNAsync(isbn))
                .ReturnsAsync(searchResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _openLibraryService.ImportBookFromISBNAsync(isbn));
            
            Assert.Contains($"Book with ISBN {isbn} not found", exception.Message);
        }

        [Fact]
        public async Task ImportBookFromTitleAndAuthorAsync_WithTitleAndAuthor_CreatesAndReturnsBook()
        {
            // Arrange
            var title = "Test Book";
            var author = "Test Author";
            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Docs = new[]
                {
                    new OpenLibraryBookDto
                    {
                        Title = title,
                        AuthorName = new[] { author },
                        FirstPublishYear = 2020
                    }
                }
            };

            var mappedBook = new Book
            {
                Title = title,
                Author = author,
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            var createdBook = new Book
            {
                Id = Guid.NewGuid(),
                Title = title,
                Author = author,
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            _mockApiClient
                .Setup(x => x.SearchBooksAsync($"title:{title} author:{author}", null, 1))
                .ReturnsAsync(searchResult);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync(title, It.IsAny<string>()))
                .ReturnsAsync((Book?)null);

            _mockMappingService
                .Setup(x => x.MapFromOpenLibraryAsync(It.IsAny<OpenLibraryBookDto>()))
                .ReturnsAsync(mappedBook);

            _mockBookService
                .Setup(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()))
                .ReturnsAsync(createdBook);

            // Act
            var result = await _openLibraryService.ImportBookFromTitleAndAuthorAsync(title, author);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Verify(x => x.SearchBooksAsync($"title:{title} author:{author}", null, 1), Times.Once);
        }

        [Fact]
        public async Task ImportBookFromTitleAndAuthorAsync_WithTitleOnly_UsesCorrectSearchMethod()
        {
            // Arrange
            var title = "Test Book";
            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Docs = new[]
                {
                    new OpenLibraryBookDto
                    {
                        Title = title,
                        AuthorName = new[] { "Some Author" },
                        FirstPublishYear = 2020
                    }
                }
            };

            var mappedBook = new Book
            {
                Title = title,
                Author = "Some Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            var createdBook = new Book
            {
                Id = Guid.NewGuid(),
                Title = title,
                Author = "Some Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            _mockApiClient
                .Setup(x => x.SearchBooksByTitleAsync(title, null, 1))
                .ReturnsAsync(searchResult);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync(title, It.IsAny<string>()))
                .ReturnsAsync((Book?)null);

            _mockMappingService
                .Setup(x => x.MapFromOpenLibraryAsync(It.IsAny<OpenLibraryBookDto>()))
                .ReturnsAsync(mappedBook);

            _mockBookService
                .Setup(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()))
                .ReturnsAsync(createdBook);

            // Act
            var result = await _openLibraryService.ImportBookFromTitleAndAuthorAsync(title);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Verify(x => x.SearchBooksByTitleAsync(title, null, 1), Times.Once);
        }

        [Theory]
        [InlineData(12345, "L", "https://covers.openlibrary.org/b/id/12345-L.jpg")]
        [InlineData(12345, "M", "https://covers.openlibrary.org/b/id/12345-M.jpg")]
        [InlineData(null, "L", "")]
        public void GetCoverImageUrl_WithVariousInputs_ReturnsExpectedUrl(int? coverId, string size, string expectedUrl)
        {
            // Arrange
            _mockApiClient
                .Setup(x => x.GetCoverImageUrl(coverId, size))
                .Returns(expectedUrl);

            // Act
            var result = _openLibraryService.GetCoverImageUrl(coverId, size);

            // Assert
            Assert.Equal(expectedUrl, result);
            _mockApiClient.Verify(x => x.GetCoverImageUrl(coverId, size), Times.Once);
        }
    }
}
