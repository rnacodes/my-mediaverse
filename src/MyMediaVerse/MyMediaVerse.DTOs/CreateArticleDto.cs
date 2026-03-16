using MyMediaVerse.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class CreateArticleDto
    {
        // Base media item properties
        [Required]
        [StringLength(500)]
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; } = MediaType.Article;

        [Url]
        [StringLength(2000)]
        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [Required]
        [JsonPropertyName("status")]
        public Status Status { get; set; } = Status.Uncharted;

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

        [Url]
        [StringLength(2000)]
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        // JSON arrays for better query performance
        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();

        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();

        // Article-specific properties
        [StringLength(500)]
        [JsonPropertyName("contentStoragePath")]
        public string? ContentStoragePath { get; set; }

        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; } = false;

        [JsonPropertyName("isStarred")]
        public bool IsStarred { get; set; } = false;

        [StringLength(200)]
        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [StringLength(200)]
        [JsonPropertyName("publication")]
        public string? Publication { get; set; }

        [JsonPropertyName("publicationDate")]
        public DateTime? PublicationDate { get; set; }

        [Range(0, 100, ErrorMessage = "Reading progress must be between 0 and 100")]
        [JsonPropertyName("readingProgress")]
        public int? ReadingProgress { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Word count must be a positive number")]
        [JsonPropertyName("wordCount")]
        public int? WordCount { get; set; }
    }
}
