using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class PodcastControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public PodcastControllerIntegrationTests(ApiFactory factory)
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
        public async Task GetPodcastSeries_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/podcast/series");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var series = JsonSerializer.Deserialize<List<PodcastSeriesResponseDto>>(content, _jsonOptions);
            Assert.NotNull(series);
        }

        [Fact]
        public async Task GetAllPodcastEpisodes_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/podcast/episodes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var episodes = JsonSerializer.Deserialize<List<PodcastEpisodeResponseDto>>(content, _jsonOptions);
            Assert.NotNull(episodes);
        }

        [Fact]
        public async Task SearchPodcastSeries_WithValidQuery_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/podcast/series/search?query=test");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<PodcastSeriesResponseDto>>(content, _jsonOptions);
            Assert.NotNull(results);
        }

        [Fact]
        public async Task SearchPodcastSeries_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/podcast/series/search?query=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPodcastSeries_WithValidId_ShouldReturnOk()
        {
            // Arrange - Create a series first
            var createDto = new CreatePodcastSeriesDto
            {
                Title = "Test Podcast Series for Get",
                Publisher = "Test Publisher",
                Status = Status.Uncharted
            };

            var json = JsonSerializer.Serialize(createDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/podcast/series", content);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var createdSeries = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(createResponseContent, _jsonOptions);
            Assert.NotNull(createdSeries);

            // Act
            var response = await _client.GetAsync($"/api/podcast/series/{createdSeries.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var retrievedSeries = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(retrievedSeries);
            Assert.Equal(createdSeries.Id, retrievedSeries.Id);
            Assert.Equal("Test Podcast Series for Get", retrievedSeries.Title);
        }

        [Fact]
        public async Task GetPodcastSeries_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/podcast/series/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region POST Tests - Series

        [Fact]
        public async Task CreatePodcastSeries_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreatePodcastSeriesDto
            {
                Title = "Integration Test Podcast Series",
                Publisher = "Test Publisher",
                Status = Status.Uncharted,
                Description = "A test podcast series for integration testing"
            };

            var json = JsonSerializer.Serialize(createDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/podcast/series", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdSeries = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(createdSeries);
            Assert.Equal("Integration Test Podcast Series", createdSeries.Title);
            Assert.Equal("Series", createdSeries.PodcastType);
        }

        [Fact]
        public async Task CreatePodcastSeries_WithNullData_ShouldReturnBadRequest()
        {
            // Arrange
            var content = new StringContent("null", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/podcast/series", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region POST Tests - Episodes

        [Fact]
        public async Task CreatePodcastEpisode_WithValidData_ShouldReturnCreated()
        {
            // Arrange - First create a series
            var seriesDto = new CreatePodcastSeriesDto
            {
                Title = "Parent Series for Episode Test",
                Publisher = "Test Publisher",
                Status = Status.Uncharted
            };

            var seriesJson = JsonSerializer.Serialize(seriesDto, _jsonOptions);
            var seriesContent = new StringContent(seriesJson, Encoding.UTF8, "application/json");
            var seriesResponse = await _client.PostAsync("/api/podcast/series", seriesContent);

            Assert.Equal(HttpStatusCode.Created, seriesResponse.StatusCode);
            var seriesResponseContent = await seriesResponse.Content.ReadAsStringAsync();
            var createdSeries = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(seriesResponseContent, _jsonOptions);
            Assert.NotNull(createdSeries);

            // Now create an episode
            var episodeDto = new CreatePodcastEpisodeDto
            {
                Title = "Integration Test Episode",
                SeriesId = createdSeries.Id,
                Status = Status.Uncharted,
                Description = "A test episode for integration testing"
            };

            var episodeJson = JsonSerializer.Serialize(episodeDto, _jsonOptions);
            var episodeContent = new StringContent(episodeJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/podcast/episodes", episodeContent);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdEpisode = JsonSerializer.Deserialize<PodcastEpisodeResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(createdEpisode);
            Assert.Equal("Integration Test Episode", createdEpisode.Title);
            Assert.Equal(createdSeries.Id, createdEpisode.SeriesId);
            Assert.Equal("Episode", createdEpisode.PodcastType);
        }

        [Fact]
        public async Task GetEpisodesBySeriesId_WithValidSeriesId_ShouldReturnOk()
        {
            // Arrange - Create a series and episode
            var seriesDto = new CreatePodcastSeriesDto
            {
                Title = "Series for Episodes Test",
                Status = Status.Uncharted
            };

            var seriesJson = JsonSerializer.Serialize(seriesDto, _jsonOptions);
            var seriesContent = new StringContent(seriesJson, Encoding.UTF8, "application/json");
            var seriesResponse = await _client.PostAsync("/api/podcast/series", seriesContent);

            Assert.Equal(HttpStatusCode.Created, seriesResponse.StatusCode);
            var seriesResponseContent = await seriesResponse.Content.ReadAsStringAsync();
            var createdSeries = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(seriesResponseContent, _jsonOptions);
            Assert.NotNull(createdSeries);

            var episodeDto = new CreatePodcastEpisodeDto
            {
                Title = "Episode for Series Test",
                SeriesId = createdSeries.Id,
                Status = Status.Uncharted
            };

            var episodeJson = JsonSerializer.Serialize(episodeDto, _jsonOptions);
            var episodeContent = new StringContent(episodeJson, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/podcast/episodes", episodeContent);

            // Act
            var response = await _client.GetAsync($"/api/podcast/series/{createdSeries.Id}/episodes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var episodes = JsonSerializer.Deserialize<List<PodcastEpisodeResponseDto>>(responseContent, _jsonOptions);
            Assert.NotNull(episodes);
            Assert.Single(episodes);
            Assert.Equal("Episode for Series Test", episodes[0].Title);
            Assert.Equal("Episode", episodes[0].PodcastType);
        }

        [Fact]
        public async Task GetEpisodesBySeriesId_EpisodeWithoutThumbnail_FallsBackToSeriesThumbnail()
        {
            // Arrange - a series with a thumbnail and an episode with none of its own
            const string seriesThumbnail = "https://example.com/series-cover.jpg";

            var seriesDto = new CreatePodcastSeriesDto
            {
                Title = "Series With Thumbnail",
                Status = Status.Uncharted,
                Thumbnail = seriesThumbnail
            };

            var seriesContent = new StringContent(
                JsonSerializer.Serialize(seriesDto, _jsonOptions), Encoding.UTF8, "application/json");
            var seriesResponse = await _client.PostAsync("/api/podcast/series", seriesContent);
            var createdSeries = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(
                await seriesResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(createdSeries);

            var episodeDto = new CreatePodcastEpisodeDto
            {
                Title = "Episode Without Thumbnail",
                SeriesId = createdSeries.Id,
                Status = Status.Uncharted
            };

            var episodeContent = new StringContent(
                JsonSerializer.Serialize(episodeDto, _jsonOptions), Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/podcast/episodes", episodeContent);

            // Act
            var response = await _client.GetAsync($"/api/podcast/series/{createdSeries.Id}/episodes");

            // Assert - the episode inherits the series thumbnail rather than returning blank
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var episodes = JsonSerializer.Deserialize<List<PodcastEpisodeResponseDto>>(
                await response.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(episodes);
            Assert.Single(episodes);
            Assert.Equal(seriesThumbnail, episodes[0].Thumbnail);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeletePodcastSeries_WithValidId_ShouldReturnNoContent()
        {
            // Arrange - Create a series first
            var createDto = new CreatePodcastSeriesDto
            {
                Title = "Test Podcast Series for Delete",
                Status = Status.Uncharted
            };

            var json = JsonSerializer.Serialize(createDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/podcast/series", content);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var createdSeries = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(createResponseContent, _jsonOptions);
            Assert.NotNull(createdSeries);

            // Act
            var response = await _client.DeleteAsync($"/api/podcast/series/{createdSeries.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify it's actually deleted
            var getResponse = await _client.GetAsync($"/api/podcast/series/{createdSeries.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeletePodcastSeries_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/podcast/series/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeletePodcastSeries_ShouldDeleteSeriesAndEpisodes()
        {
            // Arrange - Create a series with episodes
            var seriesDto = new CreatePodcastSeriesDto
            {
                Title = "Series to Delete with Episodes",
                Status = Status.Uncharted
            };

            var seriesJson = JsonSerializer.Serialize(seriesDto, _jsonOptions);
            var seriesContent = new StringContent(seriesJson, Encoding.UTF8, "application/json");
            var seriesResponse = await _client.PostAsync("/api/podcast/series", seriesContent);

            Assert.Equal(HttpStatusCode.Created, seriesResponse.StatusCode);
            var seriesResponseContent = await seriesResponse.Content.ReadAsStringAsync();
            var createdSeries = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(seriesResponseContent, _jsonOptions);
            Assert.NotNull(createdSeries);

            // Create episodes
            var episode1Dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode 1 to Delete",
                SeriesId = createdSeries.Id,
                Status = Status.Uncharted
            };

            var episode2Dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode 2 to Delete",
                SeriesId = createdSeries.Id,
                Status = Status.Uncharted
            };

            var episode1Json = JsonSerializer.Serialize(episode1Dto, _jsonOptions);
            var episode1Content = new StringContent(episode1Json, Encoding.UTF8, "application/json");
            var episode1Response = await _client.PostAsync("/api/podcast/episodes", episode1Content);
            Assert.Equal(HttpStatusCode.Created, episode1Response.StatusCode);
            var episode1ResponseContent = await episode1Response.Content.ReadAsStringAsync();
            var createdEpisode1 = JsonSerializer.Deserialize<PodcastEpisodeResponseDto>(episode1ResponseContent, _jsonOptions);
            Assert.NotNull(createdEpisode1);

            var episode2Json = JsonSerializer.Serialize(episode2Dto, _jsonOptions);
            var episode2Content = new StringContent(episode2Json, Encoding.UTF8, "application/json");
            var episode2Response = await _client.PostAsync("/api/podcast/episodes", episode2Content);
            Assert.Equal(HttpStatusCode.Created, episode2Response.StatusCode);
            var episode2ResponseContent = await episode2Response.Content.ReadAsStringAsync();
            var createdEpisode2 = JsonSerializer.Deserialize<PodcastEpisodeResponseDto>(episode2ResponseContent, _jsonOptions);
            Assert.NotNull(createdEpisode2);

            // Act - Delete the series
            var deleteResponse = await _client.DeleteAsync($"/api/podcast/series/{createdSeries.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify series is deleted
            var getSeriesResponse = await _client.GetAsync($"/api/podcast/series/{createdSeries.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getSeriesResponse.StatusCode);

            // Verify episodes are also deleted
            var getEpisode1Response = await _client.GetAsync($"/api/podcast/episodes/{createdEpisode1.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getEpisode1Response.StatusCode);

            var getEpisode2Response = await _client.GetAsync($"/api/podcast/episodes/{createdEpisode2.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getEpisode2Response.StatusCode);
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task UpdatePodcastSeries_ShouldReplaceTopicsAndGenres_RemovingOldOnes()
        {
            // Arrange - create a series with an initial set of topics/genres
            var createDto = new CreatePodcastSeriesDto
            {
                Title = "Series To Update",
                Status = Status.Uncharted,
                Topics = new[] { "alpha", "beta" },
                Genres = new[] { "old-genre" }
            };
            var createContent = new StringContent(JsonSerializer.Serialize(createDto, _jsonOptions), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/podcast/series", createContent);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var created = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(
                await createResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(created);

            // Act - replace the topics/genres (mixed case to verify normalization)
            var updateDto = new CreatePodcastSeriesDto
            {
                Title = "Series Updated",
                Status = Status.Completed,
                Topics = new[] { "Beta", "Gamma" },
                Genres = new[] { "New-Genre" }
            };
            var updateContent = new StringContent(JsonSerializer.Serialize(updateDto, _jsonOptions), Encoding.UTF8, "application/json");
            var updateResponse = await _client.PutAsync($"/api/podcast/series/{created.Id}", updateContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var getResponse = await _client.GetAsync($"/api/podcast/series/{created.Id}");
            var updated = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(
                await getResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(updated);
            Assert.Equal("Series Updated", updated.Title);
            Assert.Equal(new[] { "beta", "gamma" }, updated.Topics.OrderBy(t => t).ToArray());
            Assert.DoesNotContain("alpha", updated.Topics);
            Assert.Equal(new[] { "new-genre" }, updated.Genres.ToArray());
            Assert.DoesNotContain("old-genre", updated.Genres);
        }

        [Fact]
        public async Task UpdatePodcastSeries_ShouldPreserveSubscriptionState()
        {
            // Arrange - a subscribed series with an external id (sync plumbing)
            var createDto = new CreatePodcastSeriesDto
            {
                Title = "Subscribed Series",
                Status = Status.Uncharted,
                IsSubscribed = true,
                ExternalId = "listennotes-xyz"
            };
            var createContent = new StringContent(JsonSerializer.Serialize(createDto, _jsonOptions), Encoding.UTF8, "application/json");
            var created = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(
                await (await _client.PostAsync("/api/podcast/series", createContent)).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(created);

            // Act - edit-form payload carries no subscription/sync fields
            var updateDto = new CreatePodcastSeriesDto { Title = "Subscribed Series Renamed", Status = Status.Uncharted };
            var updateContent = new StringContent(JsonSerializer.Serialize(updateDto, _jsonOptions), Encoding.UTF8, "application/json");
            await _client.PutAsync($"/api/podcast/series/{created.Id}", updateContent);

            // Assert - subscription/sync state survives the edit
            var updated = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(
                await (await _client.GetAsync($"/api/podcast/series/{created.Id}")).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(updated);
            Assert.Equal("Subscribed Series Renamed", updated.Title);
            Assert.True(updated.IsSubscribed);
            Assert.Equal("listennotes-xyz", updated.ExternalId);
        }

        [Fact]
        public async Task UpdatePodcastSeries_WithInvalidId_ShouldReturnNotFound()
        {
            var updateDto = new CreatePodcastSeriesDto { Title = "Nope", Status = Status.Uncharted };
            var content = new StringContent(JsonSerializer.Serialize(updateDto, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _client.PutAsync($"/api/podcast/series/{Guid.NewGuid()}", content);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePodcastEpisode_ShouldUpdateFields_AndKeepSeriesId()
        {
            // Arrange - a series and an episode under it
            var seriesContent = new StringContent(
                JsonSerializer.Serialize(new CreatePodcastSeriesDto { Title = "Series For Episode Update", Status = Status.Uncharted }, _jsonOptions),
                Encoding.UTF8, "application/json");
            var series = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(
                await (await _client.PostAsync("/api/podcast/series", seriesContent)).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(series);

            var episodeContent = new StringContent(
                JsonSerializer.Serialize(new CreatePodcastEpisodeDto
                {
                    Title = "Episode Original",
                    SeriesId = series.Id,
                    Status = Status.Uncharted,
                    DurationInSeconds = 100
                }, _jsonOptions),
                Encoding.UTF8, "application/json");
            var episode = JsonSerializer.Deserialize<PodcastEpisodeResponseDto>(
                await (await _client.PostAsync("/api/podcast/episodes", episodeContent)).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(episode);

            // Act - update editable fields (SeriesId in the DTO is intentionally a different value)
            var updateDto = new CreatePodcastEpisodeDto
            {
                Title = "Episode Updated",
                SeriesId = Guid.NewGuid(),
                Status = Status.Completed,
                AudioLink = "https://example.com/ep.mp3",
                DurationInSeconds = 3600,
                EpisodeNumber = 7
            };
            var updateContent = new StringContent(JsonSerializer.Serialize(updateDto, _jsonOptions), Encoding.UTF8, "application/json");
            var updateResponse = await _client.PutAsync($"/api/podcast/episodes/{episode.Id}", updateContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updated = JsonSerializer.Deserialize<PodcastEpisodeResponseDto>(
                await (await _client.GetAsync($"/api/podcast/episodes/{episode.Id}")).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(updated);
            Assert.Equal("Episode Updated", updated.Title);
            Assert.Equal(3600, updated.DurationInSeconds);
            Assert.Equal(7, updated.EpisodeNumber);
            Assert.Equal("https://example.com/ep.mp3", updated.AudioLink);
            // The episode stays with its original series regardless of the DTO value
            Assert.Equal(series.Id, updated.SeriesId);
        }

        [Fact]
        public async Task UpdatePodcastEpisode_ShouldReplaceTopicsAndGenres_RemovingOldOnes()
        {
            // Arrange - a series and an episode that starts with its own topics/genres
            var seriesContent = new StringContent(
                JsonSerializer.Serialize(new CreatePodcastSeriesDto { Title = "Series For Episode Tags", Status = Status.Uncharted }, _jsonOptions),
                Encoding.UTF8, "application/json");
            var series = JsonSerializer.Deserialize<PodcastSeriesResponseDto>(
                await (await _client.PostAsync("/api/podcast/series", seriesContent)).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(series);

            var episodeContent = new StringContent(
                JsonSerializer.Serialize(new CreatePodcastEpisodeDto
                {
                    Title = "Tagged Episode",
                    SeriesId = series.Id,
                    Status = Status.Uncharted,
                    Topics = new[] { "alpha", "beta" },
                    Genres = new[] { "old-genre" }
                }, _jsonOptions),
                Encoding.UTF8, "application/json");
            var episode = JsonSerializer.Deserialize<PodcastEpisodeResponseDto>(
                await (await _client.PostAsync("/api/podcast/episodes", episodeContent)).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(episode);

            // Act - replace the topics/genres (mixed case to verify normalization)
            var updateDto = new CreatePodcastEpisodeDto
            {
                Title = "Tagged Episode",
                SeriesId = series.Id,
                Status = Status.Uncharted,
                Topics = new[] { "Beta", "Gamma" },
                Genres = new[] { "New-Genre" }
            };
            var updateContent = new StringContent(JsonSerializer.Serialize(updateDto, _jsonOptions), Encoding.UTF8, "application/json");
            var updateResponse = await _client.PutAsync($"/api/podcast/episodes/{episode.Id}", updateContent);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            // Assert - re-fetch so we observe persisted state surfaced through the response DTO
            var updated = JsonSerializer.Deserialize<PodcastEpisodeResponseDto>(
                await (await _client.GetAsync($"/api/podcast/episodes/{episode.Id}")).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(updated);
            Assert.Equal(new[] { "beta", "gamma" }, updated.Topics.OrderBy(t => t).ToArray());
            Assert.DoesNotContain("alpha", updated.Topics);
            Assert.Equal(new[] { "new-genre" }, updated.Genres.ToArray());
            Assert.DoesNotContain("old-genre", updated.Genres);
        }

        [Fact]
        public async Task UpdatePodcastEpisode_WithInvalidId_ShouldReturnNotFound()
        {
            var updateDto = new CreatePodcastEpisodeDto { Title = "Nope", SeriesId = Guid.NewGuid(), Status = Status.Uncharted };
            var content = new StringContent(JsonSerializer.Serialize(updateDto, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _client.PutAsync($"/api/podcast/episodes/{Guid.NewGuid()}", content);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Import Tests

        [Fact]
        public async Task ImportPodcastByName_WithEmptyName_ShouldReturnBadRequest()
        {
            // Arrange
            var importDto = new ImportPodcastByNameDto { PodcastName = "" };
            var json = JsonSerializer.Serialize(importDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/podcast/series/from-api/by-name", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion
    }
}
