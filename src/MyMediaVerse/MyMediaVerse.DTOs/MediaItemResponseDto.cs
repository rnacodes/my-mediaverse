using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class MediaItemResponseDto
    {
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
        [JsonPropertyName("genre")]
        public string? Genre { get; set; }
        [JsonPropertyName("relatedNotes")]
        public string? RelatedNotes { get; set; }
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        // Simple string arrays instead of navigation properties to avoid circular references
        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();
        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();
        
        // Simple list of mixlist names/IDs instead of full mixlist objects
        [JsonPropertyName("mixlistIds")]
        public Guid[] MixlistIds { get; set; } = Array.Empty<Guid>();

        // Website-specific properties
        [JsonPropertyName("rssFeedUrl")]
        public string? RssFeedUrl { get; set; }
        [JsonPropertyName("domain")]
        public string? Domain { get; set; }
        [JsonPropertyName("author")]
        public string? Author { get; set; }
        [JsonPropertyName("publication")]
        public string? Publication { get; set; }
        [JsonPropertyName("lastCheckedDate")]
        public DateTime? LastCheckedDate { get; set; }
    }
}
