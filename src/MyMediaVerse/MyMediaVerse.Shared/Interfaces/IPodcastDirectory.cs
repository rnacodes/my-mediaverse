using MyMediaVerse.Shared.DTOs.Podcasts;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// A podcast directory: finds shows by name and resolves directory ids to feed URLs.
    /// </summary>
    public interface IPodcastDirectory
    {
        /// <summary>Searches the directory for shows matching <paramref name="term"/>.</summary>
        Task<IReadOnlyList<DirectoryPodcast>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default);

        /// <summary>Looks up a show by its Apple Podcasts id. Returns null when the directory has no such show.</summary>
        Task<DirectoryPodcast?> LookupByAppleIdAsync(string applePodcastsId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Looks up a show by its feed URL. Returns null when the directory has no such show or
        /// cannot look shows up by feed.
        /// </summary>
        Task<DirectoryPodcast?> LookupByFeedUrlAsync(string feedUrl, CancellationToken cancellationToken = default);
    }
}
