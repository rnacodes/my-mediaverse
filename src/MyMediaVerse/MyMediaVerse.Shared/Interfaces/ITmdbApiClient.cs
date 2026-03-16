using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Shared.Interfaces
{
    public interface ITmdbApiClient
    {
        Task<TmdbMovieSearchResultDto> SearchMoviesAsync(string query, int page = 1, string language = "en-US");
        Task<TmdbTvSearchResultDto> SearchTvShowsAsync(string query, int page = 1, string language = "en-US");
        Task<TmdbMultiSearchResultDto> SearchMultiAsync(string query, int page = 1, string language = "en-US");
        Task<TmdbMovieDto> GetMovieDetailsAsync(int movieId, string language = "en-US");
        Task<TmdbTvShowDto> GetTvShowDetailsAsync(int tvShowId, string language = "en-US");
        Task<TmdbMovieSearchResultDto> GetPopularMoviesAsync(int page = 1, string language = "en-US");
        Task<TmdbTvSearchResultDto> GetPopularTvShowsAsync(int page = 1, string language = "en-US");
        Task<TmdbGenreListDto> GetMovieGenresAsync(string language = "en-US");
        Task<TmdbGenreListDto> GetTvGenresAsync(string language = "en-US");
        string GetImageUrl(string imagePath, string size = "w500");
    }
}