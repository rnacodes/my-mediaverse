using System.Net;
using System.Net.Http.Json;
using MyMediaVerse.IntegrationTests.Fixtures;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Environment gating and per-IP rate limiting.
    ///
    /// Gating: endpoints carrying [Environments] are removed from the route table on
    /// non-matching hosts, so they 404 there — no 401/403 to probe — while remaining
    /// routed on the hosts they belong to. Owner-only integrations (Trakt, Readwise)
    /// and credential login must not exist on the Demo host; the TOTP secret generator
    /// exists only in Development.
    ///
    /// Rate limiting: policies partition by originating client IP resolved through the
    /// proxy chain (CF-Connecting-IP first), so each test isolates itself with a unique
    /// fake IP. The limiter runs before authentication, which also keeps these tests
    /// hermetic: unauthenticated requests consume permits without reaching controllers.
    ///
    /// Note on 401 vs 404: the fallback authorization policy challenges anonymous
    /// requests even when no route matched, so anonymous probes see the same 401 for
    /// gated-away and never-existed routes alike. Absence is therefore asserted with
    /// authenticated clients, which pass the fallback and receive the true 404; the
    /// demo host permits anonymous GETs, so its GET probes assert 404 directly.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class EnvironmentGatingTests
    {
        private const string ClientIpHeader = "CF-Connecting-IP";

        private readonly HttpClient _testingClient;
        private readonly HttpClient _testingAuthenticatedClient;
        private readonly HttpClient _demoClient;
        private readonly HttpClient _demoAuthenticatedClient;

        public EnvironmentGatingTests(ApiFactory apiFactory, DemoApiFactory demoApiFactory)
        {
            _testingClient = apiFactory.CreateAnonymousClient();
            _testingAuthenticatedClient = apiFactory.CreateClient();
            _demoClient = demoApiFactory.CreateAnonymousClient();
            _demoAuthenticatedClient = demoApiFactory.CreateClient();
        }

        private static HttpRequestMessage RequestFrom(HttpMethod method, string uri, string clientIp)
        {
            var request = new HttpRequestMessage(method, uri);
            request.Headers.Add(ClientIpHeader, clientIp);
            return request;
        }

        // --- Demo host: gated endpoints do not exist ---

        [Theory]
        [InlineData("/api/trakt/status")]
        [InlineData("/api/readwise/validate")]
        [InlineData("/api/demo/generate-secret")]
        public async Task DemoHost_GatedGetEndpoints_Return404(string uri)
        {
            var response = await _demoClient.GetAsync(uri);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DemoHost_CredentialLogin_Returns404()
        {
            // The demo host signs in via the TOTP unlock; the credential login endpoint
            // is not routed there at all. Asserted with an authenticated caller because
            // the demo fallback policy 401s anonymous POSTs before routing is consulted.
            var response = await _demoAuthenticatedClient.PostAsync(
                "/api/auth/login",
                JsonContent.Create(new { username = "demo", password = "demo" }));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // --- Owner hosts (represented by Testing): gated endpoints remain routed ---

        [Fact]
        public async Task OwnerHost_TraktAndReadwise_RemainRouted()
        {
            // An authenticated caller passes the fallback policy, so anything but 404
            // proves the route still exists on this host.
            var trakt = await _testingAuthenticatedClient.GetAsync("/api/trakt/status");
            var readwise = await _testingAuthenticatedClient.GetAsync("/api/readwise/validate");

            Assert.NotEqual(HttpStatusCode.NotFound, trakt.StatusCode);
            Assert.NotEqual(HttpStatusCode.NotFound, readwise.StatusCode);
        }

        [Fact]
        public async Task OwnerHost_CredentialLogin_RemainsRouted()
        {
            var response = await _testingAuthenticatedClient.PostAsync(
                "/api/auth/login",
                JsonContent.Create(new { username = "wrong", password = "wrong" }));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task OwnerHost_GenerateSecret_IsDevelopmentOnly()
        {
            var response = await _testingAuthenticatedClient.GetAsync("/api/demo/generate-secret");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // --- Demo host: converted semantic reads are anonymous GETs ---

        [Fact]
        public async Task DemoHost_SemanticSearch_IsAnonymouslyReadable()
        {
            var response = await _demoClient.SendAsync(RequestFrom(
                HttpMethod.Get, "/api/search/semantic?query=test", "203.0.113.60"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DemoHost_RecommendationByVibe_IsNotBlockedByAuthOrRouting()
        {
            var response = await _demoClient.SendAsync(RequestFrom(
                HttpMethod.Get, "/api/recommendation/by-vibe?description=test", "203.0.113.61"));

            // The endpoint may fail deeper in the stack when AI services are absent from
            // the test host; this pins only that routing and authorization admit the call.
            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // --- Rate limits ---

        [Fact]
        public async Task ExpensiveRead_EleventhCallFromSameIp_Returns429()
        {
            const string ip = "198.51.100.10";

            for (var i = 0; i < 10; i++)
            {
                var permitted = await _testingClient.SendAsync(RequestFrom(
                    HttpMethod.Get, "/api/search/semantic?query=limit-test", ip));
                Assert.NotEqual(HttpStatusCode.TooManyRequests, permitted.StatusCode);
            }

            var eleventh = await _testingClient.SendAsync(RequestFrom(
                HttpMethod.Get, "/api/search/semantic?query=limit-test", ip));
            Assert.Equal(HttpStatusCode.TooManyRequests, eleventh.StatusCode);

            // A different client IP gets its own partition and is not throttled.
            var otherIp = await _testingClient.SendAsync(RequestFrom(
                HttpMethod.Get, "/api/search/semantic?query=limit-test", "198.51.100.11"));
            Assert.NotEqual(HttpStatusCode.TooManyRequests, otherIp.StatusCode);
        }

        [Fact]
        public async Task ExternalProxy_SixtyFirstCallFromSameIp_Returns429()
        {
            const string ip = "198.51.100.20";

            for (var i = 0; i < 60; i++)
            {
                var permitted = await _testingClient.SendAsync(RequestFrom(
                    HttpMethod.Get, "/api/listennotes/genres", ip));
                Assert.NotEqual(HttpStatusCode.TooManyRequests, permitted.StatusCode);
            }

            var sixtyFirst = await _testingClient.SendAsync(RequestFrom(
                HttpMethod.Get, "/api/listennotes/genres", ip));
            Assert.Equal(HttpStatusCode.TooManyRequests, sixtyFirst.StatusCode);
        }
    }
}
