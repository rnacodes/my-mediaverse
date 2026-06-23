using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly IListenNotesApiClient _listenNotesApiClient;
        private readonly IPodcastMappingService _podcastMappingService;
        private readonly ILogger<PodcastService> _logger;

        public PodcastService(
            IApplicationDbContext context,
            IListenNotesApiClient listenNotesApiClient,
            IPodcastMappingService podcastMappingService,
            ILogger<PodcastService> logger)
        {
            _context = context;
            _listenNotesApiClient = listenNotesApiClient;
            _podcastMappingService = podcastMappingService;
            _logger = logger;
        }

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

        public async Task<PodcastSeries> CreatePodcastSeriesAsync(CreatePodcastSeriesDto dto)
        {
            var series = new PodcastSeries
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
                ExternalId = dto.ExternalId,
                IsSubscribed = dto.IsSubscribed,
                LastSyncDate = dto.LastSyncDate,
                TotalEpisodes = dto.TotalEpisodes
            };

            // Handle Topics array conversion
            if (dto.Topics?.Length > 0)
            {
                foreach (var topicName in dto.Topics.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    var normalizedTopicName = topicName.ToLower();
                    var existingTopic = await _context.Topics
                        .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedTopicName);

                    if (existingTopic != null)
                    {
                        series.Topics.Add(existingTopic);
                    }
                    else
                    {
                        series.Topics.Add(new Topic { Name = normalizedTopicName });
                    }
                }
            }

            // Handle Genres array conversion
            if (dto.Genres?.Length > 0)
            {
                foreach (var genreName in dto.Genres.Where(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var normalizedGenreName = genreName.ToLower();
                    var existingGenre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == normalizedGenreName);

                    if (existingGenre != null)
                    {
                        series.Genres.Add(existingGenre);
                    }
                    else
                    {
                        series.Genres.Add(new Genre { Name = normalizedGenreName });
                    }
                }
            }

            _context.Add(series);
            await _context.SaveChangesAsync();

            return series;
        }

        public async Task<PodcastSeries> UpdatePodcastSeriesAsync(Guid id, CreatePodcastSeriesDto dto)
        {
            var series = await _context.PodcastSeries
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (series == null)
            {
                throw new InvalidOperationException($"Podcast series with ID {id} not found.");
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
            var series = await _context.FindAsync<PodcastSeries>(id);
            if (series == null)
            {
                return false;
            }

            var seriesId = series.Id;

            // Cascade delete will automatically remove episodes
            _context.Remove(series);
            await _context.SaveChangesAsync();

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

        public async Task<PodcastEpisode> CreatePodcastEpisodeAsync(CreatePodcastEpisodeDto dto)
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
                ExternalId = dto.ExternalId,
                Publisher = dto.Publisher
            };

            // Handle Topics: use DTO topics if provided, otherwise inherit from parent series
            var topicNames = dto.Topics?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
            if (topicNames == null || topicNames.Length == 0)
            {
                // Inherit topics from parent series
                topicNames = parentSeries.Topics?.Select(t => t.Name).ToArray() ?? Array.Empty<string>();
            }

            foreach (var topicName in topicNames)
            {
                var normalizedTopicName = topicName.ToLower();
                var existingTopic = await _context.Topics
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedTopicName);

                if (existingTopic != null)
                {
                    episode.Topics.Add(existingTopic);
                }
                else
                {
                    episode.Topics.Add(new Topic { Name = normalizedTopicName });
                }
            }

            // Handle Genres: use DTO genres if provided, otherwise inherit from parent series
            var genreNames = dto.Genres?.Where(g => !string.IsNullOrWhiteSpace(g)).ToArray();
            if (genreNames == null || genreNames.Length == 0)
            {
                // Inherit genres from parent series
                genreNames = parentSeries.Genres?.Select(g => g.Name).ToArray() ?? Array.Empty<string>();
            }

            foreach (var genreName in genreNames)
            {
                var normalizedGenreName = genreName.ToLower();
                var existingGenre = await _context.Genres
                    .FirstOrDefaultAsync(g => g.Name.ToLower() == normalizedGenreName);

                if (existingGenre != null)
                {
                    episode.Genres.Add(existingGenre);
                }
                else
                {
                    episode.Genres.Add(new Genre { Name = normalizedGenreName });
                }
            }

            _context.Add(episode);
            await _context.SaveChangesAsync();

            // Load the Series navigation property
            episode.Series = await _context.PodcastSeries
                .FirstOrDefaultAsync(s => s.Id == episode.SeriesId);

            return episode;
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

            var episodeId = episode.Id;

            _context.Remove(episode);
            await _context.SaveChangesAsync();

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

        // Resolve-or-create normalized (lowercase) topics into the target collection
        private async Task ApplyTopicsAsync(ICollection<Topic> target, string[]? topics)
        {
            foreach (var topicName in (topics ?? Array.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var normalizedTopicName = topicName.ToLower();
                var existingTopic = await _context.Topics
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedTopicName);
                if (existingTopic != null)
                {
                    target.Add(existingTopic);
                }
                else
                {
                    // Register the new topic explicitly so EF inserts it. A client-set Guid key
                    // looks "already-set", so reached via a tracked parent EF would assume it
                    // exists and skip the insert, breaking the join FK.
                    var newTopic = new Topic { Name = normalizedTopicName };
                    _context.Add(newTopic);
                    target.Add(newTopic);
                }
            }
        }

        // Resolve-or-create normalized (lowercase) genres into the target collection
        private async Task ApplyGenresAsync(ICollection<Genre> target, string[]? genres)
        {
            foreach (var genreName in (genres ?? Array.Empty<string>()).Where(g => !string.IsNullOrWhiteSpace(g)))
            {
                var normalizedGenreName = genreName.ToLower();
                var existingGenre = await _context.Genres
                    .FirstOrDefaultAsync(g => g.Name.ToLower() == normalizedGenreName);
                if (existingGenre != null)
                {
                    target.Add(existingGenre);
                }
                else
                {
                    // Register the new genre explicitly (see ApplyTopicsAsync) so EF inserts it
                    // instead of assuming the client-set key already exists.
                    var newGenre = new Genre { Name = normalizedGenreName };
                    _context.Add(newGenre);
                    target.Add(newGenre);
                }
            }
        }

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

        public async Task<PodcastSyncResultDto?> SyncPodcastSeriesEpisodesAsync(Guid seriesId)
        {
            var series = await GetPodcastSeriesByIdAsync(seriesId);

            if (series == null || string.IsNullOrEmpty(series.ExternalId))
            {
                _logger.LogWarning("Cannot sync series {SeriesId}: series not found or has no external ID", seriesId);
                return null;
            }

            try
            {
                _logger.LogInformation("Syncing episodes for podcast series: {Title} (External ID: {ExternalId})", 
                    series.Title, series.ExternalId);

                // Fetch podcast details from ListenNotes API (includes episodes)
                var podcastDto = await _listenNotesApiClient.GetPodcastByIdAsync(series.ExternalId);

                if (podcastDto?.Episodes == null || !podcastDto.Episodes.Any())
                {
                    _logger.LogInformation("No episodes found for podcast series: {Title}", series.Title);
                    series.LastSyncDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return new PodcastSyncResultDto
                    {
                        SeriesTitle = series.Title,
                        NewEpisodesCount = 0,
                        TotalEpisodesCount = 0,
                        LastSyncDate = series.LastSyncDate.Value
                    };
                }

                int newEpisodesCount = 0;

                // Process each episode from the API
                foreach (var episodeDto in podcastDto.Episodes)
                {
                    // Check if episode already exists by external ID
                    var existingEpisode = await _context.PodcastEpisodes
                        .FirstOrDefaultAsync(e => e.ExternalId == episodeDto.Id);

                    if (existingEpisode == null)
                    {
                        // Map and create new episode
                        var createEpisodeDto = _podcastMappingService.MapFromListenNotesEpisodeDto(episodeDto);
                        createEpisodeDto.SeriesId = seriesId;

                        var newEpisode = new PodcastEpisode
                        {
                            Title = createEpisodeDto.Title,
                            MediaType = MediaType.Podcast,
                            SeriesId = seriesId,
                            Link = createEpisodeDto.Link,
                            Status = Status.Uncharted,
                            AudioLink = createEpisodeDto.AudioLink,
                            ExternalId = createEpisodeDto.ExternalId,
                            Thumbnail = createEpisodeDto.Thumbnail,
                            ReleaseDate = DateTimeNormalizer.ToUtc(createEpisodeDto.ReleaseDate),
                            DurationInSeconds = createEpisodeDto.DurationInSeconds,
                            Description = createEpisodeDto.Description,
                            Publisher = createEpisodeDto.Publisher,
                            EpisodeNumber = createEpisodeDto.EpisodeNumber,
                            SeasonNumber = createEpisodeDto.SeasonNumber,
                            DateAdded = DateTime.UtcNow
                        };

                        // Inherit topics from parent series
                        if (series.Topics != null)
                        {
                            foreach (var topic in series.Topics)
                            {
                                var normalizedName = topic.Name.ToLower();
                                var existingTopic = await _context.Topics
                                    .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedName);
                                newEpisode.Topics.Add(existingTopic ?? new Topic { Name = normalizedName });
                            }
                        }

                        // Inherit genres from parent series
                        if (series.Genres != null)
                        {
                            foreach (var genre in series.Genres)
                            {
                                var normalizedName = genre.Name.ToLower();
                                var existingGenre = await _context.Genres
                                    .FirstOrDefaultAsync(g => g.Name.ToLower() == normalizedName);
                                newEpisode.Genres.Add(existingGenre ?? new Genre { Name = normalizedName });
                            }
                        }

                        _context.Add(newEpisode);
                        newEpisodesCount++;

                        _logger.LogInformation("Added new episode: {Title} (External ID: {ExternalId})", 
                            newEpisode.Title, newEpisode.ExternalId);
                    }
                }

                // Update last sync date
                series.LastSyncDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var totalEpisodeCount = await _context.PodcastEpisodes
                    .CountAsync(e => e.SeriesId == seriesId);

                _logger.LogInformation("Sync complete for {Title}: {NewCount} new episodes, {TotalCount} total episodes", 
                    series.Title, newEpisodesCount, totalEpisodeCount);

                return new PodcastSyncResultDto
                {
                    SeriesTitle = series.Title,
                    NewEpisodesCount = newEpisodesCount,
                    TotalEpisodesCount = totalEpisodeCount,
                    LastSyncDate = series.LastSyncDate.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing episodes for podcast series {SeriesId}", seriesId);
                throw;
            }
        }
    }
}
