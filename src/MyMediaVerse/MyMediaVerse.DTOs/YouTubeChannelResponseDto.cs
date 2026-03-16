using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for returning YouTube Channel data from the API
    /// </summary>
    public class YouTubeChannelResponseDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("channelExternalId")]
        public string ChannelExternalId { get; set; } = string.Empty;
        
        [JsonPropertyName("customUrl")]
        public string? CustomUrl { get; set; }
        
        [JsonPropertyName("subscriberCount")]
        public long? SubscriberCount { get; set; }
        
        [JsonPropertyName("videoCount")]
        public long? VideoCount { get; set; }
        
        [JsonPropertyName("viewCount")]
        public long? ViewCount { get; set; }
        
        [JsonPropertyName("uploadsPlaylistId")]
        public string? UploadsPlaylistId { get; set; }
        
        [JsonPropertyName("country")]
        public string? Country { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
        
        [JsonPropertyName("lastSyncedAt")]
        public DateTime? LastSyncedAt { get; set; }
        
        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }
        
        [JsonPropertyName("status")]
        public Status Status { get; set; }
        
        [JsonPropertyName("dateAdded")]
        public DateTime DateAdded { get; set; }
        
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
        
        [JsonPropertyName("mixlistIds")]
        public Guid[] MixlistIds { get; set; } = Array.Empty<Guid>();
        
        /// <summary>
        /// Number of videos associated with this channel in the database
        /// </summary>
        [JsonPropertyName("videoCountInDb")]
        public int VideoCountInDb { get; set; }
    }
}

