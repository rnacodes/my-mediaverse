using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.OpenLibrary
{
    public class OpenLibraryBookDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author_name")]
        public string[]? AuthorName { get; set; }

        [JsonPropertyName("author_key")]
        public string[]? AuthorKey { get; set; }

        [JsonPropertyName("first_publish_year")]
        public int? FirstPublishYear { get; set; }

        [JsonPropertyName("isbn")]
        public string[]? Isbn { get; set; }

        [JsonPropertyName("subject")]
        public string[]? Subject { get; set; }

        [JsonPropertyName("cover_i")]
        public int? CoverId { get; set; }

        [JsonPropertyName("publisher")]
        public string[]? Publisher { get; set; }

        [JsonPropertyName("language")]
        public string[]? Language { get; set; }

        [JsonPropertyName("publish_date")]
        public string[]? PublishDate { get; set; }

        [JsonPropertyName("publish_year")]
        public int[]? PublishYear { get; set; }

        [JsonPropertyName("number_of_pages_median")]
        public int? NumberOfPagesMedian { get; set; }

        [JsonPropertyName("rating_average")]
        public double? RatingAverage { get; set; }

        [JsonPropertyName("rating_count")]
        public int? RatingCount { get; set; }

        [JsonPropertyName("has_fulltext")]
        public bool? HasFulltext { get; set; }

        [JsonPropertyName("public_scan_b")]
        public bool? PublicScan { get; set; }

        [JsonPropertyName("lending_edition_s")]
        public string? LendingEdition { get; set; }

        [JsonPropertyName("lending_identifier_s")]
        public string? LendingIdentifier { get; set; }

        [JsonPropertyName("printdisabled_s")]
        public string? PrintDisabled { get; set; }

        [JsonPropertyName("seed")]
        public string[]? Seed { get; set; }

        [JsonPropertyName("edition_count")]
        public int? EditionCount { get; set; }

        [JsonPropertyName("edition_key")]
        public string[]? EditionKey { get; set; }

        [JsonPropertyName("format")]
        public string[]? Format { get; set; }

        [JsonPropertyName("ebook_count_i")]
        public int? EbookCount { get; set; }
    }
}
