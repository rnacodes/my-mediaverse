using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.Shared.Interfaces;
using Xunit;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class OpenLibraryServiceTests
    {
        private readonly IOpenLibraryApiClient _mockApiClient;
        private readonly IBookService _mockBookService;
        private readonly IBookMappingService _mockMappingService;
        private readonly ILogger<OpenLibraryService> _mockLogger;
        private readonly IOpenLibraryService _openLibraryService;

        public OpenLibraryServiceTests()
        {
            _mockApiClient = Substitute.For<IOpenLibraryApiClient>();
            _mockBookService = Substitute.For<IBookService>();
            _mockMappingService = Substitute.For<IBookMappingService>();
            _mockLogger = Substitute.For<ILogger<OpenLibraryService>>();

            _openLibraryService = new OpenLibraryService(
                _mockApiClient,
                _mockBookService,
                _mockMappingService,
                _mockLogger);
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
                .SearchBooksAsync(query, offset, limit)
                .Returns(expectedResult);

            // Act
            var result = await _openLibraryService.SearchBooksAsync(query, offset, limit);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Received(1).SearchBooksAsync(query, offset, limit);
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
                .SearchBooksByTitleAsync(title, null, null)
                .Returns(expectedResult);

            // Act
            var result = await _openLibraryService.SearchBooksByTitleAsync(title);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Received(1).SearchBooksByTitleAsync(title, null, null);
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
                .GetBookByOpenLibraryIdAsync("OL123W")
                .Returns(workDto);

            _mockBookService
                .GetBookByTitleAndAuthorAsync("Test Book", Arg.Any<string>())
                .Returns((Book?)null);

            _mockMappingService
                .MapFromOpenLibraryAsync(Arg.Any<OpenLibraryBookDto>())
                .Returns(mappedBook);

            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>())
                .Returns(createdBook);

            // Act
            var result = await _openLibraryService.ImportBookFromOpenLibraryKeyAsync(openLibraryKey);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Received(1).GetBookByOpenLibraryIdAsync("OL123W");
            _mockBookService.Received(1).GetBookByTitleAndAuthorAsync("Test Book", Arg.Any<string>());
            _mockMappingService.Received(1).MapFromOpenLibraryAsync(Arg.Any<OpenLibraryBookDto>());
            _mockBookService.Received(1).CreateBookAsync(Arg.Any<CreateBookDto>());
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
                .GetBookByOpenLibraryIdAsync("OL123W")
                .Returns(workDto);

            _mockBookService
                .GetBookByTitleAndAuthorAsync("Test Book", Arg.Any<string>())
                .Returns(existingBook);

            // Act
            var result = await _openLibraryService.ImportBookFromOpenLibraryKeyAsync(openLibraryKey);

            // Assert
            Assert.Equal(existingBook, result);
            _mockApiClient.Received(1).GetBookByOpenLibraryIdAsync("OL123W");
            _mockBookService.Received(1).GetBookByTitleAndAuthorAsync("Test Book", Arg.Any<string>());
            _mockMappingService.DidNotReceive().MapFromOpenLibraryAsync(Arg.Any<OpenLibraryBookDto>());
            _mockBookService.DidNotReceive().CreateBookAsync(Arg.Any<CreateBookDto>());
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
                .SearchBooksByISBNAsync(isbn)
                .Returns(searchResult);

            _mockBookService
                .GetBookByTitleAndAuthorAsync("Test Book", Arg.Any<string>())
                .Returns((Book?)null);

            _mockMappingService
                .MapFromOpenLibraryAsync(Arg.Any<OpenLibraryBookDto>())
                .Returns(mappedBook);

            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>())
                .Returns(createdBook);

            // Act
            var result = await _openLibraryService.ImportBookFromISBNAsync(isbn);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Received(1).SearchBooksByISBNAsync(isbn);
            _mockBookService.Received(1).GetBookByTitleAndAuthorAsync("Test Book", Arg.Any<string>());
            _mockMappingService.Received(1).MapFromOpenLibraryAsync(Arg.Any<OpenLibraryBookDto>());
            _mockBookService.Received(1).CreateBookAsync(Arg.Any<CreateBookDto>());
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
                .SearchBooksByISBNAsync(isbn)
                .Returns(searchResult);

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
                .SearchBooksAsync($"title:{title} author:{author}", null, 1)
                .Returns(searchResult);

            _mockBookService
                .GetBookByTitleAndAuthorAsync(title, Arg.Any<string>())
                .Returns((Book?)null);

            _mockMappingService
                .MapFromOpenLibraryAsync(Arg.Any<OpenLibraryBookDto>())
                .Returns(mappedBook);

            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>())
                .Returns(createdBook);

            // Act
            var result = await _openLibraryService.ImportBookFromTitleAndAuthorAsync(title, author);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Received(1).SearchBooksAsync($"title:{title} author:{author}", null, 1);
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
                .SearchBooksByTitleAsync(title, null, 1)
                .Returns(searchResult);

            _mockBookService
                .GetBookByTitleAndAuthorAsync(title, Arg.Any<string>())
                .Returns((Book?)null);

            _mockMappingService
                .MapFromOpenLibraryAsync(Arg.Any<OpenLibraryBookDto>())
                .Returns(mappedBook);

            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>())
                .Returns(createdBook);

            // Act
            var result = await _openLibraryService.ImportBookFromTitleAndAuthorAsync(title);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Received(1).SearchBooksByTitleAsync(title, null, 1);
        }

        [Theory]
        [InlineData(12345, "L", "https://covers.openlibrary.org/b/id/12345-L.jpg")]
        [InlineData(12345, "M", "https://covers.openlibrary.org/b/id/12345-M.jpg")]
        [InlineData(null, "L", "")]
        public void GetCoverImageUrl_WithVariousInputs_ReturnsExpectedUrl(int? coverId, string size, string expectedUrl)
        {
            // Arrange
            _mockApiClient
                .GetCoverImageUrl(coverId, size)
                .Returns(expectedUrl);

            // Act
            var result = _openLibraryService.GetCoverImageUrl(coverId, size);

            // Assert
            Assert.Equal(expectedUrl, result);
            _mockApiClient.Received(1).GetCoverImageUrl(coverId, size);
        }
    }
}
