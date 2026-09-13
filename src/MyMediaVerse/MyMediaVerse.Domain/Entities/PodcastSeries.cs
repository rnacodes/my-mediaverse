using System.ComponentModel.DataAnnotations;
using MyMediaVerse.Domain.Constants;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a podcast series (show) that contains multiple episodes
    /// </summary>
    public class PodcastSeries : BaseMediaItem
    {
        // Publisher/Host information
        [StringLength(500)]
        public string? Publisher { get; set; }

        /// <summary>
        /// ListenNotes podcast id. Populated only by ListenNotes dataset (CSV) imports.
        /// </summary>
        [StringLength(200)]
        public string? ExternalId { get; set; }

        /// <summary>
        /// The show's RSS feed. The feed is the source of truth for series metadata and episodes.
        /// </summary>
        [StringLength(2000)]
        public string? RssFeedUrl { get; set; }

        /// <summary>
        /// Normalized comparison key of <see cref="RssFeedUrl"/> (scheme, "www." and trailing slash
        /// ignored). The feed is a series' identity, and this column is what duplicate detection
        /// matches on. Unique when present; rows saved before the column existed are matched by
        /// RssFeedUrl instead.
        /// </summary>
        [StringLength(2000)]
        public string? FeedUrlKey { get; set; }

        /// <summary>
        /// The feed's podcast:guid. Stable across feed host moves, so it is checked before the URL.
        /// </summary>
        [StringLength(100)]
        public string? FeedGuid { get; set; }

        [StringLength(50)]
        public string? ApplePodcastsId { get; set; }

        public long? PodcastIndexId { get; set; }

        /// <summary>
        /// Which source last wrote the descriptive fields. See <see cref="PodcastMetadataSources"/>.
        /// </summary>
        [StringLength(50)]
        public string MetadataSource { get; set; } = PodcastMetadataSources.Manual;

        [StringLength(20)]
        public string? Language { get; set; }

        // Subscription tracking
        public bool IsSubscribed { get; set; } = false;

        // Last sync date for checking new episodes
        public DateTime? LastSyncDate { get; set; }

        /// <summary>
        /// UTC timestamp of the last successful metadata enrichment.
        /// Enrichment is fill-gaps-only and never overwrites user-edited/populated values.
        /// </summary>
        public DateTime? EnrichedAt { get; set; }

        /// <summary>
        /// UTC timestamp of the last enrichment attempt, successful or not. Used to space out retries
        /// for series whose feed could not be read.
        /// </summary>
        public DateTime? LastEnrichmentAttemptAt { get; set; }

        // Total episodes count (from the feed or calculated)
        public int TotalEpisodes { get; set; } = 0;

        // Navigation property to episodes
        public ICollection<PodcastEpisode> Episodes { get; set; } = new List<PodcastEpisode>();

        /// <summary>
        /// Gets the count of episodes in this series
        /// </summary>
        public int EpisodeCount => Episodes?.Count ?? 0;
    }
}

