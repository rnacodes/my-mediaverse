using MyMediaVerse.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for creating a new YouTube Channel
    /// </summary>
    public class CreateYouTubeChannelDto
    {
        [Required]
        [StringLength(500)]
        [JsonPropertyName("title")]
        public required string Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [Url]
        [StringLength(2000)]
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        
        [Url]
        [StringLength(2000)]
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [Required]
        [StringLength(100)]
        [JsonPropertyName("channelExternalId")]
        public required string ChannelExternalId { get; set; }
        
        [StringLength(200)]
        [JsonPropertyName("customUrl")]
        public string? CustomUrl { get; set; }
        
        [JsonPropertyName("subscriberCount")]
        public long? SubscriberCount { get; set; }
        
        [JsonPropertyName("videoCount")]
        public long? VideoCount { get; set; }
        
        [JsonPropertyName("viewCount")]
        public long? ViewCount { get; set; }
        
        [StringLength(100)]
        [JsonPropertyName("uploadsPlaylistId")]
        public string? UploadsPlaylistId { get; set; }
        
        [StringLength(10)]
        [JsonPropertyName("country")]
        public string? Country { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
        
        [JsonPropertyName("status")]
        public Status Status { get; set; } = Status.Uncharted;
        
        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }
        
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
        
        [JsonPropertyName("relatedNotes")]
        public string? RelatedNotes { get; set; }
        
        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();
        
        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();
    }
}

