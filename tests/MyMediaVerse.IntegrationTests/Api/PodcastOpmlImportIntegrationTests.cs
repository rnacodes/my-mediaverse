using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class PodcastOpmlImportIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public PodcastOpmlImportIntegrationTests(ApiFactory factory)
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
        public async Task ImportOpml_ValidFile_CreatesStubsAndReturnsSummary()
        {
            var opml = Opml(
                Feed("Software Engineering Daily", "https://softwareengineeringdaily.com/feed/podcast/", "1019576853"),
                Feed("Hello Internet", "http://www.hellointernet.fm/podcast?format=rss", "811377230"));

            // Act
            var response = await _client.PostAsync("/api/podcast/import-opml", OpmlForm(opml));

            // Assert - summary
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonSerializer.Deserialize<OpmlImportResultDto>(
                await response.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(2, result.Total);
            Assert.Equal(2, result.Imported);
            Assert.Equal(0, result.Skipped);
            Assert.Equal(0, result.Failed);

            // Assert - the stubs actually landed with the mapped fields
            var seriesResponse = await _client.GetAsync("/api/podcast/series");
            var series = JsonSerializer.Deserialize<List<PodcastSeriesResponseDto>>(
                await seriesResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(series);
            Assert.Equal(2, series.Count);

            var sed = series.Single(s => s.Title == "Software Engineering Daily");
            Assert.Equal("https://softwareengineeringdaily.com/feed/podcast/", sed.RssFeedUrl);
            Assert.Equal("1019576853", sed.ApplePodcastsId);
            Assert.True(sed.IsSubscribed);
            Assert.Equal(Status.Uncharted, sed.Status);
        }

        [Fact]
        public async Task ImportOpml_ReimportSameFile_SkipsDuplicates()
        {
            var opml = Opml(Feed("The Bike Shed", "https://feeds.fireside.fm/bikeshed/rss", "935763119"));

            var first = JsonSerializer.Deserialize<OpmlImportResultDto>(
                await (await _client.PostAsync("/api/podcast/import-opml", OpmlForm(opml))).Content.ReadAsStringAsync(),
                _jsonOptions);
            Assert.NotNull(first);
            Assert.Equal(1, first.Imported);

            // Re-importing the same export must be idempotent: nothing new, the feed is skipped.
            var second = JsonSerializer.Deserialize<OpmlImportResultDto>(
                await (await _client.PostAsync("/api/podcast/import-opml", OpmlForm(opml))).Content.ReadAsStringAsync(),
                _jsonOptions);
            Assert.NotNull(second);
            Assert.Equal(0, second.Imported);
            Assert.Equal(1, second.Skipped);

            // Only one row exists in the DB.
            var seriesResponse = await _client.GetAsync("/api/podcast/series");
            var series = JsonSerializer.Deserialize<List<PodcastSeriesResponseDto>>(
                await seriesResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(series);
            Assert.Single(series);
        }

        [Fact]
        public async Task ImportOpml_NoFile_ReturnsBadRequest()
        {
            using var form = new MultipartFormDataContent();

            var response = await _client.PostAsync("/api/podcast/import-opml", form);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportOpml_WrongExtension_ReturnsBadRequest()
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("hello"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            form.Add(fileContent, "file", "notes.txt");

            var response = await _client.PostAsync("/api/podcast/import-opml", form);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #region OPML builders

        private MultipartFormDataContent OpmlForm(string opml, string fileName = "subscriptions.opml")
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(opml));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            form.Add(fileContent, "file", fileName);
            return form;
        }

        private static string Opml(params string[] feedOutlines) =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<opml version=\"1.0\"><head><title>Test Subscriptions</title></head><body>" +
            "<outline text=\"feeds\">" + string.Concat(feedOutlines) + "</outline>" +
            "</body></opml>";

        private static string Feed(string title, string xmlUrl, string appleId) =>
            $"<outline type=\"rss\" text=\"{title}\" xmlUrl=\"{xmlUrl}\" applePodcastsID=\"{appleId}\"/>";

        #endregion
    }
}
