using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Fetches and parses a podcast's RSS feed, the source of truth for series metadata and episodes.
    /// </summary>
    public interface IPodcastFeedReader
    {
        /// <summary>
        /// Reads the feed at <paramref name="feedUrl"/>, returning at most <paramref name="maxItems"/>
        /// episodes (pass 0 for channel metadata and the item count only).
        /// Throws <see cref="PodcastFeedException"/> when the feed cannot be fetched or parsed.
        /// </summary>
        Task<PodcastFeed> ReadAsync(string feedUrl, int maxItems = int.MaxValue, CancellationToken cancellationToken = default);
    }
}
