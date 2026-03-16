using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class VideoResponseDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }
        
        [JsonPropertyName("status")]
        public Status Status { get; set; }
        
        [JsonPropertyName("dateAdded")]
        public DateTime DateAdded { get; set; }
        
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("videoType")]
        public VideoType VideoType { get; set; }
        
        [JsonPropertyName("parentVideoId")]
        public Guid? ParentVideoId { get; set; }
        
        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;
        
        /// <summary>
        /// Foreign Key to YouTubeChannel entity (for YouTube videos only)
        /// </summary>
        [JsonPropertyName("channelId")]
        public Guid? ChannelId { get; set; }
        
        /// <summary>
        /// Optional nested channel information for convenience
        /// </summary>
        [JsonPropertyName("channel")]
        public YouTubeChannelInfoDto? Channel { get; set; }
        
        [JsonPropertyName("lengthInSeconds")]
        public int LengthInSeconds { get; set; }
        
        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
        
        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }
        
        [JsonPropertyName("ownershipStatus")]
        public OwnershipStatus? OwnershipStatus { get; set; }
        
        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }
        
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
        
        [JsonPropertyName("relatedNotes")]
        public string? RelatedNotes { get; set; }
        
        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();
        
        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Basic playlist information for video playlist lists
    /// </summary>
    public class VideoPlaylistInfoDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("playlistExternalId")]
        public string? PlaylistExternalId { get; set; }

        [JsonPropertyName("videoCount")]
        public int? VideoCount { get; set; }
    }
}
