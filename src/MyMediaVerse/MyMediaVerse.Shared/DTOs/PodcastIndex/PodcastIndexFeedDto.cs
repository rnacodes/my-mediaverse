namespace MyMediaVerse.Shared.DTOs.PodcastIndex
{
    /// <summary>
    /// A feed as Podcast Index (https://api.podcastindex.org) describes it. Parsed by hand from the API's
    /// JSON (field names noted per property) because the API is loose with types: categories arrive as an
    /// id-to-name object or null, and not-found lookups return an empty array in place of the feed object.
    /// </summary>
    public record PodcastIndexFeedDto
    {
        /// <summary><c>id</c>: the Podcast Index feed id.</summary>
        public long Id { get; init; }

        /// <summary><c>podcastGuid</c>: the feed's podcast:guid.</summary>
        public string? PodcastGuid { get; init; }

        /// <summary><c>title</c>.</summary>
        public string? Title { get; init; }

        /// <summary><c>url</c>: the current feed URL.</summary>
        public string? Url { get; init; }

        /// <summary><c>link</c>: the show's website.</summary>
        public string? Link { get; init; }

        /// <summary><c>description</c>.</summary>
        public string? Description { get; init; }

        /// <summary><c>author</c>.</summary>
        public string? Author { get; init; }

        /// <summary><c>ownerName</c>.</summary>
        public string? OwnerName { get; init; }

        /// <summary><c>image</c>: the channel image.</summary>
        public string? Image { get; init; }

        /// <summary><c>artwork</c>: the best artwork Podcast Index found.</summary>
        public string? Artwork { get; init; }

        /// <summary><c>itunesId</c>: the Apple Podcasts id, when known.</summary>
        public long? ItunesId { get; init; }

        /// <summary><c>language</c>.</summary>
        public string? Language { get; init; }

        /// <summary>Names from the <c>categories</c> object.</summary>
        public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();

        /// <summary><c>episodeCount</c>.</summary>
        public int? EpisodeCount { get; init; }

        /// <summary><c>dead</c>: true once Podcast Index has given up on the feed.</summary>
        public bool Dead { get; init; }

        /// <summary><c>newestItemPublishTime</c> (unix seconds), as UTC.</summary>
        public DateTime? NewestItemPublishedAt { get; init; }
    }
}
