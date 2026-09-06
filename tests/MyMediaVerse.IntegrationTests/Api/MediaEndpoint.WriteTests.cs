using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class MediaEndpointWriteTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public MediaEndpointWriteTests(ApiFactory factory)
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

        [Fact]
        public async Task CreateMediaItem_WithBookType_ShouldReturnBadRequestJson()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "A Book Through The Wrong Door",
                MediaType = MediaType.Book,
                Status = Status.Uncharted
            };

            var content = new StringContent(JsonSerializer.Serialize(createDto, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.True(body.TryGetProperty("error", out var error));
            Assert.Contains("/api/book", error.GetString());
        }

        [Fact]
        public async Task CreateMediaItem_WithArticleType_ShouldReturnCreated()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "New Test Article",
                Description = "A comprehensive test article",
                MediaType = MediaType.Article,
                Link = "https://example.com/article",
                Status = Status.Uncharted,
                Rating = Rating.Like,
                Topics = new[] { "technology", "science" },
                Genres = new[] { "news", "research" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdMedia);
            Assert.Equal("New Test Article", createdMedia!.Title);
            Assert.Equal(MediaType.Article, createdMedia.MediaType);
            Assert.Contains("technology", createdMedia.Topics);
            Assert.Contains("news", createdMedia.Genres);
        }

        [Fact]
        public async Task CreateMediaItem_WithVideoType_ShouldReturnCreated()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "New Test Video",
                Description = "A test video description",
                MediaType = MediaType.Video,
                Link = "https://youtube.com/watch?v=test",
                Status = Status.Uncharted,
                Topics = new[] { "tutorial", "programming" },
                Genres = new[] { "educational" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdMedia);
            Assert.Equal("New Test Video", createdMedia!.Title);
            Assert.Equal(MediaType.Video, createdMedia.MediaType);
        }

        [Fact]
        public async Task CreateMediaItem_WithPodcastType_ShouldReturnCreated()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "New Test Podcast",
                Description = "A test podcast description",
                MediaType = MediaType.Podcast,
                Status = Status.Uncharted,
                Topics = new[] { "interview", "business" },
                Genres = new[] { "entrepreneurship" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(createdMedia);
            Assert.Equal("New Test Podcast", createdMedia!.Title);
            Assert.Equal(MediaType.Podcast, createdMedia.MediaType);
        }

        [Fact]
        public async Task CreateMediaItem_WithMovieType_ShouldReturnCreated()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "Test Movie via Media Controller",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(createdMedia);
            Assert.Equal(MediaType.Movie, createdMedia!.MediaType);
        }

        [Fact]
        public async Task UpdateMediaItem_WithValidData_ShouldReturnOk()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "Original Article Title",
                Description = "Original description",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Topics = new[] { "original" },
                Genres = new[] { "tech" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/media", createContent);
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );

            var updateDto = new CreateMediaItemDto
            {
                Title = "Updated Article Title",
                Description = "Updated description",
                MediaType = MediaType.Article,
                Link = "https://example.com/updated",
                Status = Status.ActivelyExploring,
                Rating = Rating.SuperLike,
                Topics = new[] { "updated", "modified" },
                Genres = new[] { "news", "science" }
            };

            var updateContent = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PutAsync($"/api/media/{createdMedia!.Id}", updateContent);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(responseContent, _jsonOptions);

            Assert.NotNull(updatedMedia);
            Assert.Equal("Updated Article Title", updatedMedia!.Title);
            Assert.Equal("Updated description", updatedMedia.Description);
            Assert.Equal(Status.ActivelyExploring, updatedMedia.Status);
        }

        [Fact]
        public async Task UpdateMediaItem_WithInvalidId_ShouldReturnNotFound()
        {
            var invalidId = Guid.NewGuid();
            var updateDto = new CreateMediaItemDto
            {
                Title = "Updated Article",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PutAsync($"/api/media/{invalidId}", content);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMediaItem_WithValidId_ShouldReturnNoContent()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "Article to Delete",
                Description = "This article will be deleted",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Topics = new[] { "test" },
                Genres = new[] { "test" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var createResponse = await _client.PostAsync("/api/media", createContent);
            var createdMedia = JsonSerializer.Deserialize<MediaItemResponseDto>(
                await createResponse.Content.ReadAsStringAsync(),
                _jsonOptions
            );

            var response = await _client.DeleteAsync($"/api/media/{createdMedia!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getResponse = await _client.GetAsync($"/api/media/{createdMedia.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteMediaItem_WithInvalidId_ShouldReturnNotFound()
        {
            var invalidId = Guid.NewGuid();

            var response = await _client.DeleteAsync($"/api/media/{invalidId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateMediaItem_WithMalformedJson_ShouldReturnBadRequest()
        {
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMediaItem_WithNullData_ShouldReturnBadRequest()
        {
            var content = new StringContent("null", Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMediaItem_WithEmptyTitle_ShouldReturnBadRequest()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMediaItem_WithLongTitle_ShouldReturnBadRequest()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = new string('A', 501),
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
