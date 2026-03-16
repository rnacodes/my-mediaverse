using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for updating an existing YouTube Playlist
    /// </summary>
    public class UpdateYouTubePlaylistDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("linkedYouTubeChannelId")]
        public Guid? LinkedYouTubeChannelId { get; set; }
        
        [JsonPropertyName("videoCount")]
        public int? VideoCount { get; set; }
        
        [JsonPropertyName("privacyStatus")]
        public string? PrivacyStatus { get; set; }
        
        [JsonPropertyName("status")]
        public Status? Status { get; set; }
        
        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }
        
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
        
        [JsonPropertyName("topics")]
        public List<string>? Topics { get; set; }
        
        [JsonPropertyName("genres")]
        public List<string>? Genres { get; set; }
        
        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }
    }
}

