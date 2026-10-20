namespace MyMediaVerse.Shared.DTOs.YouTube
{
    /// <summary>
    /// Whether the owner's YouTube account is connected. While the Google OAuth app is in
    /// Testing, Google ends each connection seven days after consent, so the status carries the
    /// date by which to reconnect.
    /// </summary>
    public class YouTubeAuthStatusDto
    {
        /// <summary>False when the OAuth client id, secret or redirect URI is not set.</summary>
        public bool Configured { get; set; }

        public bool Connected { get; set; }

        public string? ChannelTitle { get; set; }

        public DateTime? ConnectedAt { get; set; }

        public DateTime? ReconnectBy { get; set; }
    }

    /// <summary>One of the owner's playlists, as listed for import.</summary>
    public class YouTubeMyPlaylistDto
    {
        public required string PlaylistId { get; set; }

        public string? Title { get; set; }

        /// <summary>public, unlisted or private.</summary>
        public string? PrivacyStatus { get; set; }

        public int? ItemCount { get; set; }

        public string? ThumbnailUrl { get; set; }

        /// <summary>The MMV playlist id when this playlist is already in the library.</summary>
        public Guid? ImportedId { get; set; }
    }
}
