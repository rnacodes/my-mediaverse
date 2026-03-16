using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a single podcast episode that belongs to a podcast series
    /// </summary>
    public class PodcastEpisode : BaseMediaItem
    {
        // Foreign Key to parent series
        [Required]
        public Guid SeriesId { get; set; }
        
        // Navigation property to parent series
        public PodcastSeries? Series { get; set; }
        
        // Episode-specific properties
        [StringLength(2000)]
        public string? AudioLink { get; set; }
        
        public DateTime? ReleaseDate { get; set; }
        
        public int DurationInSeconds { get; set; } = 0;
        
        // Optional episode ordering
        public int? EpisodeNumber { get; set; }
        
        // Optional season grouping
        public int? SeasonNumber { get; set; }
        
        // External API identifier (for imported episodes from ListenNotes)
        [StringLength(200)]
        public string? ExternalId { get; set; }
        
        // Publisher information (often inherited from series)
        [StringLength(500)]
        public string? Publisher { get; set; }
        
        /// <summary>
        /// Gets the thumbnail for this episode, inheriting from parent series if not set
        /// </summary>
        public string? GetEffectiveThumbnail()
        {
            return !string.IsNullOrEmpty(Thumbnail) ? Thumbnail : Series?.Thumbnail;
        }
        
        /// <summary>
        /// Gets the formatted episode identifier (e.g., "S1E5" or "Episode 5")
        /// </summary>
        public string GetEpisodeIdentifier()
        {
            if (SeasonNumber.HasValue && EpisodeNumber.HasValue)
            {
                return $"S{SeasonNumber}E{EpisodeNumber}";
            }
            else if (EpisodeNumber.HasValue)
            {
                return $"Episode {EpisodeNumber}";
            }
            return string.Empty;
        }
    }
}

