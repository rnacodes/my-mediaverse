using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class BookEnrichmentControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public BookEnrichmentControllerIntegrationTests(ApiFactory factory)
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

        #region Auth Tests

        [Fact]
        public async Task GetStatus_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.GetAsync("/api/bookenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task RunEnrichment_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsJsonAsync("/api/bookenrichment/run", new { });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GetStatus

        [Fact]
        public async Task GetStatus_ShouldReturnOk_WithValidToken()
        {
            await _client.AuthenticateAsync();

            var response = await _client.GetAsync("/api/bookenrichment/status");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("booksNeedingEnrichment", out var count).Should().BeTrue();
            count.GetInt32().Should().BeGreaterThanOrEqualTo(0);
        }

        #endregion

        #region EnrichSingleBook

        [Fact]
        public async Task EnrichSingleBook_ShouldReturnNotFound_WhenBookDoesNotExist()
        {
            await _client.AuthenticateAsync();

            var response = await _client.PostAsync($"/api/bookenrichment/{Guid.NewGuid()}", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region RunEnrichment

        [Fact]
        public async Task RunEnrichment_ShouldReturnOk_WithEmptyDb()
        {
            await _client.AuthenticateAsync();

            var response = await _client.PostAsync("/api/bookenrichment/run", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("totalProcessed", out var total).Should().BeTrue();
            total.GetInt32().Should().Be(0);
        }

        [Fact]
        public async Task RunEnrichment_ShouldReportContractFields()
        {
            await _client.AuthenticateAsync();

            var response = await _client.PostAsync("/api/bookenrichment/run", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            result.GetProperty("operation").GetString().Should().Be("book-description-enrichment");
            result.TryGetProperty("startedAt", out _).Should().BeTrue();
            result.TryGetProperty("completedAt", out _).Should().BeTrue();
            result.TryGetProperty("notFoundCount", out _).Should().BeTrue();
            result.TryGetProperty("errorMessage", out _).Should().BeFalse("null optional fields are omitted from the JSON");
        }

        #endregion

        #region EnrichSingleBook outcomes (no external call is made on these branches)

        [Fact]
        public async Task EnrichSingleBook_PlaceholderAuthorAndNoIsbn_ReportsNoLookupKey()
        {
            await _client.AuthenticateAsync();
            var bookId = await CreateBook(new { title = "Mystery Stub", author = "Unknown Author", status = "Uncharted", format = "Digital" });

            var response = await _client.PostAsync($"/api/bookenrichment/{bookId}", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.GetProperty("success").GetBoolean().Should().BeFalse();
            result.GetProperty("noLookupKey").GetBoolean().Should().BeTrue();
            result.TryGetProperty("noIsbn", out _).Should().BeFalse("the old flag name is gone");
        }

        [Fact]
        public async Task EnrichSingleBook_AlreadyHasDescription_ReportsThatAndSucceeds()
        {
            await _client.AuthenticateAsync();
            var bookId = await CreateBook(new
            {
                title = "Described Book", author = "Some Author", status = "Uncharted", format = "Digital",
                description = "Already here"
            });

            var response = await _client.PostAsync($"/api/bookenrichment/{bookId}", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            result.GetProperty("alreadyHasDescription").GetBoolean().Should().BeTrue();
            result.GetProperty("filledFields").GetArrayLength().Should().Be(0);
        }

        private async Task<Guid> CreateBook(object dto)
        {
            var response = await _client.PostAsJsonAsync("/api/book", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            return body.GetProperty("id").GetGuid();
        }

        #endregion

        #region ConvertGoodreadsRatings

        [Fact]
        public async Task ConvertRatings_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsync("/api/bookenrichment/convert-ratings", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ConvertRatings_ShouldReturnOk_WithEmptyDb()
        {
            await _client.AuthenticateAsync();

            var response = await _client.PostAsync("/api/bookenrichment/convert-ratings", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            result.TryGetProperty("totalCandidates", out var candidates).Should().BeTrue();
            candidates.GetInt32().Should().Be(0);
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            result.GetProperty("operation").GetString().Should().Be("goodreads-rating-conversion");
            result.TryGetProperty("completedAt", out _).Should().BeTrue();
        }

        #endregion
    }
}
