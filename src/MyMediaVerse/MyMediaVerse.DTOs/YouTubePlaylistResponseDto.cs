using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for returning YouTube Playlist data from the API
    /// </summary>
    public class YouTubePlaylistResponseDto
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
        
        [JsonPropertyName("playlistExternalId")]
        public string PlaylistExternalId { get; set; } = string.Empty;

        [JsonPropertyName("videoCount")]
        public int? VideoCount { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
        
        [JsonPropertyName("lastSyncedAt")]
        public DateTime? LastSyncedAt { get; set; }
        
        [JsonPropertyName("privacyStatus")]
        public string? PrivacyStatus { get; set; }
        
        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }
        
        [JsonPropertyName("status")]
        public Status Status { get; set; }
        
        [JsonPropertyName("dateAdded")]
        public DateTime DateAdded { get; set; }
        
        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }
        
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
        
        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new();
        
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();

        [JsonPropertyName("mixlistIds")]
        public Guid[] MixlistIds { get; set; } = Array.Empty<Guid>();

        /// <summary>
        /// List of videos in this playlist (optional, loaded on demand)
        /// </summary>
        [JsonPropertyName("videos")]
        public List<VideoInfoDto>? Videos { get; set; }
    }
    
    /// <summary>
    /// Basic video information for playlist video lists
    /// </summary>
    public class VideoInfoDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("lengthInSeconds")]
        public int LengthInSeconds { get; set; }
        
        [JsonPropertyName("position")]
        public int? Position { get; set; }
        
        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
    }
}

