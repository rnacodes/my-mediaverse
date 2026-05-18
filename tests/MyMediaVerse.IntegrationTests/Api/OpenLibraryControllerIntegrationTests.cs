using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.OpenLibrary;
using MyMediaVerse.UnitTests.TestData;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class OpenLibraryControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public OpenLibraryControllerIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region Search Endpoint Tests

        [Fact]
        public async Task SearchOpenLibrary_WithValidQuery_ReturnsResults()
        {
            // Arrange
            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Docs = new[]
                {
                    CreateTestBook("/works/OL82563W", "Harry Potter and the Sorcerer's Stone", "J.K. Rowling")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "/works/OL82563W",
                Title = "Harry Potter and the Sorcerer's Stone",
                Authors = new[] { "J.K. Rowling" }
            };

            var (client, _, _) = _factory.CreateClientWithSubstitutes<IOpenLibraryService, IBookMappingService>(
                ol => ol.SearchBooksAsync("Harry Potter", null, 5).Returns(searchResult),
                map => map.MapToSearchResultDtoAsync(Arg.Any<OpenLibraryBookDto>()).Returns(mappedResult));

            // Act
            var response = await client.GetAsync("/api/book/search-openlibrary?query=Harry+Potter&searchType=General&limit=5");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<BookSearchResultDto[]>(_jsonOptions);
            results.Should().NotBeNull();
            results!.Length.Should().Be(1);
            results[0].Title.Should().Be("Harry Potter and the Sorcerer's Stone");
        }

        [Fact]
        public async Task SearchOpenLibrary_WithEmptyQuery_ReturnsBadRequest()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/book/search-openlibrary?query=&searchType=General");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchOpenLibrary_WithTitleSearchType_ReturnsResults()
        {
            // Arrange
            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Docs = new[]
                {
                    CreateTestBook("/works/OL468431W", "The Great Gatsby", "F. Scott Fitzgerald")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "/works/OL468431W",
                Title = "The Great Gatsby",
                Authors = new[] { "F. Scott Fitzgerald" }
            };

            var (client, _, _) = _factory.CreateClientWithSubstitutes<IOpenLibraryService, IBookMappingService>(
                ol => ol.SearchBooksByTitleAsync("The Great Gatsby", null, 3).Returns(searchResult),
                map => map.MapToSearchResultDtoAsync(Arg.Any<OpenLibraryBookDto>()).Returns(mappedResult));

            // Act
            var response = await client.GetAsync($"/api/book/search-openlibrary?query={Uri.EscapeDataString("The Great Gatsby")}&searchType=Title&limit=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<BookSearchResultDto[]>(_jsonOptions);
            results.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchOpenLibrary_WithAuthorSearchType_ReturnsResults()
        {
            // Arrange
            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Docs = new[]
                {
                    CreateTestBook("/works/OL82563W", "Harry Potter and the Chamber of Secrets", "J.K. Rowling")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "/works/OL82563W",
                Title = "Harry Potter and the Chamber of Secrets",
                Authors = new[] { "J.K. Rowling" }
            };

            var (client, _, _) = _factory.CreateClientWithSubstitutes<IOpenLibraryService, IBookMappingService>(
                ol => ol.SearchBooksByAuthorAsync("J.K. Rowling", null, 3).Returns(searchResult),
                map => map.MapToSearchResultDtoAsync(Arg.Any<OpenLibraryBookDto>()).Returns(mappedResult));

            // Act
            var response = await client.GetAsync($"/api/book/search-openlibrary?query={Uri.EscapeDataString("J.K. Rowling")}&searchType=Author&limit=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<BookSearchResultDto[]>(_jsonOptions);
            results.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchOpenLibrary_WithPagination_ReturnsCorrectResults()
        {
            // Arrange
            var books = Enumerable.Range(1, 3)
                .Select(i => CreateTestBook($"/works/OL{i}W", $"Science Fiction Book {i}", "Test Author"))
                .ToArray();

            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 100,
                Docs = books
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "/works/OL1W",
                Title = "Science Fiction Book 1"
            };

            var (client, _, _) = _factory.CreateClientWithSubstitutes<IOpenLibraryService, IBookMappingService>(
                ol => ol.SearchBooksAsync("science fiction", 0, 5).Returns(searchResult),
                map => map.MapToSearchResultDtoAsync(Arg.Any<OpenLibraryBookDto>()).Returns(mappedResult));

            // Act
            var response = await client.GetAsync($"/api/book/search-openlibrary?query={Uri.EscapeDataString("science fiction")}&offset=0&limit=5");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<BookSearchResultDto[]>(_jsonOptions);
            results.Should().NotBeNull();
            results!.Length.Should().BeLessOrEqualTo(5);
        }

        [Theory]
        [InlineData("General")]
        [InlineData("Title")]
        [InlineData("Author")]
        public async Task SearchOpenLibrary_WithDifferentSearchTypes_ReturnsOk(string searchType)
        {
            // Arrange
            var searchResult = new OpenLibrarySearchResultDto
            {
                NumFound = 1,
                Docs = new[]
                {
                    CreateTestBook("/works/OL1W", "Test Book", "Test Author")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "/works/OL1W",
                Title = "Test Book"
            };

            var (client, _, _) = _factory.CreateClientWithSubstitutes<IOpenLibraryService, IBookMappingService>(
                ol =>
                {
                    ol.SearchBooksAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>()).Returns(searchResult);
                    ol.SearchBooksByTitleAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>()).Returns(searchResult);
                    ol.SearchBooksByAuthorAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>()).Returns(searchResult);
                },
                map => map.MapToSearchResultDtoAsync(Arg.Any<OpenLibraryBookDto>()).Returns(mappedResult));

            // Act
            var response = await client.GetAsync($"/api/book/search-openlibrary?query=test&searchType={searchType}&limit=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Import Endpoint Tests

        [Fact]
        public async Task ImportFromOpenLibrary_WithValidTitle_CreatesBook()
        {
            // Arrange
            var expectedBook = TestDataFactory.CreateBook("The Hobbit", "J.R.R. Tolkien");

            var expectedResponse = new BookResponseDto
            {
                Id = expectedBook.Id,
                Title = "The Hobbit",
                Author = "J.R.R. Tolkien",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = expectedBook.DateAdded
            };

            var (client, _, _) = _factory.CreateClientWithSubstitutes<IOpenLibraryService, IBookMappingService>(
                ol => ol.ImportBookFromTitleAndAuthorAsync("The Hobbit", "J.R.R. Tolkien").Returns(expectedBook),
                map => map.MapToResponseDtoAsync(Arg.Any<Book>()).Returns(expectedResponse));

            var importDto = new ImportBookFromOpenLibraryDto
            {
                Title = "The Hobbit",
                Author = "J.R.R. Tolkien"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-openlibrary", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var bookResponse = await response.Content.ReadFromJsonAsync<BookResponseDto>(_jsonOptions);
            bookResponse.Should().NotBeNull();
            bookResponse!.Title.Should().Contain("Hobbit");
        }

        [Fact]
        public async Task ImportFromOpenLibrary_WithValidISBN_CreatesBook()
        {
            // Arrange
            var expectedBook = TestDataFactory.CreateBook("The Great Gatsby", "F. Scott Fitzgerald");

            var expectedResponse = new BookResponseDto
            {
                Id = expectedBook.Id,
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = expectedBook.DateAdded
            };

            var (client, _, _) = _factory.CreateClientWithSubstitutes<IOpenLibraryService, IBookMappingService>(
                ol => ol.ImportBookFromISBNAsync("9780743273565").Returns(expectedBook),
                map => map.MapToResponseDtoAsync(Arg.Any<Book>()).Returns(expectedResponse));

            var importDto = new ImportBookFromOpenLibraryDto
            {
                Isbn = "9780743273565"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-openlibrary", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task ImportFromOpenLibrary_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var importDto = new ImportBookFromOpenLibraryDto
            {
                // No title, author, ISBN, or OpenLibrary key provided
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-openlibrary", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ImportFromOpenLibrary_WithNonExistentTitle_ReturnsNotFound()
        {
            // Arrange
            var (client, _, _) = _factory.CreateClientWithSubstitutes<IOpenLibraryService, IBookMappingService>(
                ol => ol.ImportBookFromTitleAndAuthorAsync("This Book Definitely Does Not Exist 12345", "Non Existent Author 67890")
                    .Throws(new InvalidOperationException("Book not found in OpenLibrary")),
                null);

            var importDto = new ImportBookFromOpenLibraryDto
            {
                Title = "This Book Definitely Does Not Exist 12345",
                Author = "Non Existent Author 67890"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-openlibrary", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Helper Methods

        private static OpenLibraryBookDto CreateTestBook(string key, string title, string author)
        {
            return new OpenLibraryBookDto
            {
                Key = key,
                Title = title,
                AuthorName = new[] { author },
                FirstPublishYear = 2000,
                EditionCount = 5
            };
        }

        #endregion
    }
}
