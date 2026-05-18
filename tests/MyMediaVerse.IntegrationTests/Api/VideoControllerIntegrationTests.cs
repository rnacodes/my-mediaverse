using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class VideoControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public VideoControllerIntegrationTests(ApiFactory factory)
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
        public async Task GetAllVideos_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/video");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var videos = JsonSerializer.Deserialize<List<VideoResponseDto>>(content, _jsonOptions);
            Assert.NotNull(videos);
        }

        [Fact]
        public async Task GetVideoSeries_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/video/series");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var series = JsonSerializer.Deserialize<List<VideoResponseDto>>(content, _jsonOptions);
            Assert.NotNull(series);
        }

        [Fact]
        public async Task GetVideo_WithValidId_ShouldReturnOk()
        {
            // Arrange - First create a video
            var createDto = new CreateVideoDto
            {
                Title = "Test Video for Get",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                Topics = new[] { "test" },
                Genres = new[] { "educational" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/video", createContent);
            var createdVideo = JsonSerializer.Deserialize<VideoResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(createdVideo);

            // Act
            var response = await _client.GetAsync($"/api/video/{createdVideo.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var video = JsonSerializer.Deserialize<VideoResponseDto>(content, _jsonOptions);
            Assert.NotNull(video);
            Assert.Equal(createdVideo.Id, video.Id);
            Assert.Equal("Test Video for Get", video.Title);
        }

        [Fact]
        public async Task GetVideo_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/video/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // NOTE: This test is commented out because ChannelName property doesn't exist in DTOs
        // Videos now use ChannelId (Guid) and a nested Channel object instead
        // [Fact]
        // public async Task GetVideosByChannel_WithValidChannel_ShouldReturnOk()
        // {
        //     // TODO: Update this test to use ChannelId instead of ChannelName
        // }

        #endregion

        #region POST Tests

        [Fact]
        public async Task CreateVideo_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateVideoDto
            {
                Title = "New Test Video",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                Description = "Test description",
                LengthInSeconds = 3600,
                ExternalId = "test_external_id",
                Topics = new[] { "technology", "programming" },
                Genres = new[] { "educational", "tutorial" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/video", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdVideo = JsonSerializer.Deserialize<VideoResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdVideo);
            Assert.Equal("New Test Video", createdVideo.Title);
            Assert.Equal("YouTube", createdVideo.Platform);
            Assert.Equal(VideoType.Series, createdVideo.VideoType);
            Assert.Equal(MediaType.Video, createdVideo.MediaType);
            Assert.Equal("Test description", createdVideo.Description);
            Assert.Equal(3600, createdVideo.LengthInSeconds);
            Assert.Equal("test_external_id", createdVideo.ExternalId);
            Assert.Contains("technology", createdVideo.Topics);
            Assert.Contains("programming", createdVideo.Topics);
            Assert.Contains("educational", createdVideo.Genres);
            Assert.Contains("tutorial", createdVideo.Genres);
        }

        [Fact]
        public async Task CreateVideo_WithEpisodeType_ShouldReturnCreated()
        {
            // Arrange - First create a parent series
            var seriesDto = new CreateVideoDto
            {
                Title = "Parent Series",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                Topics = new[] { "test" },
                Genres = new[] { "educational" }
            };

            var seriesContent = new StringContent(
                JsonSerializer.Serialize(seriesDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var seriesResponse = await _client.PostAsync("/api/video", seriesContent);
            var parentSeries = JsonSerializer.Deserialize<VideoResponseDto>(
                await seriesResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(parentSeries);

            // Now create an episode
            var episodeDto = new CreateVideoDto
            {
                Title = "Episode 1",
                Platform = "YouTube",
                VideoType = VideoType.Episode,
                Status = Status.Uncharted,
                ParentVideoId = parentSeries.Id,
                Topics = new[] { "test" },
                Genres = new[] { "educational" }
            };

            var episodeContent = new StringContent(
                JsonSerializer.Serialize(episodeDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/video", episodeContent);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdEpisode = JsonSerializer.Deserialize<VideoResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdEpisode);
            Assert.Equal("Episode 1", createdEpisode.Title);
            Assert.Equal(VideoType.Episode, createdEpisode.VideoType);
            Assert.Equal(parentSeries.Id, createdEpisode.ParentVideoId);
        }

        [Fact]
        public async Task CreateVideo_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange - Missing required fields
            var createDto = new CreateVideoDto
            {
                Title = "", // Empty title to test validation
                Platform = "", // Empty platform to test validation
                VideoType = VideoType.Series,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/video", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region PUT Tests

        [Fact]
        public async Task UpdateVideo_WithValidData_ShouldReturnOk()
        {
            // Arrange - First create a video
            var createDto = new CreateVideoDto
            {
                Title = "Original Title",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                Topics = new[] { "original" },
                Genres = new[] { "original" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/video", createContent);
            var createdVideo = JsonSerializer.Deserialize<VideoResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(createdVideo);

            // Now update it
            var updateDto = new CreateVideoDto
            {
                Title = "Updated Title",
                Platform = "Vimeo",
                VideoType = VideoType.Episode,
                Status = Status.ActivelyExploring,
                Description = "Updated description",
                Topics = new[] { "updated", "modified" },
                Genres = new[] { "updated", "modified" }
            };

            var updateContent = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/video/{createdVideo.Id}", updateContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedVideo = JsonSerializer.Deserialize<VideoResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(updatedVideo);
            Assert.Equal("Updated Title", updatedVideo.Title);
            Assert.Equal("Vimeo", updatedVideo.Platform);
            Assert.Equal(VideoType.Episode, updatedVideo.VideoType);
            Assert.Equal(Status.ActivelyExploring, updatedVideo.Status);
            Assert.Equal("Updated description", updatedVideo.Description);
            Assert.Contains("updated", updatedVideo.Topics);
            Assert.Contains("modified", updatedVideo.Topics);
            Assert.Contains("updated", updatedVideo.Genres);
            Assert.Contains("modified", updatedVideo.Genres);
        }

        [Fact]
        public async Task UpdateVideo_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var updateDto = new CreateVideoDto
            {
                Title = "Updated Title",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/video/{invalidId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeleteVideo_WithValidId_ShouldReturnNoContent()
        {
            // Arrange - First create a video
            var createDto = new CreateVideoDto
            {
                Title = "Video to Delete",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                Topics = new[] { "test" },
                Genres = new[] { "test" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/video", createContent);
            var createdVideo = JsonSerializer.Deserialize<VideoResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );
            Assert.NotNull(createdVideo);

            // Act
            var response = await _client.DeleteAsync($"/api/video/{createdVideo.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the video is actually deleted
            var getResponse = await _client.GetAsync($"/api/video/{createdVideo.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteVideo_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/video/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateVideo_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/video", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateVideo_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var videoId = Guid.NewGuid();
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/video/{videoId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Data Validation Tests

        [Fact]
        public async Task CreateVideo_WithLongTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateVideoDto
            {
                Title = new string('A', 501), // Exceeds 500 character limit
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/video", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateVideo_WithInvalidUrl_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateVideoDto
            {
                Title = "Test Video",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                Link = "not-a-valid-url"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/video", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateVideo_WithNegativeLengthInSeconds_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateVideoDto
            {
                Title = "Test Video",
                Platform = "YouTube",
                VideoType = VideoType.Series,
                Status = Status.Uncharted,
                LengthInSeconds = -100
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/video", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion
    }
}
