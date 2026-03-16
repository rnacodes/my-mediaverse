using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Application.Interfaces
{
    public interface ITmdbService
    {
        // Search operations (return DTOs for API consumption)
        Task<TmdbMovieSearchResultDto> SearchMoviesAsync(string query, int page = 1, string language = "en-US");
        Task<TmdbTvSearchResultDto> SearchTvShowsAsync(string query, int page = 1, string language = "en-US");
        Task<TmdbMultiSearchResultDto> SearchMultiAsync(string query, int page = 1, string language = "en-US");
        
        // Detail operations (return DTOs for API consumption)
        Task<TmdbMovieDto> GetMovieDetailsAsync(int movieId, string language = "en-US");
        Task<TmdbTvShowDto> GetTvShowDetailsAsync(int tvShowId, string language = "en-US");
        
        // Popular content operations
        Task<TmdbMovieSearchResultDto> GetPopularMoviesAsync(int page = 1, string language = "en-US");
        Task<TmdbTvSearchResultDto> GetPopularTvShowsAsync(int page = 1, string language = "en-US");
        
        // Genre operations
        Task<TmdbGenreListDto> GetMovieGenresAsync(string language = "en-US");
        Task<TmdbGenreListDto> GetTvGenresAsync(string language = "en-US");
        
        // Utility operations
        string GetImageUrl(string imagePath, string size = "w500");
        
        // Import operations (business logic - convert DTOs to Domain Entities)
        Task<Movie> ImportMovieAsync(int movieId, string language = "en-US");
        Task<TvShow> ImportTvShowAsync(int tvShowId, string language = "en-US");
    }
}
