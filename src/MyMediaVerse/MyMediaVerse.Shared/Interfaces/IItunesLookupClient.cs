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
    }
}
