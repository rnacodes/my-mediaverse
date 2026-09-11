using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Clients.Wayback;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// The availability endpoint is faked through the HTTP handler; no real Wayback calls. Any
    /// problem with the archive yields null so enrichment simply leaves the snapshot link empty.
    /// </summary>
    [Trait("Category", "Unit")]
    public class WaybackMachineClientTests
    {
        private const string PageUrl = "https://example.com/article";

        private const string CaptureBody = """
            {"url":"https://example.com/article","archived_snapshots":{"closest":{"status":"200","available":true,"url":"http://web.archive.org/web/20240315120000/https://example.com/article","timestamp":"20240315120000"}}}
            """;

        private readonly TestHttpMessageHandler _handler = new();
        private readonly WaybackMachineClient _client;

        public WaybackMachineClientTests()
        {
            var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://archive.org/") };
            _client = new WaybackMachineClient(httpClient, Substitute.For<ILogger<WaybackMachineClient>>());
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsTheClosestCapture_AsAnHttpsLink()
        {
            _handler.RespondWith(HttpStatusCode.OK, CaptureBody);

            var result = await _client.FindLatestSnapshotAsync(PageUrl);

            result.Should().Be("https://web.archive.org/web/20240315120000/https://example.com/article");
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_AsksTheAvailabilityEndpoint_NotTheCdxIndex()
        {
            _handler.RespondWith(HttpStatusCode.OK, """{"url":"https://example.com/article","archived_snapshots":{}}""");

            await _client.FindLatestSnapshotAsync(PageUrl);

            var request = _handler.Requests.Should().ContainSingle().Subject;
            request.RequestUri!.AbsoluteUri.Should().Be("https://archive.org/wayback/available?url=https%3A%2F%2Fexample.com%2Farticle");
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_WhenTheArchiveHasNoCaptures()
        {
            _handler.RespondWith(HttpStatusCode.OK, """{"url":"https://example.com/article","archived_snapshots":{}}""");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_WhenTheCaptureIsNotAvailable()
        {
            _handler.RespondWith(HttpStatusCode.OK,
                """{"archived_snapshots":{"closest":{"status":"404","available":false,"url":"http://web.archive.org/web/20240315120000/https://example.com/article"}}}""");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_OnAServerError()
        {
            _handler.RespondWith(HttpStatusCode.ServiceUnavailable, "down");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Theory]
        [InlineData("<html>not json</html>")]
        [InlineData("[]")]
        [InlineData("""{"archived_snapshots":{"closest":{"available":true}}}""")]
        public async Task FindLatestSnapshotAsync_ReturnsNull_OnAMalformedOrIncompleteBody(string body)
        {
            _handler.RespondWith(HttpStatusCode.OK, body, "text/html");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_WhenTheRequestThrows()
        {
            _handler.OnSend = (_, _) => throw new HttpRequestException("no route to host");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_WhenTheArchiveTimesOut()
        {
            _handler.OnSend = (_, _) => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 10 seconds elapsing.");

            (await _client.FindLatestSnapshotAsync(PageUrl)).Should().BeNull();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_ReturnsNull_ForAnEmptyUrl_WithoutCallingTheArchive()
        {
            (await _client.FindLatestSnapshotAsync(" ")).Should().BeNull();

            _handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task FindLatestSnapshotAsync_Propagates_WhenTheCallerCancels()
        {
            using var cts = new CancellationTokenSource();
            _handler.OnSend = (_, _) =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            };

            var act = () => _client.FindLatestSnapshotAsync(PageUrl, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
