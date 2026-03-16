using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.GoogleBooks;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service interface for Google Books operations.
    /// Provides search and import functionality for books from Google Books API.
    /// </summary>
    public interface IGoogleBooksService
    {
        // Search operations (return DTOs for API consumption)

        /// <summary>
        /// Search for books using a general query.
        /// </summary>
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

        // Detail operations

        /// <summary>
        /// Get a specific volume by its Google Books volume ID.
        /// </summary>
        Task<GoogleBooksVolumeDto?> GetVolumeByIdAsync(string volumeId);

        // Import operations (business logic - convert DTOs to Domain Entities)

        /// <summary>
        /// Import a book from Google Books by volume ID.
        /// </summary>
        /// <param name="volumeId">The Google Books volume ID</param>
        /// <returns>The created or existing Book entity</returns>
        Task<Book> ImportBookFromVolumeIdAsync(string volumeId);

        /// <summary>
        /// Import a book from Google Books by ISBN.
        /// </summary>
        /// <param name="isbn">ISBN-10 or ISBN-13</param>
        /// <returns>The created or existing Book entity</returns>
        Task<Book> ImportBookFromISBNAsync(string isbn);

        /// <summary>
        /// Import a book from Google Books by title and optional author.
        /// </summary>
        /// <param name="title">Book title</param>
        /// <param name="author">Optional author name</param>
        /// <returns>The created or existing Book entity</returns>
        Task<Book> ImportBookFromTitleAndAuthorAsync(string title, string? author = null);
    }
}
