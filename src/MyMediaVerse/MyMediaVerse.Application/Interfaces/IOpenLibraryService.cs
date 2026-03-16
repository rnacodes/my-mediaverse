using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.OpenLibrary;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IOpenLibraryService
    {
        // Search operations (return DTOs for API consumption)
        Task<OpenLibrarySearchResultDto> SearchBooksAsync(string query, int? offset = null, int? limit = null);
        Task<OpenLibrarySearchResultDto> SearchBooksByTitleAsync(string title, int? offset = null, int? limit = null);
        Task<OpenLibrarySearchResultDto> SearchBooksByAuthorAsync(string author, int? offset = null, int? limit = null);
        Task<OpenLibrarySearchResultDto> SearchBooksByISBNAsync(string isbn);
        
        // Detail operations (return DTOs for API consumption)
        Task<OpenLibraryWorkDto> GetBookByOpenLibraryIdAsync(string openLibraryId);
        Task<OpenLibraryBookDto> GetBookByISBNAsync(string isbn);
        Task<OpenLibraryAuthorDto> GetAuthorAsync(string authorId);
        
        // Utility operations
        string GetCoverImageUrl(int? coverId, string size = "L");
        
        // Import operations (business logic - convert DTOs to Domain Entities)
        Task<Book> ImportBookFromOpenLibraryKeyAsync(string openLibraryKey);
        Task<Book> ImportBookFromISBNAsync(string isbn);
        Task<Book> ImportBookFromTitleAndAuthorAsync(string title, string? author = null);
    }
}
