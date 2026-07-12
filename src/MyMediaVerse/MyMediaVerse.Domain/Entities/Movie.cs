using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    public class Movie : BaseMediaItem
    {
        [StringLength(100)]
        public string? Director { get; set; }
        
        [StringLength(500)]
        public string? Cast { get; set; } // Comma-separated list of main cast members
        
        public int? ReleaseYear { get; set; }
        
        public int? RuntimeMinutes { get; set; }
        
        [StringLength(50)]
        public string? MpaaRating { get; set; } // MPAA rating (PG, PG-13, R, etc.)
        
        [StringLength(20)]
        public string? ImdbId { get; set; }
        
        /// <summary>
        /// The Movie Database (TMDb) ID for this movie. Used to fetch updated metadata from TMDb API.
        /// </summary>
        [StringLength(20)]
        public string? TmdbId { get; set; }
        
        public double? TmdbRating { get; set; }
        
        /// <summary>
        /// TMDb backdrop image path (not a full URL, just the path component).
        /// Use GetTmdbBackdropUrl() to construct the full URL.
        /// Example value: "/path-to-backdrop.jpg"
        /// </summary>
        [StringLength(2000)]
        public string? TmdbBackdropPath { get; set; }
        
        [StringLength(1000)]
        public string? Tagline { get; set; }
        
        [StringLength(2000)]
        public string? Homepage { get; set; }
        
        [StringLength(10)]
        public string? OriginalLanguage { get; set; }
        
        [StringLength(500)]
        public string? OriginalTitle { get; set; }

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

        /// <summary>
        /// UTC timestamp of the last successful metadata enrichment (TMDB).
        /// Set only when enrichment actually populated a previously-empty field; enrichment
        /// is fill-gaps-only and never overwrites user-edited/populated values.
        /// </summary>
        public DateTime? EnrichedAt { get; set; }

        /// <summary>
        /// Gets the full TMDB backdrop URL
        /// </summary>
        public string? GetTmdbBackdropUrl(string size = "w1280")
        {
            if (string.IsNullOrEmpty(TmdbBackdropPath))
                return null;
                
            return $"https://image.tmdb.org/t/p/{size}{TmdbBackdropPath}";
        }
        
        /// <summary>
        /// Gets the JustWatch search URL for "Where to Watch" functionality
        /// </summary>
        public string GetJustWatchUrl()
        {
            var encodedTitle = Uri.EscapeDataString(Title);
            return $"https://www.justwatch.com/us/search?q={encodedTitle}";
        }
        
        /// <summary>
        /// Gets the IMDB URL if ImdbId is available
        /// </summary>
        public string? GetImdbUrl()
        {
            if (string.IsNullOrEmpty(ImdbId))
                return null;
                
            return $"https://www.imdb.com/title/{ImdbId}/";
        }
    }
}
