using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class MixlistResponseDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("dateCreated")]
        public DateTime DateCreated { get; set; }
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        // Simple array of media item IDs instead of full media objects to avoid circular references
        [JsonPropertyName("mediaItemIds")]
        public Guid[] MediaItemIds { get; set; } = Array.Empty<Guid>();
        
        // Optionally include basic media info for display
        [JsonPropertyName("mediaItems")]
        public MediaItemSummary[] MediaItems { get; set; } = Array.Empty<MediaItemSummary>();

        // Linked notes
        [JsonPropertyName("linkedNotes")]
        public LinkedNoteDto[] LinkedNotes { get; set; } = Array.Empty<LinkedNoteDto>();

        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();

        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();
    }
    
    public class MediaItemSummary
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
    }
}



