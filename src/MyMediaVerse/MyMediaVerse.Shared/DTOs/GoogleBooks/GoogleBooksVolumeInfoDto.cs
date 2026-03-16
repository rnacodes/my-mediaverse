using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.GoogleBooks
{
    public class GoogleBooksVolumeInfoDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("subtitle")]
        public string? Subtitle { get; set; }

        [JsonPropertyName("authors")]
        public string[]? Authors { get; set; }

        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("publishedDate")]
        public string? PublishedDate { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }  // May contain HTML

        [JsonPropertyName("industryIdentifiers")]
        public GoogleBooksIndustryIdentifierDto[]? IndustryIdentifiers { get; set; }

        [JsonPropertyName("pageCount")]
        public int? PageCount { get; set; }

        [JsonPropertyName("categories")]
        public string[]? Categories { get; set; }

        [JsonPropertyName("averageRating")]
        public double? AverageRating { get; set; }

        [JsonPropertyName("ratingsCount")]
        public int? RatingsCount { get; set; }

        [JsonPropertyName("imageLinks")]
        public GoogleBooksImageLinksDto? ImageLinks { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("previewLink")]
        public string? PreviewLink { get; set; }

        [JsonPropertyName("infoLink")]
        public string? InfoLink { get; set; }

        [JsonPropertyName("canonicalVolumeLink")]
        public string? CanonicalVolumeLink { get; set; }

        /// <summary>
        /// Gets the best available ISBN, preferring ISBN-13 over ISBN-10.
        /// </summary>
        public string? GetBestIsbn()
        {
            if (IndustryIdentifiers == null || IndustryIdentifiers.Length == 0)
                return null;

            // Prefer ISBN_13 over ISBN_10
            var isbn13 = IndustryIdentifiers.FirstOrDefault(i => i.Type == "ISBN_13")?.Identifier;
            if (!string.IsNullOrEmpty(isbn13)) return isbn13;

            var isbn10 = IndustryIdentifiers.FirstOrDefault(i => i.Type == "ISBN_10")?.Identifier;
            return isbn10;
        }

        /// <summary>
        /// Parses the published date to extract the year.
        /// Google Books returns dates in various formats: "2021", "2021-05", "2021-05-15"
        /// </summary>
        public int? GetPublishedYear()
        {
            if (string.IsNullOrEmpty(PublishedDate)) return null;

            // Try to parse just the year part
            var yearStr = PublishedDate.Length >= 4 ? PublishedDate.Substring(0, 4) : PublishedDate;
            if (int.TryParse(yearStr, out var year) && year >= 1000 && year <= 9999)
            {
                return year;
            }
            return null;
        }
    }
}
