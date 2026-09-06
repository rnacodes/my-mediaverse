using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The generic CSV upload's Book branch: rows are deduplicated through the shared book finder
    /// (ISBN in either form, ASIN, or title+author) and reported as skipped instead of inserted twice.
    /// The endpoint's response is the historical anonymous object (a recorded cross-type variance), so
    /// these tests read it as JSON rather than through a DTO.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class UploadCsvBookRowIntegrationTests : IAsyncLifetime
    {
        private const string Endpoint = "/api/upload/csv";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public UploadCsvBookRowIntegrationTests(ApiFactory factory)
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
        public async Task UploadCsv_BookRowRepeatedInSameFile_CreatesOneAndSkipsTheOther()
        {
            var csv = Csv(
                "Book,Dune,Frank Herbert,,",
                "Book,Dune,Frank Herbert,,");

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(1);
            result.GetProperty("skippedCount").GetInt32().Should().Be(1);
            result.GetProperty("skipped").GetArrayLength().Should().Be(1);
            (await GetAllBooks()).Should().ContainSingle(b => b.Title == "Dune");
        }

        [Fact]
        public async Task UploadCsv_BookRowMatchingExistingIsbn13ByIsbn10_IsSkipped()
        {
            // Legacy rows may store ISBN-13; an export carrying the ISBN-10 of the same edition must
            // still match (the finder probes both forms) and must not create a second book.
            await _client.PostAsync(Endpoint, CsvForm(Csv("Book,The Great Gatsby,F. Scott Fitzgerald,9780743273565,")));

            var response = await _client.PostAsync(Endpoint, CsvForm(Csv("Book,The Great Gatsby (2004 ed.),Fitzgerald,074327356X,")));

            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(0);
            result.GetProperty("skippedCount").GetInt32().Should().Be(1);
            var books = await GetAllBooks();
            books.Should().ContainSingle();
            books[0].ISBN.Should().Be("9780743273565", "the stored ISBN is the canonical 13-digit form");
        }

        [Fact]
        public async Task UploadCsv_NewBookRow_StoresNormalizedIsbn13()
        {
            await _client.PostAsync(Endpoint, CsvForm(Csv("Book,Neuromancer,William Gibson,0-441-56956-0,")));

            var books = await GetAllBooks();
            books.Should().ContainSingle();
            books[0].ISBN.Should().Be("9780441569564");
        }

        #region Helpers

        private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        private async Task<List<BookResponseDto>> GetAllBooks()
        {
            var response = await _client.GetAsync("/api/book");
            return JsonSerializer.Deserialize<List<BookResponseDto>>(await response.Content.ReadAsStringAsync(), _jsonOptions)
                   ?? new List<BookResponseDto>();
        }

        private static string Csv(params string[] rows) =>
            "MediaType,Title,Author,ISBN,ASIN\n" + string.Join("\n", rows) + "\n";

        private static MultipartFormDataContent CsvForm(string csv)
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(file, "file", "library.csv");
            return form;
        }

        #endregion
    }
}
