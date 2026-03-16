using MyMediaVerse.Shared.DTOs.Trakt;

namespace MyMediaVerse.Shared.Interfaces
{
    public interface ITraktSyncService
    {
        Task<bool> IsConnectedAsync();
        Task<TraktConnectionStatusDto> GetStatusAsync();
        Task SaveTokenAsync(TraktOAuthTokenDto tokenResponse);
        Task<string?> GetValidAccessTokenAsync();
        Task DisconnectAsync();
        Task<TraktSyncResultDto> SyncWatchedAsync();
        Task<TraktSyncResultDto> SyncWatchlistAsync();
        Task<TraktSyncResultDto> SyncRatingsAsync();
        Task<TraktSyncResultDto> SyncAllAsync();
    }
}
