using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.Interfaces;
using Xunit;

namespace MyMediaVerse.UnitTests.Application
{
    public class GoogleBooksServiceTests
    {
        private readonly Mock<IGoogleBooksApiClient> _mockApiClient;
        private readonly Mock<IBookService> _mockBookService;
        private readonly Mock<IBookMappingService> _mockMappingService;
        private readonly Mock<ILogger<GoogleBooksService>> _mockLogger;
        private readonly IGoogleBooksService _googleBooksService;

        public GoogleBooksServiceTests()
        {
            _mockApiClient = new Mock<IGoogleBooksApiClient>();
            _mockBookService = new Mock<IBookService>();
            _mockMappingService = new Mock<IBookMappingService>();
            _mockLogger = new Mock<ILogger<GoogleBooksService>>();

            _googleBooksService = new GoogleBooksService(
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
                .Setup(x => x.SearchBooksAsync(query, offset, limit))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _googleBooksService.SearchBooksAsync(query, offset, limit);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Verify(x => x.SearchBooksAsync(query, offset, limit), Times.Once);
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
                .Setup(x => x.SearchBooksByTitleAsync(title, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _googleBooksService.SearchBooksByTitleAsync(title);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Verify(x => x.SearchBooksByTitleAsync(title, null, null), Times.Once);
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
                .Setup(x => x.SearchBooksByAuthorAsync(author, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _googleBooksService.SearchBooksByAuthorAsync(author);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Verify(x => x.SearchBooksByAuthorAsync(author, null, null), Times.Once);
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
                .Setup(x => x.SearchBooksByISBNAsync(isbn))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _googleBooksService.SearchBooksByISBNAsync(isbn);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockApiClient.Verify(x => x.SearchBooksByISBNAsync(isbn), Times.Once);
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
                .Setup(x => x.GetVolumeByIdAsync(volumeId))
                .ReturnsAsync(volumeDto);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()))
                .ReturnsAsync((Book?)null);

            _mockMappingService
                .Setup(x => x.MapFromGoogleBooksAsync(It.IsAny<GoogleBooksVolumeDto>()))
                .ReturnsAsync(mappedBook);

            _mockBookService
                .Setup(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()))
                .ReturnsAsync(createdBook);

            // Act
            var result = await _googleBooksService.ImportBookFromVolumeIdAsync(volumeId);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Verify(x => x.GetVolumeByIdAsync(volumeId), Times.Once);
            _mockBookService.Verify(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()), Times.Once);
            _mockMappingService.Verify(x => x.MapFromGoogleBooksAsync(It.IsAny<GoogleBooksVolumeDto>()), Times.Once);
            _mockBookService.Verify(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()), Times.Once);
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
                .Setup(x => x.GetVolumeByIdAsync(volumeId))
                .ReturnsAsync(volumeDto);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()))
                .ReturnsAsync(existingBook);

            // Act
            var result = await _googleBooksService.ImportBookFromVolumeIdAsync(volumeId);

            // Assert
            Assert.Equal(existingBook, result);
            _mockApiClient.Verify(x => x.GetVolumeByIdAsync(volumeId), Times.Once);
            _mockBookService.Verify(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()), Times.Once);
            _mockMappingService.Verify(x => x.MapFromGoogleBooksAsync(It.IsAny<GoogleBooksVolumeDto>()), Times.Never);
            _mockBookService.Verify(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()), Times.Never);
        }

        [Fact]
        public async Task ImportBookFromVolumeIdAsync_WithNotFoundVolume_ThrowsException()
        {
            // Arrange
            var volumeId = "invalid-id";

            _mockApiClient
                .Setup(x => x.GetVolumeByIdAsync(volumeId))
                .ReturnsAsync((GoogleBooksVolumeDto?)null);

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
                .Setup(x => x.SearchBooksByISBNAsync(isbn))
                .ReturnsAsync(searchResult);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()))
                .ReturnsAsync((Book?)null);

            _mockMappingService
                .Setup(x => x.MapFromGoogleBooksAsync(It.IsAny<GoogleBooksVolumeDto>()))
                .ReturnsAsync(mappedBook);

            _mockBookService
                .Setup(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()))
                .ReturnsAsync(createdBook);

            // Act
            var result = await _googleBooksService.ImportBookFromISBNAsync(isbn);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Verify(x => x.SearchBooksByISBNAsync(isbn), Times.Once);
            _mockBookService.Verify(x => x.GetBookByTitleAndAuthorAsync("Test Book", It.IsAny<string>()), Times.Once);
            _mockMappingService.Verify(x => x.MapFromGoogleBooksAsync(It.IsAny<GoogleBooksVolumeDto>()), Times.Once);
            _mockBookService.Verify(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()), Times.Once);
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
                .Setup(x => x.SearchBooksByISBNAsync(isbn))
                .ReturnsAsync(searchResult);

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
                .Setup(x => x.SearchBooksAsync($"intitle:{title} inauthor:{author}", null, 1))
                .ReturnsAsync(searchResult);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync(title, It.IsAny<string>()))
                .ReturnsAsync((Book?)null);

            _mockMappingService
                .Setup(x => x.MapFromGoogleBooksAsync(It.IsAny<GoogleBooksVolumeDto>()))
                .ReturnsAsync(mappedBook);

            _mockBookService
                .Setup(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()))
                .ReturnsAsync(createdBook);

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
                .Setup(x => x.SearchBooksByTitleAsync(title, null, 1))
                .ReturnsAsync(searchResult);

            _mockBookService
                .Setup(x => x.GetBookByTitleAndAuthorAsync(title, It.IsAny<string>()))
                .ReturnsAsync((Book?)null);

            _mockMappingService
                .Setup(x => x.MapFromGoogleBooksAsync(It.IsAny<GoogleBooksVolumeDto>()))
                .ReturnsAsync(mappedBook);

            _mockBookService
                .Setup(x => x.CreateBookAsync(It.IsAny<CreateBookDto>()))
                .ReturnsAsync(createdBook);

            // Act
            var result = await _googleBooksService.ImportBookFromTitleAndAuthorAsync(title);

            // Assert
            Assert.Equal(createdBook, result);
            _mockApiClient.Verify(x => x.SearchBooksByTitleAsync(title, null, 1), Times.Once);
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
                .Setup(x => x.SearchBooksByTitleAsync(title, null, 1))
                .ReturnsAsync(searchResult);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _googleBooksService.ImportBookFromTitleAndAuthorAsync(title));

            // Error message format: "Book with title '{title}' and author '' not found in Google Books"
            Assert.Contains($"Book with title '{title}'", exception.Message);
            Assert.Contains("not found in Google Books", exception.Message);
        }
    }
}
