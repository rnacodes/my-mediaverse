using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.IntegrationTests.Fixtures;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class YouTubeImportValidationIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubeImportValidationIntegrationTests(ApiFactory factory)
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

        private StringContent CreateJsonContent(object data)
        {
            return new StringContent(
                JsonSerializer.Serialize(data, _jsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        #region YouTube Video Import Validation Tests

        [Fact]
        public async Task ImportVideo_WithEmptyVideoId_ShouldReturnBadRequest()
        {
            // Arrange - empty video ID in route
            // Act
            var response = await _client.PostAsync("/api/youtube/import/video/ ", null);

            // Assert - should get 400 or 404 (route won't match empty)
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected 400 or 404 but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task ImportVideo_WithInvalidVideoId_ShouldReturnErrorResponse()
        {
            // Arrange - substitute IYouTubeService so the test never reaches the real YouTube API.
            // The controller catches InvalidOperationException as NotFound, which matches the assertion.
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.ImportVideoAsync(Arg.Any<string>())
                    .Throws(new InvalidOperationException("Video not found")));

            // Act
            var response = await client.PostAsync("/api/youtube/import/video/INVALID_ID_X", null);

            // Assert - should fail (either 400 or 500 depending on YouTube API behavior)
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected error response but got {(int)response.StatusCode}");
        }

        #endregion

        #region YouTube Channel Import Validation Tests

        [Fact]
        public async Task ImportChannel_WithEmptyChannelId_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/youtube/import/channel/ ", null);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected 400 or 404 but got {(int)response.StatusCode}");
        }

        #endregion

        #region YouTube Playlist Import Validation Tests

        [Fact]
        public async Task ImportPlaylist_WithEmptyPlaylistId_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/youtube/import/playlist/ ", null);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected 400 or 404 but got {(int)response.StatusCode}");
        }

        #endregion

        #region YouTube URL Import Validation Tests

        [Fact]
        public async Task ImportFromUrl_WithEmptyUrl_ShouldReturnBadRequest()
        {
            // Arrange
            var data = new { url = "" };

            // Act
            var response = await _client.PostAsync("/api/youtube/import/url", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportFromUrl_WithNullBody_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/youtube/import/url",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportFromUrl_WithNonYouTubeUrl_ShouldReturnBadRequest()
        {
            // Arrange - a valid URL but not YouTube
            var data = new { url = "https://www.google.com" };

            // Act
            var response = await _client.PostAsync("/api/youtube/import/url", CreateJsonContent(data));

            // Assert - should return 400 since we can't extract a YouTube ID
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected 400 or 404 but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task ImportFromUrl_WithPlainText_ShouldReturnBadRequest()
        {
            // Arrange - plain text that's not a URL
            var data = new { url = "not a url at all" };

            // Act
            var response = await _client.PostAsync("/api/youtube/import/url", CreateJsonContent(data));

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected error response but got {(int)response.StatusCode}");
        }

        #endregion

        #region YouTube Channel Controller Validation Tests

        [Fact]
        public async Task CreateYouTubeChannel_WithMissingTitle_ShouldReturnBadRequest()
        {
            // Arrange - title is required
            var data = new
            {
                channelExternalId = "UCtest123"
            };

            // Act
            var response = await _client.PostAsync("/api/youtubechannel", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateYouTubeChannel_WithMissingExternalId_ShouldReturnBadRequest()
        {
            // Arrange - channelExternalId is required
            var data = new
            {
                title = "Test Channel"
            };

            // Act
            var response = await _client.PostAsync("/api/youtubechannel", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportYouTubeChannel_WithEmptyExternalId_ShouldReturnError()
        {
            // Act
            var response = await _client.PostAsync("/api/youtubechannel/import/ ", null);

            // Assert - 405 when route param is whitespace (doesn't match {channelId} template)
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.MethodNotAllowed,
                $"Expected 400, 404, or 405 but got {(int)response.StatusCode}");
        }

        #endregion

        #region YouTube Playlist Controller Validation Tests

        [Fact]
        public async Task ImportYouTubePlaylist_WithEmptyExternalId_ShouldReturnError()
        {
            // Act
            var response = await _client.PostAsync("/api/youtubeplaylist/import/ ", null);

            // Assert - 405 when route param is whitespace (doesn't match {externalId} template)
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.MethodNotAllowed,
                $"Expected 400, 404, or 405 but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task ImportYouTubePlaylist_WithInvalidExternalId_ShouldReturnErrorResponse()
        {
            // Arrange - substitute IYouTubePlaylistService so the test never reaches the real YouTube API.
            // The controller catches InvalidOperationException as NotFound, which matches the assertion.
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubePlaylistService>(mock =>
                mock.ImportPlaylistFromYouTubeAsync(Arg.Any<string>())
                    .Throws(new InvalidOperationException("Playlist not found")));

            // Act - use a clearly invalid playlist ID
            var response = await client.PostAsync("/api/youtubeplaylist/import/INVALID_PL_ID", null);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected error response but got {(int)response.StatusCode}");
        }

        #endregion
    }
}
