using CsvHelper.Configuration.Attributes;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for mapping Goodreads CSV export columns
    /// </summary>
    public class GoodreadsCsvImportDto
    {
        /// <summary>
        /// Goodreads' stable per-book id ("Book Id" CSV column). The primary dedup key for
        /// re-imports — Kindle editions frequently ship with a blank ISBN in the export.
        /// </summary>
        [Name("Book Id")]
        public long? GoodreadsBookId { get; set; }

        [Name("Title")]
        public string Title { get; set; } = string.Empty;

        [Name("Author")]
        public string Author { get; set; } = string.Empty;

        [Name("ISBN")]
        public string? ISBN { get; set; }

        [Name("ISBN13")]
        public string? ISBN13 { get; set; }

        [Name("My Rating")]
        public int? MyRating { get; set; }

        [Name("Average Rating")]
        public decimal? AverageRating { get; set; }

        [Name("Publisher")]
        public string? Publisher { get; set; }

        [Name("Binding")]
        public string? Binding { get; set; }

        [Name("Year Published")]
        public int? YearPublished { get; set; }

        [Name("Original Publication Year")]
        public int? OriginalPublicationYear { get; set; }

        [Name("Date Read")]
        public string? DateRead { get; set; }

        [Name("Date Added")]
        public string? DateAdded { get; set; }

        // Goodreads exports the current shelf as "Exclusive Shelf"; "Shelves" is kept as a fallback
        // for older/renamed exports. Matching only "Shelves" left Status null on every real import.
        [Name("Exclusive Shelf", "Shelves")]
        public string? Shelves { get; set; }

        [Name("Bookshelves")]
        public string? Bookshelves { get; set; }

        [Name("My Review")]
        public string? MyReview { get; set; }
    }
}
