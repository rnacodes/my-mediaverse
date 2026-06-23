using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class PodcastEpisodeResponseDto
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
        
        [JsonPropertyName("seriesId")]
        public Guid SeriesId { get; set; }
        
        [JsonPropertyName("seriesTitle")]
        public string? SeriesTitle { get; set; }
        
        [JsonPropertyName("audioLink")]
        public string? AudioLink { get; set; }
        
        [JsonPropertyName("releaseDate")]
        public DateTime? ReleaseDate { get; set; }
        
        [JsonPropertyName("durationInSeconds")]
        public int DurationInSeconds { get; set; }
        
        [JsonPropertyName("episodeNumber")]
        public int? EpisodeNumber { get; set; }
        
        [JsonPropertyName("seasonNumber")]
        public int? SeasonNumber { get; set; }
        
        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
        
        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new List<string>();

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new List<string>();

        [JsonPropertyName("podcastType")]
        public string PodcastType { get; set; } = "Episode";
    }
}

