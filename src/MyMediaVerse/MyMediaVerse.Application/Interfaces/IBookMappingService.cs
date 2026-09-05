using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.GoogleBooks;
using MyMediaVerse.Shared.DTOs.OpenLibrary;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IBookMappingService
    {
        Task<BookResponseDto> MapToResponseDtoAsync(Book book);

        // OpenLibrary mapping
        Task<Book> MapFromOpenLibraryAsync(OpenLibraryBookDto openLibraryBook);
        Task<BookSearchResultDto> MapToSearchResultDtoAsync(OpenLibraryBookDto openLibraryBook);

        // Google Books mapping
        Task<Book> MapFromGoogleBooksAsync(GoogleBooksVolumeDto volume);
        Task<BookSearchResultDto> MapGoogleBooksToSearchResultDtoAsync(GoogleBooksVolumeDto volume);
    }
}
