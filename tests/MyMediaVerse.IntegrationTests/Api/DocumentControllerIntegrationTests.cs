using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class DocumentControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public DocumentControllerIntegrationTests(ApiFactory factory)
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

        private CreateDocumentDto CreateValidDocumentDto(string? suffix = null)
        {
            suffix ??= Guid.NewGuid().ToString()[..8];
            return new CreateDocumentDto
            {
                Title = $"Test Document {suffix}",
                DocumentType = "Invoice",
                Correspondent = $"Test Corp {suffix}",
                Description = "A test document",
                Status = Status.Uncharted
            };
        }

        #region Auth Tests

        [Fact]
        public async Task GetAll_ShouldReturnUnauthorized_WithoutToken()
        {
            var response = await _client.GetAsync("/api/document");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_ShouldReturnUnauthorized_WithoutToken()
        {
            var dto = CreateValidDocumentDto();

            var response = await _client.PostAsJsonAsync("/api/document", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GetAllDocuments

        [Fact]
        public async Task GetAllDocuments_ShouldReturnOk_WithToken()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/document");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var documents = await response.Content.ReadFromJsonAsync<IEnumerable<DocumentResponseDto>>(_jsonOptions);
            documents.Should().NotBeNull();
        }

        #endregion

        #region CreateDocument

        [Fact]
        public async Task CreateDocument_ShouldReturnCreated_WhenValidDataProvided()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidDocumentDto();

            var response = await _client.PostAsJsonAsync("/api/document", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<DocumentResponseDto>(_jsonOptions);
            created.Should().NotBeNull();
            created!.Title.Should().Be(dto.Title);
            created.DocumentType.Should().Be(dto.DocumentType);
            created.Correspondent.Should().Be(dto.Correspondent);
        }

        #endregion

        #region GetDocument

        [Fact]
        public async Task GetDocument_ShouldReturnOk_WhenDocumentExists()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidDocumentDto();
            var createResponse = await _client.PostAsJsonAsync("/api/document", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<DocumentResponseDto>(_jsonOptions);

            var response = await _client.GetAsync($"/api/document/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var document = await response.Content.ReadFromJsonAsync<DocumentResponseDto>(_jsonOptions);
            document.Should().NotBeNull();
            document!.Id.Should().Be(created.Id);
        }

        [Fact]
        public async Task GetDocument_ShouldReturnNotFound_WhenDocumentDoesNotExist()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync($"/api/document/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region UpdateDocument

        [Fact]
        public async Task UpdateDocument_ShouldReturnOk_WhenDocumentExists()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidDocumentDto();
            var createResponse = await _client.PostAsJsonAsync("/api/document", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<DocumentResponseDto>(_jsonOptions);

            var updateDto = CreateValidDocumentDto("updated");
            updateDto.Description = "Updated description";

            var response = await _client.PutAsJsonAsync($"/api/document/{created!.Id}", updateDto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await response.Content.ReadFromJsonAsync<DocumentResponseDto>(_jsonOptions);
            updated.Should().NotBeNull();
            updated!.Title.Should().Be(updateDto.Title);
        }

        #endregion

        #region DeleteDocument

        [Fact]
        public async Task DeleteDocument_ShouldReturnNoContent_WhenDocumentExists()
        {
            await _client.AuthenticateAsync();

            var dto = CreateValidDocumentDto();
            var createResponse = await _client.PostAsJsonAsync("/api/document", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<DocumentResponseDto>(_jsonOptions);

            var response = await _client.DeleteAsync($"/api/document/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var getResponse = await _client.GetAsync($"/api/document/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Filtering Endpoints

        [Fact]
        public async Task GetDocumentsByType_ShouldReturnSuccessfully()
        {
            await _client.AuthenticateAsync();

            // Create a document first so the query has data
            var dto = CreateValidDocumentDto();
            await _client.PostAsJsonAsync("/api/document", dto);

            var response = await _client.GetAsync("/api/document/by-type/Invoice");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task GetDocumentsByCorrespondent_ShouldReturnSuccessfully()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/document/by-correspondent/TestCorp");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task GetArchivedDocuments_ShouldReturnOk()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/document/archived");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var documents = await response.Content.ReadFromJsonAsync<IEnumerable<DocumentResponseDto>>(_jsonOptions);
            documents.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchDocuments_ShouldReturnSuccessfully_WhenQueryProvided()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/document/search?query=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchDocuments_ShouldReturnBadRequest_WhenQueryEmpty()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/document/search?query=");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetDocumentsByDateRange_ShouldReturnOk()
        {
            await _client.AuthenticateAsync();

            var startDate = DateTime.UtcNow.AddDays(-30).ToString("o");
            var endDate = DateTime.UtcNow.ToString("o");

            var response = await _client.GetAsync($"/api/document/by-date-range?startDate={startDate}&endDate={endDate}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion
    }
}
