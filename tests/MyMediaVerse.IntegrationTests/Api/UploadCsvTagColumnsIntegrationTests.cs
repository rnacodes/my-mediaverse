using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The generic CSV upload reads the optional Topics and Genres columns for every media arm:
    /// names are split on ";" (or "|"), trimmed and lowercased, and a tag shared by several rows in
    /// one file resolves to a single Topic/Genre row. Duplicate rows stay skipped and gain nothing.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class UploadCsvTagColumnsIntegrationTests : IAsyncLifetime
    {
        private const string Endpoint = "/api/upload/csv";
        private const string Header = "MediaType,Title,Author,Url,Link,Topics,Genres";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public UploadCsvTagColumnsIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Theory]
        [InlineData("Book", "Book,Tagged Book,Some Author,,,Tech; AI,Blog|News")]
        [InlineData("Movie", "Movie,Tagged Movie,,,,Tech; AI,Blog|News")]
        [InlineData("TVShow", "TVShow,Tagged Show,,,,Tech; AI,Blog|News")]
        [InlineData("Article", "Article,Tagged Article,Some Author,https://example.com/article,https://example.com/article,Tech; AI,Blog|News")]
        [InlineData("Video", "Video,Tagged Video,,https://example.com/video,https://example.com/video,Tech; AI,Blog|News")]
        [InlineData("Website", "Website,Tagged Site,,https://example.com/site,https://example.com/site,Tech; AI,Blog|News")]
        public async Task UploadCsv_ReadsTopicsAndGenresColumns_ForEveryArm(string mediaType, string row)
        {
            var response = await _client.PostAsync(Endpoint, CsvForm($"{Header}\n{row}\n"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(1, $"{mediaType}: {ErrorsOf(result)}");
            var id = result.GetProperty("importedItems")[0].GetProperty("id").GetGuid();

            var item = await ReadJson(await _client.GetAsync($"/api/media/{id}"));
            Names(item, "topics").Should().BeEquivalentTo(new[] { "tech", "ai" });
            Names(item, "genres").Should().BeEquivalentTo(new[] { "blog", "news" });
        }

        [Fact]
        public async Task UploadCsv_SharesOneTagRow_AcrossRowsInTheSameFile()
        {
            var csv = $"{Header}\n" +
                      "Book,First,Author One,,,shared-topic,shared-genre\n" +
                      "Movie,Second,,,,Shared-Topic ,SHARED-GENRE\n" +
                      "Website,Third,,https://example.com/third,,shared-topic,shared-genre\n";

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(3, ErrorsOf(result));
            var topics = await ReadJson(await _client.GetAsync("/api/topics"));
            topics.EnumerateArray().Count(t => Name(t) == "shared-topic").Should().Be(1);
            var genres = await ReadJson(await _client.GetAsync("/api/genres"));
            genres.EnumerateArray().Count(g => Name(g) == "shared-genre").Should().Be(1);
        }

        [Fact]
        public async Task UploadCsv_WithoutTagColumns_StillImports()
        {
            var response = await _client.PostAsync(Endpoint, CsvForm("MediaType,Title,Url\nWebsite,Plain,https://example.com/plain\n"));

            var result = await ReadJson(response);
            result.GetProperty("successCount").GetInt32().Should().Be(1, ErrorsOf(result));
        }

        #region Helpers

        private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        private static string ErrorsOf(JsonElement result) =>
            result.TryGetProperty("errors", out var errors) ? string.Join(" | ", errors.EnumerateArray().Select(e => e.GetString())) : string.Empty;

        // Tag collections are serialized either as names or as { id, name } objects depending on the endpoint.
        private static List<string?> Names(JsonElement item, string property)
        {
            if (!item.TryGetProperty(property, out var array)) return new List<string?>();
            return array.EnumerateArray().Select(Name).ToList();
        }

        private static string? Name(JsonElement element) =>
            element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetProperty("name").GetString();

        private static MultipartFormDataContent CsvForm(string csv)
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(file, "file", "tagged.csv");
            return form;
        }

        #endregion
    }
}
