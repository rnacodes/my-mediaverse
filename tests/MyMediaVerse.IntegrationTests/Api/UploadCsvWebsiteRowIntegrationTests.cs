using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The generic CSV upload's Website branch: rows are deduplicated through the shared website
    /// finder (normalized URL, scheme-insensitive) and reported as skipped instead of inserted twice.
    /// The endpoint's response is the historical anonymous object (a recorded cross-type variance), so
    /// these tests read it as JSON rather than through a DTO.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class UploadCsvWebsiteRowIntegrationTests : IAsyncLifetime
    {
        private const string Endpoint = "/api/upload/csv";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public UploadCsvWebsiteRowIntegrationTests(ApiFactory factory)
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
        public async Task UploadCsv_NewWebsiteRow_StoresNormalizedLinkKeyAndDomain()
        {
            var response = await _client.PostAsync(Endpoint, CsvForm(Csv(
                "Website,The Verge,https://WWW.TheVerge.com/tech/?utm_source=rss#top")));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(1);

            var websites = await GetAllWebsites();
            websites.Should().ContainSingle();
            websites[0].Link.Should().Be("https://theverge.com/tech");
            websites[0].Domain.Should().Be("theverge.com");
        }

        [Fact]
        public async Task UploadCsv_WebsiteRowRepeatedInSameFile_CreatesOneAndSkipsTheOther()
        {
            var csv = Csv(
                "Website,Example,https://example.com/page",
                "Website,Example again,http://www.example.com/page/");

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(1);
            result.GetProperty("skippedCount").GetInt32().Should().Be(1);
            result.GetProperty("skipped").GetArrayLength().Should().Be(1);
            (await GetAllWebsites()).Should().ContainSingle(w => w.Title == "Example");
        }

        [Fact]
        public async Task UploadCsv_WebsiteRowMatchingExistingWebsite_IsSkipped()
        {
            var existing = await _client.PostAsJsonAsync("/api/website", new CreateWebsiteDto
            {
                Title = "Already here",
                Url = "https://example.com/article"
            });
            existing.StatusCode.Should().Be(HttpStatusCode.Created);

            var response = await _client.PostAsync(Endpoint, CsvForm(Csv(
                "Website,Duplicate,https://example.com/article?utm_campaign=x")));

            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(0);
            result.GetProperty("skippedCount").GetInt32().Should().Be(1);
            (await GetAllWebsites()).Should().ContainSingle(w => w.Title == "Already here");
        }

        [Fact]
        public async Task UploadCsv_WebsiteRow_AcceptsLinkColumnName()
        {
            var csv = "MediaType,Title,Link\nWebsite,Linked,https://example.com/linked\n";

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(1);
            (await GetAllWebsites()).Should().ContainSingle(w => w.Link == "https://example.com/linked");
        }

        #region Helpers

        private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        private async Task<List<WebsiteResponseDto>> GetAllWebsites()
        {
            var response = await _client.GetAsync("/api/website");
            return JsonSerializer.Deserialize<List<WebsiteResponseDto>>(await response.Content.ReadAsStringAsync(), _jsonOptions)
                   ?? new List<WebsiteResponseDto>();
        }

        private static string Csv(params string[] rows) =>
            "MediaType,Title,Url\n" + string.Join("\n", rows) + "\n";

        private static MultipartFormDataContent CsvForm(string csv)
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(file, "file", "bookmarks.csv");
            return form;
        }

        #endregion
    }
}
