using ProjectLoopbreaker.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLoopbreaker.DTOs
{
    public class CreateTvShowEpisodeDto
    {
        // Base media item properties
        [Required]
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

        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();

        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();

        // TV Show Episode specific properties
        [Required]
        [JsonPropertyName("showId")]
        public Guid ShowId { get; set; }

        [JsonPropertyName("seasonNumber")]
        public int? SeasonNumber { get; set; }

        [JsonPropertyName("episodeNumber")]
        public int? EpisodeNumber { get; set; }

        [JsonPropertyName("airDate")]
        public DateTime? AirDate { get; set; }

        [JsonPropertyName("durationInMinutes")]
        public int? DurationInMinutes { get; set; }

        [JsonPropertyName("tmdbEpisodeId")]
        public int? TmdbEpisodeId { get; set; }

        [JsonPropertyName("stillPath")]
        public string? StillPath { get; set; }
    }
}
