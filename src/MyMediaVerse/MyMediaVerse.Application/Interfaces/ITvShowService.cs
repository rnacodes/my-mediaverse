using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Outcome of a TV show create: the show, and whether this call created it
    /// (false when a show with the same TMDB id, or the same title and year, was already stored).
    /// </summary>
    public record TvShowCreationResult(TvShow TvShow, bool Created);

    public interface ITvShowService
    {
        Task<IEnumerable<TvShow>> GetAllTvShowsAsync();
        Task<TvShow?> GetTvShowByIdAsync(Guid id);
        Task<IEnumerable<TvShow>> GetTvShowsByCreatorAsync(string creator);
        Task<IEnumerable<TvShow>> GetTvShowsByYearAsync(int year);
        /// <summary>
        /// Creates a TV show unless it is already in the library. A stored show is matched by TMDB id
        /// first, then by title and first air year, and is returned untouched.
        /// </summary>
        /// <param name="fromTmdb">True when the DTO was built from a TMDB details payload; stamps the refresh timestamp.</param>
        Task<TvShowCreationResult> CreateTvShowAsync(CreateTvShowDto dto, bool fromTmdb = false);
        Task<TvShow> UpdateTvShowAsync(Guid id, CreateTvShowDto dto);
        Task<bool> DeleteTvShowAsync(Guid id);
        Task<bool> TvShowExistsAsync(string title, int? firstAirYear = null);
        Task<TvShow?> GetTvShowByTitleAndYearAsync(string title, int? firstAirYear = null);
        Task<TvShow?> GetTvShowByTmdbIdAsync(string tmdbId);

        // Episode methods
        Task<IEnumerable<TvShowEpisode>> GetEpisodesByShowIdAsync(Guid showId);
        Task<TvShowEpisode?> GetTvShowEpisodeByIdAsync(Guid id);
        Task<TvShowEpisode> CreateTvShowEpisodeAsync(CreateTvShowEpisodeDto dto);
        Task<bool> DeleteTvShowEpisodeAsync(Guid id);
        Task<bool> TvShowEpisodeExistsAsync(Guid showId, int seasonNumber, int episodeNumber);
    }
}
