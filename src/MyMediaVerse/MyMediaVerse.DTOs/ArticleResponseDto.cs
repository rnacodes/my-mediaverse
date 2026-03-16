using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class ArticleResponseDto
    {
        // Base media item properties
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }

        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }

        [JsonPropertyName("ownershipStatus")]
        public OwnershipStatus? OwnershipStatus { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("relatedNotes")]
        public string? RelatedNotes { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();

        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();

        // Article-specific properties
        [JsonPropertyName("contentStoragePath")]
        public string? ContentStoragePath { get; set; }

        [JsonPropertyName("contentUrl")]
        public string? ContentUrl { get; set; }

        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; }

        [JsonPropertyName("isStarred")]
        public bool IsStarred { get; set; }

        [JsonPropertyName("lastSyncDate")]
        public DateTime? LastSyncDate { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("publication")]
        public string? Publication { get; set; }

        [JsonPropertyName("publicationDate")]
        public DateTime? PublicationDate { get; set; }

        [JsonPropertyName("readingProgress")]
        public int? ReadingProgress { get; set; }

        [JsonPropertyName("wordCount")]
        public int? WordCount { get; set; }

        [JsonPropertyName("estimatedReadingTime")]
        public int? EstimatedReadingTime { get; set; }

        [JsonPropertyName("readwiseDocumentId")]
        public string? ReadwiseDocumentId { get; set; }

        [JsonPropertyName("hasFullTextContent")]
        public bool HasFullTextContent { get; set; }

        [JsonPropertyName("readerLocation")]
        public string? ReaderLocation { get; set; }
    }
}
