using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Clients.ListenNotes;
using NSubstitute;
using Xunit.Abstractions;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Opt-in smoke tests that hit the REAL ListenNotes production API. These validate the
    /// live endpoint contract (path, parameter names, response shape) and our DTO
    /// deserialization end-to-end — the one thing the mocked unit tests cannot cover.
    ///
    /// These are excluded from the normal suite by both the "Smoke" trait and the
    /// <see cref="SmokeFactAttribute"/> gate: a plain `dotnet test` reports them as Skipped
    /// and makes no network call. To run them:
    ///
    ///   $env:RUN_LISTENNOTES_SMOKE = "1"
    ///   $env:LISTENNOTES_API_KEY   = "&lt;your key&gt;"   # if not already set in the shell
    ///   dotnet test tests/MyMediaVerse.UnitTests/MyMediaVerse.UnitTests.csproj --filter Category=Smoke
    /// </summary>
    [Trait("Category", "Smoke")]
    public class ListenNotesApiClientSmokeTests
    {
        // The New York Times' "The Daily" — a stable, well-indexed show with a known publisher.
        private const string TheDailyItunesId = "1200361736";

        private readonly ITestOutputHelper _output;

        public ListenNotesApiClientSmokeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SmokeFact]
        public async Task GetPodcastByItunesIdAsync_ReturnsMatchingPodcast_FromLiveApi()
        {
            // Arrange
            var client = CreateLiveClient();

            // Act
            var result = await client.GetPodcastByItunesIdAsync(TheDailyItunesId);

            // Assert — the live batch endpoint resolves the iTunes id and our DTO binds it.
            result.Should().NotBeNull("the live ListenNotes API should resolve a known iTunes id");
            result!.Id.Should().NotBeNullOrEmpty("a resolved podcast must carry a ListenNotes id");
            result.Title.Should().Contain("Daily");
            result.Publisher.Should().Contain("New York Times");

            _output.WriteLine($"Resolved iTunes {TheDailyItunesId} -> " +
                $"ListenNotes id '{result.Id}', title '{result.Title}', publisher '{result.Publisher}'.");
        }

        [SmokeFact]
        public async Task GetPodcastByItunesIdAsync_ReturnsNull_ForUnknownItunesId_FromLiveApi()
        {
            // Arrange
            var client = CreateLiveClient();

            // Act — an id that ListenNotes will not have indexed should yield an empty podcasts array.
            var result = await client.GetPodcastByItunesIdAsync("1");

            // Assert
            result.Should().BeNull("an unindexed iTunes id should map to no podcast, not throw");
        }

        /// <summary>
        /// Builds a client pointed at the real ListenNotes production API, mirroring how
        /// <c>AddListenNotesApiClient</c> configures the base address and auth header in the Web.API.
        /// </summary>
        private static ListenNotesApiClient CreateLiveClient()
        {
            var apiKey = Environment.GetEnvironmentVariable("LISTENNOTES_API_KEY");
            apiKey.Should().NotBeNullOrEmpty("LISTENNOTES_API_KEY must be set to run the ListenNotes smoke tests");

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://listen-api.listennotes.com/api/v2/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            httpClient.DefaultRequestHeaders.Add("X-ListenAPI-Key", apiKey);

            var logger = Substitute.For<ILogger<ListenNotesApiClient>>();
            var configuration = Substitute.For<IConfiguration>();
            configuration["ApiKeys:ListenNotes"].Returns(apiKey);

            return new ListenNotesApiClient(httpClient, logger, configuration);
        }
    }

    /// <summary>
    /// Marks a fact as an opt-in smoke test. Unless <c>RUN_LISTENNOTES_SMOKE</c> is set, the test
    /// is reported as Skipped at discovery time so a normal `dotnet test` never makes a real API call.
    /// </summary>
    public sealed class SmokeFactAttribute : FactAttribute
    {
        public SmokeFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUN_LISTENNOTES_SMOKE")))
            {
                Skip = "Opt-in smoke test. Set RUN_LISTENNOTES_SMOKE=1 (and LISTENNOTES_API_KEY) to run.";
            }
        }
    }
}
