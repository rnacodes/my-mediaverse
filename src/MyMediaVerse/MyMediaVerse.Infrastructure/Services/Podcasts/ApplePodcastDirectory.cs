using System.Globalization;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Podcasts
{
    /// <summary>
    /// The Apple Podcasts directory, over the free, keyless iTunes Search and Lookup APIs.
    /// Apple cannot look a show up by feed URL, so <see cref="LookupByFeedUrlAsync"/> always
    /// returns null.
    /// </summary>
    public class ApplePodcastDirectory : IPodcastDirectory
    {
        // Apple files every show under the catch-all "Podcasts" genre as well as its real ones.
        private const string CatchAllGenre = "Podcasts";

        private readonly IItunesLookupClient _itunesClient;

        public ApplePodcastDirectory(IItunesLookupClient itunesClient)
        {
            _itunesClient = itunesClient;
        }

        public async Task<IReadOnlyList<DirectoryPodcast>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default)
        {
            var results = await _itunesClient.SearchPodcastsAsync(term, limit, cancellationToken);
            return results.Select(Map).OfType<DirectoryPodcast>().ToList();
        }

        public async Task<DirectoryPodcast?> LookupByAppleIdAsync(string applePodcastsId, CancellationToken cancellationToken = default)
        {
            var result = await _itunesClient.GetPodcastByCollectionIdAsync(applePodcastsId, cancellationToken);
            return result == null ? null : Map(result);
        }

        public Task<DirectoryPodcast?> LookupByFeedUrlAsync(string feedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult<DirectoryPodcast?>(null);

        /// <summary>Maps an iTunes entry, or returns null for one with no id or title.</summary>
        public static DirectoryPodcast? Map(ItunesPodcastDto podcast)
        {
            if (podcast.CollectionId <= 0 || string.IsNullOrWhiteSpace(podcast.CollectionName))
                return null;

            var genres = podcast.Genres is { Count: > 0 }
                ? podcast.Genres
                : new List<string?> { podcast.PrimaryGenreName }.OfType<string>().ToList();

            return new DirectoryPodcast
            {
                Title = podcast.CollectionName.Trim(),
                Publisher = BlankToNull(podcast.ArtistName),
                FeedUrl = BlankToNull(podcast.FeedUrl),
                ApplePodcastsId = podcast.CollectionId.ToString(CultureInfo.InvariantCulture),
                ArtworkUrl = BlankToNull(podcast.ArtworkUrl600),
                Genres = genres
                    .Where(g => !string.IsNullOrWhiteSpace(g) && !string.Equals(g.Trim(), CatchAllGenre, StringComparison.OrdinalIgnoreCase))
                    .Select(g => g.Trim())
                    .ToList(),
                EpisodeCount = podcast.TrackCount,
                StoreUrl = BlankToNull(podcast.CollectionViewUrl),
                LatestReleaseDate = podcast.ReleaseDate?.ToUniversalTime(),
                Source = DirectoryPodcast.AppleSource
            };
        }

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
