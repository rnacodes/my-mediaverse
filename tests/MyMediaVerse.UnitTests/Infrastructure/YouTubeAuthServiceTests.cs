using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Clients.YouTube;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    [Trait("Category", "Unit")]
    public class YouTubeAuthServiceTests
    {
        private readonly TestHttpMessageHandler _handler = new();
        private readonly InMemorySyncState _syncState = new();
        private readonly List<string> _postedForms = new();

        private YouTubeAuthService CreateService(bool configured = true)
        {
            var configuration = Substitute.For<IConfiguration>();
            if (configured)
            {
                configuration["YouTubeOAuth:ClientId"].Returns("client-id");
                configuration["YouTubeOAuth:ClientSecret"].Returns("client-secret");
                configuration["YouTubeOAuth:RedirectUri"].Returns("http://localhost:5033/api/youtube/auth/callback");
            }

            return new YouTubeAuthService(
                new HttpClient(_handler), _syncState, configuration, Substitute.For<ILogger<YouTubeAuthService>>());
        }

        /// <summary>Answers Google's token endpoint with the given body and the channel lookup with a title.</summary>
        private void GoogleResponds(HttpStatusCode tokenStatus, string tokenBody)
        {
            _handler.OnSend = async (request, _) =>
            {
                if (request.RequestUri!.AbsoluteUri.StartsWith("https://oauth2.googleapis.com/token"))
                {
                    _postedForms.Add(await request.Content!.ReadAsStringAsync());
                    return Json(tokenStatus, tokenBody);
                }

                return Json(HttpStatusCode.OK, """{"items":[{"snippet":{"title":"My Channel"}}]}""");
            };
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
            new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

        private static string StateFrom(string authUrl) =>
            Uri.UnescapeDataString(authUrl.Split('&').Single(p => p.StartsWith("state=")).Substring("state=".Length));

        [Fact]
        public async Task GetStatusAsync_ReportsNotConfigured_WhenOAuthSettingsAreMissing()
        {
            var status = await CreateService(configured: false).GetStatusAsync();

            status.Configured.Should().BeFalse();
            status.Connected.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAuthorizationUrlAsync_AsksForOfflineReadOnlyAccessWithStateAndPkce()
        {
            var url = await CreateService().CreateAuthorizationUrlAsync();

            url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
            url.Should().Contain("response_type=code");
            url.Should().Contain("access_type=offline");
            url.Should().Contain("prompt=consent");
            url.Should().Contain("code_challenge_method=S256");
            url.Should().Contain("code_challenge=");
            url.Should().Contain(Uri.EscapeDataString("https://www.googleapis.com/auth/youtube.readonly"));
            url.Should().Contain(Uri.EscapeDataString("http://localhost:5033/api/youtube/auth/callback"));
            StateFrom(url).Should().NotBeNullOrEmpty();
            _syncState.Values.Should().ContainKey(YouTubeAuthService.PendingKey);
        }

        [Fact]
        public async Task CompleteAuthorizationAsync_StoresTheConnection_WhenTheStateMatches()
        {
            var service = CreateService();
            var state = StateFrom(await service.CreateAuthorizationUrlAsync());
            GoogleResponds(HttpStatusCode.OK, """{"access_token":"access-1","refresh_token":"refresh-1","expires_in":3600}""");

            var outcome = await service.CompleteAuthorizationAsync("the-code", state, null);

            outcome.Should().Be(YouTubeAuthCallbackOutcome.Connected);
            _postedForms.Should().ContainSingle();
            _postedForms[0].Should().Contain("grant_type=authorization_code").And.Contain("code=the-code").And.Contain("code_verifier=");
            _syncState.Values[YouTubeAuthService.PendingKey].Should().BeNull("the state value is single-use");

            var status = await CreateService().GetStatusAsync();
            status.Connected.Should().BeTrue();
            status.ChannelTitle.Should().Be("My Channel");
            status.ReconnectBy.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task CompleteAuthorizationAsync_RefusesAWrongState_AndKeepsTheRealSignInPending()
        {
            var service = CreateService();
            await service.CreateAuthorizationUrlAsync();
            GoogleResponds(HttpStatusCode.OK, """{"access_token":"a","refresh_token":"r","expires_in":3600}""");

            var outcome = await service.CompleteAuthorizationAsync("the-code", "forged-state", null);

            outcome.Should().Be(YouTubeAuthCallbackOutcome.Error);
            _postedForms.Should().BeEmpty("no code exchange happens for an unknown state");
            _syncState.Values[YouTubeAuthService.PendingKey].Should().NotBeNull();
            _syncState.Values.Should().NotContainKey(YouTubeAuthService.ConnectionKey);
        }

        [Fact]
        public async Task CompleteAuthorizationAsync_RefusesAReusedState()
        {
            var service = CreateService();
            var state = StateFrom(await service.CreateAuthorizationUrlAsync());
            GoogleResponds(HttpStatusCode.OK, """{"access_token":"a","refresh_token":"r","expires_in":3600}""");
            await service.CompleteAuthorizationAsync("the-code", state, null);

            var second = await service.CompleteAuthorizationAsync("another-code", state, null);

            second.Should().Be(YouTubeAuthCallbackOutcome.Error);
            _postedForms.Should().ContainSingle();
        }

        [Fact]
        public async Task CompleteAuthorizationAsync_RefusesAnExpiredSignIn()
        {
            var service = CreateService();
            var state = StateFrom(await service.CreateAuthorizationUrlAsync());
            var pending = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(_syncState.Values[YouTubeAuthService.PendingKey]!)!;
            _syncState.Values[YouTubeAuthService.PendingKey] = JsonSerializer.Serialize(new
            {
                state,
                codeVerifier = pending["codeVerifier"].GetString(),
                createdAt = DateTime.UtcNow.AddMinutes(-11)
            });
            GoogleResponds(HttpStatusCode.OK, """{"access_token":"a","refresh_token":"r","expires_in":3600}""");

            var outcome = await service.CompleteAuthorizationAsync("the-code", state, null);

            outcome.Should().Be(YouTubeAuthCallbackOutcome.Error);
            _postedForms.Should().BeEmpty();
        }

        [Fact]
        public async Task CompleteAuthorizationAsync_ReportsDenied_WhenConsentIsRefused()
        {
            var service = CreateService();
            var state = StateFrom(await service.CreateAuthorizationUrlAsync());

            var outcome = await service.CompleteAuthorizationAsync(null, state, "access_denied");

            outcome.Should().Be(YouTubeAuthCallbackOutcome.Denied);
            _syncState.Values.Should().NotContainKey(YouTubeAuthService.ConnectionKey);
        }

        [Fact]
        public async Task GetAccessTokenAsync_RefreshesAnExpiredToken_AndStoresTheNewOne()
        {
            StoreConnection(accessTokenExpiresAt: DateTime.UtcNow.AddMinutes(-5));
            GoogleResponds(HttpStatusCode.OK, """{"access_token":"fresh-token","expires_in":3600}""");

            var token = await CreateService().GetAccessTokenAsync();

            token.Should().Be("fresh-token");
            _postedForms.Single().Should().Contain("grant_type=refresh_token").And.Contain("refresh_token=stored-refresh");
            _syncState.Values[YouTubeAuthService.ConnectionKey].Should().Contain("fresh-token").And.Contain("stored-refresh");
        }

        [Fact]
        public async Task GetAccessTokenAsync_ReturnsTheStoredToken_WhileItIsStillValid()
        {
            StoreConnection(accessTokenExpiresAt: DateTime.UtcNow.AddMinutes(30));

            var token = await CreateService().GetAccessTokenAsync();

            token.Should().Be("stored-access");
            _handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAccessTokenAsync_ForgetsTheConnection_WhenGoogleSaysTheGrantIsInvalid()
        {
            StoreConnection(accessTokenExpiresAt: DateTime.UtcNow.AddMinutes(-5));
            GoogleResponds(HttpStatusCode.BadRequest, """{"error":"invalid_grant","error_description":"Token has been expired or revoked."}""");
            var service = CreateService();

            var token = await service.GetAccessTokenAsync();

            token.Should().BeNull();
            _syncState.Values[YouTubeAuthService.ConnectionKey].Should().BeNull();
            (await service.GetStatusAsync()).Connected.Should().BeFalse();
        }

        private void StoreConnection(DateTime accessTokenExpiresAt) =>
            _syncState.Values[YouTubeAuthService.ConnectionKey] = JsonSerializer.Serialize(new
            {
                accessToken = "stored-access",
                refreshToken = "stored-refresh",
                accessTokenExpiresAt,
                connectedAt = DateTime.UtcNow.AddDays(-1),
                channelTitle = "My Channel"
            });

        /// <summary>Just the value store the auth service uses; the cursor methods are not needed here.</summary>
        private sealed class InMemorySyncState : ISyncStateService
        {
            public Dictionary<string, string?> Values { get; } = new();

            public Task<string?> GetValueAsync(string key) =>
                Task.FromResult(Values.TryGetValue(key, out var value) ? value : null);

            public Task SetValueAsync(string key, string? value)
            {
                if (value != null || Values.ContainsKey(key))
                {
                    Values[key] = value;
                }
                return Task.CompletedTask;
            }

            public Task<DateTime?> GetLastSuccessfulSyncAsync(string key) => throw new NotSupportedException();

            public Task<IncrementalSyncWindow> GetIncrementalWindowAsync(string key, DateTime now, TimeSpan defaultLookBack, TimeSpan overlap) =>
                throw new NotSupportedException();

            public Task MarkSyncSucceededAsync(string key, DateTime runStartedAt) => throw new NotSupportedException();
        }
    }
}
