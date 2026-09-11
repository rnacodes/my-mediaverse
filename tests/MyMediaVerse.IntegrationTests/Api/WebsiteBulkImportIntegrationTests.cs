using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Pins the HTTP contract of the bulk website intake: a browser bookmark export or a pasted
    /// URL list becomes website stubs (folders as topics), duplicates inside the file and against
    /// the library are skipped, the result body follows the reporting contract, the reindex hook
    /// fires with the changed count, the export round-trips back in as all-skipped, bad uploads
    /// get a 400 <c>{ error }</c>, and every action requires a token.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class WebsiteBulkImportIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public WebsiteBulkImportIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        private const string BookmarksHtml = """
            <!DOCTYPE NETSCAPE-Bookmark-file-1>
            <TITLE>Bookmarks</TITLE>
            <H1>Bookmarks</H1>
            <DL><p>
                <DT><H3 PERSONAL_TOOLBAR_FOLDER="true">Bookmarks bar</H3>
                <DL><p>
                    <DT><H3>Dev</H3>
                    <DL><p>
                        <DT><A HREF="https://example.com/dev" ADD_DATE="1700000000">Dev &amp; Tools</A>
                        <DT><A HREF="https://www.example.com/dev/?utm_source=x" ADD_DATE="1700000000">Dev again</A>
                    </DL><p>
                    <DT><A HREF="javascript:void(0)">Bookmarklet</A>
                    <DT><A HREF="https://example.com/toolbar" TAGS="reading">Toolbar</A>
                </DL><p>
                <DT><H3>Other bookmarks</H3>
                <DL><p>
                    <DT><A HREF="https://example.org/other">Other</A>
                </DL><p>
            </DL><p>
            """;

        private static MultipartFormDataContent BookmarkForm(string content, string fileName = "bookmarks.html", string mediaType = "text/html")
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            file.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            form.Add(file, "file", fileName);
            return form;
        }

        private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        private async Task<List<WebsiteResponseDto>> GetAllWebsites(HttpClient? client = null) =>
            await (client ?? _client).GetFromJsonAsync<List<WebsiteResponseDto>>("/api/website", _jsonOptions) ?? new();

        #region Bookmark file

        [Fact]
        public async Task ImportBookmarkFile_CreatesStubsWithFolderTopics_SkipsDuplicates_AndReindexes()
        {
            var (client, reindex) = _factory.CreateClientWithSubstitute<IImportReindexService>();

            var response = await client.PostAsync("/api/website/from-bookmark-file", BookmarkForm(BookmarksHtml));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<WebsiteBulkImportResultDto>(_jsonOptions);
            result!.Success.Should().BeTrue();
            result.Operation.Should().Be("bookmark-import");
            result.TotalProcessed.Should().Be(4);
            result.CreatedCount.Should().Be(3);
            result.SkippedCount.Should().Be(1, "the second /dev entry is the same page");
            result.NonWebLinkCount.Should().Be(1);
            result.FoldersFound.Should().Be(1);
            result.TopicsCreatedCount.Should().Be(2, "dev (folder) and reading (tag)");
            result.PendingEnrichmentCount.Should().Be(3);
            result.ReindexTriggered.Should().BeTrue();
            result.CompletedAt.Should().NotBeNull();
            await reindex.Received(1).ReindexAfterImportAsync(3, "bookmark import");

            var websites = await GetAllWebsites(client);
            websites.Should().HaveCount(3);
            var dev = websites.Single(w => w.Link == "https://example.com/dev");
            dev.Title.Should().Be("Dev & Tools");
            dev.Domain.Should().Be("example.com");
            dev.EnrichedAt.Should().BeNull();
            dev.Topics.Should().Contain("dev");
            websites.Single(w => w.Link == "https://example.com/toolbar").Topics.Should().Contain("reading");
        }

        [Fact]
        public async Task ImportBookmarkFile_Twice_SkipsEverythingTheSecondTime_WithoutReindexing()
        {
            var (client, reindex) = _factory.CreateClientWithSubstitute<IImportReindexService>();
            await client.PostAsync("/api/website/from-bookmark-file", BookmarkForm(BookmarksHtml));

            var second = await client.PostAsync("/api/website/from-bookmark-file", BookmarkForm(BookmarksHtml));

            var result = await second.Content.ReadFromJsonAsync<WebsiteBulkImportResultDto>(_jsonOptions);
            result!.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(0);
            result.SkippedCount.Should().Be(4);
            result.ReindexTriggered.Should().BeFalse();
            await reindex.Received(1).ReindexAfterImportAsync(Arg.Any<int>(), Arg.Any<string>());
            (await GetAllWebsites(client)).Should().HaveCount(3);
        }

        [Fact]
        public async Task ImportBookmarkFile_AppliesOptions_FromTheFormFields()
        {
            var form = BookmarkForm(BookmarksHtml);
            form.Add(new StringContent("false"), "foldersAsTopics");
            form.Add(new StringContent("ActivelyExploring"), "defaultStatus");
            form.Add(new StringContent("Imported"), "extraTopics");
            form.Add(new StringContent("blog"), "extraGenres");

            var response = await _client.PostAsync("/api/website/from-bookmark-file", form);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var dev = (await GetAllWebsites()).Single(w => w.Link == "https://example.com/dev");
            dev.Topics.Should().BeEquivalentTo(new[] { "imported" }, "folders were switched off");
            dev.Genres.Should().BeEquivalentTo(new[] { "blog" });
            dev.Status.ToString().Should().Be("ActivelyExploring");
        }

        [Fact]
        public async Task PreviewBookmarkFile_ReportsCounts_WithoutWriting()
        {
            await _client.PostAsJsonAsync("/api/website", new CreateWebsiteDto { Title = "Known", Url = "https://example.org/other" });

            var response = await _client.PostAsync("/api/website/from-bookmark-file/preview", BookmarkForm(BookmarksHtml));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var preview = await response.Content.ReadFromJsonAsync<WebsiteBulkImportPreviewDto>(_jsonOptions);
            preview!.TotalCount.Should().Be(4);
            preview.ValidCount.Should().Be(4);
            preview.DuplicateInFileCount.Should().Be(1);
            preview.AlreadyInLibraryCount.Should().Be(1);
            preview.NewCount.Should().Be(2);
            preview.NonWebLinkCount.Should().Be(1);
            preview.Folders.Should().Equal("Dev");
            preview.Sample.Should().HaveCount(3);
            (await GetAllWebsites()).Should().ContainSingle();
        }

        [Fact]
        public async Task ImportBookmarkFile_WithoutAFile_ReturnsBadRequestErrorObject()
        {
            // Options submitted, file part forgotten (a form with no parts at all is rejected by the framework).
            var form = new MultipartFormDataContent { { new StringContent("true"), "foldersAsTopics" } };

            var response = await _client.PostAsync("/api/website/from-bookmark-file", form);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task ImportBookmarkFile_WithAnUnrecognizedFile_ReturnsBadRequestErrorObject()
        {
            var response = await _client.PostAsync("/api/website/from-bookmark-file", BookmarkForm("url,title\nhttps://example.com,x", "export.csv", "text/csv"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(response)).GetProperty("error").GetString().Should().Contain("bookmark export");
        }

        [Fact]
        public async Task ImportBookmarkFile_OverTheSizeCap_ReturnsBadRequestErrorObject()
        {
            var oversized = new string('x', 10 * 1024 * 1024 + 1);

            var response = await _client.PostAsync("/api/website/from-bookmark-file", BookmarkForm(oversized));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(response)).GetProperty("error").GetString().Should().Contain("10 MB");
        }

        #endregion

        #region URL list

        [Fact]
        public async Task ImportUrlList_CreatesStubs_AndSkipsRepeats()
        {
            var request = new UrlListImportRequestDto
            {
                Urls = "https://example.com/one\n- example.com/two\nhttps://www.example.com/one/?utm_source=z",
                Options = new BookmarkImportOptionsDto { ExtraTopics = new List<string> { "pasted" } }
            };

            var response = await _client.PostAsJsonAsync("/api/website/from-url-list", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<WebsiteBulkImportResultDto>(_jsonOptions);
            result!.Operation.Should().Be("url-list-import");
            result.CreatedCount.Should().Be(2);
            result.SkippedCount.Should().Be(1);
            var websites = await GetAllWebsites();
            websites.Select(w => w.Link).Should().BeEquivalentTo(new[] { "https://example.com/one", "https://example.com/two" });
            websites.Should().OnlyContain(w => w.Title == "example.com", "a pasted URL has no title, so the domain stands in");
            websites.Should().OnlyContain(w => w.Topics.Contains("pasted"));
        }

        [Fact]
        public async Task PreviewUrlList_ReportsCounts()
        {
            var response = await _client.PostAsJsonAsync("/api/website/from-url-list/preview", new UrlListImportRequestDto { Urls = "https://example.com/a hello" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var preview = await response.Content.ReadFromJsonAsync<WebsiteBulkImportPreviewDto>(_jsonOptions);
            preview!.NewCount.Should().Be(1);
            preview.NonWebLinkCount.Should().Be(1);
        }

        [Fact]
        public async Task ImportUrlList_WithNoWebLinks_ReturnsBadRequestErrorObject()
        {
            var response = await _client.PostAsJsonAsync("/api/website/from-url-list", new UrlListImportRequestDto { Urls = "nothing here" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        #endregion

        #region Export

        [Fact]
        public async Task ExportBookmarks_ReturnsANetscapeFile_ThatReimportsAsAllSkipped()
        {
            await _client.PostAsync("/api/website/from-bookmark-file", BookmarkForm(BookmarksHtml));

            var response = await _client.GetAsync("/api/website/export");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
            response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
            response.Content.Headers.ContentDisposition.FileName!.Trim('"').Should().StartWith("mymediaverse-bookmarks-").And.EndWith(".html");
            var html = await response.Content.ReadAsStringAsync();
            html.Should().StartWith("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
            html.Should().Contain("HREF=\"https://example.com/toolbar\"").And.Contain("TAGS=\"reading\"");

            var reimport = await _client.PostAsync("/api/website/from-bookmark-file", BookmarkForm(html));
            var result = await reimport.Content.ReadFromJsonAsync<WebsiteBulkImportResultDto>(_jsonOptions);
            result!.CreatedCount.Should().Be(0);
            result.SkippedCount.Should().Be(3);
        }

        #endregion

        #region Authentication

        [Theory]
        [InlineData("POST", "/api/website/from-bookmark-file")]
        [InlineData("POST", "/api/website/from-bookmark-file/preview")]
        [InlineData("POST", "/api/website/from-url-list")]
        [InlineData("POST", "/api/website/from-url-list/preview")]
        [InlineData("GET", "/api/website/export")]
        public async Task BulkEndpoints_WithoutToken_ReturnUnauthorized(string method, string path)
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion
    }
}
