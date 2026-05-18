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
    public class MediaEndpointExportTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public MediaEndpointExportTests(ApiFactory factory)
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
        public async Task ExportMediaItem_WithValidId_ShouldReturnCsvFile()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "Article to Export",
                Description = "Export test",
                MediaType = MediaType.Article,
                Status = Status.Uncharted
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

            var response = await _client.GetAsync($"/api/media/{createdMedia!.Id}/export");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("attachment", response.Content.Headers.ContentDisposition?.ToString());
        }

        [Fact]
        public async Task ExportMediaItem_WithInvalidId_ShouldReturnNotFound()
        {
            var invalidId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/media/{invalidId}/export");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
