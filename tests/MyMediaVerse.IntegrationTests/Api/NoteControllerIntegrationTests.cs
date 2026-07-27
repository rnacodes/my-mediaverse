using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class NoteControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public NoteControllerIntegrationTests(ApiFactory factory)
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

        private CreateNoteDto CreateValidNoteDto(string? suffix = null)
        {
            suffix ??= Guid.NewGuid().ToString()[..8];
            return new CreateNoteDto
            {
                Slug = $"test-note-{suffix}",
                Title = $"Test Note {suffix}",
                Content = "This is test note content.",
                Description = "A test note description",
                VaultName = "test-vault",
                Tags = new List<string> { "test", "integration" }
            };
        }

        #region Auth Tests

        [Fact]
        public async Task Create_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();
            var dto = CreateValidNoteDto();

            var response = await client.PostAsJsonAsync("/api/note", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GetAll

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithToken()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/note");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Create

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenValidDataProvided()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidNoteDto();

            var response = await _client.PostAsJsonAsync("/api/note", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);
            created.Should().NotBeNull();
            created!.Title.Should().Be(dto.Title);
            created.Slug.Should().Be(dto.Slug);
            created.VaultName.Should().Be(dto.VaultName);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenMissingRequiredFields()
        {
            await _client.AuthenticateAsync();

            var dto = new CreateNoteDto
            {
                Slug = "",
                Title = "",
                VaultName = ""
            };

            var response = await _client.PostAsJsonAsync("/api/note", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenNoteExists()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidNoteDto();
            var createResponse = await _client.PostAsJsonAsync("/api/note", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);

            var response = await _client.GetAsync($"/api/note/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var note = await response.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);
            note.Should().NotBeNull();
            note!.Id.Should().Be(created.Id);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenNoteDoesNotExist()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync($"/api/note/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_ShouldReturnOk_WhenNoteExists()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidNoteDto();
            var createResponse = await _client.PostAsJsonAsync("/api/note", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);

            var updateDto = new UpdateNoteDto
            {
                Title = "Updated Note Title",
                Content = "Updated content"
            };

            var response = await _client.PutAsJsonAsync($"/api/note/{created!.Id}", updateDto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);
            updated.Should().NotBeNull();
            updated!.Title.Should().Be("Updated Note Title");
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenNoteExists()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidNoteDto();
            var createResponse = await _client.PostAsJsonAsync("/api/note", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);

            var response = await _client.DeleteAsync($"/api/note/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/note/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region GetBySlug

        [Fact]
        public async Task GetBySlug_ShouldReturnOk_WhenNoteExists()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidNoteDto();
            var createResponse = await _client.PostAsJsonAsync("/api/note", dto);
            createResponse.EnsureSuccessStatusCode();

            var response = await _client.GetAsync($"/api/note/slug/{dto.VaultName}/{dto.Slug}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var note = await response.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);
            note.Should().NotBeNull();
            note!.Slug.Should().Be(dto.Slug);
        }

        #endregion

        #region LinkToMedia

        [Fact]
        public async Task LinkToMedia_ShouldWork_WhenBothExist()
        {
            await _client.AuthenticateAsync();

            // Create a note (auth-required)
            var noteDto = CreateValidNoteDto();
            var noteResponse = await _client.PostAsJsonAsync("/api/note", noteDto);
            var createdNote = await noteResponse.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);

            // BookController doesn't require auth, but the bearer token is fine to keep on the client.
            var bookDto = TestDataFactory.CreateBookDto("Link Test Book", "Author");
            var bookResponse = await _client.PostAsJsonAsync("/api/book", bookDto);
            var createdBook = await bookResponse.Content.ReadFromJsonAsync<BookResponseDto>(_jsonOptions);

            // Link note to media
            var linkDto = new LinkNoteToMediaDto
            {
                MediaItemId = createdBook!.Id,
                LinkDescription = "Test link"
            };

            var response = await _client.PostAsJsonAsync($"/api/note/{createdNote!.Id}/link", linkDto);

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        #endregion
    }
}
