using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    public class TvShow : BaseMediaItem
    {
        [StringLength(100)]
        public string? Creator { get; set; }
        
        [StringLength(500)]
        public string? Cast { get; set; } // Comma-separated list of main cast members
        
        public int? FirstAirYear { get; set; }
        
        public int? LastAirYear { get; set; }
        
        public int? NumberOfSeasons { get; set; }
        
        public int? NumberOfEpisodes { get; set; }
        
        [StringLength(50)]
        public string? ContentRating { get; set; } // TV rating (TV-PG, TV-14, etc.)
        
        
        /// <summary>
        /// The Movie Database (TMDb) ID for this TV show. Used to fetch updated metadata from TMDb API.
        /// </summary>
        [StringLength(20)]
        public string? TmdbId { get; set; }
        
        public double? TmdbRating { get; set; }
        
        /// <summary>
        /// TMDb poster image path (not a full URL, just the path component).
        /// Use GetTmdbPosterUrl() to construct the full URL.
        /// Example value: "/path-to-poster.jpg"
        /// </summary>
        [StringLength(2000)]
        public string? TmdbPosterPath { get; set; }
        
        [StringLength(1000)]
        public string? Tagline { get; set; }
        
        [StringLength(2000)]
        public string? Homepage { get; set; }
        
        [StringLength(10)]
        public string? OriginalLanguage { get; set; }
        
        [StringLength(500)]
        public string? OriginalName { get; set; }

        // Trakt.tv integration fields
        public int? TraktId { get; set; }

        [StringLength(200)]
        public string? TraktSlug { get; set; }

        public int? TraktPlays { get; set; }

        public DateTime? TraktLastWatchedAt { get; set; }

        /// <summary>
        /// Raw Trakt rating on 1-10 scale
        /// </summary>
        public int? TraktRating { get; set; }

        // Navigation property to episodes
        public ICollection<TvShowEpisode> Episodes { get; set; } = new List<TvShowEpisode>();

        /// <summary>
        /// Gets the count of episodes tracked in the app
        /// </summary>
        public int TrackedEpisodeCount => Episodes?.Count ?? 0;

        /// <summary>
        /// Gets the full TMDB poster URL
        /// </summary>
        public string? GetTmdbPosterUrl(string size = "w500")
        {
            if (string.IsNullOrEmpty(TmdbPosterPath))
                return null;
                
            return $"https://image.tmdb.org/t/p/{size}{TmdbPosterPath}";
        }
        
        /// <summary>
        /// Gets the effective thumbnail (TMDB poster or fallback to base thumbnail)
        /// </summary>
        public string? GetEffectiveThumbnail()
        {
            return GetTmdbPosterUrl() ?? Thumbnail;
        }
        
        /// <summary>
        /// Gets the air years as a formatted string
        /// </summary>
        public string? GetAirYears()
        {
            if (FirstAirYear.HasValue && LastAirYear.HasValue)
            {
                if (FirstAirYear == LastAirYear)
                    return FirstAirYear.ToString();
                return $"{FirstAirYear}-{LastAirYear}";
            }
            else if (FirstAirYear.HasValue)
            {
                return $"{FirstAirYear}-";
            }
            return null;
        }
        
        /// <summary>
        /// Gets the episode count as a formatted string
        /// </summary>
        public string? GetEpisodeCount()
        {
            if (NumberOfSeasons.HasValue && NumberOfEpisodes.HasValue)
            {
                return $"{NumberOfSeasons} season{(NumberOfSeasons > 1 ? "s" : "")}, {NumberOfEpisodes} episode{(NumberOfEpisodes > 1 ? "s" : "")}";
            }
            else if (NumberOfSeasons.HasValue)
            {
                return $"{NumberOfSeasons} season{(NumberOfSeasons > 1 ? "s" : "")}";
            }
            else if (NumberOfEpisodes.HasValue)
            {
                return $"{NumberOfEpisodes} episode{(NumberOfEpisodes > 1 ? "s" : "")}";
            }
            return null;
        }
        
        /// <summary>
        /// Gets the JustWatch search URL for "Where to Watch" functionality
        /// </summary>
        public string GetJustWatchUrl()
        {
            var encodedTitle = Uri.EscapeDataString(Title);
            return $"https://www.justwatch.com/us/search?q={encodedTitle}";
        }
    }
}
