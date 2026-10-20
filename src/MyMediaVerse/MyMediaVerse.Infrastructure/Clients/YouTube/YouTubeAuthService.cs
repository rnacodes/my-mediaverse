using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.YouTube
{
    /// <summary>
    /// Google OAuth 2.0 for web server applications (authorization code + PKCE), used to connect
    /// the owner's YouTube account with the read-only YouTube scope. The connection and the
    /// in-flight sign-in both live in SyncStates, so no table of their own is needed.
    /// </summary>
    public class YouTubeAuthService : IYouTubeAuthService
    {
        public const string ConnectionKey = "youtube-oauth";
        public const string PendingKey = "youtube-oauth-pending";
        public const string Scope = "https://www.googleapis.com/auth/youtube.readonly";

        // Google ends authorizations seven days after consent while the OAuth app is in Testing.
        public static readonly TimeSpan TestingAuthorizationLifetime = TimeSpan.FromDays(7);

        // How long a started sign-in may take before its state value stops being accepted.
        public static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(10);

        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";
        private const string MyChannelEndpoint = "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true";
        private const string DefaultFrontendUrl = "https://www.mymediaverseuniverse.com";

        // Refresh a little early so a token never expires between the check and the call.
        private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(1);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly ISyncStateService _syncState;
        private readonly ILogger<YouTubeAuthService> _logger;
        private readonly string? _clientId;
        private readonly string? _clientSecret;
        private readonly string? _redirectUri;
        private readonly string _frontendUrl;

        // The connection read once per request scope, so repeated playlist calls don't re-read it.
        private StoredConnection? _cachedConnection;

        public YouTubeAuthService(
            HttpClient httpClient,
            ISyncStateService syncState,
            IConfiguration configuration,
            ILogger<YouTubeAuthService> logger)
        {
            _httpClient = httpClient;
            _syncState = syncState;
            _logger = logger;
            _clientId = Setting("YOUTUBE_OAUTH_CLIENT_ID", configuration["YouTubeOAuth:ClientId"]);
            _clientSecret = Setting("YOUTUBE_OAUTH_CLIENT_SECRET", configuration["YouTubeOAuth:ClientSecret"]);
            _redirectUri = Setting("YOUTUBE_OAUTH_REDIRECT_URI", configuration["YouTubeOAuth:RedirectUri"]);
            _frontendUrl = (Setting("FRONTEND_URL", null) ?? DefaultFrontendUrl).TrimEnd('/');
        }

        private bool IsConfigured =>
            _clientId != null && _clientSecret != null && _redirectUri != null;

        public async Task<YouTubeAuthStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = new YouTubeAuthStatusDto { Configured = IsConfigured };
            if (!IsConfigured)
            {
                return status;
            }

            var connection = await LoadConnectionAsync();
            if (connection != null)
            {
                status.Connected = true;
                status.ChannelTitle = connection.ChannelTitle;
                status.ConnectedAt = connection.ConnectedAt;
                status.ReconnectBy = connection.ConnectedAt + TestingAuthorizationLifetime;
            }

            return status;
        }

        public async Task<string> CreateAuthorizationUrlAsync(CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var pending = new PendingSignIn
            {
                State = RandomUrlSafe(32),
                CodeVerifier = RandomUrlSafe(64),
                CreatedAt = DateTime.UtcNow
            };
            await _syncState.SetValueAsync(PendingKey, JsonSerializer.Serialize(pending, JsonOptions));

            var query = new Dictionary<string, string>
            {
                ["client_id"] = _clientId!,
                ["redirect_uri"] = _redirectUri!,
                ["response_type"] = "code",
                ["scope"] = Scope,
                // Offline access plus a forced consent prompt makes Google return a refresh token
                // every time, including on a reconnect.
                ["access_type"] = "offline",
                ["prompt"] = "consent",
                ["include_granted_scopes"] = "true",
                ["state"] = pending.State,
                ["code_challenge"] = CodeChallenge(pending.CodeVerifier),
                ["code_challenge_method"] = "S256"
            };

            return AuthorizationEndpoint + "?" + string.Join("&",
                query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }

        public async Task<YouTubeAuthCallbackOutcome> CompleteAuthorizationAsync(
            string? code, string? state, string? error, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("YouTube OAuth callback received but OAuth is not configured");
                return YouTubeAuthCallbackOutcome.Error;
            }

            var pending = await LoadPendingAsync();
            if (pending == null || string.IsNullOrEmpty(state) || !FixedTimeEquals(pending.State, state))
            {
                // A wrong state leaves the real sign-in untouched, so a forged callback can't cancel it.
                _logger.LogWarning("YouTube OAuth callback refused: no matching sign-in in progress");
                return YouTubeAuthCallbackOutcome.Error;
            }

            // The state matched, so it is used up whatever happens next.
            await _syncState.SetValueAsync(PendingKey, null);

            if (DateTime.UtcNow - pending.CreatedAt > PendingLifetime)
            {
                _logger.LogWarning("YouTube OAuth callback refused: the sign-in started more than {Minutes} minutes ago",
                    PendingLifetime.TotalMinutes);
                return YouTubeAuthCallbackOutcome.Error;
            }

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogInformation("YouTube OAuth consent was not granted: {Error}", error);
                return error == "access_denied" ? YouTubeAuthCallbackOutcome.Denied : YouTubeAuthCallbackOutcome.Error;
            }

            if (string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("YouTube OAuth callback had neither a code nor an error");
                return YouTubeAuthCallbackOutcome.Error;
            }

            var token = await PostTokenRequestAsync(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _clientId!,
                ["client_secret"] = _clientSecret!,
                ["redirect_uri"] = _redirectUri!,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = pending.CodeVerifier
            }, "code exchange", cancellationToken);

            if (token?.AccessToken == null || string.IsNullOrEmpty(token.RefreshToken))
            {
                _logger.LogError("YouTube OAuth code exchange did not return both an access token and a refresh token");
                return YouTubeAuthCallbackOutcome.Error;
            }

            var now = DateTime.UtcNow;
            var connection = new StoredConnection
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                AccessTokenExpiresAt = now.AddSeconds(token.ExpiresIn),
                ConnectedAt = now,
                ChannelTitle = await GetChannelTitleAsync(token.AccessToken, cancellationToken)
            };
            await SaveConnectionAsync(connection);

            _logger.LogInformation("YouTube account connected ({Channel})", connection.ChannelTitle ?? "channel title unknown");
            return YouTubeAuthCallbackOutcome.Connected;
        }

        public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                return null;
            }

            var connection = await LoadConnectionAsync();
            if (connection == null)
            {
                return null;
            }

            if (connection.AccessTokenExpiresAt - ExpiryMargin > DateTime.UtcNow)
            {
                return connection.AccessToken;
            }

            var refreshed = await PostTokenRequestAsync(new Dictionary<string, string>
            {
                ["client_id"] = _clientId!,
                ["client_secret"] = _clientSecret!,
                ["refresh_token"] = connection.RefreshToken,
                ["grant_type"] = "refresh_token"
            }, "token refresh", cancellationToken);

            if (refreshed?.Error == "invalid_grant")
            {
                // The seven-day Testing limit was reached, or access was revoked: forget the
                // connection so the status says to reconnect.
                _logger.LogWarning("YouTube connection has lapsed (invalid_grant); reconnect from the Import page");
                await ForgetConnectionAsync();
                return null;
            }

            if (refreshed?.AccessToken == null)
            {
                return null;
            }

            connection.AccessToken = refreshed.AccessToken;
            connection.AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn);
            // Google normally keeps the original refresh token; store a new one if it sends one.
            if (!string.IsNullOrEmpty(refreshed.RefreshToken))
            {
                connection.RefreshToken = refreshed.RefreshToken;
            }
            await SaveConnectionAsync(connection);

            return connection.AccessToken;
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            var connection = await LoadConnectionAsync();
            if (connection != null)
            {
                try
                {
                    // Revoking the refresh token also revokes the access tokens issued from it.
                    using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = connection.RefreshToken });
                    using var response = await _httpClient.PostAsync(RevokeEndpoint, content, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Google did not confirm the YouTube token revocation (HTTP {Status})", (int)response.StatusCode);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Could not revoke the YouTube token at Google; forgetting it locally anyway");
                }
            }

            await ForgetConnectionAsync();
            _logger.LogInformation("YouTube account disconnected");
        }

        public string BuildFrontendReturnUrl(YouTubeAuthCallbackOutcome outcome) =>
            $"{_frontendUrl}/import-media?section=youtube&youtube={outcome.ToString().ToLowerInvariant()}";

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "YouTube account connection is not configured. Set YOUTUBE_OAUTH_CLIENT_ID, YOUTUBE_OAUTH_CLIENT_SECRET and YOUTUBE_OAUTH_REDIRECT_URI.");
            }
        }

        private async Task<TokenResponse?> PostTokenRequestAsync(
            Dictionary<string, string> form, string operation, CancellationToken cancellationToken)
        {
            try
            {
                using var content = new FormUrlEncodedContent(form);
                using var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var token = string.IsNullOrWhiteSpace(body) ? null : JsonSerializer.Deserialize<TokenResponse>(body, JsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    // Google's error codes are safe to log; the body never carries a token on failure.
                    _logger.LogWarning("Google OAuth {Operation} failed (HTTP {Status}, error {Error})",
                        operation, (int)response.StatusCode, token?.Error ?? "unknown");
                    return token == null ? null : new TokenResponse { Error = token.Error };
                }

                return token;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Google OAuth {Operation} failed", operation);
                return null;
            }
        }

        private async Task<string?> GetChannelTitleAsync(string accessToken, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, MyChannelEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0 &&
                    items[0].TryGetProperty("snippet", out var snippet) &&
                    snippet.TryGetProperty("title", out var title))
                {
                    return title.GetString();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The title is only a label on the status card; the connection works without it.
                _logger.LogWarning(ex, "Could not read the connected YouTube channel's title");
            }

            return null;
        }

        private async Task<StoredConnection?> LoadConnectionAsync()
        {
            if (_cachedConnection != null)
            {
                return _cachedConnection;
            }

            var connection = Deserialize<StoredConnection>(await _syncState.GetValueAsync(ConnectionKey));
            _cachedConnection = string.IsNullOrEmpty(connection?.RefreshToken) ? null : connection;
            return _cachedConnection;
        }

        private async Task<PendingSignIn?> LoadPendingAsync() =>
            Deserialize<PendingSignIn>(await _syncState.GetValueAsync(PendingKey));

        private async Task SaveConnectionAsync(StoredConnection connection)
        {
            await _syncState.SetValueAsync(ConnectionKey, JsonSerializer.Serialize(connection, JsonOptions));
            _cachedConnection = connection;
        }

        private async Task ForgetConnectionAsync()
        {
            await _syncState.SetValueAsync(ConnectionKey, null);
            _cachedConnection = null;
        }

        private T? Deserialize<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Stored YouTube OAuth value could not be read; treating it as absent");
                return null;
            }
        }

        private static string? Setting(string environmentVariable, string? configured)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariable) ?? configured;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string RandomUrlSafe(int byteCount) => Base64Url(RandomNumberGenerator.GetBytes(byteCount));

        private static string CodeChallenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static bool FixedTimeEquals(string expected, string actual) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));

        private sealed class StoredConnection
        {
            public required string AccessToken { get; set; }
            public required string RefreshToken { get; set; }
            public DateTime AccessTokenExpiresAt { get; set; }
            public DateTime ConnectedAt { get; set; }
            public string? ChannelTitle { get; set; }
        }

        private sealed class PendingSignIn
        {
            public required string State { get; set; }
            public required string CodeVerifier { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private sealed class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }
    }
}
