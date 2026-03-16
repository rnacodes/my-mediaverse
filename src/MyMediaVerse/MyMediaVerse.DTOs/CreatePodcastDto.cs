using MyMediaVerse.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class CreatePodcastDto
    {
        // Base media item properties
        [Required]
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; } = MediaType.Podcast;

        [Url]
        [StringLength(2000)]
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
        
        [Required]
        [JsonPropertyName("status")]
        public Status Status { get; set; } = Status.Uncharted;
        
        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }
        
        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }
        
        [JsonPropertyName("ownershipStatus")]
        public OwnershipStatus? OwnershipStatus { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("relatedNotes")]
        public string? RelatedNotes { get; set; }
        
        [Url]
        [StringLength(2000)]
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        // JSON arrays for better query performance
        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();
        
        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();

        // Unified Podcast properties
        [Required]
        [JsonPropertyName("podcastType")]
        public PodcastType PodcastType { get; set; } = PodcastType.Series;

        // For episodes: Foreign Key to parent series
        [JsonPropertyName("parentPodcastId")]
        public Guid? ParentPodcastId { get; set; }

        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
        
        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [Url]
        [StringLength(2000)]
        [JsonPropertyName("audioLink")]
        public string? AudioLink { get; set; }
        
        [JsonPropertyName("releaseDate")]
        public DateTime? ReleaseDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Duration must be a positive number")]
        [JsonPropertyName("durationInSeconds")]
        public int DurationInSeconds { get; set; }
        
        // Subscription tracking (for Series)
        [JsonPropertyName("isSubscribed")]
        public bool IsSubscribed { get; set; } = false;
        
        // Last sync date for checking new episodes (for Series)
        [JsonPropertyName("lastSyncDate")]
        public DateTime? LastSyncDate { get; set; }
    }
}
