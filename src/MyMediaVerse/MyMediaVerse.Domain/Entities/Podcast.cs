using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    public class Podcast : BaseMediaItem
    {
        // Podcast type - Series or Episode
        [Required]
        public PodcastType PodcastType { get; set; }
        
        // For Episodes: Foreign Key to parent series
        public Guid? ParentPodcastId { get; set; }
        public Podcast? ParentPodcast { get; set; }
        
        // For Series: Navigation property to episodes
        public ICollection<Podcast> Episodes { get; set; } = new List<Podcast>();
        
        // Episode-specific properties
        public string? AudioLink { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int DurationInSeconds { get; set; } = 0;
        
        // External API identifier (for imported podcasts)
        public string? ExternalId { get; set; }
        
        // Publisher information
        public string? Publisher { get; set; }
        
        // Subscription tracking (for Series)
        public bool IsSubscribed { get; set; } = false;
        
        // Last sync date for checking new episodes (for Series)
        public DateTime? LastSyncDate { get; set; }
        
        /// <summary>
        /// Gets the thumbnail for this podcast, inheriting from parent series if it's an episode
        /// </summary>
        public string? GetEffectiveThumbnail()
        {
            // Return podcast-specific thumbnail if set, otherwise inherit from parent series
            return !string.IsNullOrEmpty(Thumbnail) ? Thumbnail : ParentPodcast?.Thumbnail;
        }
        
        /// <summary>
        /// Gets whether this podcast is a series (has episodes)
        /// </summary>
        public bool IsSeries => PodcastType == PodcastType.Series;
        
        /// <summary>
        /// Gets whether this podcast is an episode
        /// </summary>
        public bool IsEpisode => PodcastType == PodcastType.Episode;
    }
    
    public enum PodcastType
    {
        Series,
        Episode
    }
}
