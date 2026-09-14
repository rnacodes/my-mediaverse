using MyMediaVerse.Shared.DTOs.Itunes;

namespace MyMediaVerse.Shared.Interfaces
{
    public interface IItunesLookupClient
    {
        /// <summary>
        /// Looks up a podcast by its Apple Podcasts (iTunes) collection id.
        /// Returns null when Apple has no matching podcast.
        /// </summary>
        Task<ItunesPodcastDto?> GetPodcastByCollectionIdAsync(string collectionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches Apple Podcasts for shows matching <paramref name="term"/>, returning at most
        /// <paramref name="limit"/> podcasts.
        /// </summary>
        Task<IReadOnlyList<ItunesPodcastDto>> SearchPodcastsAsync(string term, int limit, CancellationToken cancellationToken = default);
    }
}
