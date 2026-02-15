using Microsoft.AspNetCore.Mvc.Testing;
using ProjectLoopbreaker.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace ProjectLoopbreaker.IntegrationTests.Controllers
{
    public class GenresControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public GenresControllerIntegrationTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        #region GET Tests

        [Fact]
        public async Task GetAllGenres_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/genres");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var genres = JsonSerializer.Deserialize<List<GenreResponseDto>>(content, _jsonOptions);
            Assert.NotNull(genres);
        }

        [Fact]
        public async Task GetGenre_WithValidId_ShouldReturnOk()
        {
            // Arrange - First create a genre
            var createDto = new CreateGenreDto
            {
                Name = "Test Genre for Get"
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/genres", createContent);
            var createdGenre = JsonSerializer.Deserialize<GenreResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Act
            var response = await _client.GetAsync($"/api/genres/{createdGenre.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var genre = JsonSerializer.Deserialize<GenreResponseDto>(content, _jsonOptions);
            Assert.NotNull(genre);
            Assert.Equal(createdGenre.Id, genre.Id);
            Assert.Equal("test genre for get", genre.Name); // Normalized to lowercase
        }

        [Fact]
        public async Task GetGenre_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/genres/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region POST Tests

        [Fact]
        public async Task CreateGenre_WithValidName_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateGenreDto
            {
                Name = "New Unique Genre Name " + Guid.NewGuid().ToString()[..8]
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/genres", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdGenre = JsonSerializer.Deserialize<GenreResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdGenre);
            Assert.NotEqual(Guid.Empty, createdGenre.Id);
            Assert.Contains("new unique genre name", createdGenre.Name);
        }

        [Fact]
        public async Task CreateGenre_WithDuplicateName_ShouldReturnExistingGenre()
        {
            // Arrange - Create a genre first
            var genreName = "Duplicate Genre Test " + Guid.NewGuid().ToString()[..8];
            var createDto = new CreateGenreDto { Name = genreName };

            var content1 = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var firstResponse = await _client.PostAsync("/api/genres", content1);
            var firstGenre = JsonSerializer.Deserialize<GenreResponseDto>(
                await firstResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Try to create the same genre again
            var content2 = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/genres", content2);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Returns OK with existing genre
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var returnedGenre = JsonSerializer.Deserialize<GenreResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(returnedGenre);
            Assert.Equal(firstGenre.Id, returnedGenre.Id); // Same ID as first genre
        }

        [Fact]
        public async Task CreateGenre_WithCaseInsensitiveDuplicate_ShouldReturnExistingGenre()
        {
            // Arrange - Create a genre first
            var genreName = "CaseInsensitive Genre Test " + Guid.NewGuid().ToString()[..8];
            var createDto1 = new CreateGenreDto { Name = genreName.ToLower() };

            var content1 = new StringContent(
                JsonSerializer.Serialize(createDto1, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var firstResponse = await _client.PostAsync("/api/genres", content1);
            var firstGenre = JsonSerializer.Deserialize<GenreResponseDto>(
                await firstResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Try to create with different case
            var createDto2 = new CreateGenreDto { Name = genreName.ToUpper() };
            var content2 = new StringContent(
                JsonSerializer.Serialize(createDto2, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/genres", content2);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var returnedGenre = JsonSerializer.Deserialize<GenreResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(returnedGenre);
            Assert.Equal(firstGenre.Id, returnedGenre.Id);
        }

        [Fact]
        public async Task CreateGenre_WithEmptyName_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateGenreDto { Name = "" };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/genres", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateGenre_WithWhitespaceName_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateGenreDto { Name = "   " };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/genres", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateGenre_WithTrimmableWhitespace_ShouldTrimAndCreate()
        {
            // Arrange
            var createDto = new CreateGenreDto { Name = "  Trimmed Genre " + Guid.NewGuid().ToString()[..8] + "  " };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/genres", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdGenre = JsonSerializer.Deserialize<GenreResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdGenre);
            Assert.DoesNotContain("  ", createdGenre.Name); // Whitespace should be trimmed
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeleteGenre_WithValidIdAndNoMediaItems_ShouldReturnNoContent()
        {
            // Arrange - Create a genre
            var createDto = new CreateGenreDto { Name = "Genre to Delete " + Guid.NewGuid().ToString()[..8] };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/genres", createContent);
            var createdGenre = JsonSerializer.Deserialize<GenreResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Act
            var response = await _client.DeleteAsync($"/api/genres/{createdGenre.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the genre is actually deleted
            var getResponse = await _client.GetAsync($"/api/genres/{createdGenre.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteGenre_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/genres/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchGenres_WithValidQuery_ShouldReturnMatchingGenres()
        {
            // Arrange - Create genres with searchable names
            var uniquePrefix = "GenreSearchTest" + Guid.NewGuid().ToString()[..8];
            var createDto1 = new CreateGenreDto { Name = $"{uniquePrefix} Genre One" };
            var createDto2 = new CreateGenreDto { Name = $"{uniquePrefix} Genre Two" };
            var createDto3 = new CreateGenreDto { Name = "Different Genre" };

            var content1 = new StringContent(JsonSerializer.Serialize(createDto1, _jsonOptions), Encoding.UTF8, "application/json");
            var content2 = new StringContent(JsonSerializer.Serialize(createDto2, _jsonOptions), Encoding.UTF8, "application/json");
            var content3 = new StringContent(JsonSerializer.Serialize(createDto3, _jsonOptions), Encoding.UTF8, "application/json");

            await _client.PostAsync("/api/genres", content1);
            await _client.PostAsync("/api/genres", content2);
            await _client.PostAsync("/api/genres", content3);

            // Act
            var response = await _client.GetAsync($"/api/genres/search?query={uniquePrefix.ToLower()}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var genres = JsonSerializer.Deserialize<List<GenreResponseDto>>(content, _jsonOptions);
            Assert.NotNull(genres);
            Assert.True(genres.Count >= 2);
            Assert.All(genres, g => Assert.Contains(uniquePrefix.ToLower(), g.Name));
        }

        [Fact]
        public async Task SearchGenres_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // Act - Empty query should return BadRequest as it's not a valid search
            var response = await _client.GetAsync("/api/genres/search?query=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SearchGenres_WithNoMatches_ShouldReturnEmptyList()
        {
            // Act
            var response = await _client.GetAsync($"/api/genres/search?query=NonExistentSearchTerm{Guid.NewGuid()}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var genres = JsonSerializer.Deserialize<List<GenreResponseDto>>(content, _jsonOptions);
            Assert.NotNull(genres);
            Assert.Empty(genres);
        }

        [Fact]
        public async Task SearchGenres_IsCaseInsensitive_ShouldReturnMatches()
        {
            // Arrange - Create a genre
            var uniqueName = "GenreCaseSearchTest" + Guid.NewGuid().ToString()[..8];
            var createDto = new CreateGenreDto { Name = uniqueName };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/genres", createContent);

            // Act - Search with different case
            var response = await _client.GetAsync($"/api/genres/search?query={uniqueName.ToUpper()}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var genres = JsonSerializer.Deserialize<List<GenreResponseDto>>(content, _jsonOptions);
            Assert.NotNull(genres);
            Assert.True(genres.Count >= 1);
            Assert.Contains(genres, g => g.Name.Contains(uniqueName.ToLower()));
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateGenre_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/genres", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateGenre_WithNullData_ShouldReturnBadRequest()
        {
            // Arrange
            var content = new StringContent("null", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/genres", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Integration with Media Items

        [Fact]
        public async Task CreateMediaWithGenre_ShouldAssociateGenreWithMedia()
        {
            // Arrange - Create a unique genre name
            var genreName = "MediaAssocGenreTest" + Guid.NewGuid().ToString()[..8];
            
            // Create media item with the genre
            var mediaDto = new CreateMediaItemDto
            {
                Title = "Media with Genre",
                MediaType = Domain.Entities.MediaType.Article,
                Status = Domain.Entities.Status.Uncharted,
                Genres = new[] { genreName }
            };

            var mediaContent = new StringContent(
                JsonSerializer.Serialize(mediaDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/media", mediaContent);

            // Act - Search for the genre to get its ID
            var searchResponse = await _client.GetAsync($"/api/genres/search?query={genreName}");
            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

            var searchContent = await searchResponse.Content.ReadAsStringAsync();
            var genres = JsonSerializer.Deserialize<List<GenreResponseDto>>(searchContent, _jsonOptions);
            Assert.NotNull(genres);
            Assert.Single(genres);

            var genreFromSearch = genres.First();
            Assert.Contains(genreName.ToLower(), genreFromSearch.Name);

            // Get genre by ID to verify media association (search endpoint omits MediaItemIds for performance)
            var response = await _client.GetAsync($"/api/genres/{genreFromSearch.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var genre = JsonSerializer.Deserialize<GenreResponseDto>(content, _jsonOptions);
            Assert.NotNull(genre);
            Assert.NotEmpty(genre.MediaItemIds); // Should have associated media
        }

        #endregion

        #region Normalization Tests

        [Fact]
        public async Task GenreName_ShouldBeNormalizedToLowerCase()
        {
            // Arrange
            var createDto = new CreateGenreDto { Name = "UPPERCASE GENRE " + Guid.NewGuid().ToString()[..8] };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/genres", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdGenre = JsonSerializer.Deserialize<GenreResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdGenre);
            Assert.Equal(createDto.Name.Trim().ToLowerInvariant(), createdGenre.Name);
        }

        #endregion
    }
}

