using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.Itunes;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Podcasts
{
    /// <summary>
    /// The Apple Podcasts directory, over the free, keyless iTunes Search and Lookup APIs.
    /// Apple cannot look a show up by feed URL, so <see cref="LookupByFeedUrlAsync"/> always
    /// returns null. Results are cached (Apple asks callers to cache, and allows about 20 calls a
    /// minute); a lookup that found nothing is cached for a shorter time.
    /// </summary>
    public class ApplePodcastDirectory : IPodcastDirectory
    {
        // Apple files every show under the catch-all "Podcasts" genre as well as its real ones.
        private const string CatchAllGenre = "Podcasts";

        private readonly IItunesLookupClient _itunesClient;
        private readonly IMemoryCache _cache;
        private readonly PodcastDirectoryOptions _options;

        public ApplePodcastDirectory(IItunesLookupClient itunesClient, IMemoryCache cache, IOptions<PodcastDirectoryOptions> options)
        {
            _itunesClient = itunesClient;
            _cache = cache;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<DirectoryPodcast>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default)
        {
            var key = $"podcastdir:apple:search:{term.Trim().ToLowerInvariant()}:{limit}";
            if (_cache.TryGetValue(key, out IReadOnlyList<DirectoryPodcast>? cached) && cached != null)
                return cached;

            var results = await _itunesClient.SearchPodcastsAsync(term, limit, cancellationToken);
            IReadOnlyList<DirectoryPodcast> mapped = results.Select(Map).OfType<DirectoryPodcast>().ToList();

            _cache.Set(key, mapped, mapped.Count > 0 ? FoundTtl : MissTtl);
            return mapped;
        }

        public async Task<DirectoryPodcast?> LookupByAppleIdAsync(string applePodcastsId, CancellationToken cancellationToken = default)
        {
            var key = $"podcastdir:apple:id:{applePodcastsId.Trim()}";
            if (_cache.TryGetValue(key, out CachedLookup? cached) && cached != null)
                return cached.Podcast;

            var result = await _itunesClient.GetPodcastByCollectionIdAsync(applePodcastsId, cancellationToken);
            var podcast = result == null ? null : Map(result);

            _cache.Set(key, new CachedLookup(podcast), podcast != null ? FoundTtl : MissTtl);
            return podcast;
        }

        private TimeSpan FoundTtl => TimeSpan.FromHours(Math.Max(0, _options.AppleCacheHours));
        private TimeSpan MissTtl => TimeSpan.FromMinutes(Math.Max(0, _options.AppleMissCacheMinutes));

        // Wraps a lookup result so a cached "not found" is told apart from a cache miss.
        private sealed record CachedLookup(DirectoryPodcast? Podcast);

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
