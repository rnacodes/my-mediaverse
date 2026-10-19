using System.Net;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Infrastructure.Clients.Trakt;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class TraktApiClientTests
    {
        private readonly TestHttpMessageHandler _handler = new();
        private readonly TraktApiClient _client;

        public TraktApiClientTests()
        {
            var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://api.trakt.tv/") };
            _client = new TraktApiClient(httpClient, Substitute.For<ILogger<TraktApiClient>>(), Substitute.For<IConfiguration>());
        }

        private static Func<HttpResponseMessage> Page(string json, int? pageCount)
            => () =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                if (pageCount.HasValue)
                    response.Headers.Add("X-Pagination-Page-Count", pageCount.Value.ToString());
                return response;
            };

        private static string Movie(int traktId, string title)
            => "{\"plays\":1,\"last_watched_at\":\"2026-01-01T00:00:00.000Z\",\"movie\":{\"title\":\"" + title
               + "\",\"year\":2020,\"ids\":{\"trakt\":" + traktId + ",\"tmdb\":" + (traktId + 1000) + "}}}";

        [Fact]
        public async Task GetWatchedMoviesAsync_CombinesEveryPage()
        {
            _handler.RespondInSequence(
                Page($"[{Movie(1, "First")},{Movie(2, "Second")}]", pageCount: 2),
                Page($"[{Movie(3, "Third")}]", pageCount: 2));

            var result = await _client.GetWatchedMoviesAsync("token");

            result.Select(m => m.Movie.Title).Should().Equal("First", "Second", "Third");
            _handler.Requests.Select(r => r.RequestUri!.PathAndQuery).Should().Equal(
                "/sync/watched/movies?extended=full&page=1&limit=250",
                "/sync/watched/movies?extended=full&page=2&limit=250");
        }

        [Fact]
        public async Task GetWatchedShowsAsync_RequestsProgress_AndParsesEpisodes()
        {
            const string json = """
                [{"plays":3,"last_watched_at":"2026-02-01T00:00:00.000Z",
                  "show":{"title":"Severance","year":2022,"ids":{"trakt":10,"tmdb":95396}},
                  "seasons":[{"number":1,"episodes":[
                    {"number":1,"plays":1,"last_watched_at":"2026-01-01T00:00:00.000Z"},
                    {"number":2,"plays":2,"last_watched_at":"2026-02-01T00:00:00.000Z"}]}]}]
                """;
            _handler.RespondInSequence(Page(json, pageCount: 1));

            var result = await _client.GetWatchedShowsAsync("token");

            _handler.Requests.Single().RequestUri!.PathAndQuery
                .Should().Be("/sync/watched/shows?extended=progress&page=1&limit=100");
            var show = result.Single();
            show.Show.Title.Should().Be("Severance");
            show.Seasons.Single().Episodes.Select(e => e.Number).Should().Equal(1, 2);
        }

        [Fact]
        public async Task GetRatingsMoviesAsync_StopsAfterOnePage_WhenNoPagingHeader()
        {
            const string json = """[{"rating":8,"rated_at":"2026-01-01T00:00:00.000Z","type":"movie","movie":{"title":"Heat","year":1995,"ids":{"trakt":5,"tmdb":949}}}]""";
            _handler.RespondInSequence(Page(json, pageCount: null), Page(json, pageCount: null));

            var result = await _client.GetRatingsMoviesAsync("token");

            result.Should().HaveCount(1);
            _handler.Requests.Should().HaveCount(1);
        }
    }
}
