using Microsoft.AspNetCore.Mvc.Testing;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    public class MovieControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public MovieControllerIntegrationTests(WebApplicationFactory factory)
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
        public async Task GetAllMovies_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/movie");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var movies = JsonSerializer.Deserialize<List<MovieResponseDto>>(content, _jsonOptions);
            Assert.NotNull(movies);
        }

        [Fact]
        public async Task GetMovie_WithValidId_ShouldReturnOk()
        {
            // Arrange - First create a movie
            var createDto = new CreateMovieDto
            {
                Title = "Test Movie for Get",
                Description = "A test movie description",
                ReleaseYear = 2023,
                Director = "Test Director",
                RuntimeMinutes = 120,
                Status = Status.Uncharted,
                MediaType = MediaType.Movie,
                Topics = new[] { "action", "adventure" },
                Genres = new[] { "thriller", "sci-fi" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/movie", createContent);
            var createdMovie = JsonSerializer.Deserialize<MovieResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Act
            var response = await _client.GetAsync($"/api/movie/{createdMovie.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var movie = JsonSerializer.Deserialize<MovieResponseDto>(content, _jsonOptions);
            Assert.NotNull(movie);
            Assert.Equal(createdMovie.Id, movie.Id);
            Assert.Equal("Test Movie for Get", movie.Title);
        }

        [Fact]
        public async Task GetMovie_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/movie/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetMoviesByDirector_WithValidDirector_ShouldReturnOk()
        {
            // Arrange - First create a movie with a specific director
            var createDto = new CreateMovieDto
            {
                Title = "Directed Movie",
                Description = "A movie by a specific director",
                Director = "Christopher Nolan",
                ReleaseYear = 2023,
                RuntimeMinutes = 150,
                Status = Status.Uncharted,
                MediaType = MediaType.Movie,
                Topics = new[] { "test" },
                Genres = new[] { "drama" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/movie", createContent);

            // Act
            var response = await _client.GetAsync("/api/movie/by-director/Christopher%20Nolan");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var movies = JsonSerializer.Deserialize<List<MovieResponseDto>>(content, _jsonOptions);
            Assert.NotNull(movies);
            Assert.True(movies.Count >= 1);
            Assert.All(movies, m => Assert.Equal("Christopher Nolan", m.Director));
        }

        [Fact]
        public async Task GetMoviesByYear_WithValidYear_ShouldReturnOk()
        {
            // Arrange - First create a movie with a specific year
            var createDto = new CreateMovieDto
            {
                Title = "Year-Specific Movie",
                Description = "A movie from a specific year",
                ReleaseYear = 2020,
                Director = "Test Director",
                RuntimeMinutes = 100,
                Status = Status.Uncharted,
                MediaType = MediaType.Movie,
                Topics = new[] { "test" },
                Genres = new[] { "comedy" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/movie", createContent);

            // Act
            var response = await _client.GetAsync("/api/movie/by-year/2020");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var movies = JsonSerializer.Deserialize<List<MovieResponseDto>>(content, _jsonOptions);
            Assert.NotNull(movies);
            Assert.True(movies.Count >= 1);
            Assert.All(movies, m => Assert.Equal(2020, m.ReleaseYear));
        }

        #endregion

        #region POST Tests

        [Fact]
        public async Task CreateMovie_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateMovieDto
            {
                Title = "New Test Movie",
                Description = "A comprehensive test movie",
                ReleaseYear = 2024,
                Director = "Jane Director",
                Cast = "Actor One, Actor Two, Actor Three",
                RuntimeMinutes = 135,
                MpaaRating = "PG-13",
                ImdbId = "tt1234567",
                Status = Status.Uncharted,
                MediaType = MediaType.Movie,
                Rating = Rating.Like,
                Topics = new[] { "action", "sci-fi", "adventure" },
                Genres = new[] { "science fiction", "action" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/movie", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMovie = JsonSerializer.Deserialize<MovieResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdMovie);
            Assert.Equal("New Test Movie", createdMovie.Title);
            Assert.Equal("A comprehensive test movie", createdMovie.Description);
            Assert.Equal(2024, createdMovie.ReleaseYear);
            Assert.Equal("Jane Director", createdMovie.Director);
            Assert.Equal("Actor One, Actor Two, Actor Three", createdMovie.Cast);
            Assert.Equal(135, createdMovie.RuntimeMinutes);
            Assert.Equal("PG-13", createdMovie.MpaaRating);
            Assert.Equal("tt1234567", createdMovie.ImdbId);
            Assert.Equal(MediaType.Movie, createdMovie.MediaType);
            Assert.Contains("action", createdMovie.Topics);
            Assert.Contains("sci-fi", createdMovie.Topics);
            Assert.Contains("science fiction", createdMovie.Genres);
        }

        [Fact]
        public async Task CreateMovie_WithMinimalData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateMovieDto
            {
                Title = "Minimal Movie",
                Status = Status.Uncharted,
                MediaType = MediaType.Movie
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/movie", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMovie = JsonSerializer.Deserialize<MovieResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdMovie);
            Assert.Equal("Minimal Movie", createdMovie.Title);
            Assert.Equal(MediaType.Movie, createdMovie.MediaType);
        }

        [Fact]
        public async Task CreateMovie_WithTmdbData_ShouldReturnCreated()
        {
            // Arrange - Create a movie with TMDB-specific fields
            var createDto = new CreateMovieDto
            {
                Title = "TMDB Test Movie",
                Description = "A movie imported from TMDB",
                TmdbId = "12345",
                TmdbRating = 8.5,
                TmdbBackdropPath = "/backdrop123.jpg",
                Tagline = "An epic adventure",
                Homepage = "https://example.com/movie",
                OriginalLanguage = "en",
                OriginalTitle = "TMDB Test Movie Original",
                ReleaseYear = 2023,
                RuntimeMinutes = 140,
                Status = Status.Uncharted,
                MediaType = MediaType.Movie
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/movie", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMovie = JsonSerializer.Deserialize<MovieResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(createdMovie);
            Assert.Equal("12345", createdMovie.TmdbId);
            Assert.Equal(8.5, createdMovie.TmdbRating);
            Assert.Equal("/backdrop123.jpg", createdMovie.TmdbBackdropPath);
            Assert.Equal("An epic adventure", createdMovie.Tagline);
            Assert.Equal("https://example.com/movie", createdMovie.Homepage);
            Assert.Equal("en", createdMovie.OriginalLanguage);
            Assert.Equal("TMDB Test Movie Original", createdMovie.OriginalTitle);
        }

        [Fact]
        public async Task CreateMovie_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange - Missing required fields
            var createDto = new CreateMovieDto
            {
                Title = "", // Empty title
                Status = Status.Uncharted,
                MediaType = MediaType.Movie
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/movie", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task UpdateMovie_WithValidData_ShouldReturnOk()
        {
            // Arrange - First create a movie
            var createDto = new CreateMovieDto
            {
                Title = "Original Movie Title",
                Description = "Original description",
                ReleaseYear = 2020,
                Director = "Original Director",
                RuntimeMinutes = 100,
                Status = Status.Uncharted,
                MediaType = MediaType.Movie,
                Topics = new[] { "original" },
                Genres = new[] { "drama" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/movie", createContent);
            var createdMovie = JsonSerializer.Deserialize<MovieResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Now update it
            var updateDto = new CreateMovieDto
            {
                Title = "Updated Movie Title",
                Description = "Updated description",
                ReleaseYear = 2021,
                Director = "Updated Director",
                Cast = "New Cast Members",
                RuntimeMinutes = 120,
                MpaaRating = "R",
                Status = Status.ActivelyExploring,
                MediaType = MediaType.Movie,
                Rating = Rating.SuperLike,
                Topics = new[] { "updated", "modified" },
                Genres = new[] { "action", "thriller" }
            };

            var updateContent = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/movie/{createdMovie.Id}", updateContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedMovie = JsonSerializer.Deserialize<MovieResponseDto>(responseContent, _jsonOptions);
            
            Assert.NotNull(updatedMovie);
            Assert.Equal("Updated Movie Title", updatedMovie.Title);
            Assert.Equal("Updated description", updatedMovie.Description);
            Assert.Equal(2021, updatedMovie.ReleaseYear);
            Assert.Equal("Updated Director", updatedMovie.Director);
            Assert.Equal("New Cast Members", updatedMovie.Cast);
            Assert.Equal(120, updatedMovie.RuntimeMinutes);
            Assert.Equal("R", updatedMovie.MpaaRating);
            Assert.Equal(Status.ActivelyExploring, updatedMovie.Status);
            Assert.Contains("updated", updatedMovie.Topics);
            Assert.Contains("modified", updatedMovie.Topics);
            Assert.Contains("action", updatedMovie.Genres);
            Assert.Contains("thriller", updatedMovie.Genres);
        }

        [Fact]
        public async Task UpdateMovie_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var updateDto = new CreateMovieDto
            {
                Title = "Updated Movie",
                Status = Status.Uncharted,
                MediaType = MediaType.Movie
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/movie/{invalidId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeleteMovie_WithValidId_ShouldReturnNoContent()
        {
            // Arrange - First create a movie
            var createDto = new CreateMovieDto
            {
                Title = "Movie to Delete",
                Description = "This movie will be deleted",
                ReleaseYear = 2023,
                Status = Status.Uncharted,
                MediaType = MediaType.Movie,
                Topics = new[] { "test" },
                Genres = new[] { "test" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/movie", createContent);
            var createdMovie = JsonSerializer.Deserialize<MovieResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), 
                _jsonOptions
            );

            // Act
            var response = await _client.DeleteAsync($"/api/movie/{createdMovie.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the movie is actually deleted
            var getResponse = await _client.GetAsync($"/api/movie/{createdMovie.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteMovie_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/movie/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateMovie_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/movie", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateMovie_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/movie/{movieId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Data Validation Tests

        [Fact]
        public async Task CreateMovie_WithLongTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateMovieDto
            {
                Title = new string('A', 501), // Exceeds 500 character limit
                Status = Status.Uncharted,
                MediaType = MediaType.Movie
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/movie", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMovie_WithInvalidYear_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateMovieDto
            {
                Title = "Test Movie",
                ReleaseYear = -100, // Invalid year
                Status = Status.Uncharted,
                MediaType = MediaType.Movie
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/movie", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMovie_WithNegativeRuntime_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateMovieDto
            {
                Title = "Test Movie",
                RuntimeMinutes = -50, // Negative runtime
                Status = Status.Uncharted,
                MediaType = MediaType.Movie
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/movie", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region TMDB Integration Tests

        [Fact]
        public async Task SearchTmdbMovies_WithValidQuery_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/movie/search-tmdb?query=inception");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<List<MovieSearchResultDto>>(content, _jsonOptions);
            Assert.NotNull(searchResults);
        }

        [Fact]
        public async Task SearchTmdbMovies_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/movie/search-tmdb?query=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SearchTmdbMovies_WithPagination_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/movie/search-tmdb?query=matrix&page=2");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        #endregion
    }
}

