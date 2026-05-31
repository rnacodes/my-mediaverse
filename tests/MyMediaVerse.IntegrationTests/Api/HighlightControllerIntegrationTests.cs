using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class HighlightControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public HighlightControllerIntegrationTests(ApiFactory factory)
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

        private CreateHighlightDto CreateValidHighlightDto(string? suffix = null)
        {
            suffix ??= Guid.NewGuid().ToString()[..8];
            return new CreateHighlightDto
            {
                Text = $"This is a highlight text {suffix}",
                Title = $"Source Title {suffix}",
                Author = $"Source Author {suffix}",
                Note = "A note about this highlight",
                Category = "books"
            };
        }

        #region GetAllHighlights

        [Fact]
        public async Task GetAllHighlights_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/highlight");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var highlights = await response.Content.ReadFromJsonAsync<IEnumerable<HighlightResponseDto>>(_jsonOptions);
            highlights.Should().NotBeNull();
        }

        #endregion

        #region CreateHighlight

        [Fact]
        public async Task CreateHighlight_ShouldReturnCreated_WhenValidDataProvided()
        {
            var dto = CreateValidHighlightDto();

            var response = await _client.PostAsJsonAsync("/api/highlight", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<HighlightResponseDto>(_jsonOptions);
            created.Should().NotBeNull();
            created!.text.Should().Be(dto.Text);
            created.title.Should().Be(dto.Title);
            created.author.Should().Be(dto.Author);
        }

        #endregion

        #region GetHighlight

        [Fact]
        public async Task GetHighlight_ShouldReturnOk_WhenHighlightExists()
        {
            var dto = CreateValidHighlightDto();
            var createResponse = await _client.PostAsJsonAsync("/api/highlight", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<HighlightResponseDto>(_jsonOptions);

            var response = await _client.GetAsync($"/api/highlight/{created!.id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var highlight = await response.Content.ReadFromJsonAsync<HighlightResponseDto>(_jsonOptions);
            highlight.Should().NotBeNull();
            highlight!.id.Should().Be(created.id);
        }

        [Fact]
        public async Task GetHighlight_ShouldReturnNotFound_WhenHighlightDoesNotExist()
        {
            var response = await _client.GetAsync($"/api/highlight/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region UpdateHighlight

        [Fact]
        public async Task UpdateHighlight_ShouldReturnOk_WhenHighlightExists()
        {
            var dto = CreateValidHighlightDto();
            var createResponse = await _client.PostAsJsonAsync("/api/highlight", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<HighlightResponseDto>(_jsonOptions);

            var updateDto = CreateValidHighlightDto("updated");
            updateDto.Note = "Updated note";

            var response = await _client.PutAsJsonAsync($"/api/highlight/{created!.id}", updateDto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content.ReadFromJsonAsync<HighlightResponseDto>(_jsonOptions);
            updated.Should().NotBeNull();
            updated!.text.Should().Be(updateDto.Text);
        }

        #endregion

        #region DeleteHighlight

        [Fact]
        public async Task DeleteHighlight_ShouldDeleteHighlight_WhenHighlightExists()
        {
            var dto = CreateValidHighlightDto();
            var createResponse = await _client.PostAsJsonAsync("/api/highlight", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<HighlightResponseDto>(_jsonOptions);

            var response = await _client.DeleteAsync($"/api/highlight/{created!.id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/highlight/{created.id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Filtering Endpoints

        [Fact]
        public async Task GetUnlinkedHighlights_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/highlight/unlinked");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var highlights = await response.Content.ReadFromJsonAsync<IEnumerable<HighlightResponseDto>>(_jsonOptions);
            highlights.Should().NotBeNull();
        }

        [Fact]
        public async Task GetHighlightsByTag_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/highlight/tag/test-tag");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var highlights = await response.Content.ReadFromJsonAsync<IEnumerable<HighlightResponseDto>>(_jsonOptions);
            highlights.Should().NotBeNull();
        }

        [Fact]
        public async Task GetHighlightsByArticle_ShouldReturnOk()
        {
            var response = await _client.GetAsync($"/api/highlight/article/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var highlights = await response.Content.ReadFromJsonAsync<IEnumerable<HighlightResponseDto>>(_jsonOptions);
            highlights.Should().NotBeNull();
        }

        [Fact]
        public async Task GetHighlightsByBook_ShouldReturnOk()
        {
            var response = await _client.GetAsync($"/api/highlight/book/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var highlights = await response.Content.ReadFromJsonAsync<IEnumerable<HighlightResponseDto>>(_jsonOptions);
            highlights.Should().NotBeNull();
        }

        #endregion
    }
}
