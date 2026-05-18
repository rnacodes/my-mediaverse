using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.IntegrationTests.Fixtures;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class InputValidationIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public InputValidationIntegrationTests(ApiFactory factory)
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

        #region Media Item Validation Tests

        [Fact]
        public async Task CreateMedia_WithMissingTitle_ShouldReturnBadRequest()
        {
            // Arrange - title is required
            var data = new
            {
                mediaType = "Book",
                status = "Uncharted",
                author = "Test Author"
            };

            // Act
            var response = await _client.PostAsync("/api/media", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateBook_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var data = new
            {
                title = "Valid Book Title",
                author = "Valid Author",
                mediaType = "Book",
                status = "Uncharted",
                format = "Digital",
                partOfSeries = false,
                topics = Array.Empty<string>(),
                genres = Array.Empty<string>()
            };

            // Act
            var response = await _client.PostAsync("/api/book", CreateJsonContent(data));

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 201 or 200 but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task CreateBook_WithMissingAuthor_ShouldReturnBadRequest()
        {
            // Arrange - author is required for books
            var data = new
            {
                title = "Book Without Author",
                mediaType = "Book",
                status = "Uncharted",
                topics = Array.Empty<string>(),
                genres = Array.Empty<string>()
            };

            // Act
            var response = await _client.PostAsync("/api/book", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Website URL Validation Tests

        [Fact]
        public async Task CreateWebsite_WithValidUrl_ShouldSucceed()
        {
            // Arrange
            var data = new
            {
                title = "Valid Website",
                url = "https://example.com",
                topics = Array.Empty<string>(),
                genres = Array.Empty<string>()
            };

            // Act
            var response = await _client.PostAsync("/api/website", CreateJsonContent(data));

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.OK,
                $"Expected success but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task CreateWebsite_WithInvalidUrl_ShouldReturnBadRequest()
        {
            // Arrange - "not-a-url" should fail URL validation
            var data = new
            {
                title = "Invalid URL Website",
                url = "not-a-url",
                topics = Array.Empty<string>(),
                genres = Array.Empty<string>()
            };

            // Act
            var response = await _client.PostAsync("/api/website", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateWebsite_WithMissingUrl_ShouldReturnBadRequest()
        {
            // Arrange - URL is required for websites
            var data = new
            {
                title = "Website Without URL",
                topics = Array.Empty<string>(),
                genres = Array.Empty<string>()
            };

            // Act
            var response = await _client.PostAsync("/api/website", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Video Validation Tests

        [Fact]
        public async Task CreateVideo_WithValidData_ShouldSucceed()
        {
            // Arrange
            var data = new
            {
                title = "Valid Video",
                platform = "YouTube",
                videoType = "Episode",
                mediaType = "Video",
                status = "Uncharted",
                link = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                topics = Array.Empty<string>(),
                genres = Array.Empty<string>()
            };

            // Act
            var response = await _client.PostAsync("/api/video", CreateJsonContent(data));

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.OK,
                $"Expected success but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task CreateVideo_WithMissingPlatform_ShouldReturnBadRequest()
        {
            // Arrange - platform is required for videos
            var data = new
            {
                title = "Video Without Platform",
                videoType = "Episode",
                mediaType = "Video",
                status = "Uncharted",
                topics = Array.Empty<string>(),
                genres = Array.Empty<string>()
            };

            // Act
            var response = await _client.PostAsync("/api/video", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Topic/Genre Validation Tests

        [Fact]
        public async Task CreateTopic_WithValidName_ShouldSucceed()
        {
            // Arrange
            var data = new { name = "test validation topic" };

            // Act
            var response = await _client.PostAsync("/api/topics", CreateJsonContent(data));

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.OK,
                $"Expected success but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task CreateTopic_WithEmptyName_ShouldReturnBadRequest()
        {
            // Arrange
            var data = new { name = "" };

            // Act
            var response = await _client.PostAsync("/api/topics", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateGenre_WithValidName_ShouldSucceed()
        {
            // Arrange
            var data = new { name = "test validation genre" };

            // Act
            var response = await _client.PostAsync("/api/genres", CreateJsonContent(data));

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.OK,
                $"Expected success but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task CreateGenre_WithEmptyName_ShouldReturnBadRequest()
        {
            // Arrange
            var data = new { name = "" };

            // Act
            var response = await _client.PostAsync("/api/genres", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Mixlist Validation Tests

        [Fact]
        public async Task CreateMixlist_WithValidData_ShouldSucceed()
        {
            // Arrange
            var data = new
            {
                name = "Test Validation Mixlist",
                description = "A test mixlist"
            };

            // Act
            var response = await _client.PostAsync("/api/mixlist", CreateJsonContent(data));

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.OK,
                $"Expected success but got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task CreateMixlist_WithMissingName_ShouldReturnBadRequest()
        {
            // Arrange - name is required
            var data = new
            {
                description = "A mixlist without a name"
            };

            // Act
            var response = await _client.PostAsync("/api/mixlist", CreateJsonContent(data));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region Nonexistent Resource Tests

        [Fact]
        public async Task GetMedia_WithNonexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonexistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/media/{nonexistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetMixlist_WithNonexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonexistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/mixlist/{nonexistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Podcast Import Validation Tests

        [Fact]
        public async Task ImportPodcastByName_WithEmptyName_ShouldReturnBadRequest()
        {
            // Arrange
            var data = new { name = "" };

            // Act
            var response = await _client.PostAsync("/api/podcast/series/from-api/by-name", CreateJsonContent(data));

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Expected error response but got {(int)response.StatusCode}");
        }

        #endregion
    }
}
