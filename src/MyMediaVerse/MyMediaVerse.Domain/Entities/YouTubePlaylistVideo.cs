using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Junction table for the many-to-many relationship between YouTubePlaylists and Videos.
    /// A playlist can contain many videos, and a video can belong to many playlists.
    /// </summary>
    public class YouTubePlaylistVideo
    {
        /// <summary>
        /// Foreign key to the YouTubePlaylist
        /// </summary>
        [Required]
        public Guid YouTubePlaylistId { get; set; }
        
        /// <summary>
        /// Navigation property to the YouTubePlaylist
        /// </summary>
        public YouTubePlaylist YouTubePlaylist { get; set; } = null!;
        
        /// <summary>
        /// Foreign key to the Video
        /// </summary>
        [Required]
        public Guid VideoId { get; set; }
        
        /// <summary>
        /// Navigation property to the Video
        /// </summary>
        public Video Video { get; set; } = null!;
        
        /// <summary>
        /// The position/order of the video in the playlist (0-indexed)
        /// Preserves the playlist order from YouTube
        /// </summary>
        public int? Position { get; set; }
        
        /// <summary>
        /// When this video was added to the playlist
        /// </summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When this video was added to the YouTube playlist (from YouTube API)
        /// </summary>
        public DateTime? VideoPublishedAt { get; set; }
    }
}

