using System.Globalization;
using MyMediaVerse.Shared.DTOs.PodcastIndex;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Podcasts
{
    /// <summary>
    /// The Podcast Index directory. Unlike Apple it can look a show up by feed URL, and it knows feeds Apple
    /// does not list, so it backs up Apple rather than replacing it.
    /// </summary>
    public class PodcastIndexDirectory : IPodcastDirectory
    {
        private readonly IPodcastIndexClient _client;

        public PodcastIndexDirectory(IPodcastIndexClient client)
        {
            _client = client;
        }

        public async Task<IReadOnlyList<DirectoryPodcast>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default)
        {
            var feeds = await _client.SearchByTermAsync(term, limit, cancellationToken);
            return feeds.Where(f => !f.Dead).Select(Map).OfType<DirectoryPodcast>().ToList();
        }

        public async Task<DirectoryPodcast?> LookupByAppleIdAsync(string applePodcastsId, CancellationToken cancellationToken = default)
        {
            if (!long.TryParse(applePodcastsId, NumberStyles.None, CultureInfo.InvariantCulture, out var itunesId))
                return null;

            var feed = await _client.GetByItunesIdAsync(itunesId, cancellationToken);
            return feed == null ? null : Map(feed);
        }

        public async Task<DirectoryPodcast?> LookupByFeedUrlAsync(string feedUrl, CancellationToken cancellationToken = default)
        {
            var feed = await _client.GetByFeedUrlAsync(feedUrl, cancellationToken);
            return feed == null ? null : Map(feed);
        }

        /// <summary>Maps a Podcast Index feed, or returns null for one with no id or title.</summary>
        public static DirectoryPodcast? Map(PodcastIndexFeedDto feed)
        {
            if (feed.Id <= 0 || string.IsNullOrWhiteSpace(feed.Title))
                return null;

            return new DirectoryPodcast
            {
                Title = feed.Title,
                Publisher = feed.Author ?? feed.OwnerName,
                FeedUrl = feed.Url,
                ApplePodcastsId = feed.ItunesId?.ToString(CultureInfo.InvariantCulture),
                PodcastIndexId = feed.Id,
                ArtworkUrl = feed.Artwork ?? feed.Image,
                Genres = feed.Categories,
                EpisodeCount = feed.EpisodeCount,
                LatestReleaseDate = feed.NewestItemPublishedAt,
                Source = DirectoryPodcast.PodcastIndexSource
            };
        }
    }
}
