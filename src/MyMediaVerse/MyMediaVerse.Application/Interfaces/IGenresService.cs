using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IGenresService
    {
        Task<IReadOnlyList<GenreResponseDto>> GetAllGenresAsync();
        Task<IReadOnlyList<GenreResponseDto>> SearchGenresAsync(string query);
        Task<GenreResponseDto?> GetGenreAsync(Guid id);

        // Returns the existing genre when one with the same normalized name already exists,
        // and a flag indicating whether a new row was created.
        Task<(GenreResponseDto Genre, bool Created)> CreateGenreAsync(CreateGenreDto dto);

        // Returns null when no genre exists for the id; throws InvalidOperationException
        // when another genre already has the new name.
        Task<GenreResponseDto?> UpdateGenreAsync(Guid id, CreateGenreDto dto);

        Task<bool> DeleteGenreAsync(Guid id);

        Task<BulkImportResultDto> ImportGenresFromJsonAsync(IReadOnlyList<CreateGenreDto> genres);
        Task<BulkImportResultDto> ImportGenresFromCsvAsync(Stream csvStream);
    }
}
