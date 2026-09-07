using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Clients.Wayback;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// The CDX index is faked through the HTTP handler; no real Wayback calls. Any problem with the
    /// archive yields null so enrichment simply leaves the snapshot link empty.
    /// </summary>
    [Trait("Category", "Unit")]
    public class WaybackMachineClientTests
    {
        private const string PageUrl = "https://example.com/article";

        private readonly TestHttpMessageHandler _handler = new();
        private readonly WaybackMachineClient _client;

        public WaybackMachineClientTests()
        {
            var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://web.archive.org/") };
            _client = new WaybackMachineClient(httpClient, Substitute.For<ILogger<WaybackMachineClient>>());
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_BuildsTheSnapshotUrl_FromTheNewestCdxRow()
        {
            _handler.RespondWith(HttpStatusCode.OK,
                """[["timestamp","original"],["20240315120000","https://example.com/article"]]""");

            var result = await _client.FindLatestSnapshotAsync(PageUrl);

            result.Should().Be("https://web.archive.org/web/20240315120000/https://example.com/article");
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_QueriesTheCdxIndex_ForSuccessfulCapturesNewestFirst()
        {
            _handler.RespondWith(HttpStatusCode.OK, "[]");

            await _client.FindLatestSnapshotAsync(PageUrl);

            var request = _handler.Requests.Should().ContainSingle().Subject;
            var query = request.RequestUri!.AbsoluteUri;
            query.Should().StartWith("https://web.archive.org/cdx/search/cdx?url=https%3A%2F%2Fexample.com%2Farticle");
            query.Should().Contain("output=json");
            query.Should().Contain("limit=-1");
            query.Should().Contain("filter=statuscode:200");
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_WhenTheArchiveHasNoCaptures()
        {
            _handler.RespondWith(HttpStatusCode.OK, "[]");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_WhenOnlyTheHeaderRowComesBack()
        {
            _handler.RespondWith(HttpStatusCode.OK, """[["timestamp","original"]]""");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_OnAServerError()
        {
            _handler.RespondWith(HttpStatusCode.ServiceUnavailable, "down");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_OnAMalformedBody()
        {
            _handler.RespondWith(HttpStatusCode.OK, "<html>not json</html>", "text/html");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_WhenTheRequestThrows()
        {
            _handler.OnSend = (_, _) => throw new HttpRequestException("no route to host");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_ForAnEmptyUrl_WithoutCallingTheArchive()
        {
            (await _client.FindLatestSnapshotAsync(" ")).Should().BeNull();

            _handler.Requests.Should().BeEmpty();
        }
    }
}
