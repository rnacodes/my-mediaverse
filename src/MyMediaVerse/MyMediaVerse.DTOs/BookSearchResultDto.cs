using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for importing a book from OpenLibrary (kept for potential future use).
    /// </summary>
    public class ImportBookFromOpenLibraryDto
    {
        [JsonPropertyName("openLibraryKey")]
        public string? OpenLibraryKey { get; set; }

        [JsonPropertyName("isbn")]
        public string? Isbn { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }
    }

    /// <summary>
    /// DTO for importing a book from Google Books.
    /// </summary>
    public class ImportBookFromGoogleBooksDto
    {
        /// <summary>
        /// Google Books Volume ID (e.g., "EMMWDwAAQBAJ")
        /// </summary>
        [JsonPropertyName("volumeId")]
        public string? VolumeId { get; set; }

        [JsonPropertyName("isbn")]
        public string? Isbn { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }
    }

    public class SearchBooksDto
    {
        [Required]
        [JsonPropertyName("query")]
        public required string Query { get; set; }

        [JsonPropertyName("searchType")]
        public BookSearchType SearchType { get; set; } = BookSearchType.General;

        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }

    public enum BookSearchType
    {
        General,
        Title,
        Author,
        ISBN
    }

    public class BookSearchResultDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("authors")]
        public string[]? Authors { get; set; }

        [JsonPropertyName("firstPublishYear")]
        public int? FirstPublishYear { get; set; }

        [JsonPropertyName("isbn")]
        public string[]? Isbn { get; set; }

        [JsonPropertyName("subjects")]
        public string[]? Subjects { get; set; }

        [JsonPropertyName("coverUrl")]
        public string? CoverUrl { get; set; }

        [JsonPropertyName("publishers")]
        public string[]? Publishers { get; set; }

        [JsonPropertyName("languages")]
        public string[]? Languages { get; set; }

        [JsonPropertyName("pageCount")]
        public int? PageCount { get; set; }

        [JsonPropertyName("averageRating")]
        public double? AverageRating { get; set; }

        [JsonPropertyName("ratingCount")]
        public int? RatingCount { get; set; }

        [JsonPropertyName("hasFulltext")]
        public bool? HasFulltext { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("editionCount")]
        public int? EditionCount { get; set; }
    }
}
