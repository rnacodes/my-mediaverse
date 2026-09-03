using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.Interfaces;
using Xunit;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class GoogleBooksServiceTests
    {
        private readonly IGoogleBooksApiClient _mockApiClient;
        private readonly IBookService _mockBookService;
        private readonly IBookMappingService _mockMappingService;
        private readonly ILogger<GoogleBooksService> _mockLogger;
        private readonly IGoogleBooksService _googleBooksService;

        public GoogleBooksServiceTests()
        {
            _mockApiClient = Substitute.For<IGoogleBooksApiClient>();
            _mockBookService = Substitute.For<IBookService>();
            _mockMappingService = Substitute.For<IBookMappingService>();
            _mockLogger = Substitute.For<ILogger<GoogleBooksService>>();

            _googleBooksService = new GoogleBooksService(
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
            var expectedResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    new GoogleBooksVolumeDto
                    {
                        Id = "abc123",
                        VolumeInfo = new GoogleBooksVolumeInfoDto { Title = "Test Book" }
                    }
                }
            };

            _mockApiClient
                .SearchBooksAsync(query, offset, limit)
                .Returns(expectedResult);

            // Act
            var result = await _googleBooksService.SearchBooksAsync(query, offset, limit);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Received(1).SearchBooksAsync(query, offset, limit);
        }

        [Fact]
        public async Task SearchBooksByTitleAsync_CallsApiClient_ReturnsResult()
        {
            // Arrange
            var title = "Test Book";
            var expectedResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    new GoogleBooksVolumeDto
                    {
                        Id = "abc123",
                        VolumeInfo = new GoogleBooksVolumeInfoDto { Title = title }
                    }
                }
            };

            _mockApiClient
                .SearchBooksByTitleAsync(title, null, null)
                .Returns(expectedResult);

            // Act
            var result = await _googleBooksService.SearchBooksByTitleAsync(title);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Received(1).SearchBooksByTitleAsync(title, null, null);
        }

        [Fact]
        public async Task SearchBooksByAuthorAsync_CallsApiClient_ReturnsResult()
        {
            // Arrange
            var author = "Test Author";
            var expectedResult = new GoogleBooksSearchResultDto
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

            _mockApiClient
                .SearchBooksByAuthorAsync(author, null, null)
                .Returns(expectedResult);

            // Act
            var result = await _googleBooksService.SearchBooksByAuthorAsync(author);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Received(1).SearchBooksByAuthorAsync(author, null, null);
        }

        [Fact]
        public async Task SearchBooksByISBNAsync_CallsApiClient_ReturnsResult()
        {
            // Arrange
            var isbn = "9780123456789";
            var expectedResult = new GoogleBooksSearchResultDto
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
                                new GoogleBooksIndustryIdentifierDto { Type = "ISBN_13", Identifier = isbn }
                            }
                        }
                    }
                }
            };

            _mockApiClient
                .SearchBooksByISBNAsync(isbn)
                .Returns(expectedResult);

            // Act
            var result = await _googleBooksService.SearchBooksByISBNAsync(isbn);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Received(1).SearchBooksByISBNAsync(isbn);
        }

        [Fact]
        public async Task ImportBookFromVolumeIdAsync_WithNewBook_CreatesAndReturnsBook()
        {
            // Arrange
            var volumeId = "abc123";
            var volumeDto = new GoogleBooksVolumeDto
            {
                Id = volumeId,
                VolumeInfo = new GoogleBooksVolumeInfoDto
                {
                    Title = "Test Book",
                    Authors = new[] { "Test Author" },
                    Description = "A test description",
                    Publisher = "Test Publisher"
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
                .GetVolumeByIdAsync(volumeId)
                .Returns(volumeDto);

            _mockMappingService
                .MapFromGoogleBooksAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedBook);

            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>(), Arg.Any<BookIdentity?>())
                .Returns(new BookCreationResult(createdBook, Created: true));

            // Act
            var result = await _googleBooksService.ImportBookFromVolumeIdAsync(volumeId);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Received(1).GetVolumeByIdAsync(volumeId);
            _mockMappingService.Received(1).MapFromGoogleBooksAsync(Arg.Any<GoogleBooksVolumeDto>());
            // The Google volume id must ride along so dedup + persistence can use it
            await _mockBookService.Received(1).CreateBookAsync(
                Arg.Any<CreateBookDto>(),
                Arg.Is<BookIdentity?>(i => i != null && i.GoogleVolumeId == volumeId));
        }

        [Fact]
        public async Task ImportBookFromVolumeIdAsync_WithExistingBook_ReturnsExistingBook()
        {
            // Arrange
            var volumeId = "abc123";
            var volumeDto = new GoogleBooksVolumeDto
            {
                Id = volumeId,
                VolumeInfo = new GoogleBooksVolumeInfoDto
                {
                    Title = "Test Book",
                    Authors = new[] { "Test Author" }
                }
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
                .GetVolumeByIdAsync(volumeId)
                .Returns(volumeDto);

            _mockMappingService
                .MapFromGoogleBooksAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(new Book
                {
                    Title = "Test Book",
                    Author = "Test Author",
                    MediaType = MediaType.Book,
                    Status = Status.Uncharted,
                    DateAdded = DateTime.UtcNow
                });

            // Dedup now happens inside CreateBookAsync, which reports the match
            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>(), Arg.Any<BookIdentity?>())
                .Returns(new BookCreationResult(existingBook, Created: false));

            // Act
            var result = await _googleBooksService.ImportBookFromVolumeIdAsync(volumeId);

            // Assert
            Assert.Equal(existingBook, result);
            _mockApiClient.Received(1).GetVolumeByIdAsync(volumeId);
            await _mockBookService.Received(1).CreateBookAsync(Arg.Any<CreateBookDto>(), Arg.Any<BookIdentity?>());
        }

        [Fact]
        public async Task ImportBookFromVolumeIdAsync_WithNotFoundVolume_ThrowsException()
        {
            // Arrange
            var volumeId = "invalid-id";

            _mockApiClient
                .GetVolumeByIdAsync(volumeId)
                .Returns((GoogleBooksVolumeDto?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _googleBooksService.ImportBookFromVolumeIdAsync(volumeId));

            Assert.Contains($"Volume with ID {volumeId} not found in Google Books", exception.Message);
        }

        [Fact]
        public async Task ImportBookFromISBNAsync_WithValidISBN_CreatesAndReturnsBook()
        {
            // Arrange
            var isbn = "9780123456789";
            var searchResult = new GoogleBooksSearchResultDto
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
                            IndustryIdentifiers = new[]
                            {
                                new GoogleBooksIndustryIdentifierDto { Type = "ISBN_13", Identifier = isbn }
                            }
                        }
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

            _mockMappingService
                .MapFromGoogleBooksAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedBook);

            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>(), Arg.Any<BookIdentity?>())
                .Returns(new BookCreationResult(createdBook, Created: true));

            // Act
            var result = await _googleBooksService.ImportBookFromISBNAsync(isbn);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Received(1).SearchBooksByISBNAsync(isbn);
            _mockMappingService.Received(1).MapFromGoogleBooksAsync(Arg.Any<GoogleBooksVolumeDto>());
            await _mockBookService.Received(1).CreateBookAsync(
                Arg.Any<CreateBookDto>(),
                Arg.Is<BookIdentity?>(i => i != null && i.GoogleVolumeId == "abc123"));
        }

        [Fact]
        public async Task ImportBookFromISBNAsync_WithNotFoundISBN_ThrowsException()
        {
            // Arrange
            var isbn = "9780123456789";
            var searchResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 0,
                Items = null
            };

            _mockApiClient
                .SearchBooksByISBNAsync(isbn)
                .Returns(searchResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _googleBooksService.ImportBookFromISBNAsync(isbn));

            Assert.Contains($"Book with ISBN {isbn} not found", exception.Message);
        }

        [Fact]
        public async Task ImportBookFromTitleAndAuthorAsync_WithTitleAndAuthor_CreatesAndReturnsBook()
        {
            // Arrange
            var title = "Test Book";
            var author = "Test Author";
            var searchResult = new GoogleBooksSearchResultDto
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
                            Authors = new[] { author }
                        }
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

            // The service uses "intitle:{title} inauthor:{author}" (space, not +) and maxResults: 1
            _mockApiClient
                .SearchBooksAsync($"intitle:{title} inauthor:{author}", null, 1)
                .Returns(searchResult);

            _mockMappingService
                .MapFromGoogleBooksAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedBook);

            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>(), Arg.Any<BookIdentity?>())
                .Returns(new BookCreationResult(createdBook, Created: true));

            // Act
            var result = await _googleBooksService.ImportBookFromTitleAndAuthorAsync(title, author);

            // Assert
            Assert.Equal(createdBook, result);
        }

        [Fact]
        public async Task ImportBookFromTitleAndAuthorAsync_WithTitleOnly_UsesCorrectSearchMethod()
        {
            // Arrange
            var title = "Test Book";
            var searchResult = new GoogleBooksSearchResultDto
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
                            Authors = new[] { "Some Author" }
                        }
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

            _mockMappingService
                .MapFromGoogleBooksAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedBook);

            _mockBookService
                .CreateBookAsync(Arg.Any<CreateBookDto>(), Arg.Any<BookIdentity?>())
                .Returns(new BookCreationResult(createdBook, Created: true));

            // Act
            var result = await _googleBooksService.ImportBookFromTitleAndAuthorAsync(title);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Received(1).SearchBooksByTitleAsync(title, null, 1);
        }

        [Fact]
        public async Task ImportBookFromTitleAndAuthorAsync_WithNoResults_ThrowsException()
        {
            // Arrange
            var title = "Nonexistent Book";
            var searchResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 0,
                Items = null
            };

            _mockApiClient
                .SearchBooksByTitleAsync(title, null, 1)
                .Returns(searchResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _googleBooksService.ImportBookFromTitleAndAuthorAsync(title));

            // Error message format: "Book with title '{title}' and author '' not found in Google Books"
            Assert.Contains($"Book with title '{title}'", exception.Message);
            Assert.Contains("not found in Google Books", exception.Message);
        }
    }
}
