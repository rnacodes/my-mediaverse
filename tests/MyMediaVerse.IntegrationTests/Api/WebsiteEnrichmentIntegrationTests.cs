using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Pins the HTTP contract of the website enrichment endpoints: result bodies follow the
    /// sync/import reporting contract, a stub is filled and stamped through the real service and
    /// database, the reindex hook fires with the enriched count, per-item enrich answers 404
    /// <c>{ error }</c> for unknown ids, and every action requires a token. The scraper, screenshot
    /// renderer, Wayback client, and link checker are substituted, so nothing leaves the host.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class WebsiteEnrichmentIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public WebsiteEnrichmentIntegrationTests(ApiFactory factory)
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

        private static ScrapedWebsiteDataDto Scraped(string url) => new()
        {
            Url = url,
            Title = "Scraped Title",
            Description = "Scraped description",
            ImageUrl = "https://cdn.example.com/og.png",
            Domain = "example.com"
        };

        private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        private static async Task<WebsiteResponseDto> CreateStub(HttpClient client, string url, string title)
        {
            var response = await client.PostAsJsonAsync("/api/website", new CreateWebsiteDto { Title = title, Url = url });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<WebsiteResponseDto>())!;
        }

        #region Status

        [Fact]
        public async Task GetStatus_ReportsThePendingBacklogAndScreenshotBudget()
        {
            await CreateStub(_client, "https://example.com/one", "example.com");
            await CreateStub(_client, "https://example.com/two", "example.com");

            var response = await _client.GetAsync("/api/website/enrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var status = await response.Content.ReadFromJsonAsync<WebsiteEnrichmentStatusDto>();
            status!.PendingCount.Should().Be(2);
            status.ScreenshotQuotaRemaining.Should().BeGreaterThan(0);
        }

        #endregion

        #region Run

        [Fact]
        public async Task RunEnrichment_FillsStubs_StampsThem_AndReindexesTheEnrichedCount()
        {
            var (client, scraper, reindex) = _factory.CreateClientWithSubstitutes<IWebsiteScraperService, IImportReindexService>(
                s => s.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(call => Scraped(call.Arg<string>())));
            var stub = await CreateStub(client, "https://example.com/stub", "example.com");

            var response = await client.PostAsync("/api/website/enrichment/run?limit=10", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadJson(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("operation").GetString().Should().Be("website-enrichment");
            body.GetProperty("totalProcessed").GetInt32().Should().Be(1);
            body.GetProperty("enrichedCount").GetInt32().Should().Be(1);
            body.GetProperty("pendingCount").GetInt32().Should().Be(0);
            body.GetProperty("quotaReached").GetBoolean().Should().BeFalse();
            body.TryGetProperty("startedAt", out _).Should().BeTrue();
            body.TryGetProperty("completedAt", out _).Should().BeTrue();
            body.TryGetProperty("duration", out _).Should().BeTrue();

            var enriched = await client.GetFromJsonAsync<WebsiteResponseDto>($"/api/website/{stub.Id}");
            enriched!.Title.Should().Be("Scraped Title", "the stub title was the bare domain");
            enriched.Description.Should().Be("Scraped description");
            enriched.Thumbnail.Should().Be("https://cdn.example.com/og.png");
            enriched.EnrichedAt.Should().NotBeNull();
            enriched.LastHttpStatus.Should().Be(200);

            await reindex.Received(1).ReindexAfterImportAsync(1, "website enrichment");
            await scraper.Received(1).ScrapeWebsiteAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task RunEnrichment_RecordsAnUnreachablePage_AsSkipped()
        {
            var (client, _, reindex) = _factory.CreateClientWithSubstitutes<IWebsiteScraperService, IImportReindexService>(
                s => s.ScrapeWebsiteAsync(Arg.Any<string>())
                    .Returns<ScrapedWebsiteDataDto>(_ => throw new HttpRequestException("gone", null, HttpStatusCode.NotFound)));
            var stub = await CreateStub(client, "https://example.com/gone", "example.com");

            var response = await client.PostAsync("/api/website/enrichment/run", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadJson(response);
            body.GetProperty("skippedCount").GetInt32().Should().Be(1);
            body.GetProperty("enrichedCount").GetInt32().Should().Be(0);
            body.GetProperty("warningMessage").GetString().Should().NotBeNullOrEmpty();

            var website = await client.GetFromJsonAsync<WebsiteResponseDto>($"/api/website/{stub.Id}");
            website!.LastHttpStatus.Should().Be(404);
            website.EnrichedAt.Should().NotBeNull();
            await reindex.Received(1).ReindexAfterImportAsync(0, "website enrichment");
        }

        [Fact]
        public async Task RunEnrichment_DefersTheReindex_WhileATimeBudgetedPageLeavesWorkPending()
        {
            var scraper = Substitute.For<IWebsiteScraperService>();
            scraper.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(async call =>
            {
                await Task.Delay(1200);
                return Scraped(call.Arg<string>());
            });
            var reindex = Substitute.For<IImportReindexService>();
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WebsiteEnrichment:RunTimeBudgetSeconds"] = "1"
                }));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IWebsiteScraperService>();
                    services.RemoveAll<IImportReindexService>();
                    services.AddSingleton(scraper);
                    services.AddSingleton(reindex);
                });
            });
            var client = factory.CreateClient();
            await CreateStub(client, "https://example.com/first", "example.com");
            await CreateStub(client, "https://example.com/second", "example.com");

            var first = await client.PostAsync("/api/website/enrichment/run?limit=10", null);

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            var firstBody = await ReadJson(first);
            firstBody.GetProperty("timeBudgetReached").GetBoolean().Should().BeTrue();
            firstBody.GetProperty("totalProcessed").GetInt32().Should().Be(1);
            firstBody.GetProperty("pendingCount").GetInt32().Should().Be(1);
            await reindex.DidNotReceive().ReindexAfterImportAsync(Arg.Any<int>(), "website enrichment");

            var second = await client.PostAsync("/api/website/enrichment/run?limit=10", null);

            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var secondBody = await ReadJson(second);
            secondBody.GetProperty("pendingCount").GetInt32().Should().Be(0);
            await reindex.Received(1).ReindexAfterImportAsync(1, "website enrichment");
        }

        [Fact]
        public async Task RunEnrichment_RejectsAnOutOfRangeLimit()
        {
            var response = await _client.PostAsync("/api/website/enrichment/run?limit=0", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        [Fact]
        public async Task RunEnrichment_ReturnsServerErrorWithTheResultBody_WhenTheRunAborts()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IWebsiteEnrichmentService>(
                s => s.EnrichPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(new WebsiteEnrichmentResult { Success = false, ErrorMessage = "Enrichment run failed: database unavailable" }));

            var response = await client.PostAsync("/api/website/enrichment/run", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ReadJson(response);
            body.GetProperty("success").GetBoolean().Should().BeFalse();
            body.GetProperty("errorMessage").GetString().Should().Contain("database unavailable");
        }

        #endregion

        #region Check links

        [Fact]
        public async Task CheckLinks_RecordsTheStatus_AndListsNewlyBrokenLinks()
        {
            var (client, checker, reindex) = _factory.CreateClientWithSubstitutes<ILinkChecker, IImportReindexService>(
                c => c.CheckAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(404));
            var stub = await CreateStub(client, "https://example.com/dead", "Dead Link");

            var response = await client.PostAsync("/api/website/enrichment/check-links?limit=10&olderThanDays=30", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadJson(response);
            body.GetProperty("operation").GetString().Should().Be("website-link-check");
            body.GetProperty("totalProcessed").GetInt32().Should().Be(1);
            body.GetProperty("brokenCount").GetInt32().Should().Be(1);
            var newlyBroken = body.GetProperty("newlyBroken");
            newlyBroken.GetArrayLength().Should().Be(1);
            newlyBroken[0].GetProperty("id").GetGuid().Should().Be(stub.Id);
            newlyBroken[0].GetProperty("status").GetInt32().Should().Be(404);

            var website = await client.GetFromJsonAsync<WebsiteResponseDto>($"/api/website/{stub.Id}");
            website!.LastHttpStatus.Should().Be(404);
            await checker.Received(1).CheckAsync("https://example.com/dead", Arg.Any<CancellationToken>());
            await reindex.Received(1).ReindexAfterImportAsync(1, "website link check");
        }

        #endregion

        #region Repair thumbnails

        [Fact]
        public async Task RepairThumbnails_ClearsAProviderThumbnail_WhenNothingCanBeRendered()
        {
            // The host's renderer substitute renders nothing, so the provider URL is dropped.
            var dto = new CreateWebsiteDto
            {
                Title = "Placeholder",
                Url = "https://example.com/placeholder",
                Thumbnail = "https://image.thum.io/get/width/1200/https://example.com/placeholder"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/website", dto);
            var created = await createResponse.Content.ReadFromJsonAsync<WebsiteResponseDto>();

            var response = await _client.PostAsync("/api/website/enrichment/repair-thumbnails?limit=10", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadJson(response);
            body.GetProperty("operation").GetString().Should().Be("website-thumbnail-repair");
            body.GetProperty("totalProcessed").GetInt32().Should().Be(1);
            body.GetProperty("skippedCount").GetInt32().Should().Be(1);

            var website = await _client.GetFromJsonAsync<WebsiteResponseDto>($"/api/website/{created!.Id}");
            website!.Thumbnail.Should().BeNull();
        }

        #endregion

        #region Per-item enrich

        [Fact]
        public async Task EnrichWebsite_FillsOneRow_AndReindexesIt()
        {
            var (client, _, reindex) = _factory.CreateClientWithSubstitutes<IWebsiteScraperService, IImportReindexService>(
                s => s.ScrapeWebsiteAsync(Arg.Any<string>()).Returns(call => Scraped(call.Arg<string>())));
            var stub = await CreateStub(client, "https://example.com/single", "example.com");

            var response = await client.PostAsync($"/api/website/{stub.Id}/enrich", null);
            var again = await client.PostAsync($"/api/website/{stub.Id}/enrich", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<SingleWebsiteEnrichmentResult>();
            result!.Success.Should().BeTrue();
            result.FilledFields.Should().Contain("description");
            await reindex.Received(1).ReindexItemAfterImportAsync(stub.Id, "website enrichment");

            var second = await again.Content.ReadFromJsonAsync<SingleWebsiteEnrichmentResult>();
            second!.AlreadyEnriched.Should().BeTrue("a second call without force is a no-op");
        }

        [Fact]
        public async Task EnrichWebsite_UnknownWebsite_ReturnsNotFoundErrorObject()
        {
            var response = await _client.PostAsync($"/api/website/{Guid.NewGuid()}/enrich", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ReadJson(response)).TryGetProperty("error", out _).Should().BeTrue();
        }

        #endregion

        #region Authentication

        [Theory]
        [InlineData("GET", "/api/website/enrichment/status")]
        [InlineData("POST", "/api/website/enrichment/run")]
        [InlineData("POST", "/api/website/enrichment/check-links")]
        [InlineData("POST", "/api/website/enrichment/repair-thumbnails")]
        [InlineData("POST", "/api/website/00000000-0000-0000-0000-000000000001/enrich")]
        public async Task EnrichmentEndpoints_WithoutToken_ReturnUnauthorized(string method, string path)
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion
    }
}
