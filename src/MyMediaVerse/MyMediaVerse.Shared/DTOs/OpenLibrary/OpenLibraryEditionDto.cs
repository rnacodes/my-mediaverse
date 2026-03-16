using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.OpenLibrary
{
    /// <summary>
    /// Represents a book edition from Open Library's /isbn/{isbn}.json endpoint.
    /// This contains the link to the Work which has the description.
    /// </summary>
    public class OpenLibraryEditionDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("works")]
        public OpenLibraryWorkReference[]? Works { get; set; }

        [JsonPropertyName("authors")]
        public OpenLibraryAuthorReference[]? Authors { get; set; }

        [JsonPropertyName("publishers")]
        public string[]? Publishers { get; set; }

        [JsonPropertyName("publish_date")]
        public string? PublishDate { get; set; }

        [JsonPropertyName("isbn_10")]
        public string[]? Isbn10 { get; set; }

        [JsonPropertyName("isbn_13")]
        public string[]? Isbn13 { get; set; }

        [JsonPropertyName("covers")]
        public int[]? Covers { get; set; }

        [JsonPropertyName("number_of_pages")]
        public int? NumberOfPages { get; set; }

        [JsonPropertyName("physical_format")]
        public string? PhysicalFormat { get; set; }

        /// <summary>
        /// Gets the Work ID (e.g., "OL12345W") from the works reference, if available.
        /// </summary>
        public string? GetWorkId()
        {
            var workKey = Works?.FirstOrDefault()?.Key;
            if (string.IsNullOrEmpty(workKey))
                return null;

            // Key format is "/works/OL12345W", extract just the ID
            return workKey.Replace("/works/", "");
        }
    }

    /// <summary>
    /// Reference to a Work from an Edition.
    /// </summary>
    public class OpenLibraryWorkReference
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }
}
