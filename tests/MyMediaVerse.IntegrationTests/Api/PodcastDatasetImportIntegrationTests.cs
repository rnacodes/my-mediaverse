using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class PodcastDatasetImportIntegrationTests : IAsyncLifetime
    {
        private const string Endpoint = "/api/podcast/series/from-dataset";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public PodcastDatasetImportIntegrationTests(ApiFactory factory)
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
        public async Task ImportDataset_PodcastsFile_CreatesStubs_AndReindexesOnlyThem()
        {
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();
            var csv = "title,publisher,description,website,artwork_thumbnail,explicit\n" +
                      "Show A,Publisher A,About A,https://a.example.com,https://cdn.example.com/a.jpg,no\n" +
                      "Show B,Publisher B,About B,https://b.example.com,https://cdn.example.com/b.jpg,yes\n";

            var response = await client.PostAsync(Endpoint, CsvForm(csv));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<PodcastDatasetImportResultDto>(
                await response.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(result);
            Assert.Equal("podcasts", result.FileKind);
            Assert.Equal(2, result.CreatedCount);
            Assert.True(result.ReindexTriggered);
            await typesense.Received(1).ReindexMediaItemsByIdsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2));
            await typesense.DidNotReceive().BulkReindexAllMediaItemsAsync();

            var series = JsonSerializer.Deserialize<List<PodcastSeriesResponseDto>>(
                await (await client.GetAsync("/api/podcast/series")).Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(series);
            Assert.Equal(2, series.Count);
            Assert.All(series, s =>
            {
                Assert.Equal(PodcastMetadataSources.ListenNotesDataset, s.MetadataSource);
                Assert.False(s.IsSubscribed);
                Assert.Null(s.RssFeedUrl);
            });

            // A second upload adds nothing, so nothing is reindexed (still one call in total).
            var second = await client.PostAsync(Endpoint, CsvForm(csv));
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            await typesense.Received(1).ReindexMediaItemsByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>());
        }

        [Fact]
        public async Task ImportDataset_NoFile_ReturnsBadRequest()
        {
            using var form = new MultipartFormDataContent();

            var response = await _client.PostAsync(Endpoint, form);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportDataset_WrongExtension_ReturnsBadRequest()
        {
            var response = await _client.PostAsync(Endpoint, CsvForm("title\nShow A\n", "shows.txt"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportDataset_NotAListenNotesExport_ReturnsBadRequestWithTheReason()
        {
            var response = await _client.PostAsync(Endpoint, CsvForm("name,url\nSomething,https://example.com\n"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = JsonSerializer.Deserialize<PodcastDatasetImportResultDto>(
                await response.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("title", result.ErrorMessage);
        }

        private static MultipartFormDataContent CsvForm(string csv, string fileName = "listen-notes-podcasts.csv")
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(fileContent, "file", fileName);
            return form;
        }
    }
}
