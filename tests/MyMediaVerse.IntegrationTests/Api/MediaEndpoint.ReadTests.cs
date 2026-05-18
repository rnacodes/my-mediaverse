using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Controllers
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
            var createDto = new CreateMediaItemDto
            {
                Title = "Test Article for Get",
                Description = "A test article description",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Topics = new[] { "test", "article" },
                Genres = new[] { "news", "technology" }
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

            var response = await _client.GetAsync($"/api/media/{createdMedia!.Id}");

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

        [Fact]
        public async Task SearchMedia_WithValidQuery_ShouldReturnOk()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "Searchable Unique Article Title",
                Description = "A searchable article",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/media", createContent);

            var response = await _client.GetAsync("/api/media/search?query=Searchable");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
            Assert.NotNull(searchResults);
        }

        [Fact]
        public async Task SearchMedia_WithEmptyQuery_ShouldReturnBadRequest()
        {
            var response = await _client.GetAsync("/api/media/search?query=");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMediaByType_WithValidType_ShouldReturnOk()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "Test Article for Type Filter",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/media", createContent);

            var response = await _client.GetAsync("/api/media/by-type/Article");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mediaItems = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mediaItems);
            Assert.All(mediaItems!, item => Assert.Equal(MediaType.Article, item.MediaType));
        }

        [Fact]
        public async Task GetMediaByType_WithInvalidType_ShouldReturnBadRequest()
        {
            var response = await _client.GetAsync("/api/media/by-type/InvalidType");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMediaByTopic_WithValidTopicId_ShouldReturnOk()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "Article with Specific Topic",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Topics = new[] { "uniquetopicfortesting" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/media", createContent);

            var topicsResponse = await _client.GetAsync("/api/topics");
            var topicsContent = await topicsResponse.Content.ReadAsStringAsync();
            var topics = JsonSerializer.Deserialize<List<TopicResponseDto>>(topicsContent, _jsonOptions);
            var uniqueTopic = topics!.FirstOrDefault(t => t.Name == "uniquetopicfortesting");

            Assert.NotNull(uniqueTopic);

            var response = await _client.GetAsync($"/api/media/by-topic/{uniqueTopic!.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mediaItems = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mediaItems);
            Assert.All(mediaItems!, item => Assert.Contains("uniquetopicfortesting", item.Topics));
        }

        [Fact]
        public async Task GetMediaByGenre_WithValidGenreId_ShouldReturnOk()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "Article with Specific Genre",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                Genres = new[] { "uniquegenrefortesting" }
            };

            var createContent = new StringContent(
                JsonSerializer.Serialize(createDto, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );

            await _client.PostAsync("/api/media", createContent);

            var genresResponse = await _client.GetAsync("/api/genres");
            var genresContent = await genresResponse.Content.ReadAsStringAsync();
            var genres = JsonSerializer.Deserialize<List<GenreResponseDto>>(genresContent, _jsonOptions);
            var uniqueGenre = genres!.FirstOrDefault(g => g.Name == "uniquegenrefortesting");

            Assert.NotNull(uniqueGenre);

            var response = await _client.GetAsync($"/api/media/by-genre/{uniqueGenre!.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var mediaItems = JsonSerializer.Deserialize<List<MediaItemResponseDto>>(content, _jsonOptions);
            Assert.NotNull(mediaItems);
            Assert.All(mediaItems!, item => Assert.Contains("uniquegenrefortesting", item.Genres));
        }
    }
}
