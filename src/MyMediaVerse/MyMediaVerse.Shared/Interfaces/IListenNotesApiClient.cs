using MyMediaVerse.Shared.DTOs.ListenNotes;

namespace MyMediaVerse.Shared.Interfaces
{
    public interface IListenNotesApiClient
    {
        // Search operations
        Task<SearchResultDto> SearchAsync(string query, string? type = null, int? offset = null, 
            int? lenMin = null, int? lenMax = null, string? genreIds = null, 
            string? publishedBefore = null, string? publishedAfter = null, 
            string? onlyIn = null, string? language = null, string? region = null, 
            string? sortByDate = null, string? safeMode = null, string? uniquePodcasts = null);

        // Podcast operations
        Task<PodcastSeriesDto> GetPodcastByIdAsync(string id, string? nextEpisodePubDate = null);
        Task<ListenNotesBestPodcastsDto> GetBestPodcastsAsync(int? genreId = null, int? page = null, 
            string? region = null, string? sortByDate = null, bool? safeMode = null);
        Task<ListenNotesRecommendationsDto> GetPodcastRecommendationsAsync(string id, bool? safeMode = null);

        // Episode operations
        Task<PodcastEpisodeDto> GetEpisodeByIdAsync(string id);
        Task<ListenNotesRecommendationsDto> GetEpisodeRecommendationsAsync(string id, bool? safeMode = null);

        // Playlist operations
        Task<ListenNotesPlaylistsDto> GetPlaylistsAsync();
        Task<ListenNotesPlaylistDto> GetPlaylistByIdAsync(string id);

        // Genre operations
        Task<ListenNotesGenresDto> GetGenresAsync();

        // Curated content operations
        Task<ListenNotesCuratedPodcastsDto> GetCuratedPodcastsAsync(int? page = null);
        Task<ListenNotesCuratedPodcastDto> GetCuratedPodcastByIdAsync(string id);
    }
}
