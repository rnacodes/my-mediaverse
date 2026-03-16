using MyMediaVerse.Shared.DTOs.OpenLibrary;

namespace MyMediaVerse.Shared.Interfaces
{
    public interface IOpenLibraryApiClient
    {
        // Search operations
        Task<OpenLibrarySearchResultDto> SearchBooksAsync(string query, int? offset = null, int? limit = null);
        Task<OpenLibrarySearchResultDto> SearchBooksByTitleAsync(string title, int? offset = null, int? limit = null);
        Task<OpenLibrarySearchResultDto> SearchBooksByAuthorAsync(string author, int? offset = null, int? limit = null);
        Task<OpenLibrarySearchResultDto> SearchBooksByISBNAsync(string isbn);
        
        // Detail operations
        Task<OpenLibraryWorkDto> GetBookByOpenLibraryIdAsync(string openLibraryId);
        Task<OpenLibraryBookDto> GetBookByISBNAsync(string isbn);
        Task<OpenLibraryEditionDto?> GetEditionByISBNAsync(string isbn);
        Task<OpenLibraryAuthorDto> GetAuthorAsync(string authorId);

        /// <summary>
        /// Gets the book description by ISBN using the two-step lookup:
        /// 1. Get edition by ISBN to find Work ID
        /// 2. Get work to retrieve description
        /// </summary>
        /// <param name="isbn">ISBN-10 or ISBN-13</param>
        /// <returns>Description text or null if not found</returns>
        Task<string?> GetBookDescriptionByISBNAsync(string isbn);
        
        // Utility operations
        Task<string> GetSubjectsAsync(); // Returns raw JSON for subjects - can be enhanced later
        string GetCoverImageUrl(int? coverId, string size = "L");
    }
}
