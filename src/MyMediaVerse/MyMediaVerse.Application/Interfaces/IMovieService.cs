using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Outcome of a movie create: the movie, and whether this call created it
    /// (false when a movie with the same TMDB id, or the same title and year, was already stored).
    /// </summary>
    public record MovieCreationResult(Movie Movie, bool Created);

    public interface IMovieService
    {
        Task<IEnumerable<Movie>> GetAllMoviesAsync();
        Task<Movie?> GetMovieByIdAsync(Guid id);
        Task<IEnumerable<Movie>> GetMoviesByDirectorAsync(string director);
        Task<IEnumerable<Movie>> GetMoviesByYearAsync(int year);
        /// <summary>
        /// Creates a movie unless it is already in the library. A stored movie is matched by TMDB id
        /// first, then by title and year, and is returned untouched.
        /// </summary>
        /// <param name="fromTmdb">True when the DTO was built from a TMDB details payload; stamps the refresh timestamp.</param>
        Task<MovieCreationResult> CreateMovieAsync(CreateMovieDto dto, bool fromTmdb = false);
        Task<Movie> UpdateMovieAsync(Guid id, CreateMovieDto dto);
        Task<bool> DeleteMovieAsync(Guid id);
        Task<bool> MovieExistsAsync(string title, int? releaseYear = null);
        Task<Movie?> GetMovieByTitleAndYearAsync(string title, int? releaseYear = null);
        Task<Movie?> GetMovieByTmdbIdAsync(string tmdbId);
    }
}
