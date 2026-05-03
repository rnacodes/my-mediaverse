using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.IntegrationTests.Controllers
{
    public class NoteControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _validUsername;
        private readonly string _validPassword;

        public NoteControllerIntegrationTests(WebApplicationFactory factory)
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
            _validUsername = Environment.GetEnvironmentVariable("AUTH_USERNAME") ?? "admin";
            _validPassword = Environment.GetEnvironmentVariable("AUTH_PASSWORD") ?? "password123";
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var loginData = new { username = _validUsername, password = _validPassword };
            var content = new StringContent(JsonSerializer.Serialize(loginData, _jsonOptions), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/auth/login", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            return loginResponse.GetProperty("token").GetString()!;
        }

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
        public async Task GetAll_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/note");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_ShouldReturnUnauthorized_WithoutToken()
        {
            _client.DefaultRequestHeaders.Authorization = null;
            var dto = CreateValidNoteDto();

            var response = await _client.PostAsJsonAsync("/api/note", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GetAll

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithToken()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/note");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region Create

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenValidDataProvided()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = CreateValidNoteDto();

            var response = await _client.PostAsJsonAsync("/api/note", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);
            created.Should().NotBeNull();
            created!.Title.Should().Be(dto.Title);
            created.Slug.Should().Be(dto.Slug);
            created.VaultName.Should().Be(dto.VaultName);

            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenMissingRequiredFields()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new CreateNoteDto
            {
                Slug = "",
                Title = "",
                VaultName = ""
            };

            var response = await _client.PostAsJsonAsync("/api/note", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenNoteExists()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = CreateValidNoteDto();
            var createResponse = await _client.PostAsJsonAsync("/api/note", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);

            var response = await _client.GetAsync($"/api/note/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var note = await response.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);
            note.Should().NotBeNull();
            note!.Id.Should().Be(created.Id);

            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenNoteDoesNotExist()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/note/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region Update

        [Fact]
        public async Task Update_ShouldReturnOk_WhenNoteExists()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region Delete

        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenNoteExists()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = CreateValidNoteDto();
            var createResponse = await _client.PostAsJsonAsync("/api/note", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);

            var response = await _client.DeleteAsync($"/api/note/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/note/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region GetBySlug

        [Fact]
        public async Task GetBySlug_ShouldReturnOk_WhenNoteExists()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = CreateValidNoteDto();
            var createResponse = await _client.PostAsJsonAsync("/api/note", dto);
            createResponse.EnsureSuccessStatusCode();

            var response = await _client.GetAsync($"/api/note/slug/{dto.VaultName}/{dto.Slug}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var note = await response.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);
            note.Should().NotBeNull();
            note!.Slug.Should().Be(dto.Slug);

            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region LinkToMedia

        [Fact]
        public async Task LinkToMedia_ShouldWork_WhenBothExist()
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Create a note
            var noteDto = CreateValidNoteDto();
            var noteResponse = await _client.PostAsJsonAsync("/api/note", noteDto);
            var createdNote = await noteResponse.Content.ReadFromJsonAsync<NoteResponseDto>(_jsonOptions);

            // Create a book as the media item
            _client.DefaultRequestHeaders.Authorization = null;
            var bookDto = TestDataFactory.CreateBookDto($"Link Test Book {Guid.NewGuid().ToString()[..8]}", "Author");
            var bookResponse = await _client.PostAsJsonAsync("/api/book", bookDto);
            var createdBook = await bookResponse.Content.ReadFromJsonAsync<BookResponseDto>(_jsonOptions);

            // Link note to media
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var linkDto = new LinkNoteToMediaDto
            {
                MediaItemId = createdBook!.Id,
                LinkDescription = "Test link"
            };

            var response = await _client.PostAsJsonAsync($"/api/note/{createdNote!.Id}/link", linkDto);

            response.IsSuccessStatusCode.Should().BeTrue();

            _client.DefaultRequestHeaders.Authorization = null;
        }

        #endregion
    }
}
