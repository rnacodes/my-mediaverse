using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    public class Video : BaseMediaItem
    {
        // Video type - Series or Episode
        [Required]
        public VideoType VideoType { get; set; }
        
        // For Episodes: Foreign Key to parent series
        public Guid? ParentVideoId { get; set; }
        public Video? ParentVideo { get; set; }
        
        // For Series: Navigation property to episodes
        public ICollection<Video> Episodes { get; set; } = new List<Video>();
        
        // Video-specific properties
        [Required]
        [StringLength(100)]
        public required string Platform { get; set; } // YouTube, Vimeo, Twitch, etc.
        
        /// <summary>
        /// Foreign Key to the YouTubeChannel entity (for YouTube videos only)
        /// Links this video to its parent channel, enabling channel-based organization
        /// </summary>
        public Guid? ChannelId { get; set; }
        
        /// <summary>
        /// Navigation property to the associated YouTube channel
        /// </summary>
        public YouTubeChannel? Channel { get; set; }
        
        /// <summary>
        /// Navigation property to the junction table for many-to-many relationship with YouTubePlaylists
        /// </summary>
        public ICollection<YouTubePlaylistVideo> PlaylistVideos { get; set; } = new List<YouTubePlaylistVideo>();
        
        [Range(0, int.MaxValue, ErrorMessage = "Length must be a positive number")]
        public int LengthInSeconds { get; set; } = 0;
        
        // External API identifier (for imported videos)
        [StringLength(200)]
        public string? ExternalId { get; set; }
        
        /// <summary>
        /// Gets the thumbnail for this video, inheriting from parent series if it's an episode
        /// </summary>
        public string? GetEffectiveThumbnail()
        {
            // Return video-specific thumbnail if set, otherwise inherit from parent series
            return !string.IsNullOrEmpty(Thumbnail) ? Thumbnail : ParentVideo?.Thumbnail;
        }
        
        /// <summary>
        /// Gets whether this video is a series (has episodes)
        /// </summary>
        public bool IsSeries => VideoType == Domain.Entities.VideoType.Series;
        
        /// <summary>
        /// Gets whether this video is an episode
        /// </summary>
        public bool IsEpisode => VideoType == Domain.Entities.VideoType.Episode;
    }
    
    public enum VideoType
    {
        Series,
        Episode,
        Channel
    }
}