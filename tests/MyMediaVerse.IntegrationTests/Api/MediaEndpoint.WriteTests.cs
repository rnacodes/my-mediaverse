using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;
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
        public async Task CreateMediaItem_ThroughTheGenericRoute_ShouldReturnMethodNotAllowed()
        {
            // Every media type is created through its own endpoint; /api/media only reads,
            // updates, and deletes.
            var createDto = new CreateMediaItemDto
            {
                Title = "Through The Wrong Door",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var content = new StringContent(JsonSerializer.Serialize(createDto, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/media", content);

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [Fact]
        public async Task UpdateMediaItem_WithValidData_ShouldReturnOk()
        {
            var createdMedia = await _client.CreateArticleAsync(
                "Original Article Title",
                topics: new[] { "original" },
                genres: new[] { "tech" },
                description: "Original description");

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

            var response = await _client.PutAsync($"/api/media/{createdMedia.Id}", updateContent);

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
            var createdMedia = await _client.CreateArticleAsync(
                "Article to Delete",
                topics: new[] { "test" },
                genres: new[] { "test" },
                description: "This article will be deleted");

            var response = await _client.DeleteAsync($"/api/media/{createdMedia.Id}");

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
    }
}
