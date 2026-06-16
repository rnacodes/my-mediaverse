using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for creating a new YouTube Playlist
    /// </summary>
    public class CreateYouTubePlaylistDto
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("playlistExternalId")]
        public required string PlaylistExternalId { get; set; }

        [JsonPropertyName("videoCount")]
        public int? VideoCount { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
        
        [JsonPropertyName("privacyStatus")]
        public string? PrivacyStatus { get; set; }
        
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
        
        [JsonPropertyName("topics")]
        public List<string>? Topics { get; set; }
        
        [JsonPropertyName("genres")]
        public List<string>? Genres { get; set; }
    }
}

