using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.IntegrationTests.Api
{
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class SearchControllerIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public SearchControllerIntegrationTests(ApiFactory factory)
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

        #region Health

        [Fact]
        public async Task Health_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/search/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Search (Typesense auto-substituted by ApiFactory)

        [Fact]
        public async Task Search_ShouldReturnSuccessfully_WhenQueryProvided()
        {
            var response = await _client.GetAsync("/api/search?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchByType_ShouldReturnSuccessfully()
        {
            var response = await _client.GetAsync("/api/search/by-type/book?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchMixlists_ShouldReturnSuccessfully()
        {
            var response = await _client.GetAsync("/api/search/mixlists?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchNotes_ShouldReturnSuccessfully()
        {
            var response = await _client.GetAsync("/api/search/notes?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task SearchHighlights_ShouldReturnSuccessfully()
        {
            var response = await _client.GetAsync("/api/search/highlights?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task Search_ForwardsSortByToTypesense_WhenProvided()
        {
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();

            var response = await client.GetAsync("/api/search?q=test&sort_by=date_added:desc");

            response.IsSuccessStatusCode.Should().BeTrue();
            await typesense.Received(1).SearchAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), "date_added:desc");
        }

        [Fact]
        public async Task Search_ForwardsNullSortBy_WhenOmitted()
        {
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();

            var response = await client.GetAsync("/api/search?q=test");

            response.IsSuccessStatusCode.Should().BeTrue();
            await typesense.Received(1).SearchAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), null);
        }

        [Fact]
        public async Task SearchMixlists_ForwardsSortByToTypesense_WhenProvided()
        {
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();

            var response = await client.GetAsync("/api/search/mixlists?q=test&sort_by=date_created:desc");

            response.IsSuccessStatusCode.Should().BeTrue();
            await typesense.Received(1).SearchMixlistsAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), "date_created:desc");
        }

        #endregion

        #region Auth-Required Endpoints

        [Fact]
        public async Task Reindex_ShouldReturnUnauthorized_WithoutToken()
        {
            var response = await _client.PostAsync("/api/search/reindex", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReindexMixlists_ShouldReturnUnauthorized_WithoutToken()
        {
            var response = await _client.PostAsync("/api/search/reindex-mixlists", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Reset_ShouldReturnUnauthorized_WithoutToken()
        {
            var response = await _client.PostAsync("/api/search/reset", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Reindex_ShouldReturnOk_WithValidToken()
        {
            await _client.AuthenticateAsync();

            var response = await _client.PostAsync("/api/search/reindex", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion
    }
}
