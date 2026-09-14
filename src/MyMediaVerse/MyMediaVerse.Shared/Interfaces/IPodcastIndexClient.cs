using MyMediaVerse.Shared.DTOs.PodcastIndex;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// The Podcast Index API (free, keyed). Every call is signed with the configured key and secret; when
    /// they are not configured, calls return nothing without reaching the API.
    /// </summary>
    public interface IPodcastIndexClient
    {
        /// <summary>True when an API key and secret are configured.</summary>
        bool IsConfigured { get; }

        Task<IReadOnlyList<PodcastIndexFeedDto>> SearchByTermAsync(string term, int max, CancellationToken cancellationToken = default);

        /// <summary>Returns null when Podcast Index does not know the feed.</summary>
        Task<PodcastIndexFeedDto?> GetByFeedUrlAsync(string feedUrl, CancellationToken cancellationToken = default);

        /// <summary>Returns null when Podcast Index has no feed with this Apple Podcasts id.</summary>
        Task<PodcastIndexFeedDto?> GetByItunesIdAsync(long itunesId, CancellationToken cancellationToken = default);
    }
}
