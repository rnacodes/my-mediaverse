using CsvHelper.Configuration.Attributes;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for mapping Goodreads CSV export columns
    /// </summary>
    public class GoodreadsCsvImportDto
    {
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

        [Name("Shelves")]
        public string? Shelves { get; set; }

        [Name("Bookshelves")]
        public string? Bookshelves { get; set; }

        [Name("My Review")]
        public string? MyReview { get; set; }
    }
}
