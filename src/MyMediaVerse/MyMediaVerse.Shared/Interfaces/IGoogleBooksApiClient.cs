using MyMediaVerse.Shared.DTOs.GoogleBooks;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Client interface for Google Books API.
    /// Base URL: https://www.googleapis.com/books/v1/
    /// </summary>
    public interface IGoogleBooksApiClient
    {
        /// <summary>
        /// Search for books using a general query.
        /// </summary>
        /// <param name="query">Search query (can include special syntax like intitle:, inauthor:, isbn:)</param>
        /// <param name="startIndex">Index of first result to return (for pagination)</param>
        /// <param name="maxResults">Maximum number of results (1-40)</param>
        Task<GoogleBooksSearchResultDto> SearchBooksAsync(string query, int? startIndex = null, int? maxResults = null);

        /// <summary>
        /// Search for books by title.
        /// </summary>
        Task<GoogleBooksSearchResultDto> SearchBooksByTitleAsync(string title, int? startIndex = null, int? maxResults = null);

        /// <summary>
        /// Search for books by author.
        /// </summary>
        Task<GoogleBooksSearchResultDto> SearchBooksByAuthorAsync(string author, int? startIndex = null, int? maxResults = null);

        /// <summary>
        /// Search for a book by ISBN.
        /// </summary>
        Task<GoogleBooksSearchResultDto> SearchBooksByISBNAsync(string isbn);

        /// <summary>
        /// Get a specific volume by its Google Books volume ID.
        /// </summary>
        /// <param name="volumeId">The Google Books volume ID</param>
        Task<GoogleBooksVolumeDto?> GetVolumeByIdAsync(string volumeId);
    }
}
