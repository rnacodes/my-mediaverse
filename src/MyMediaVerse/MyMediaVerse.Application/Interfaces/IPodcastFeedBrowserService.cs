using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Lets a user page through a podcast series' feed and import single episodes from it, typically the
    /// back catalog that episode sync leaves alone. Parsed feeds are cached briefly so paging does not
    /// refetch the feed.
    /// </summary>
    public interface IPodcastFeedBrowserService
    {
        /// <summary>
        /// Returns one page of feed items. Throws <see cref="KeyNotFoundException"/> when the series does
        /// not exist, <see cref="InvalidOperationException"/> when it has no feed URL, and
        /// <see cref="Shared.Exceptions.PodcastFeedException"/> when the feed cannot be read.
        /// </summary>
        Task<PodcastFeedEpisodesPageDto> GetFeedEpisodesAsync(
            Guid seriesId, int offset, int limit, bool refresh = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports one feed item, found by guid or audio URL, or returns the library episode it already is.
        /// Throws <see cref="ArgumentException"/> when neither is given, <see cref="KeyNotFoundException"/>
        /// when the series or item is not found (or the item has nothing to import), plus the feed
        /// exceptions of <see cref="GetFeedEpisodesAsync"/>.
        /// </summary>
        Task<PodcastEpisodeCreationResult> ImportEpisodeFromFeedAsync(
            Guid seriesId, string? guid, string? audioUrl, CancellationToken cancellationToken = default);
    }
}
