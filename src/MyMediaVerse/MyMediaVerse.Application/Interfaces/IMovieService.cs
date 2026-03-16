using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IMovieService
    {
        Task<IEnumerable<Movie>> GetAllMoviesAsync();
        Task<Movie?> GetMovieByIdAsync(Guid id);
        Task<IEnumerable<Movie>> GetMoviesByDirectorAsync(string director);
        Task<IEnumerable<Movie>> GetMoviesByYearAsync(int year);
        Task<Movie> CreateMovieAsync(CreateMovieDto dto);
        Task<Movie> UpdateMovieAsync(Guid id, CreateMovieDto dto);
        Task<bool> DeleteMovieAsync(Guid id);
        Task<bool> MovieExistsAsync(string title, int? releaseYear = null);
        Task<Movie?> GetMovieByTitleAndYearAsync(string title, int? releaseYear = null);
    }
}
