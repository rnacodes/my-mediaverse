using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.TMDB;
using NSubstitute;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class TvShowControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public TvShowControllerIntegrationTests(ApiFactory factory)
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

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region GET Tests

        [Fact]
        public async Task GetAllTvShows_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/tvshow");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var tvShows = JsonSerializer.Deserialize<List<TvShowResponseDto>>(content, _jsonOptions);
            Assert.NotNull(tvShows);
        }

        [Fact]
        public async Task GetTvShow_WithValidId_ShouldReturnOk()
        {
            // Arrange - First create a TV show
            var createDto = new CreateTvShowDto
            {
                Title = "Test TV Show for Get",
                Description = "A test TV show description",
                FirstAirYear = 2020,
                LastAirYear = 2023,
                Creator = "Test Creator",
                NumberOfSeasons = 3,
                NumberOfEpisodes = 30,
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow,
                Topics = new[] { "drama", "mystery" },
                Genres = new[] { "thriller", "crime" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/tvshow", createContent);
            var createdTvShow = JsonSerializer.Deserialize<TvShowResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );

            // Act
            var response = await _client.GetAsync($"/api/tvshow/{createdTvShow!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var tvShow = JsonSerializer.Deserialize<TvShowResponseDto>(content, _jsonOptions);
            Assert.NotNull(tvShow);
            Assert.Equal(createdTvShow.Id, tvShow.Id);
            Assert.Equal("Test TV Show for Get", tvShow.Title);
        }

        [Fact]
        public async Task GetTvShow_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/tvshow/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTvShowsByCreator_WithValidCreator_ShouldReturnOk()
        {
            // Arrange - First create a TV show with a specific creator
            var createDto = new CreateTvShowDto
            {
                Title = "Created TV Show",
                Description = "A TV show by a specific creator",
                Creator = "Vince Gilligan",
                FirstAirYear = 2008,
                LastAirYear = 2013,
                NumberOfSeasons = 5,
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow,
                Topics = new[] { "test" },
                Genres = new[] { "drama" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/tvshow", createContent);

            // Act
            var response = await _client.GetAsync("/api/tvshow/by-creator/Vince%20Gilligan");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var tvShows = JsonSerializer.Deserialize<List<TvShowResponseDto>>(content, _jsonOptions);
            Assert.NotNull(tvShows);
            Assert.True(tvShows.Count >= 1);
            Assert.All(tvShows, t => Assert.Equal("Vince Gilligan", t.Creator));
        }

        [Fact]
        public async Task GetTvShowsByYear_WithValidYear_ShouldReturnOk()
        {
            // Arrange - First create a TV show with a specific year
            var createDto = new CreateTvShowDto
            {
                Title = "Year-Specific TV Show",
                Description = "A TV show from a specific year",
                FirstAirYear = 2019,
                Creator = "Test Creator",
                NumberOfSeasons = 2,
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow,
                Topics = new[] { "test" },
                Genres = new[] { "comedy" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/tvshow", createContent);

            // Act
            var response = await _client.GetAsync("/api/tvshow/by-year/2019");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var tvShows = JsonSerializer.Deserialize<List<TvShowResponseDto>>(content, _jsonOptions);
            Assert.NotNull(tvShows);
            Assert.True(tvShows.Count >= 1);
            Assert.All(tvShows, t => Assert.Equal(2019, t.FirstAirYear));
        }

        #endregion

        #region POST Tests

        [Fact]
        public async Task CreateTvShow_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateTvShowDto
            {
                Title = "New Test TV Show",
                Description = "A comprehensive test TV show",
                FirstAirYear = 2024,
                LastAirYear = null, // Still airing
                Creator = "Jane Creator",
                Cast = "Actor One, Actor Two, Actor Three",
                NumberOfSeasons = 2,
                NumberOfEpisodes = 20,
                ContentRating = "TV-14",
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow,
                Rating = Rating.Like,
                Topics = new[] { "sci-fi", "drama", "adventure" },
                Genres = new[] { "science fiction", "drama" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdTvShow = JsonSerializer.Deserialize<TvShowResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdTvShow);
            Assert.Equal("New Test TV Show", createdTvShow.Title);
            Assert.Equal("A comprehensive test TV show", createdTvShow.Description);
            Assert.Equal(2024, createdTvShow.FirstAirYear);
            Assert.Null(createdTvShow.LastAirYear);
            Assert.Equal("Jane Creator", createdTvShow.Creator);
            Assert.Equal("Actor One, Actor Two, Actor Three", createdTvShow.Cast);
            Assert.Equal(2, createdTvShow.NumberOfSeasons);
            Assert.Equal(20, createdTvShow.NumberOfEpisodes);
            Assert.Equal("TV-14", createdTvShow.ContentRating);
            Assert.Equal(MediaType.TVShow, createdTvShow.MediaType);
            Assert.Contains("sci-fi", createdTvShow.Topics);
            Assert.Contains("drama", createdTvShow.Topics);
            Assert.Contains("science fiction", createdTvShow.Genres);
        }

        [Fact]
        public async Task CreateTvShow_WithCompletedShow_ShouldReturnCreated()
        {
            // Arrange - Create a completed TV show with both air years
            var createDto = new CreateTvShowDto
            {
                Title = "Completed TV Show",
                Description = "A show that has ended",
                FirstAirYear = 2015,
                LastAirYear = 2020,
                Creator = "Test Creator",
                NumberOfSeasons = 5,
                NumberOfEpisodes = 50,
                Status = Status.Completed,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdTvShow = JsonSerializer.Deserialize<TvShowResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdTvShow);
            Assert.Equal(2015, createdTvShow.FirstAirYear);
            Assert.Equal(2020, createdTvShow.LastAirYear);
            Assert.Equal(Status.Completed, createdTvShow.Status);
        }

        [Fact]
        public async Task CreateTvShow_WithTmdbData_ShouldReturnCreated()
        {
            // Arrange - Create a TV show with TMDB-specific fields
            var createDto = new CreateTvShowDto
            {
                Title = "TMDB Test TV Show",
                Description = "A TV show imported from TMDB",
                TmdbId = "54321",
                TmdbRating = 9.0,
                TmdbPosterPath = "/poster123.jpg",
                Tagline = "The best show ever",
                Homepage = "https://example.com/tvshow",
                OriginalLanguage = "en",
                OriginalName = "TMDB Test TV Show Original",
                FirstAirYear = 2022,
                NumberOfSeasons = 3,
                NumberOfEpisodes = 30,
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdTvShow = JsonSerializer.Deserialize<TvShowResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdTvShow);
            Assert.Equal("54321", createdTvShow.TmdbId);
            Assert.Equal(9.0, createdTvShow.TmdbRating);
            Assert.Equal("/poster123.jpg", createdTvShow.TmdbPosterPath);
            Assert.Equal("The best show ever", createdTvShow.Tagline);
            Assert.Equal("https://example.com/tvshow", createdTvShow.Homepage);
            Assert.Equal("en", createdTvShow.OriginalLanguage);
            Assert.Equal("TMDB Test TV Show Original", createdTvShow.OriginalName);
        }

        [Fact]
        public async Task CreateTvShow_WithMinimalData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateTvShowDto
            {
                Title = "Minimal TV Show",
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdTvShow = JsonSerializer.Deserialize<TvShowResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdTvShow);
            Assert.Equal("Minimal TV Show", createdTvShow.Title);
            Assert.Equal(MediaType.TVShow, createdTvShow.MediaType);
        }

        [Fact]
        public async Task CreateTvShow_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange - Missing required fields
            var createDto = new CreateTvShowDto
            {
                Title = "", // Empty title
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task UpdateTvShow_WithValidData_ShouldReturnOk()
        {
            // Arrange - First create a TV show
            var createDto = new CreateTvShowDto
            {
                Title = "Original TV Show Title",
                Description = "Original description",
                FirstAirYear = 2020,
                Creator = "Original Creator",
                NumberOfSeasons = 2,
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow,
                Topics = new[] { "original" },
                Genres = new[] { "drama" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/tvshow", createContent);
            var createdTvShow = JsonSerializer.Deserialize<TvShowResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );

            // Now update it
            var updateDto = new CreateTvShowDto
            {
                Title = "Updated TV Show Title",
                Description = "Updated description",
                FirstAirYear = 2020,
                LastAirYear = 2024, // Show has ended
                Creator = "Updated Creator",
                Cast = "New Cast Members",
                NumberOfSeasons = 4,
                NumberOfEpisodes = 40,
                ContentRating = "TV-MA",
                Status = Status.Completed,
                MediaType = MediaType.TVShow,
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
            var response = await _client.PutAsync($"/api/tvshow/{createdTvShow!.Id}", updateContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedTvShow = JsonSerializer.Deserialize<TvShowResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(updatedTvShow);
            Assert.Equal("Updated TV Show Title", updatedTvShow.Title);
            Assert.Equal("Updated description", updatedTvShow.Description);
            Assert.Equal(2024, updatedTvShow.LastAirYear);
            Assert.Equal("Updated Creator", updatedTvShow.Creator);
            Assert.Equal("New Cast Members", updatedTvShow.Cast);
            Assert.Equal(4, updatedTvShow.NumberOfSeasons);
            Assert.Equal(40, updatedTvShow.NumberOfEpisodes);
            Assert.Equal("TV-MA", updatedTvShow.ContentRating);
            Assert.Equal(Status.Completed, updatedTvShow.Status);
            Assert.Contains("updated", updatedTvShow.Topics);
            Assert.Contains("modified", updatedTvShow.Topics);
            Assert.Contains("action", updatedTvShow.Genres);
            Assert.Contains("thriller", updatedTvShow.Genres);
        }

        [Fact]
        public async Task UpdateTvShow_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var updateDto = new CreateTvShowDto
            {
                Title = "Updated TV Show",
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/tvshow/{invalidId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeleteTvShow_WithValidId_ShouldReturnNoContent()
        {
            // Arrange - First create a TV show
            var createDto = new CreateTvShowDto
            {
                Title = "TV Show to Delete",
                Description = "This TV show will be deleted",
                FirstAirYear = 2023,
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow,
                Topics = new[] { "test" },
                Genres = new[] { "test" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/tvshow", createContent);
            var createdTvShow = JsonSerializer.Deserialize<TvShowResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );

            // Act
            var response = await _client.DeleteAsync($"/api/tvshow/{createdTvShow!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the TV show is actually deleted
            var getResponse = await _client.GetAsync($"/api/tvshow/{createdTvShow.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteTvShow_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/tvshow/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateTvShow_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTvShow_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var tvShowId = Guid.NewGuid();
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/tvshow/{tvShowId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Data Validation Tests

        [Fact]
        public async Task CreateTvShow_WithLongTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateTvShowDto
            {
                Title = new string('A', 501), // Exceeds 500 character limit
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTvShow_WithInvalidYear_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateTvShowDto
            {
                Title = "Test TV Show",
                FirstAirYear = -100, // Invalid year
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTvShow_WithNegativeSeasons_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateTvShowDto
            {
                Title = "Test TV Show",
                NumberOfSeasons = -5, // Negative seasons
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTvShow_WithNegativeEpisodes_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateTvShowDto
            {
                Title = "Test TV Show",
                NumberOfEpisodes = -10, // Negative episodes
                Status = Status.Uncharted,
                MediaType = MediaType.TVShow
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region TMDB Integration Tests

        [Fact]
        public async Task SearchTmdbTvShows_WithValidQuery_ShouldReturnOk()
        {
            // Arrange - substitute ITmdbService so the controller's external call is hermetic.
            // Default NSubstitute returns null for reference types; the controller dereferences
            // SearchTvShowsAsync(...).Results, so we explicitly return an empty result envelope.
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.SearchTvShowsAsync("breaking bad", 1).Returns(new TmdbTvSearchResultDto()));

            // Act
            var response = await client.GetAsync("/api/tvshow/search-tmdb?query=breaking+bad");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<List<TvShowSearchResultDto>>(content, _jsonOptions);
            Assert.NotNull(searchResults);
        }

        [Fact]
        public async Task SearchTmdbTvShows_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // The controller validates the query before reaching ITmdbService, so no substitute needed.

            // Act
            var response = await _client.GetAsync("/api/tvshow/search-tmdb?query=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SearchTmdbTvShows_WithPagination_ShouldReturnOk()
        {
            // Arrange
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.SearchTvShowsAsync("game of thrones", 2).Returns(new TmdbTvSearchResultDto()));

            // Act
            var response = await client.GetAsync("/api/tvshow/search-tmdb?query=game+of+thrones&page=2");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        #endregion
    }
}
