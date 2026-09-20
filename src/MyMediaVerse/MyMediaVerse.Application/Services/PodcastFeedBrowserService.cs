using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class PodcastFeedBrowserService : IPodcastFeedBrowserService
    {
        public const int MaxPageSize = 100;
        public const int DescriptionPreviewLength = 500;

        private readonly IApplicationDbContext _context;
        private readonly IPodcastFeedReader _feedReader;
        private readonly IPodcastService _podcastService;
        private readonly IMemoryCache _cache;
        private readonly PodcastSyncOptions _options;
        private readonly ILogger<PodcastFeedBrowserService> _logger;

        public PodcastFeedBrowserService(
            IApplicationDbContext context,
            IPodcastFeedReader feedReader,
            IPodcastService podcastService,
            IMemoryCache cache,
            IOptions<PodcastSyncOptions> options,
            ILogger<PodcastFeedBrowserService> logger)
        {
            _context = context;
            _feedReader = feedReader;
            _podcastService = podcastService;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<PodcastFeedEpisodesPageDto> GetFeedEpisodesAsync(
            Guid seriesId, int offset, int limit, bool refresh = false, CancellationToken cancellationToken = default)
        {
            var series = await LoadSeriesAsync(seriesId, cancellationToken);
            var (feed, fetchedAt) = await GetFeedAsync(series, refresh, cancellationToken);

            offset = Math.Max(0, offset);
            limit = Math.Clamp(limit, 1, MaxPageSize);

            var index = await PodcastEpisodeIdentityIndex.BuildAsync(_context.PodcastEpisodes, series.Id, cancellationToken);

            return new PodcastFeedEpisodesPageDto
            {
                SeriesId = series.Id,
                FeedItemCount = feed.TotalItemCount,
                Offset = offset,
                Limit = limit,
                FetchedAt = fetchedAt,
                Items = feed.Episodes.Skip(offset).Take(limit).Select(item => new PodcastFeedEpisodeItemDto
                {
                    Guid = item.Guid,
                    Title = item.Title,
                    PublishedAt = item.PublishedAt,
                    DurationSeconds = item.DurationSeconds,
                    AudioUrl = item.EnclosureUrl,
                    ImageUrl = item.ImageUrl,
                    EpisodeNumber = item.EpisodeNumber,
                    SeasonNumber = item.SeasonNumber,
                    Description = Preview(item.Description),
                    Importable = PodcastEpisodeFeedMapper.IsImportable(item),
                    ExistingEpisodeId = index.Find(item.Guid, item.EnclosureUrl, item.Title, item.PublishedAt)?.Id
                }).ToList()
            };
        }

        public async Task<PodcastEpisodeCreationResult> ImportEpisodeFromFeedAsync(
            Guid seriesId, string? guid, string? audioUrl, CancellationToken cancellationToken = default)
        {
            guid = string.IsNullOrWhiteSpace(guid) ? null : guid.Trim();
            var audioKey = UrlNormalizer.GetComparisonKey(audioUrl);
            if (guid == null && string.IsNullOrEmpty(audioKey))
                throw new ArgumentException("Provide the episode's guid or audio URL.");

            var series = await LoadSeriesAsync(seriesId, cancellationToken);
            var (feed, _) = await GetFeedAsync(series, refresh: false, cancellationToken);

            var item = (guid != null ? feed.Episodes.FirstOrDefault(e => e.Guid == guid) : null)
                ?? (!string.IsNullOrEmpty(audioKey)
                    ? feed.Episodes.FirstOrDefault(e => UrlNormalizer.GetComparisonKey(e.EnclosureUrl) == audioKey)
                    : null);

            var dto = item == null ? null : PodcastEpisodeFeedMapper.ToCreateDto(item, series);
            if (dto == null)
                throw new KeyNotFoundException("That episode is not in the series' feed, or it has no audio to import.");

            return await _podcastService.CreatePodcastEpisodeAsync(dto);
        }

        private async Task<PodcastSeries> LoadSeriesAsync(Guid seriesId, CancellationToken cancellationToken)
        {
            var series = await _context.PodcastSeries
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == seriesId, cancellationToken)
                ?? throw new KeyNotFoundException($"Podcast series with ID {seriesId} not found.");

            if (!UrlNormalizer.IsValid(series.RssFeedUrl))
                throw new InvalidOperationException("This podcast series has no RSS feed URL.");

            return series;
        }

        private async Task<(PodcastFeed Feed, DateTime FetchedAt)> GetFeedAsync(
            PodcastSeries series, bool refresh, CancellationToken cancellationToken)
        {
            var key = CacheKey(series);
            if (!refresh && _cache.TryGetValue(key, out CachedFeed? cached) && cached != null)
                return (cached.Feed, cached.FetchedAt);

            var feed = await _feedReader.ReadAsync(series.RssFeedUrl!, cancellationToken: cancellationToken);
            var entry = new CachedFeed(feed, DateTime.UtcNow);

            var minutes = Math.Max(0, _options.FeedCacheMinutes);
            if (minutes > 0)
                _cache.Set(key, entry, TimeSpan.FromMinutes(minutes));

            _logger.LogInformation("Read feed for podcast series {Id} ({Items} items)", series.Id, feed.TotalItemCount);
            return (entry.Feed, entry.FetchedAt);
        }

        // Keyed by the normalized feed so http/https variants of one feed share an entry.
        private static string CacheKey(PodcastSeries series) =>
            $"podcastfeed:{series.FeedUrlKey ?? UrlNormalizer.GetComparisonKey(series.RssFeedUrl)}";

        private static string? Preview(string? description) =>
            description == null || description.Length <= DescriptionPreviewLength
                ? description
                : description[..DescriptionPreviewLength].TrimEnd() + "…";

        private sealed record CachedFeed(PodcastFeed Feed, DateTime FetchedAt);
    }
}
