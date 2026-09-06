using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IGoodreadsImportService
    {
        /// <summary>
        /// Import books from a Goodreads CSV export stream
        /// </summary>
        /// <param name="csvStream">The CSV file stream</param>
        /// <param name="updateExisting">Whether to update existing books on match</param>
        /// <returns>Import result with counts and any errors</returns>
        Task<GoodreadsImportResultDto> ImportFromCsvAsync(Stream csvStream, bool updateExisting = true);

        /// <summary>
        /// Map Goodreads shelf value to PLB Status enum
        /// </summary>
        Status MapShelfToStatus(string? shelf);

        /// <summary>
        /// Map Goodreads My Rating (1-5) to PLB Rating enum
        /// </summary>
        Rating? MapMyRatingToPlbRating(int? myRating);

        /// <summary>
        /// Map Goodreads Binding to PLB BookFormat enum
        /// </summary>
        BookFormat MapBindingToFormat(string? binding);

        /// <summary>
        /// Parse space-separated Bookshelves string into list of tags
        /// </summary>
        List<string> ParseBookshelves(string? bookshelves);
    }
}
