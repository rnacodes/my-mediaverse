using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class TvShowResponseDto
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

        // TV Show-specific properties
        [JsonPropertyName("creator")]
        public string? Creator { get; set; }
        
        [JsonPropertyName("cast")]
        public string? Cast { get; set; }
        
        [JsonPropertyName("firstAirYear")]
        public int? FirstAirYear { get; set; }
        
        [JsonPropertyName("lastAirYear")]
        public int? LastAirYear { get; set; }
        
        [JsonPropertyName("numberOfSeasons")]
        public int? NumberOfSeasons { get; set; }
        
        [JsonPropertyName("numberOfEpisodes")]
        public int? NumberOfEpisodes { get; set; }
        
        [JsonPropertyName("contentRating")]
        public string? ContentRating { get; set; }
        
        
        [JsonPropertyName("tmdbId")]
        public string? TmdbId { get; set; }
        
        [JsonPropertyName("tmdbRating")]
        public double? TmdbRating { get; set; }
        
        [JsonPropertyName("tmdbPosterPath")]
        public string? TmdbPosterPath { get; set; }
        
        [JsonPropertyName("tagline")]
        public string? Tagline { get; set; }
        
        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }
        
        [JsonPropertyName("originalLanguage")]
        public string? OriginalLanguage { get; set; }
        
        [JsonPropertyName("originalName")]
        public string? OriginalName { get; set; }
        
        // Computed properties
        [JsonPropertyName("tmdbPosterUrl")]
        public string? TmdbPosterUrl { get; set; }
        
        [JsonPropertyName("airYears")]
        public string? AirYears { get; set; }
        
        [JsonPropertyName("episodeCount")]
        public string? EpisodeCount { get; set; }
    }
}
