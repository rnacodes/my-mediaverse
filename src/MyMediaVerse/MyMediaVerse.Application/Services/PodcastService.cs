using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class PodcastService : IPodcastService
    {
        private readonly IApplicationDbContext _context;
        private readonly ITypesenseService _typesenseService;
        private readonly ILogger<PodcastService> _logger;

        public PodcastService(
            IApplicationDbContext context,
            ITypesenseService typesenseService,
            ILogger<PodcastService> logger)
        {
            _context = context;
            _typesenseService = typesenseService;
            _logger = logger;
        }

        /// <summary>Tracked series query with the tag collections loaded, for paths that write to the row.</summary>
        private IQueryable<PodcastSeries> TrackedSeriesWithTags => _context.PodcastSeries
            .Include(p => p.Topics)
            .Include(p => p.Genres);

        // Podcast Series methods
        public async Task<IEnumerable<PodcastSeries>> GetAllPodcastSeriesAsync()
        {
            return await _context.PodcastSeries
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Mixlists)
                .ToListAsync();
        }

        public async Task<PodcastSeries?> GetPodcastSeriesByIdAsync(Guid id)
        {
            return await _context.PodcastSeries
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Episodes)
                .Include(p => p.Mixlists)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<PodcastSeries>> SearchPodcastSeriesAsync(string query)
        {
            var lowerQuery = query.ToLowerInvariant();
            return await _context.PodcastSeries
                .AsNoTracking()
                .AsSplitQuery()
                .Where(p => p.Title.ToLower().Contains(lowerQuery) ||
                           (p.Publisher != null && p.Publisher.ToLower().Contains(lowerQuery)))
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Mixlists)
                .ToListAsync();
        }

        public async Task<PodcastSeriesCreationResult> CreatePodcastSeriesAsync(
            CreatePodcastSeriesDto dto, string metadataSource = PodcastMetadataSources.Manual)
        {
            var feedUrl = NormalizeFeedUrlOrThrow(dto.RssFeedUrl);

            var incoming = new PodcastSeries
            {
                Title = dto.Title,
                MediaType = MediaType.Podcast,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail,
                Publisher = dto.Publisher,
                ExternalId = BlankToNull(dto.ExternalId),
                RssFeedUrl = feedUrl,
                FeedUrlKey = UrlNormalizer.GetComparisonKey(feedUrl),
                FeedGuid = BlankToNull(dto.FeedGuid),
                ApplePodcastsId = BlankToNull(dto.ApplePodcastsId),
                PodcastIndexId = dto.PodcastIndexId,
                Language = BlankToNull(dto.Language),
                MetadataSource = metadataSource,
                IsSubscribed = dto.IsSubscribed,
                LastSyncDate = dto.LastSyncDate,
                TotalEpisodes = dto.TotalEpisodes
            };

            await ApplyTopicsAsync(incoming.Topics, dto.Topics);
            await ApplyGenresAsync(incoming.Genres, dto.Genres);

            var identity = PodcastSeriesIdentity.From(incoming);
            var existing = await PodcastSeriesDuplicateFinder.FindExistingAsync(TrackedSeriesWithTags, identity);
            if (existing != null)
            {
                var identityChanged = await PodcastSeriesDuplicateFinder.AbsorbIdentityAsync(
                    _context.PodcastSeries, existing, identity);
                var metadataChanged = PodcastSeriesDuplicateFinder.AbsorbMetadata(existing, incoming);
                if (identityChanged || metadataChanged)
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Podcast series already exists for {Title} (ID: {Id}); returning existing row",
                    incoming.Title, existing.Id);
                return new PodcastSeriesCreationResult(existing, Created: false);
            }

            _context.Add(incoming);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created podcast series with ID: {Id}, Title: {Title}", incoming.Id, incoming.Title);
            return new PodcastSeriesCreationResult(incoming, Created: true);
        }

        public async Task<PodcastSeries> UpdatePodcastSeriesAsync(Guid id, CreatePodcastSeriesDto dto)
        {
            var series = await TrackedSeriesWithTags.FirstOrDefaultAsync(p => p.Id == id);
            if (series == null)
            {
                throw new KeyNotFoundException($"Podcast series with ID {id} not found.");
            }

            var feedUrl = NormalizeFeedUrlOrThrow(dto.RssFeedUrl);
            var feedKey = UrlNormalizer.GetComparisonKey(feedUrl);
            var appleId = BlankToNull(dto.ApplePodcastsId);

            // The feed URL and Apple id are unique identities; refuse an edit that would take one
            // from another series rather than letting the unique index fail the save.
            if (!string.IsNullOrEmpty(feedKey) && feedKey != series.FeedUrlKey)
            {
                var other = await PodcastSeriesDuplicateFinder.FindExistingAsync(
                    _context.PodcastSeries, new PodcastSeriesIdentity { FeedUrl = feedUrl });
                if (other != null && other.Id != id)
                {
                    throw new InvalidOperationException(
                        $"Another podcast series already uses this feed URL (ID: {other.Id}).");
                }
            }

            if (appleId != null && appleId != series.ApplePodcastsId &&
                await _context.PodcastSeries.AnyAsync(p => p.Id != id && p.ApplePodcastsId == appleId))
            {
                throw new InvalidOperationException("Another podcast series already uses this Apple Podcasts id.");
            }

            series.Title = dto.Title;
            series.Link = dto.Link;
            series.Notes = dto.Notes;
            series.Status = dto.Status;
            series.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);
            series.Rating = dto.Rating;
            series.OwnershipStatus = dto.OwnershipStatus;
            series.Description = dto.Description;
            series.RelatedNotes = dto.RelatedNotes;
            series.Thumbnail = dto.Thumbnail;
            series.Publisher = dto.Publisher;
            series.RssFeedUrl = feedUrl;
            series.FeedUrlKey = feedKey;
            series.ApplePodcastsId = appleId;

            series.Topics.Clear();
            series.Genres.Clear();
            await _context.SaveChangesAsync();

            await ApplyTopicsAsync(series.Topics, dto.Topics);
            await ApplyGenresAsync(series.Genres, dto.Genres);

            await _context.SaveChangesAsync();

            return await GetPodcastSeriesByIdAsync(id) ?? series;
        }

        public async Task<bool> DeletePodcastSeriesAsync(Guid id)
        {
            var series = await _context.PodcastSeries.FirstOrDefaultAsync(p => p.Id == id);
            if (series == null)
            {
                return false;
            }

            // Episodes are removed through EF rather than left to the database cascade on SeriesId.
            // Each media type is split across MediaItems plus its own table, and that cascade only
            // reaches the PodcastEpisodes rows — it would leave every episode's MediaItems row behind
            // with no type, which breaks every query over all media.
            var episodes = await _context.PodcastEpisodes.Where(e => e.SeriesId == id).ToListAsync();
            foreach (var episode in episodes)
            {
                _context.Remove(episode);
            }

            _context.Remove(series);
            await _context.SaveChangesAsync();

            // Eager search-index cleanup so the series and its episodes stop appearing in search
            // immediately. Best effort: the next bulk reindex reconciles anything this misses.
            await SearchIndexCleanup.TryDeleteAsync(
                () => _typesenseService.DeleteMediaItemAsync(id), _logger, "podcast series", id);
            foreach (var episode in episodes)
            {
                await SearchIndexCleanup.TryDeleteAsync(
                    () => _typesenseService.DeleteMediaItemAsync(episode.Id), _logger, "podcast episode", episode.Id);
            }

            _logger.LogInformation("Deleted podcast series {Id} and {EpisodeCount} episode(s)", id, episodes.Count);
            return true;
        }

        public async Task<bool> PodcastSeriesExistsAsync(string title, string? publisher = null)
        {
            var query = _context.PodcastSeries.AsQueryable();
            query = query.Where(p => p.Title.ToLower() == title.ToLower());

            if (!string.IsNullOrWhiteSpace(publisher))
            {
                query = query.Where(p => p.Publisher != null && p.Publisher.ToLower() == publisher.ToLower());
            }

            return await query.AnyAsync();
        }

        public async Task<PodcastSeries?> GetPodcastSeriesByTitleAsync(string title, string? publisher = null)
        {
            var query = _context.PodcastSeries.AsQueryable();
            query = query.Where(p => p.Title.ToLower() == title.ToLower());

            if (!string.IsNullOrWhiteSpace(publisher))
            {
                query = query.Where(p => p.Publisher != null && p.Publisher.ToLower() == publisher.ToLower());
            }

            return await query.FirstOrDefaultAsync();
        }

        // Podcast Episode methods
        public async Task<IEnumerable<PodcastEpisode>> GetEpisodesBySeriesIdAsync(Guid seriesId)
        {
            return await _context.PodcastEpisodes
                .AsNoTracking()
                .AsSplitQuery()
                .Where(e => e.SeriesId == seriesId)
                .Include(e => e.Series)
                .Include(e => e.Topics)
                .Include(e => e.Genres)
                .OrderByDescending(e => e.ReleaseDate)
                .ToListAsync();
        }

        public async Task<PodcastEpisode?> GetPodcastEpisodeByIdAsync(Guid id)
        {
            return await _context.PodcastEpisodes
                .AsNoTracking()
                .AsSplitQuery()
                .Include(e => e.Series)
                .Include(e => e.Topics)
                .Include(e => e.Genres)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<PodcastEpisode>> GetAllPodcastEpisodesAsync()
        {
            return await _context.PodcastEpisodes
                .AsNoTracking()
                .AsSplitQuery()
                .Include(e => e.Series)
                .Include(e => e.Topics)
                .Include(e => e.Genres)
                .ToListAsync();
        }

        public async Task<PodcastEpisodeCreationResult> CreatePodcastEpisodeAsync(CreatePodcastEpisodeDto dto)
        {
            // Verify the parent series exists (include Topics/Genres for inheritance)
            var parentSeries = await _context.PodcastSeries
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .FirstOrDefaultAsync(p => p.Id == dto.SeriesId);

            if (parentSeries == null)
            {
                throw new ArgumentException($"Parent podcast series with ID {dto.SeriesId} not found.");
            }

            var identity = new PodcastEpisodeIdentity
            {
                SeriesId = dto.SeriesId,
                RssGuid = dto.RssGuid,
                ExternalId = dto.ExternalId,
                AudioLink = dto.AudioLink,
                Title = dto.Title,
                ReleaseDate = dto.ReleaseDate
            };

            var existing = await PodcastEpisodeDuplicateFinder.FindExistingAsync(
                _context.PodcastEpisodes.Include(e => e.Series).Include(e => e.Topics).Include(e => e.Genres), identity);
            if (existing != null)
            {
                if (await PodcastEpisodeDuplicateFinder.AbsorbIdentityAsync(_context.PodcastEpisodes, existing, identity))
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Podcast episode already exists for {Title} (ID: {Id}); returning existing row",
                    dto.Title, existing.Id);
                return new PodcastEpisodeCreationResult(existing, Created: false);
            }

            var episode = new PodcastEpisode
            {
                Title = dto.Title,
                MediaType = MediaType.Podcast,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail,
                SeriesId = dto.SeriesId,
                AudioLink = dto.AudioLink,
                ReleaseDate = DateTimeNormalizer.ToUtc(dto.ReleaseDate),
                DurationInSeconds = dto.DurationInSeconds,
                EpisodeNumber = dto.EpisodeNumber,
                SeasonNumber = dto.SeasonNumber,
                ExternalId = BlankToNull(dto.ExternalId),
                RssGuid = BlankToNull(dto.RssGuid),
                Publisher = dto.Publisher
            };

            // Topics and genres: use the DTO's when provided, otherwise inherit from the parent series
            var topicNames = dto.Topics?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
            if (topicNames == null || topicNames.Length == 0)
            {
                topicNames = parentSeries.Topics?.Select(t => t.Name).ToArray() ?? Array.Empty<string>();
            }
            await ApplyTopicsAsync(episode.Topics, topicNames);

            var genreNames = dto.Genres?.Where(g => !string.IsNullOrWhiteSpace(g)).ToArray();
            if (genreNames == null || genreNames.Length == 0)
            {
                genreNames = parentSeries.Genres?.Select(g => g.Name).ToArray() ?? Array.Empty<string>();
            }
            await ApplyGenresAsync(episode.Genres, genreNames);

            _context.Add(episode);
            await _context.SaveChangesAsync();

            episode.Series = parentSeries;

            return new PodcastEpisodeCreationResult(episode, Created: true);
        }

        public async Task<PodcastEpisode> UpdatePodcastEpisodeAsync(Guid id, CreatePodcastEpisodeDto dto)
        {
            // Load tracked (with topics/genres) so EF can detect removed relationships
            // when we replace the collections below.
            var episode = await _context.PodcastEpisodes
                .Include(e => e.Topics)
                .Include(e => e.Genres)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (episode == null)
            {
                throw new InvalidOperationException($"Podcast episode with ID {id} not found.");
            }

            // Update user-editable properties. SeriesId is intentionally not reassigned
            // (an episode stays with its series) and ExternalId is preserved for sync.
            episode.Title = dto.Title;
            episode.Link = dto.Link;
            episode.Notes = dto.Notes;
            episode.Status = dto.Status;
            episode.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);
            episode.Rating = dto.Rating;
            episode.OwnershipStatus = dto.OwnershipStatus;
            episode.Description = dto.Description;
            episode.RelatedNotes = dto.RelatedNotes;
            episode.Thumbnail = dto.Thumbnail;
            episode.AudioLink = dto.AudioLink;
            episode.ReleaseDate = DateTimeNormalizer.ToUtc(dto.ReleaseDate);
            episode.DurationInSeconds = dto.DurationInSeconds;
            episode.EpisodeNumber = dto.EpisodeNumber;
            episode.SeasonNumber = dto.SeasonNumber;
            episode.Publisher = dto.Publisher;

            // Clear existing topics and genres and save immediately so the removed
            // join rows are persisted before the new ones are added.
            episode.Topics.Clear();
            episode.Genres.Clear();
            await _context.SaveChangesAsync();

            // Replace topics and genres from the submitted values (an edit is explicit,
            // so unlike create we do not inherit from the parent series)
            await ApplyTopicsAsync(episode.Topics, dto.Topics);
            await ApplyGenresAsync(episode.Genres, dto.Genres);

            await _context.SaveChangesAsync();

            // Reload with clean navigation properties (including Series) for the response
            return await GetPodcastEpisodeByIdAsync(id) ?? episode;
        }

        public async Task<bool> DeletePodcastEpisodeAsync(Guid id)
        {
            var episode = await _context.FindAsync<PodcastEpisode>(id);
            if (episode == null)
            {
                return false;
            }

            _context.Remove(episode);
            await _context.SaveChangesAsync();

            await SearchIndexCleanup.TryDeleteAsync(
                () => _typesenseService.DeleteMediaItemAsync(id), _logger, "podcast episode", id);

            return true;
        }

        public async Task<bool> PodcastEpisodeExistsAsync(Guid seriesId, string episodeTitle)
        {
            return await _context.PodcastEpisodes
                .AnyAsync(e => e.SeriesId == seriesId && e.Title.ToLower() == episodeTitle.ToLower());
        }

        public async Task<PodcastEpisode?> GetPodcastEpisodeByTitleAsync(Guid seriesId, string episodeTitle)
        {
            return await _context.PodcastEpisodes
                .FirstOrDefaultAsync(e => e.SeriesId == seriesId && e.Title.ToLower() == episodeTitle.ToLower());
        }

        // Resolve-or-create normalized (trimmed, lowercase) topics into the target collection.
        // The resolver registers new topics explicitly and dedupes repeats within one call.
        private async Task ApplyTopicsAsync(ICollection<Topic> target, IEnumerable<string>? topics)
        {
            var resolver = new TopicResolver(_context);
            foreach (var name in (topics ?? Array.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var topic = await resolver.GetOrCreateAsync(name.Trim().ToLowerInvariant());
                if (topic != null && !target.Contains(topic))
                {
                    target.Add(topic);
                }
            }
        }

        // Resolve-or-create normalized (trimmed, lowercase) genres into the target collection
        private async Task ApplyGenresAsync(ICollection<Genre> target, IEnumerable<string>? genres)
        {
            var resolver = new GenreResolver(_context);
            foreach (var name in (genres ?? Array.Empty<string>()).Where(g => !string.IsNullOrWhiteSpace(g)))
            {
                var genre = await resolver.GetOrCreateAsync(name.Trim().ToLowerInvariant());
                if (genre != null && !target.Contains(genre))
                {
                    target.Add(genre);
                }
            }
        }

        /// <summary>
        /// Trims a feed URL and checks it is an absolute http(s) URL. Blank means "no feed".
        /// The URL is stored as given (not normalized) so it stays fetchable exactly as published;
        /// only the comparison key is normalized.
        /// </summary>
        private static string? NormalizeFeedUrlOrThrow(string? feedUrl)
        {
            if (string.IsNullOrWhiteSpace(feedUrl))
                return null;

            var trimmed = feedUrl.Trim();
            if (!UrlNormalizer.IsValid(trimmed))
                throw new ArgumentException("The RSS feed URL must be an absolute http or https URL.");

            return trimmed;
        }

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // Subscription management methods
        public async Task<PodcastSeries?> SubscribeToPodcastSeriesAsync(Guid seriesId)
        {
            var series = await _context.PodcastSeries.FirstOrDefaultAsync(p => p.Id == seriesId);

            if (series == null)
            {
                return null;
            }

            series.IsSubscribed = true;
            await _context.SaveChangesAsync();

            return series;
        }

        public async Task<PodcastSeries?> UnsubscribeFromPodcastSeriesAsync(Guid seriesId)
        {
            var series = await _context.PodcastSeries.FirstOrDefaultAsync(p => p.Id == seriesId);

            if (series == null)
            {
                return null;
            }

            series.IsSubscribed = false;
            await _context.SaveChangesAsync();

            return series;
        }

        public async Task<IEnumerable<PodcastSeries>> GetSubscribedPodcastSeriesAsync()
        {
            return await _context.PodcastSeries
                .AsNoTracking()
                .AsSplitQuery()
                .Where(p => p.IsSubscribed)
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .ToListAsync();
        }

        /// <summary>
        /// Episode sync is being rebuilt on RSS feeds; until then this always throws
        /// <see cref="NotSupportedException"/>.
        /// </summary>
        public Task<PodcastSyncResultDto?> SyncPodcastSeriesEpisodesAsync(Guid seriesId) =>
            throw new NotSupportedException("Episode sync is being rebuilt on RSS feeds.");
    }
}
