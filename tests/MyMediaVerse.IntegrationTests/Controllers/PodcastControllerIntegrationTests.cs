using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Infrastructure.Data;
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
    public class PodcastControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public PodcastControllerIntegrationTests(WebApplicationFactory factory)
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

            var episode2Json = JsonSerializer.Serialize(episode2Dto, _jsonOptions);
            var episode2Content = new StringContent(episode2Json, Encoding.UTF8, "application/json");
            var episode2Response = await _client.PostAsync("/api/podcast/episodes", episode2Content);
            Assert.Equal(HttpStatusCode.Created, episode2Response.StatusCode);
            var episode2ResponseContent = await episode2Response.Content.ReadAsStringAsync();
            var createdEpisode2 = JsonSerializer.Deserialize<PodcastEpisodeResponseDto>(episode2ResponseContent, _jsonOptions);

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
