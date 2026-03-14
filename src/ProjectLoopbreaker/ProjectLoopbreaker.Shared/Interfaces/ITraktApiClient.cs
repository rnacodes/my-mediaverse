using ProjectLoopbreaker.Shared.DTOs.Trakt;

namespace ProjectLoopbreaker.Shared.Interfaces
{
    public interface ITraktApiClient
    {
        // Device auth flow
        Task<TraktDeviceCodeDto> GetDeviceCodeAsync();
        Task<TraktOAuthTokenDto?> PollDeviceTokenAsync(string deviceCode);

        // Token management
        Task<TraktOAuthTokenDto> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string accessToken);

        // Sync endpoints (all require OAuth access token)
        Task<List<TraktWatchedMovieDto>> GetWatchedMoviesAsync(string accessToken);
        Task<List<TraktWatchedShowDto>> GetWatchedShowsAsync(string accessToken);
        Task<List<TraktWatchlistItemDto>> GetWatchlistMoviesAsync(string accessToken);
        Task<List<TraktWatchlistItemDto>> GetWatchlistShowsAsync(string accessToken);
        Task<List<TraktRatingItemDto>> GetRatingsMoviesAsync(string accessToken);
        Task<List<TraktRatingItemDto>> GetRatingsShowsAsync(string accessToken);
        Task<TraktLastActivitiesDto> GetLastActivitiesAsync(string accessToken);
    }
}
