using Microsoft.AspNetCore.Mvc.Testing;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    public class TvShowEpisodeControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public TvShowEpisodeControllerIntegrationTests(WebApplicationFactory factory)
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

        private async Task<Guid> CreateTestShowAsync()
        {
            var createShowDto = new CreateTvShowDto
            {
                Title = "Test Show",
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                FirstAirYear = 2020
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createShowDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/tvshow", content);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdShow = JsonSerializer.Deserialize<TvShowResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(createdShow);

            return createdShow.Id;
        }

        private async Task<TvShowEpisodeResponseDto> CreateTestEpisodeAsync(Guid showId, int season = 1, int episode = 1, string title = "S1E1")
        {
            var createEpisodeDto = new
            {
                title = title,
                mediaType = "TVShow",
                status = "Completed",
                showId = showId,
                seasonNumber = season,
                episodeNumber = episode
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createEpisodeDto),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/tvshow/episodes", content);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdEpisode = JsonSerializer.Deserialize<TvShowEpisodeResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(createdEpisode);

            return createdEpisode;
        }

        #region GET Episodes by Show Tests

        [Fact]
        public async Task GetEpisodesByShowId_WithNoEpisodes_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            var showId = await CreateTestShowAsync();

            // Act
            var response = await _client.GetAsync($"/api/tvshow/{showId}/episodes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var episodes = JsonSerializer.Deserialize<List<TvShowEpisodeResponseDto>>(content, _jsonOptions);
            Assert.NotNull(episodes);
            Assert.Empty(episodes);
        }

        #endregion

        #region POST Episode Tests

        [Fact]
        public async Task CreateEpisode_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var showId = await CreateTestShowAsync();

            var createEpisodeDto = new
            {
                title = "S1E1",
                mediaType = "TVShow",
                status = "Completed",
                showId = showId,
                seasonNumber = 1,
                episodeNumber = 1
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createEpisodeDto),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/tvshow/episodes", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var episode = JsonSerializer.Deserialize<TvShowEpisodeResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(episode);
            Assert.Equal("S1E1", episode.Title);
            Assert.Equal(showId, episode.ShowId);
            Assert.Equal(1, episode.SeasonNumber);
            Assert.Equal(1, episode.EpisodeNumber);
            Assert.Equal(Status.Completed, episode.Status);
        }

        #endregion

        #region GET Episode by ID Tests

        [Fact]
        public async Task GetEpisode_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var showId = await CreateTestShowAsync();
            var createdEpisode = await CreateTestEpisodeAsync(showId);

            // Act
            var response = await _client.GetAsync($"/api/tvshow/episodes/{createdEpisode.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var episode = JsonSerializer.Deserialize<TvShowEpisodeResponseDto>(content, _jsonOptions);
            Assert.NotNull(episode);
            Assert.Equal(createdEpisode.Id, episode.Id);
            Assert.Equal("S1E1", episode.Title);
            Assert.Equal(showId, episode.ShowId);
            Assert.Equal(1, episode.SeasonNumber);
            Assert.Equal(1, episode.EpisodeNumber);
        }

        [Fact]
        public async Task GetEpisode_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/tvshow/episodes/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region DELETE Episode Tests

        [Fact]
        public async Task DeleteEpisode_WithValidId_ShouldReturnNoContent()
        {
            // Arrange
            var showId = await CreateTestShowAsync();
            var createdEpisode = await CreateTestEpisodeAsync(showId);

            // Act
            var response = await _client.DeleteAsync($"/api/tvshow/episodes/{createdEpisode.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the episode is actually deleted
            var getResponse = await _client.GetAsync($"/api/tvshow/episodes/{createdEpisode.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteEpisode_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/tvshow/episodes/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion
    }
}
