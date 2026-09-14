namespace MyMediaVerse.Shared.DTOs.Podcasts
{
    /// <summary>
    /// A parsed podcast RSS feed: the channel, up to the requested number of episodes, and how many
    /// items the feed holds in total.
    /// </summary>
    public record PodcastFeed
    {
        public required FeedSeries Series { get; init; }
        public IReadOnlyList<FeedEpisode> Episodes { get; init; } = Array.Empty<FeedEpisode>();

        /// <summary>Every &lt;item&gt; in the feed, including any beyond the episodes returned.</summary>
        public int TotalItemCount { get; init; }
    }

    /// <summary>
    /// Channel-level metadata. Text fields are plain text (HTML stripped) and blank values are null.
    /// </summary>
    public record FeedSeries
    {
        public string? Title { get; init; }
        public string? Description { get; init; }

        /// <summary>itunes:author, falling back to the itunes:owner name.</summary>
        public string? Publisher { get; init; }

        /// <summary>itunes:image, falling back to the RSS &lt;image&gt; url.</summary>
        public string? ImageUrl { get; init; }

        public string? Link { get; init; }
        public string? Language { get; init; }

        /// <summary>The podcast:guid, which stays the same when a show moves feed hosts.</summary>
        public string? PodcastGuid { get; init; }

        /// <summary>itunes:category names (nested subcategories included), in feed order, without duplicates.</summary>
        public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();

        public bool? Explicit { get; init; }
    }

    /// <summary>
    /// One &lt;item&gt;. Only audio and video enclosures are kept; dates are UTC and null when the feed's
    /// value could not be parsed.
    /// </summary>
    public record FeedEpisode
    {
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? EnclosureUrl { get; init; }
        public string? EnclosureType { get; init; }
        public DateTime? PublishedAt { get; init; }
        public string? Guid { get; init; }
        public int? DurationSeconds { get; init; }
        public string? ImageUrl { get; init; }
        public int? EpisodeNumber { get; init; }
        public int? SeasonNumber { get; init; }
        public string? Link { get; init; }
    }
}
