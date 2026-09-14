namespace MyMediaVerse.Shared.DTOs.Podcasts
{
    /// <summary>
    /// A show as a podcast directory (Apple Podcasts, Podcast Index) describes it. Directories
    /// find shows and resolve their feeds; the feed itself stays the source of stored metadata.
    /// </summary>
    public record DirectoryPodcast
    {
        /// <summary><see cref="Source"/> value for the Apple Podcasts directory.</summary>
        public const string AppleSource = "apple";

        public string Title { get; init; } = string.Empty;
        public string? Publisher { get; init; }
        public string? FeedUrl { get; init; }
        public string? ApplePodcastsId { get; init; }
        public long? PodcastIndexId { get; init; }
        public string? ArtworkUrl { get; init; }

        /// <summary>Genre names as the directory lists them (not yet normalized).</summary>
        public IReadOnlyList<string> Genres { get; init; } = Array.Empty<string>();

        public int? EpisodeCount { get; init; }

        /// <summary>The show's page in the directory (e.g. its Apple Podcasts page).</summary>
        public string? StoreUrl { get; init; }

        /// <summary>UTC release date of the newest episode, when the directory reports it.</summary>
        public DateTime? LatestReleaseDate { get; init; }

        /// <summary>Which directory produced this entry, e.g. <see cref="AppleSource"/>.</summary>
        public string Source { get; init; } = string.Empty;
    }
}
