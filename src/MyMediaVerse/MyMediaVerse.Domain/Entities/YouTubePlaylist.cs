using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a YouTube Playlist as a first-class media entity.
    /// Playlists inherit from BaseMediaItem, making them independently organizable in Mixlists.
    /// Videos can be linked to playlists via the YouTubePlaylistVideo junction table.
    /// </summary>
    public class YouTubePlaylist : BaseMediaItem
    {
        /// <summary>
        /// The unique YouTube playlist ID from the YouTube API (e.g., "PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf")
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string PlaylistExternalId { get; set; }
        
        /// <summary>
        /// The YouTube channel ID that owns this playlist (external YouTube ID)
        /// </summary>
        [StringLength(100)]
        public string? ChannelExternalId { get; set; }
        
        /// <summary>
        /// Foreign key to the YouTubeChannel entity if the channel has been imported
        /// </summary>
        public Guid? LinkedYouTubeChannelId { get; set; }
        
        /// <summary>
        /// Navigation property to the associated YouTube channel (if imported)
        /// </summary>
        public YouTubeChannel? LinkedYouTubeChannel { get; set; }
        
        /// <summary>
        /// Number of videos in the playlist (snapshot at time of last sync)
        /// </summary>
        public int? VideoCount { get; set; }
        
        /// <summary>
        /// When the YouTube playlist was created/published
        /// </summary>
        public DateTime? PublishedAt { get; set; }
        
        /// <summary>
        /// Last time playlist metadata was synced from YouTube API
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }
        
        /// <summary>
        /// Privacy status of the playlist (public, private, unlisted)
        /// </summary>
        [StringLength(50)]
        public string? PrivacyStatus { get; set; }
        
        /// <summary>
        /// Navigation property to the junction table for many-to-many relationship with Videos
        /// </summary>
        public ICollection<YouTubePlaylistVideo> PlaylistVideos { get; set; } = new List<YouTubePlaylistVideo>();
    }
}

