using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Result of a create-or-find book operation. <see cref="Created"/> is false when
    /// the incoming book matched an existing row (which then absorbed any new external ids).
    /// </summary>
    public record BookCreationResult(Book Book, bool Created);

    public interface IBookService
    {
        Task<IEnumerable<Book>> GetAllBooksAsync();
        Task<Book?> GetBookByIdAsync(Guid id);
        Task<IEnumerable<Book>> GetBooksByAuthorAsync(string author);
        Task<IEnumerable<Book>> GetBookSeriesAsync();

        /// <summary>
        /// Creates a book unless <see cref="BookDuplicateFinder"/> matches an existing row.
        /// Import paths pass an <paramref name="identity"/> carrying their external ids
        /// (Open Library key, Google volume id, …) so both the probe and the saved row
        /// include them; the dto's ISBN/ASIN/title/author always participate.
        /// </summary>
        Task<BookCreationResult> CreateBookAsync(CreateBookDto dto, BookIdentity? identity = null);

        Task<Book> UpdateBookAsync(Guid id, CreateBookDto dto);
        Task<bool> DeleteBookAsync(Guid id);
    }
}
