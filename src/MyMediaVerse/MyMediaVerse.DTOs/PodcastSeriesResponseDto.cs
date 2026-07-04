using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class PodcastSeriesResponseDto
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
        
        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }
        
        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }
        
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }
        
        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }

        [JsonPropertyName("rssFeedUrl")]
        public string? RssFeedUrl { get; set; }

        [JsonPropertyName("applePodcastsId")]
        public string? ApplePodcastsId { get; set; }

        [JsonPropertyName("isSubscribed")]
        public bool IsSubscribed { get; set; }
        
        [JsonPropertyName("lastSyncDate")]
        public DateTime? LastSyncDate { get; set; }
        
        [JsonPropertyName("totalEpisodes")]
        public int TotalEpisodes { get; set; }
        
        [JsonPropertyName("episodeCount")]
        public int EpisodeCount { get; set; }
        
        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new List<string>();
        
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new List<string>();
        
        [JsonPropertyName("mixlistIds")]
        public List<Guid> MixlistIds { get; set; } = new List<Guid>();
        
        [JsonPropertyName("podcastType")]
        public string PodcastType { get; set; } = "Series";
    }
}

