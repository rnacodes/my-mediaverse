using System.ComponentModel.DataAnnotations;

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
        
        // External API identifier (for imported podcasts from ListenNotes)
        [StringLength(200)]
        public string? ExternalId { get; set; }
        
        // Subscription tracking
        public bool IsSubscribed { get; set; } = false;
        
        // Last sync date for checking new episodes
        public DateTime? LastSyncDate { get; set; }
        
        // Total episodes count (from API or calculated)
        public int TotalEpisodes { get; set; } = 0;
        
        // Navigation property to episodes
        public ICollection<PodcastEpisode> Episodes { get; set; } = new List<PodcastEpisode>();
        
        /// <summary>
        /// Gets the count of episodes in this series
        /// </summary>
        public int EpisodeCount => Episodes?.Count ?? 0;
    }
}

