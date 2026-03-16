using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class MovieResponseDto
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

        // Movie-specific properties
        [JsonPropertyName("director")]
        public string? Director { get; set; }
        
        [JsonPropertyName("cast")]
        public string? Cast { get; set; }
        
        [JsonPropertyName("releaseYear")]
        public int? ReleaseYear { get; set; }
        
        [JsonPropertyName("runtimeMinutes")]
        public int? RuntimeMinutes { get; set; }
        
        [JsonPropertyName("mpaaRating")]
        public string? MpaaRating { get; set; }
        
        [JsonPropertyName("imdbId")]
        public string? ImdbId { get; set; }
        
        [JsonPropertyName("tmdbId")]
        public string? TmdbId { get; set; }
        
        [JsonPropertyName("tmdbRating")]
        public double? TmdbRating { get; set; }
        
        [JsonPropertyName("tmdbBackdropPath")]
        public string? TmdbBackdropPath { get; set; }
        
        [JsonPropertyName("tagline")]
        public string? Tagline { get; set; }
        
        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }
        
        [JsonPropertyName("originalLanguage")]
        public string? OriginalLanguage { get; set; }
        
        [JsonPropertyName("originalTitle")]
        public string? OriginalTitle { get; set; }
        
        // Computed properties
        [JsonPropertyName("tmdbBackdropUrl")]
        public string? TmdbBackdropUrl { get; set; }
        
        [JsonPropertyName("formattedRuntime")]
        public string? FormattedRuntime { get; set; }
    }
}
