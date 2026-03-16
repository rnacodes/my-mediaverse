using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a single TV show episode that belongs to a TV show
    /// </summary>
    public class TvShowEpisode : BaseMediaItem
    {
        // Foreign Key to parent TV show
        [Required]
        public Guid ShowId { get; set; }

        // Navigation property to parent TV show
        public TvShow? Show { get; set; }

        // Episode-specific properties
        public int? SeasonNumber { get; set; }

        public int? EpisodeNumber { get; set; }

        public DateTime? AirDate { get; set; }

        public int? DurationInMinutes { get; set; }

        // External API identifiers
        public int? TmdbEpisodeId { get; set; }

        public int? TraktEpisodeId { get; set; }

        /// <summary>
        /// TMDB still image path (not a full URL, just the path component).
        /// Use GetStillUrl() to construct the full URL.
        /// </summary>
        [StringLength(2000)]
        public string? StillPath { get; set; }

        // Trakt watch tracking
        public int? TraktPlays { get; set; }

        public DateTime? TraktLastWatchedAt { get; set; }

        /// <summary>
        /// Gets the thumbnail for this episode, using TMDB still or inheriting from parent show
        /// </summary>
        public string? GetEffectiveThumbnail()
        {
            if (!string.IsNullOrEmpty(Thumbnail))
                return Thumbnail;

            var stillUrl = GetStillUrl();
            if (stillUrl != null)
                return stillUrl;

            return Show?.GetTmdbPosterUrl() ?? Show?.Thumbnail;
        }

        /// <summary>
        /// Gets the full TMDB still URL
        /// </summary>
        public string? GetStillUrl(string size = "w500")
        {
            if (string.IsNullOrEmpty(StillPath))
                return null;

            return $"https://image.tmdb.org/t/p/{size}{StillPath}";
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
