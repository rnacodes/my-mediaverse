using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Podcasts;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Outcome of a from-feed import: the series, whether it was newly inserted, whether its feed was
    /// read, and a user-facing warning when the series was saved without feed details.
    /// </summary>
    public record PodcastFeedImportResult(PodcastSeries Series, bool Created, bool FeedRead, string? WarningMessage);

    /// <summary>A directory search result, with the id of the library series it already matches (if any).</summary>
    public record PodcastDirectorySearchHit(DirectoryPodcast Podcast, Guid? ExistingSeriesId);

    /// <summary>
    /// Adds podcast series from their feeds ("the directory resolves, the feed fills") and searches the
    /// podcast directory.
    /// </summary>
    public interface IPodcastFeedImportService
    {
        /// <summary>
        /// Imports a series from its feed URL, or from an Apple Podcasts id resolved to a feed through the
        /// directory. Returns the existing series when one already matches. When the feed cannot be read
        /// the series is still saved as a stub, with a warning.
        /// Throws <see cref="ArgumentException"/> for missing or invalid input and
        /// <see cref="KeyNotFoundException"/> when the directory has no show with the Apple id.
        /// </summary>
        Task<PodcastFeedImportResult> ImportSeriesFromFeedAsync(
            string? feedUrl, string? applePodcastsId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches the podcast directory. Throws <see cref="ArgumentException"/> for a blank term.
        /// </summary>
        Task<IReadOnlyList<PodcastDirectorySearchHit>> SearchDirectoryAsync(
            string term, int limit, CancellationToken cancellationToken = default);
    }
}
