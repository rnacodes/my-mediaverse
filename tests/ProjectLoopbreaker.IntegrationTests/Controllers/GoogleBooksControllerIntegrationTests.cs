using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProjectLoopbreaker.DTOs;
using ProjectLoopbreaker.Shared.DTOs.GoogleBooks;
using Xunit;

namespace ProjectLoopbreaker.IntegrationTests.Controllers
{
    public class GoogleBooksControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GoogleBooksControllerIntegrationTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task SearchGoogleBooks_WithValidQuery_ReturnsResults()
        {
            // Arrange
            var query = "Harry Potter";
            var searchType = "General";

            // Act
            var response = await _client.GetAsync($"/api/book/search-googlebooks?query={Uri.EscapeDataString(query)}&searchType={searchType}&limit=5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<BookSearchResultDto[]>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(results);
            // Note: This test depends on Google Books API being available and having Harry Potter books
            // In a real scenario, you might want to mock the external API calls
        }

        [Fact]
        public async Task SearchGoogleBooks_WithEmptyQuery_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/book/search-googlebooks?query=&searchType=General");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SearchGoogleBooks_WithTitleSearchType_ReturnsResults()
        {
            // Arrange
            var query = "The Great Gatsby";
            var searchType = "Title";

            // Act
            var response = await _client.GetAsync($"/api/book/search-googlebooks?query={Uri.EscapeDataString(query)}&searchType={searchType}&limit=3");

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
        public async Task SearchGoogleBooks_WithAuthorSearchType_ReturnsResults()
        {
            // Arrange
            var query = "J.K. Rowling";
            var searchType = "Author";

            // Act
            var response = await _client.GetAsync($"/api/book/search-googlebooks?query={Uri.EscapeDataString(query)}&searchType={searchType}&limit=3");

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
        public async Task SearchGoogleBooks_WithISBNSearchType_ReturnsResults()
        {
            // Arrange - Using a well-known ISBN for The Great Gatsby
            var query = "9780743273565";
            var searchType = "ISBN";

            // Act
            var response = await _client.GetAsync($"/api/book/search-googlebooks?query={Uri.EscapeDataString(query)}&searchType={searchType}");

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
        public async Task ImportFromGoogleBooks_WithValidTitle_CreatesBook()
        {
            // Arrange
            var importDto = new ImportBookFromGoogleBooksDto
            {
                Title = "The Hobbit",
                Author = "J.R.R. Tolkien"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

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
        public async Task ImportFromGoogleBooks_WithValidISBN_CreatesBook()
        {
            // Arrange - Using a well-known ISBN for The Great Gatsby
            var importDto = new ImportBookFromGoogleBooksDto
            {
                Isbn = "9780743273565"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task ImportFromGoogleBooks_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var importDto = new ImportBookFromGoogleBooksDto
            {
                // No title, author, ISBN, or VolumeId provided
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportFromGoogleBooks_WithNonExistentTitle_ReturnsNotFound()
        {
            // Arrange
            var importDto = new ImportBookFromGoogleBooksDto
            {
                Title = "This Book Definitely Does Not Exist In Google Books 12345",
                Author = "Non Existent Author 67890"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SearchGoogleBooks_WithPagination_ReturnsCorrectResults()
        {
            // Arrange
            var query = "science fiction";
            var offset = 0;
            var limit = 5;

            // Act
            var response = await _client.GetAsync($"/api/book/search-googlebooks?query={Uri.EscapeDataString(query)}&offset={offset}&limit={limit}");

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
        public async Task SearchGoogleBooks_WithDifferentSearchTypes_ReturnsOk(string searchType)
        {
            // Arrange
            var query = "test";

            // Act
            var response = await _client.GetAsync($"/api/book/search-googlebooks?query={query}&searchType={searchType}&limit=1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ImportFromGoogleBooks_WithValidVolumeId_CreatesBook()
        {
            // First, search for a book to get a valid Volume ID
            var searchResponse = await _client.GetAsync("/api/book/search-googlebooks?query=1984+George+Orwell&limit=1");

            if (searchResponse.StatusCode == HttpStatusCode.OK)
            {
                var content = await searchResponse.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<BookSearchResultDto[]>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (results != null && results.Length > 0 && !string.IsNullOrEmpty(results[0].Key))
                {
                    // Arrange
                    var importDto = new ImportBookFromGoogleBooksDto
                    {
                        VolumeId = results[0].Key
                    };

                    // Act
                    var response = await _client.PostAsJsonAsync("/api/book/import-from-googlebooks", importDto);

                    // Assert
                    Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
                }
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
