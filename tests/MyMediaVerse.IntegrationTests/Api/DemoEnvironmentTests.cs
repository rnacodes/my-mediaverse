using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyMediaVerse.IntegrationTests.Fixtures;
using OtpNet;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Demo-environment behavior: the write gate middleware, the anonymous-GET authorization
    /// policy, and the TOTP unlock flow. Pins the expected status-code matrix:
    ///
    /// <code>
    /// anonymous            GET    → 200
    /// anonymous            write  → 403 + code "demo_read_only"
    /// TOTP cookie + JWT    write  → success
    /// JWT, cookie expired  write  → 403 (unlock again)
    /// cookie, JWT expired  write  → 401
    /// </code>
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class DemoEnvironmentTests : IAsyncLifetime
    {
        private const string DemoReadOnlyCode = "demo_read_only";
        private const string TotpCookie = "Demo_Write_Access=true";

        private readonly DemoApiFactory _factory;
        private readonly HttpClient _authenticatedClient;
        private readonly HttpClient _anonymousClient;

        public DemoEnvironmentTests(DemoApiFactory factory)
        {
            _factory = factory;
            _authenticatedClient = factory.CreateClient();
            _anonymousClient = factory.CreateAnonymousClient();
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        private static string ComputeValidTotpCode()
        {
            var totp = new Totp(Base32Encoding.ToBytes(DemoApiFactory.TotpSecret));
            return totp.ComputeTotp();
        }

        private static HttpRequestMessage WriteRequest(bool withCookie)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/topics")
            {
                Content = JsonContent.Create(new { name = "demo-gate-test-topic" })
            };
            if (withCookie)
            {
                request.Headers.Add("Cookie", TotpCookie);
            }
            return request;
        }

        [Fact]
        public async Task AnonymousGet_IsAllowed()
        {
            var response = await _anonymousClient.GetAsync("/api/media");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AnonymousWrite_Returns403WithMachineReadableCode()
        {
            var response = await _anonymousClient.SendAsync(WriteRequest(withCookie: false));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            Assert.Equal(DemoReadOnlyCode, body.GetProperty("code").GetString());
            Assert.Equal("POST", body.GetProperty("blockedOperation").GetString());
        }

        [Fact]
        public async Task Write_WithCookieAndJwt_Succeeds()
        {
            var response = await _authenticatedClient.SendAsync(WriteRequest(withCookie: true));

            Assert.True(response.IsSuccessStatusCode,
                $"Expected a success status code but got {(int)response.StatusCode} {response.StatusCode}.");
        }

        [Fact]
        public async Task Write_WithJwtButNoCookie_Returns403()
        {
            // The write window (cookie) has closed even though the token is still valid:
            // the gate answers before authentication, so this is "unlock again", not 401.
            var response = await _authenticatedClient.SendAsync(WriteRequest(withCookie: false));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            Assert.Equal(DemoReadOnlyCode, body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task Write_WithCookieButNoJwt_Returns401()
        {
            // The window is open but the caller has no identity: the gate passes the request
            // through and the authorization policy rejects it.
            var response = await _anonymousClient.SendAsync(WriteRequest(withCookie: true));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AllowAnonymousWrite_IsNotBlockedByTheGate()
        {
            // POST /api/demo/lock is [AllowAnonymous]: one of the host's entry points,
            // reachable while the site is read-only.
            var response = await _anonymousClient.PostAsync("/api/demo/lock", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DemoStatus_ReportsDemoEnvironment()
        {
            var response = await _anonymousClient.GetAsync("/api/demo/status");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            Assert.True(body.GetProperty("isDemoEnvironment").GetBoolean());
        }

        [Fact]
        public async Task Unlock_WithValidCode_MintsTokenThatAuthorizesWrites()
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, $"/api/demo/unlock?code={ComputeValidTotpCode()}");
            request.Headers.Add("X-Forwarded-For", "203.0.113.50");

            var response = await _anonymousClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            var token = body.GetProperty("token").GetString();
            Assert.False(string.IsNullOrEmpty(token));
            Assert.Equal("demo", body.GetProperty("username").GetString());
            Assert.Equal(20, body.GetProperty("expiresInMinutes").GetInt32());

            // The full unlocked flow: minted token + write-window cookie → write succeeds.
            var write = WriteRequest(withCookie: true);
            write.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var writeResponse = await _anonymousClient.SendAsync(write);

            Assert.True(writeResponse.IsSuccessStatusCode,
                $"Expected a success status code but got {(int)writeResponse.StatusCode} {writeResponse.StatusCode}.");
        }

        [Fact]
        public async Task Unlock_WithInvalidCode_Returns401AndNoToken()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/demo/unlock?code=000000");
            request.Headers.Add("X-Forwarded-For", "203.0.113.51");

            var response = await _anonymousClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("token", body);
        }
    }
}
