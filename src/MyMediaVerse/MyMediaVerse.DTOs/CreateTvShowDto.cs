using MyMediaVerse.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class CreateTvShowDto
    {
        // Base media item properties
        [Required]
        [StringLength(500)]
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; } = MediaType.TVShow;

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

        // TV Show-specific properties
        [StringLength(100)]
        [JsonPropertyName("creator")]
        public string? Creator { get; set; }
        
        [StringLength(500)]
        [JsonPropertyName("cast")]
        public string? Cast { get; set; } // Comma-separated list of main cast members
        
        [Range(1800, 2100, ErrorMessage = "First air year must be between 1800 and 2100")]
        [JsonPropertyName("firstAirYear")]
        public int? FirstAirYear { get; set; }
        
        [Range(1800, 2100, ErrorMessage = "Last air year must be between 1800 and 2100")]
        [JsonPropertyName("lastAirYear")]
        public int? LastAirYear { get; set; }
        
        [Range(1, 100, ErrorMessage = "Number of seasons must be between 1 and 100")]
        [JsonPropertyName("numberOfSeasons")]
        public int? NumberOfSeasons { get; set; }
        
        [Range(1, 10000, ErrorMessage = "Number of episodes must be between 1 and 10000")]
        [JsonPropertyName("numberOfEpisodes")]
        public int? NumberOfEpisodes { get; set; }
        
        [StringLength(50)]
        [JsonPropertyName("contentRating")]
        public string? ContentRating { get; set; } // TV rating (TV-PG, TV-14, etc.)
        
        [StringLength(20)]
        [JsonPropertyName("tmdbId")]
        public string? TmdbId { get; set; } // The Movie Database ID
        
        [Range(0.0, 10.0, ErrorMessage = "TMDB rating must be between 0.0 and 10.0")]
        [JsonPropertyName("tmdbRating")]
        public double? TmdbRating { get; set; }
        
        [StringLength(2000)]
        [JsonPropertyName("tmdbPosterPath")]
        public string? TmdbPosterPath { get; set; }
        
        [StringLength(1000)]
        [JsonPropertyName("tagline")]
        public string? Tagline { get; set; }
        
        [Url]
        [StringLength(2000)]
        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }
        
        [StringLength(10)]
        [JsonPropertyName("originalLanguage")]
        public string? OriginalLanguage { get; set; }
        
        [StringLength(500)]
        [JsonPropertyName("originalName")]
        public string? OriginalName { get; set; }
    }
}
