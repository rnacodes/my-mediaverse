using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a YouTube Channel as a first-class media entity.
    /// Channels inherit from BaseMediaItem, making them independently organizable in Mixlists.
    /// Videos can be linked to channels via the ChannelId foreign key.
    /// </summary>
    public class YouTubeChannel : BaseMediaItem
    {
        /// <summary>
        /// The unique YouTube channel ID from the YouTube API (e.g., "UC_x5XG1OV2P6uZZ5FSM9Ttw")
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string ChannelExternalId { get; set; }
        
        /// <summary>
        /// Custom YouTube URL for the channel (e.g., "@username" or "c/channelname")
        /// </summary>
        [StringLength(200)]
        public string? CustomUrl { get; set; }
        
        /// <summary>
        /// Number of subscribers to the channel (snapshot at time of last sync)
        /// </summary>
        public long? SubscriberCount { get; set; }
        
        /// <summary>
        /// Total number of videos published on the channel (snapshot at time of last sync)
        /// </summary>
        public long? VideoCount { get; set; }
        
        /// <summary>
        /// Total view count across all channel videos (snapshot at time of last sync)
        /// </summary>
        public long? ViewCount { get; set; }
        
        /// <summary>
        /// The playlist ID for the channel's uploads playlist (used for syncing videos)
        /// This is retrieved from YouTube API's channels.list with contentDetails part
        /// </summary>
        [StringLength(100)]
        public string? UploadsPlaylistId { get; set; }
        
        /// <summary>
        /// Country associated with the channel (ISO 3166-1 alpha-2 code, e.g., "US")
        /// </summary>
        [StringLength(10)]
        public string? Country { get; set; }
        
        /// <summary>
        /// When the YouTube channel was created/published
        /// </summary>
        public DateTime? PublishedAt { get; set; }
        
        /// <summary>
        /// Last time channel metadata was synced from YouTube API
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }
        
        /// <summary>
        /// Navigation property to all videos associated with this channel
        /// </summary>
        public ICollection<Video> Videos { get; set; } = new List<Video>();
    }
}

