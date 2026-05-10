using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.UnitTests.TestData;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    public class GoogleBooksControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public GoogleBooksControllerIntegrationTests(WebApplicationFactory factory)
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

        #region Search Endpoint Tests

        [Fact]
        public async Task SearchGoogleBooks_WithValidQuery_ReturnsResults()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

            var searchResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    CreateTestVolume("vol1", "Harry Potter and the Sorcerer's Stone")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "vol1",
                Title = "Harry Potter and the Sorcerer's Stone",
                Authors = new[] { "J.K. Rowling" }
            };

            mockGoogleBooksService
                .SearchBooksAsync("Harry Potter", null, 5)
                .Returns(searchResult);

            mockBookMappingService
                .MapGoogleBooksToSearchResultDtoAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedResult);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            // Act
            var response = await client.GetAsync("/api/book/search-googlebooks?query=Harry+Potter&searchType=General&limit=5");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<BookSearchResultDto[]>(_jsonOptions);
            results.Should().NotBeNull();
            results!.Length.Should().Be(1);
            results[0].Title.Should().Be("Harry Potter and the Sorcerer's Stone");
        }

        [Fact]
        public async Task SearchGoogleBooks_WithEmptyQuery_ReturnsBadRequest()
        {
            // Act
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/book/search-googlebooks?query=&searchType=General");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SearchGoogleBooks_WithTitleSearchType_ReturnsResults()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

            var searchResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    CreateTestVolume("vol2", "The Great Gatsby")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "vol2",
                Title = "The Great Gatsby",
                Authors = new[] { "F. Scott Fitzgerald" }
            };

            mockGoogleBooksService
                .SearchBooksByTitleAsync("The Great Gatsby", null, 3)
                .Returns(searchResult);

            mockBookMappingService
                .MapGoogleBooksToSearchResultDtoAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedResult);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            // Act
            var response = await client.GetAsync($"/api/book/search-googlebooks?query={Uri.EscapeDataString("The Great Gatsby")}&searchType=Title&limit=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<BookSearchResultDto[]>(_jsonOptions);
            results.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchGoogleBooks_WithAuthorSearchType_ReturnsResults()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

            var searchResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    CreateTestVolume("vol3", "Harry Potter and the Chamber of Secrets")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "vol3",
                Title = "Harry Potter and the Chamber of Secrets",
                Authors = new[] { "J.K. Rowling" }
            };

            mockGoogleBooksService
                .SearchBooksByAuthorAsync("J.K. Rowling", null, 3)
                .Returns(searchResult);

            mockBookMappingService
                .MapGoogleBooksToSearchResultDtoAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedResult);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            // Act
            var response = await client.GetAsync($"/api/book/search-googlebooks?query={Uri.EscapeDataString("J.K. Rowling")}&searchType=Author&limit=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<BookSearchResultDto[]>(_jsonOptions);
            results.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchGoogleBooks_WithISBNSearchType_ReturnsResults()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

            var searchResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    CreateTestVolume("vol4", "The Great Gatsby")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "vol4",
                Title = "The Great Gatsby",
                Isbn = new[] { "9780743273565" }
            };

            mockGoogleBooksService
                .SearchBooksByISBNAsync("9780743273565")
                .Returns(searchResult);

            mockBookMappingService
                .MapGoogleBooksToSearchResultDtoAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedResult);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            // Act
            var response = await client.GetAsync("/api/book/search-googlebooks?query=9780743273565&searchType=ISBN");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = await response.Content.ReadFromJsonAsync<BookSearchResultDto[]>(_jsonOptions);
            results.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchGoogleBooks_WithPagination_ReturnsCorrectResults()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

            var volumes = Enumerable.Range(1, 3)
                .Select(i => CreateTestVolume($"vol{i}", $"Science Fiction Book {i}"))
                .ToArray();

            var searchResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 100,
                Items = volumes
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "vol1",
                Title = "Science Fiction Book 1"
            };

            mockGoogleBooksService
                .SearchBooksAsync("science fiction", 0, 5)
                .Returns(searchResult);

            mockBookMappingService
                .MapGoogleBooksToSearchResultDtoAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedResult);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            // Act
            var response = await client.GetAsync($"/api/book/search-googlebooks?query={Uri.EscapeDataString("science fiction")}&offset=0&limit=5");

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
        public async Task SearchGoogleBooks_WithDifferentSearchTypes_ReturnsOk(string searchType)
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

            var searchResult = new GoogleBooksSearchResultDto
            {
                TotalItems = 1,
                Items = new[]
                {
                    CreateTestVolume("vol1", "Test Book")
                }
            };

            var mappedResult = new BookSearchResultDto
            {
                Key = "vol1",
                Title = "Test Book"
            };

            // Setup all possible search type methods
            mockGoogleBooksService
                .SearchBooksAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>())
                .Returns(searchResult);
            mockGoogleBooksService
                .SearchBooksByTitleAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>())
                .Returns(searchResult);
            mockGoogleBooksService
                .SearchBooksByAuthorAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>())
                .Returns(searchResult);

            mockBookMappingService
                .MapGoogleBooksToSearchResultDtoAsync(Arg.Any<GoogleBooksVolumeDto>())
                .Returns(mappedResult);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            // Act
            var response = await client.GetAsync($"/api/book/search-googlebooks?query=test&searchType={searchType}&limit=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Import Endpoint Tests

        [Fact]
        public async Task ImportFromGoogleBooks_WithValidTitle_CreatesBook()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

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

            mockGoogleBooksService
                .ImportBookFromTitleAndAuthorAsync("The Hobbit", "J.R.R. Tolkien")
                .Returns(expectedBook);

            mockBookMappingService
                .MapToResponseDtoAsync(Arg.Any<Book>())
                .Returns(expectedResponse);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            var importDto = new ImportBookFromGoogleBooksDto
            {
                Title = "The Hobbit",
                Author = "J.R.R. Tolkien"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var bookResponse = await response.Content.ReadFromJsonAsync<BookResponseDto>(_jsonOptions);
            bookResponse.Should().NotBeNull();
            bookResponse!.Title.Should().Contain("Hobbit");
        }

        [Fact]
        public async Task ImportFromGoogleBooks_WithValidISBN_CreatesBook()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

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

            mockGoogleBooksService
                .ImportBookFromISBNAsync("9780743273565")
                .Returns(expectedBook);

            mockBookMappingService
                .MapToResponseDtoAsync(Arg.Any<Book>())
                .Returns(expectedResponse);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            var importDto = new ImportBookFromGoogleBooksDto
            {
                Isbn = "9780743273565"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task ImportFromGoogleBooks_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var importDto = new ImportBookFromGoogleBooksDto
            {
                // No title, author, ISBN, or VolumeId provided
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ImportFromGoogleBooks_WithNonExistentTitle_ReturnsNotFound()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

            mockGoogleBooksService
                .ImportBookFromTitleAndAuthorAsync("This Book Definitely Does Not Exist 12345", "Non Existent Author 67890")
                .Throws(new InvalidOperationException("Book not found in Google Books"));

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            var importDto = new ImportBookFromGoogleBooksDto
            {
                Title = "This Book Definitely Does Not Exist 12345",
                Author = "Non Existent Author 67890"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ImportFromGoogleBooks_WithValidVolumeId_CreatesBook()
        {
            // Arrange
            var mockGoogleBooksService = Substitute.For<IGoogleBooksService>();
            var mockBookMappingService = Substitute.For<IBookMappingService>();

            var expectedBook = TestDataFactory.CreateBook("1984", "George Orwell");

            var expectedResponse = new BookResponseDto
            {
                Id = expectedBook.Id,
                Title = "1984",
                Author = "George Orwell",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = expectedBook.DateAdded
            };

            mockGoogleBooksService
                .ImportBookFromVolumeIdAsync("test-volume-id")
                .Returns(expectedBook);

            mockBookMappingService
                .MapToResponseDtoAsync(Arg.Any<Book>())
                .Returns(expectedResponse);

            var client = CreateClientWithMocks(mockGoogleBooksService, mockBookMappingService);

            var importDto = new ImportBookFromGoogleBooksDto
            {
                VolumeId = "test-volume-id"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var bookResponse = await response.Content.ReadFromJsonAsync<BookResponseDto>(_jsonOptions);
            bookResponse.Should().NotBeNull();
            bookResponse!.Title.Should().Be("1984");
        }

        #endregion

        #region Helper Methods

        private HttpClient CreateClientWithMocks(
            IGoogleBooksService? mockGoogleBooksService = null,
            IBookMappingService? mockBookMappingService = null)
        {
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    if (mockGoogleBooksService != null)
                    {
                        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGoogleBooksService));
                        if (descriptor != null)
                            services.Remove(descriptor);
                        services.AddSingleton(mockGoogleBooksService);
                    }

                    if (mockBookMappingService != null)
                    {
                        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBookMappingService));
                        if (descriptor != null)
                            services.Remove(descriptor);
                        services.AddSingleton(mockBookMappingService);
                    }
                });
            });

            return factory.CreateClient();
        }

        private static GoogleBooksVolumeDto CreateTestVolume(string id, string title)
        {
            return new GoogleBooksVolumeDto
            {
                Id = id,
                VolumeInfo = new GoogleBooksVolumeInfoDto
                {
                    Title = title,
                    Authors = new[] { "Test Author" },
                    PublishedDate = "2020-01-01",
                    Description = "A test book description"
                }
            };
        }

        #endregion
    }
}
