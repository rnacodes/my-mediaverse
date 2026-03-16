using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Simplified YouTube Channel information for embedding in other DTOs
    /// </summary>
    public class YouTubeChannelInfoDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("channelExternalId")]
        public string ChannelExternalId { get; set; } = string.Empty;
        
        [JsonPropertyName("customUrl")]
        public string? CustomUrl { get; set; }
        
        [JsonPropertyName("subscriberCount")]
        public long? SubscriberCount { get; set; }
    }
}

