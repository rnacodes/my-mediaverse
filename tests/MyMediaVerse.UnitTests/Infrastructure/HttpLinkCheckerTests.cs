using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Services.Web;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// The target servers are faked through the HTTP handler. The checker reports the status the
    /// page finally answers with, so a redirect to a live page is not "broken" and a server that
    /// refuses HEAD is probed with GET before being judged.
    /// </summary>
    [Trait("Category", "Unit")]
    public class HttpLinkCheckerTests
    {
        private const string PageUrl = "https://example.com/page";

        private readonly TestHttpMessageHandler _handler = new();
        private readonly HttpLinkChecker _checker;

        public HttpLinkCheckerTests()
        {
            _checker = new HttpLinkChecker(new HttpClient(_handler), Substitute.For<ILogger<HttpLinkChecker>>());
        }

        private static HttpResponseMessage Redirect(HttpStatusCode status, string location) =>
            new(status) { Headers = { Location = new Uri(location, UriKind.RelativeOrAbsolute) } };

        [Fact]
        public async Task CheckAsync_UsesHead_AndReportsTheStatus()
        {
            _handler.RespondWith(new HttpResponseMessage(HttpStatusCode.OK));

            var status = await _checker.CheckAsync(PageUrl);

            status.Should().Be(200);
            _handler.Requests.Should().ContainSingle().Which.Method.Should().Be(HttpMethod.Head);
        }

        [Theory]
        [InlineData(HttpStatusCode.MethodNotAllowed)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NotImplemented)]
        public async Task CheckAsync_FallsBackToGet_WhenTheServerRefusesHead(HttpStatusCode headStatus)
        {
            _handler.RespondInSequence(
                () => new HttpResponseMessage(headStatus),
                () => new HttpResponseMessage(HttpStatusCode.OK));

            var status = await _checker.CheckAsync(PageUrl);

            status.Should().Be(200);
            _handler.Requests.Select(r => r.Method).Should().Equal(HttpMethod.Head, HttpMethod.Get);
        }

        [Fact]
        public async Task CheckAsync_ReportsAMissingPage()
        {
            _handler.RespondWith(new HttpResponseMessage(HttpStatusCode.NotFound));

            (await _checker.CheckAsync(PageUrl)).Should().Be(404);
        }

        [Fact]
        public async Task CheckAsync_FollowsRedirects_AndReportsTheFinalStatus()
        {
            _handler.RespondInSequence(
                () => Redirect(HttpStatusCode.MovedPermanently, "https://example.com/new-home"),
                () => Redirect(HttpStatusCode.Found, "/final"),
                () => new HttpResponseMessage(HttpStatusCode.OK));

            var status = await _checker.CheckAsync(PageUrl);

            status.Should().Be(200);
            _handler.Requests.Should().HaveCount(3);
            _handler.Requests[1].RequestUri.Should().Be(new Uri("https://example.com/new-home"));
            _handler.Requests[2].RequestUri.Should().Be(new Uri("https://example.com/final"), "a relative Location resolves against the previous hop");
        }

        [Fact]
        public async Task CheckAsync_FollowsAnHttpsToHttpDowngrade_AndReportsTheFinalStatus()
        {
            // The metadata scraper's client refuses https→http hops (so such pages enrich with a
            // status but no metadata); the link checker follows them so the page is not marked broken.
            _handler.RespondInSequence(
                () => Redirect(HttpStatusCode.MovedPermanently, "http://example.com/page"),
                () => new HttpResponseMessage(HttpStatusCode.OK));

            var status = await _checker.CheckAsync(PageUrl);

            status.Should().Be(200);
            _handler.Requests.Should().HaveCount(2);
            _handler.Requests[1].RequestUri!.Scheme.Should().Be("http");
            _handler.Requests[1].Method.Should().Be(HttpMethod.Head, "a 301 keeps the method");
        }

        [Fact]
        public async Task CheckAsync_ReportsTheErrorBehindARedirect()
        {
            _handler.RespondInSequence(
                () => Redirect(HttpStatusCode.MovedPermanently, "https://example.com/gone"),
                () => new HttpResponseMessage(HttpStatusCode.Gone));

            (await _checker.CheckAsync(PageUrl)).Should().Be(410);
        }

        [Fact]
        public async Task CheckAsync_StopsFollowing_AfterTooManyRedirects()
        {
            _handler.OnSend = (_, _) => Task.FromResult(Redirect(HttpStatusCode.Found, "https://example.com/loop"));

            var status = await _checker.CheckAsync(PageUrl);

            status.Should().Be(302, "the last hop's status is reported instead of looping forever");
            _handler.Requests.Count.Should().BeLessThanOrEqualTo(7);
        }

        [Fact]
        public async Task CheckAsync_ReturnsZero_WhenTheHostCannotBeReached()
        {
            _handler.OnSend = (_, _) => throw new HttpRequestException("name resolution failed");

            (await _checker.CheckAsync(PageUrl)).Should().Be(0);
        }

        [Fact]
        public async Task CheckAsync_ReturnsZero_WhenTheRequestTimesOut()
        {
            _handler.OnSend = (_, _) => throw new TaskCanceledException("timed out");

            (await _checker.CheckAsync(PageUrl)).Should().Be(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not a url")]
        [InlineData("ftp://example.com/file")]
        public async Task CheckAsync_ReturnsZero_ForAnUnusableUrl_WithoutSendingAnything(string url)
        {
            (await _checker.CheckAsync(url)).Should().Be(0);

            _handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task CheckAsync_Propagates_WhenTheCallerCancels()
        {
            using var cts = new CancellationTokenSource();
            _handler.OnSend = (_, _) =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            };

            var act = () => _checker.CheckAsync(PageUrl, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
