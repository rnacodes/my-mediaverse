using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// A website's identity is its normalized URL. These tests pin the HTTP contract that follows:
    /// a second save of the same page returns the first row (200, not 201), the preview endpoint
    /// reports the existing website, error bodies are <c>{ error }</c> objects without exception
    /// text, the two endpoints that fetch a user-supplied URL require a token, and deleting a
    /// website removes it from the search index.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class WebsiteIdentityIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public WebsiteIdentityIntegrationTests(ApiFactory factory)
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

        private static CreateWebsiteDto Dto(string url, string title = "Site") => new() { Title = title, Url = url };

        private static ScrapedWebsiteDataDto Scraped(string url, string title = "Scraped") => new()
        {
            Url = url,
            Title = title,
            Description = "Scraped description",
            Domain = "example.com"
        };

        private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        #region Create dedup

        [Fact]
        public async Task CreateWebsite_SameUrlInAnyVariant_ReturnsExistingRowWith200()
        {
            var first = await _client.PostAsJsonAsync("/api/website", Dto("https://Example.com/Page?utm_source=x", "First"));
            first.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await first.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            var second = await _client.PostAsJsonAsync("/api/website", Dto("http://www.example.com/page/", "Second"));
            var third = await _client.PostAsJsonAsync("/api/website", Dto("https://example.com/page#section", "Third"));

            second.StatusCode.Should().Be(HttpStatusCode.OK);
            third.StatusCode.Should().Be(HttpStatusCode.OK);
            (await second.Content.ReadFromJsonAsync<WebsiteResponseDto>())!.Id.Should().Be(created!.Id);
            (await third.Content.ReadFromJsonAsync<WebsiteResponseDto>())!.Id.Should().Be(created.Id);

            var all = await _client.GetFromJsonAsync<List<WebsiteResponseDto>>("/api/website");
            all.Should().ContainSingle();
            all![0].Title.Should().Be("First", "the title is never overwritten by a later save");
            all[0].Link.Should().Be("https://example.com/Page", "the host is lowercased, the path keeps its case");
        }

        [Fact]
        public async Task CreateWebsite_ExistingUrl_AbsorbsMissingMetadata()
        {
            var first = Dto("https://example.com/article", "Article");
            first.Description = null;
            await _client.PostAsJsonAsync("/api/website", first);

            var second = Dto("https://example.com/article", "Ignored title");
            second.Description = "Filled in";
            second.Topics = new List<string> { "News" };
            var response = await _client.PostAsJsonAsync("/api/website", second);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var website = await response.Content.ReadFromJsonAsync<WebsiteResponseDto>();
            website!.Title.Should().Be("Article");
            website.Description.Should().Be("Filled in");
            website.Topics.Should().Contain("news");
        }

        [Fact]
        public async Task CreateWebsite_InvalidUrl_ReturnsBadRequestErrorObject()
        {
            var response = await _client.PostAsJsonAsync("/api/website", Dto("ftp://example.com/file"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await ReadJson(response);
            body.TryGetProperty("error", out _).Should().BeTrue();
        }

        #endregion

        #region Import from URL

        [Fact]
        public async Task ImportFromUrl_NewThenKnownUrl_Returns201Then200AndScrapesOnce()
        {
            var (client, scraper, _) = _factory.CreateClientWithSubstitutes<IWebsiteScraperService, ITypesenseService>(
                s => s.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(call => Scraped(call.Arg<string>())));

            var first = await client.PostAsJsonAsync("/api/website/from-url", new ImportWebsiteDto { Url = "https://example.com/post" });
            var second = await client.PostAsJsonAsync("/api/website/from-url", new ImportWebsiteDto { Url = "http://www.example.com/post/?utm_medium=email" });

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var created = await first.Content.ReadFromJsonAsync<WebsiteResponseDto>();
            var existing = await second.Content.ReadFromJsonAsync<WebsiteResponseDto>();
            existing!.Id.Should().Be(created!.Id);
            created.Title.Should().Be("Scraped");
            await scraper.Received(1).ScrapeWebsiteAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task ImportFromUrl_ReindexesTheNewItem_ButNotAnExistingOne()
        {
            var (client, _, reindex) = _factory.CreateClientWithSubstitutes<IWebsiteScraperService, IImportReindexService>(
                s => s.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(call => Scraped(call.Arg<string>())));

            var first = await client.PostAsJsonAsync("/api/website/from-url", new ImportWebsiteDto { Url = "https://example.com/indexed" });
            await client.PostAsJsonAsync("/api/website/from-url", new ImportWebsiteDto { Url = "https://example.com/indexed" });

            var created = await first.Content.ReadFromJsonAsync<WebsiteResponseDto>();
            await reindex.Received(1).ReindexItemAfterImportAsync(created!.Id, "website import");
        }

        [Fact]
        public async Task ImportFromUrl_ScrapeFails_ReturnsBadRequestWithoutExceptionText()
        {
            var (client, _, _) = _factory.CreateClientWithSubstitutes<IWebsiteScraperService, ITypesenseService>(
                s => s.ScrapeWebsiteAsync(Arg.Any<string>())
                    .Returns<ScrapedWebsiteDataDto>(_ => throw new HttpRequestException("secret upstream detail")));

            var response = await client.PostAsJsonAsync("/api/website/from-url", new ImportWebsiteDto { Url = "https://example.com/down" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("\"error\"");
            body.Should().NotContain("secret upstream detail");
        }

        [Fact]
        public async Task ImportFromUrl_WithoutToken_ReturnsUnauthorized()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsJsonAsync("/api/website/from-url", new ImportWebsiteDto { Url = "https://example.com" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Preview

        [Fact]
        public async Task ScrapePreview_ObjectBody_ReportsExistingWebsiteWhenUrlIsKnown()
        {
            var (client, _, _) = _factory.CreateClientWithSubstitutes<IWebsiteScraperService, ITypesenseService>(
                s => s.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(call => Scraped(call.Arg<string>())));
            var createResponse = await client.PostAsJsonAsync("/api/website", Dto("https://example.com/known", "Known"));
            var known = await createResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            var newPreview = await client.PostAsJsonAsync("/api/website/scrape-preview", new ScrapePreviewRequestDto { Url = "https://example.com/new" });
            var knownPreview = await client.PostAsJsonAsync("/api/website/scrape-preview", new ScrapePreviewRequestDto { Url = "http://www.example.com/known/" });

            newPreview.StatusCode.Should().Be(HttpStatusCode.OK);
            var fresh = await newPreview.Content.ReadFromJsonAsync<WebsitePreviewDto>();
            fresh!.Title.Should().Be("Scraped");
            fresh.ExistingWebsiteId.Should().BeNull();

            var existing = await knownPreview.Content.ReadFromJsonAsync<WebsitePreviewDto>();
            existing!.ExistingWebsiteId.Should().Be(known!.Id);
            existing.ExistingTitle.Should().Be("Known");
        }

        [Fact]
        public async Task ScrapePreview_WithoutToken_ReturnsUnauthorized()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsJsonAsync("/api/website/scrape-preview", new ScrapePreviewRequestDto { Url = "https://example.com" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Update / delete / not found

        [Fact]
        public async Task UpdateWebsite_ToAnotherWebsitesUrl_ReturnsConflict()
        {
            await _client.PostAsJsonAsync("/api/website", Dto("https://example.com/one", "One"));
            var twoResponse = await _client.PostAsJsonAsync("/api/website", Dto("https://example.com/two", "Two"));
            var two = await twoResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            var response = await _client.PutAsJsonAsync($"/api/website/{two!.Id}", Dto("http://www.example.com/one/", "Two"));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ReadJson(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task UpdateWebsite_NewUrl_ReKeysTheRow()
        {
            var createResponse = await _client.PostAsJsonAsync("/api/website", Dto("https://example.com/old", "Site"));
            var created = await createResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            var update = await _client.PutAsJsonAsync($"/api/website/{created!.Id}", Dto("https://Example.com/New/", "Site"));
            update.StatusCode.Should().Be(HttpStatusCode.OK);

            // The old URL is free again and the new one is taken by this row.
            var oldAgain = await _client.PostAsJsonAsync("/api/website", Dto("https://example.com/old", "Fresh"));
            var newAgain = await _client.PostAsJsonAsync("/api/website", Dto("https://example.com/new", "Dup"));
            oldAgain.StatusCode.Should().Be(HttpStatusCode.Created);
            newAgain.StatusCode.Should().Be(HttpStatusCode.OK);
            (await newAgain.Content.ReadFromJsonAsync<WebsiteResponseDto>())!.Id.Should().Be(created.Id);
        }

        [Fact]
        public async Task GetAndDeleteWebsite_NotFound_ReturnErrorObjects()
        {
            var id = Guid.NewGuid();

            var get = await _client.GetAsync($"/api/website/{id}");
            var delete = await _client.DeleteAsync($"/api/website/{id}");

            get.StatusCode.Should().Be(HttpStatusCode.NotFound);
            delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ReadJson(get)).TryGetProperty("error", out _).Should().BeTrue();
            (await ReadJson(delete)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task DeleteWebsite_RemovesTheRowFromTheSearchIndex()
        {
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();
            var createResponse = await client.PostAsJsonAsync("/api/website", Dto("https://example.com/gone", "Gone"));
            var created = await createResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            var response = await client.DeleteAsync($"/api/website/{created!.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            await typesense.Received(1).DeleteMediaItemAsync(created.Id);
            (await client.GetAsync($"/api/website/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Regenerate screenshot

        [Fact]
        public async Task RegenerateScreenshot_SkipsAWebsiteWithAThumbnail_AndForceRendersThroughTheRenderer()
        {
            var dto = Dto("https://example.com/shot", "Shot");
            dto.Thumbnail = "https://example.com/og.png";
            var createResponse = await _client.PostAsJsonAsync("/api/website", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            var skipped = await _client.PostAsync($"/api/website/{created!.Id}/screenshot", null);
            // The test host substitutes the renderer with one that renders nothing.
            var forced = await _client.PostAsync($"/api/website/{created.Id}/screenshot?force=true", null);

            skipped.StatusCode.Should().Be(HttpStatusCode.OK);
            var skippedResult = await skipped.Content.ReadFromJsonAsync<WebsiteScreenshotResultDto>();
            skippedResult!.Skipped.Should().BeTrue();
            skippedResult.Thumbnail.Should().Be("https://example.com/og.png");

            forced.StatusCode.Should().Be(HttpStatusCode.OK);
            var forcedResult = await forced.Content.ReadFromJsonAsync<WebsiteScreenshotResultDto>();
            forcedResult!.Success.Should().BeTrue();
            forcedResult.Rendered.Should().BeFalse();
            forcedResult.WarningMessage.Should().NotBeNullOrEmpty();
            forcedResult.Operation.Should().Be("website-screenshot");
        }

        [Fact]
        public async Task RegenerateScreenshot_UnknownWebsite_ReturnsNotFoundErrorObject()
        {
            var response = await _client.PostAsync($"/api/website/{Guid.NewGuid()}/screenshot", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ReadJson(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task RegenerateScreenshot_WithoutToken_ReturnsUnauthorized()
        {
            var response = await _factory.CreateAnonymousClient().PostAsync($"/api/website/{Guid.NewGuid()}/screenshot", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Generic media endpoint

        [Fact]
        public async Task AddMediaItem_WebsiteThroughGenericEndpoint_ReturnsBadRequestErrorObject()
        {
            var createDto = new CreateMediaItemDto
            {
                Title = "A Website Through The Wrong Door",
                MediaType = MediaType.Website,
                Status = Status.Uncharted
            };
            var content = new StringContent(JsonSerializer.Serialize(createDto, _jsonOptions), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/media", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await ReadJson(response);
            body.TryGetProperty("error", out var error).Should().BeTrue();
            error.GetString().Should().Contain("/api/website");
        }

        #endregion
    }
}
