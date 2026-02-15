using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProjectLoopbreaker.DTOs;
using ProjectLoopbreaker.Shared.DTOs.OpenLibrary;
using Xunit;

namespace ProjectLoopbreaker.IntegrationTests.Controllers
{
    public class OpenLibraryControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OpenLibraryControllerIntegrationTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task SearchOpenLibrary_WithValidQuery_ReturnsResults()
        {
            // Arrange
            var query = "Harry Potter";
            var searchType = "General";

            // Act
            var response = await _client.GetAsync($"/api/book/search-openlibrary?query={Uri.EscapeDataString(query)}&searchType={searchType}&limit=5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<BookSearchResultDto[]>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(results);
            // Note: This test depends on OpenLibrary API being available and having Harry Potter books
            // In a real scenario, you might want to mock the external API calls
        }

        [Fact]
        public async Task SearchOpenLibrary_WithEmptyQuery_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/book/search-openlibrary?query=&searchType=General");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SearchOpenLibrary_WithTitleSearchType_ReturnsResults()
        {
            // Arrange
            var query = "The Great Gatsby";
            var searchType = "Title";

            // Act
            var response = await _client.GetAsync($"/api/book/search-openlibrary?query={Uri.EscapeDataString(query)}&searchType={searchType}&limit=3");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<BookSearchResultDto[]>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(results);
        }

        [Fact]
        public async Task SearchOpenLibrary_WithAuthorSearchType_ReturnsResults()
        {
            // Arrange
            var query = "J.K. Rowling";
            var searchType = "Author";

            // Act
            var response = await _client.GetAsync($"/api/book/search-openlibrary?query={Uri.EscapeDataString(query)}&searchType={searchType}&limit=3");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<BookSearchResultDto[]>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(results);
        }

        [Fact]
        public async Task ImportFromOpenLibrary_WithValidTitle_CreatesBook()
        {
            // Arrange
            var importDto = new ImportBookFromOpenLibraryDto
            {
                Title = "The Hobbit",
                Author = "J.R.R. Tolkien"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/book/import-from-openlibrary", importDto);

            // Assert
            // Note: This might return 201 (Created) if the book is successfully imported
            // or 200 (OK) if the book already exists in the database
            Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
            
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var content = await response.Content.ReadAsStringAsync();
                var bookResponse = JsonSerializer.Deserialize<BookResponseDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });

                Assert.NotNull(bookResponse);
                Assert.Contains("Hobbit", bookResponse.Title);
            }
        }

        [Fact]
        public async Task ImportFromOpenLibrary_WithValidISBN_CreatesBook()
        {
            // Arrange - Using a well-known ISBN for The Great Gatsby
            var importDto = new ImportBookFromOpenLibraryDto
            {
                Isbn = "9780743273565"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/book/import-from-openlibrary", importDto);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task ImportFromOpenLibrary_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var importDto = new ImportBookFromOpenLibraryDto
            {
                // No title, author, ISBN, or OpenLibrary key provided
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/book/import-from-openlibrary", importDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportFromOpenLibrary_WithNonExistentTitle_ReturnsNotFound()
        {
            // Arrange
            var importDto = new ImportBookFromOpenLibraryDto
            {
                Title = "This Book Definitely Does Not Exist In OpenLibrary 12345",
                Author = "Non Existent Author 67890"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/book/import-from-openlibrary", importDto);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SearchOpenLibrary_WithPagination_ReturnsCorrectResults()
        {
            // Arrange
            var query = "science fiction";
            var offset = 0;
            var limit = 5;

            // Act
            var response = await _client.GetAsync($"/api/book/search-openlibrary?query={Uri.EscapeDataString(query)}&offset={offset}&limit={limit}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<BookSearchResultDto[]>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(results);
            Assert.True(results.Length <= limit);
        }

        [Theory]
        [InlineData("General")]
        [InlineData("Title")]
        [InlineData("Author")]
        public async Task SearchOpenLibrary_WithDifferentSearchTypes_ReturnsOk(string searchType)
        {
            // Arrange
            var query = "test";

            // Act
            var response = await _client.GetAsync($"/api/book/search-openlibrary?query={query}&searchType={searchType}&limit=1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
