using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class MediaEndpointReadTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public MediaEndpointReadTests(ApiFactory factory)
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
        public async Task GetAllMedia_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/media");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mediaItems = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mediaItems);
        }

        [Fact]
        public async Task GetMediaItem_WithValidId_ShouldReturnOk()
        {
            var createdMedia = await _client.CreateArticleAsync(
                "Test Article for Get",
                topics: new[] { "test", "article" },
                genres: new[] { "news", "technology" },
                description: "A test article description");

            var response = await _client.GetAsync($"/api/media/{createdMedia.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mediaItem = JsonSerializer.Deserialize<MediaItemResponseDto>(content, _jsonOptions);
            Assert.NotNull(mediaItem);
            Assert.Equal(createdMedia.Id, mediaItem!.Id);
            Assert.Equal("Test Article for Get", mediaItem.Title);
        }

        [Fact]
        public async Task GetMediaItem_WithInvalidId_ShouldReturnNotFound()
        {
            var invalidId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/media/{invalidId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
