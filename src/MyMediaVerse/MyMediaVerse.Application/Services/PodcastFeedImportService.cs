using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class PodcastFeedImportService : IPodcastFeedImportService
    {
        public const int DefaultSearchLimit = 20;
        public const int MaxSearchLimit = 50;

        private readonly IApplicationDbContext _context;
        private readonly IPodcastDirectory _directory;
        private readonly IPodcastFeedReader _feedReader;
        private readonly ILogger<PodcastFeedImportService> _logger;

        public PodcastFeedImportService(
            IApplicationDbContext context,
            IPodcastDirectory directory,
            IPodcastFeedReader feedReader,
            ILogger<PodcastFeedImportService> logger)
        {
            _context = context;
            _directory = directory;
            _feedReader = feedReader;
            _logger = logger;
        }

        private IQueryable<PodcastSeries> TrackedSeriesWithTags => _context.PodcastSeries
            .Include(p => p.Topics)
            .Include(p => p.Genres);

        public async Task<PodcastFeedImportResult> ImportSeriesFromFeedAsync(
            string? feedUrl, string? applePodcastsId, CancellationToken cancellationToken = default)
        {
            feedUrl = BlankToNull(feedUrl);
            var appleId = BlankToNull(applePodcastsId);

            if (feedUrl == null && appleId == null)
                throw new ArgumentException("Provide a feed URL or an Apple Podcasts id.");
            if (feedUrl != null && !UrlNormalizer.IsValid(feedUrl))
                throw new ArgumentException("The feed URL must be an absolute http or https URL.");
            if (appleId != null && (appleId.Length > 50 || !appleId.All(char.IsAsciiDigit)))
                throw new ArgumentException("The Apple Podcasts id must be a number.");

            // The directory resolves: an Apple id alone is turned into a feed URL first.
            DirectoryPodcast? directoryHit = null;
            if (feedUrl == null)
            {
                directoryHit = await _directory.LookupByAppleIdAsync(appleId!, cancellationToken)
                    ?? throw new KeyNotFoundException($"Apple Podcasts has no show with id {appleId}.");
                feedUrl = UrlNormalizer.IsValid(directoryHit.FeedUrl) ? directoryHit.FeedUrl!.Trim() : null;
            }

            // Probe before fetching, so a show already in the library costs no feed request.
            var identity = new PodcastSeriesIdentity { FeedUrl = feedUrl, ApplePodcastsId = appleId };
            var existing = await PodcastSeriesDuplicateFinder.FindExistingAsync(TrackedSeriesWithTags, identity);
            if (existing != null)
            {
                if (await PodcastSeriesDuplicateFinder.AbsorbIdentityAsync(_context.PodcastSeries, existing, identity))
                    await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Podcast feed import matched existing series {Id}", existing.Id);
                return new PodcastFeedImportResult(existing, Created: false, FeedRead: false, WarningMessage: null);
            }

            PodcastFeed? feed = null;
            string? warning;
            if (feedUrl == null)
            {
                warning = "Apple Podcasts lists no feed for this show, so it was saved with directory details only.";
            }
            else
            {
                try
                {
                    // Channel metadata and the item count only; episodes are added by sync.
                    feed = await _feedReader.ReadAsync(feedUrl, maxItems: 0, cancellationToken);
                    warning = null;
                }
                catch (PodcastFeedException ex)
                {
                    // The reader already logged the underlying exception.
                    _logger.LogWarning("Could not read podcast feed {FeedUrl} ({Reason}); saving a stub", feedUrl, ex.Reason);
                    warning = $"{ex.Message} The show was saved without feed details; enrichment will try the feed again later.";
                }
            }

            var now = DateTime.UtcNow;

            if (feed?.Series.PodcastGuid is { } podcastGuid)
            {
                // The feed's GUID survives host moves, so it can find a show saved under an older feed URL.
                var movedShow = await PodcastSeriesDuplicateFinder.FindExistingAsync(
                    TrackedSeriesWithTags, new PodcastSeriesIdentity { FeedGuid = podcastGuid });
                if (movedShow != null)
                {
                    await PodcastSeriesDuplicateFinder.AbsorbIdentityAsync(
                        _context.PodcastSeries, movedShow, identity with { FeedGuid = podcastGuid });
                    var genres = PodcastFeedMapper.ApplyToSeries(movedShow, feed, directoryHit, fillOnly: true);
                    await AddGenresAsync(movedShow, genres);
                    movedShow.LastEnrichmentAttemptAt = now;
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Podcast feed import matched existing series {Id} by feed GUID", movedShow.Id);
                    return new PodcastFeedImportResult(movedShow, Created: false, FeedRead: true, WarningMessage: null);
                }
            }

            var series = new PodcastSeries
            {
                Title = PodcastFeedMapper.PlaceholderTitle(feedUrl),
                MediaType = MediaType.Podcast,
                DateAdded = now,
                RssFeedUrl = feedUrl,
                FeedUrlKey = feedUrl == null ? null : UrlNormalizer.GetComparisonKey(feedUrl),
                ApplePodcastsId = appleId ?? directoryHit?.ApplePodcastsId,
                PodcastIndexId = directoryHit?.PodcastIndexId,
                IsSubscribed = true,
                LastEnrichmentAttemptAt = now
            };

            IReadOnlyList<string> genreNames;
            if (feed != null)
            {
                genreNames = PodcastFeedMapper.ApplyToSeries(series, feed, directoryHit, fillOnly: false);
                series.MetadataSource = PodcastMetadataSources.Rss;
                series.EnrichedAt = now;
            }
            else
            {
                genreNames = PodcastFeedMapper.ApplyDirectory(series, directoryHit);
                series.MetadataSource = directoryHit != null ? PodcastMetadataSources.Apple : PodcastMetadataSources.Manual;
            }

            await AddGenresAsync(series, genreNames);

            _context.Add(series);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Imported podcast series {Id} ({Title}) from its feed; feed read: {FeedRead}",
                series.Id, series.Title, feed != null);
            return new PodcastFeedImportResult(series, Created: true, FeedRead: feed != null, WarningMessage: warning);
        }

        public async Task<IReadOnlyList<PodcastDirectorySearchHit>> SearchDirectoryAsync(
            string term, int limit, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(term))
                throw new ArgumentException("A search term is required.");

            var results = await _directory.SearchAsync(term.Trim(), Math.Clamp(limit, 1, MaxSearchLimit), cancellationToken);

            var hits = new List<PodcastDirectorySearchHit>(results.Count);
            foreach (var podcast in results)
            {
                var match = await PodcastSeriesDuplicateFinder.FindExistingAsync(
                    _context.PodcastSeries.AsNoTracking(),
                    new PodcastSeriesIdentity
                    {
                        FeedUrl = podcast.FeedUrl,
                        ApplePodcastsId = podcast.ApplePodcastsId,
                        PodcastIndexId = podcast.PodcastIndexId
                    });
                hits.Add(new PodcastDirectorySearchHit(podcast, match?.Id));
            }

            return hits;
        }

        private async Task AddGenresAsync(PodcastSeries series, IEnumerable<string> genreNames)
        {
            var resolver = new GenreResolver(_context);
            foreach (var name in genreNames)
            {
                var genre = await resolver.GetOrCreateAsync(name);
                if (genre != null && series.Genres.All(g => g.Name != genre.Name))
                    series.Genres.Add(genre);
            }
        }

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
