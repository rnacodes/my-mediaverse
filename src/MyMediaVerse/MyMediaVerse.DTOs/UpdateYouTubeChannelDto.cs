using MyMediaVerse.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for updating an existing YouTube Channel
    /// </summary>
    public class UpdateYouTubeChannelDto
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
        public Status Status { get; set; }
        
        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }
        
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

