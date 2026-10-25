using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Hands the YouTube client an OAuth access token for the connected account, refreshing it
    /// when it has expired. Returns null when no account is connected (or the connection has
    /// lapsed), so callers fall back to the API key.
    /// </summary>
    public interface IYouTubeAccessTokenProvider
    {
        Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Connects the owner's YouTube account through Google's OAuth 2.0 authorization code flow
    /// for web server applications (with PKCE), so private playlists can be listed and read.
    /// </summary>
    public interface IYouTubeAuthService : IYouTubeAccessTokenProvider
    {
        Task<YouTubeAuthStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a single-use state value and PKCE verifier, and returns the Google consent URL
        /// the browser should be sent to.
        /// </summary>
        Task<string> CreateAuthorizationUrlAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles Google's redirect: checks the state, exchanges the code for tokens and stores
        /// them. Returns the outcome the frontend is told about.
        /// </summary>
        Task<YouTubeAuthCallbackOutcome> CompleteAuthorizationAsync(
            string? code, string? state, string? error, CancellationToken cancellationToken = default);

        /// <summary>Revokes the token at Google (best effort) and forgets it.</summary>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>Where the browser returns after the callback, with the outcome appended.</summary>
        string BuildFrontendReturnUrl(YouTubeAuthCallbackOutcome outcome);
    }

    public enum YouTubeAuthCallbackOutcome
    {
        Connected,
        Denied,
        Error
    }
}
