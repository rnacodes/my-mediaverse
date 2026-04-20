using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    // Business-level facade for importing/searching books from external sources.
    // Today this delegates to Google Books; the seam exists so the source can be
    // swapped (e.g., to OpenLibrary) without touching callers.
    public interface IExternalBookService
    {
        Task<IEnumerable<BookSearchResultDto>> SearchAsync(SearchBooksDto query);

        Task<Book> ImportAsync(ImportBookFromGoogleBooksDto dto);
    }
}
