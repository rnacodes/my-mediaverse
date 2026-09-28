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
using MyMediaVerse.Shared.Exceptions;

namespace MyMediaVerse.Application.Services
{
    public class VideoService : IVideoService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<VideoService> _logger;

        public VideoService(
            IApplicationDbContext context,
            ILogger<VideoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Standard CRUD operations
        public async Task<IEnumerable<Video>> GetAllVideosAsync()
        {
            try
            {
                return await _context.Videos
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(v => v.Topics)
                    .Include(v => v.Genres)
                    .Include(v => v.Channel)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all videos");
                throw;
            }
        }

        public async Task<Video?> GetVideoByIdAsync(Guid id)
        {
            try
            {
                return await _context.Videos
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(v => v.Topics)
                    .Include(v => v.Genres)
                    .Include(v => v.Channel)
                    .FirstOrDefaultAsync(v => v.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving video with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Video>> GetVideosByChannelAsync(Guid channelId)
        {
            try
            {
                return await _context.Videos
                    .Include(v => v.Topics)
                    .Include(v => v.Genres)
                    .Include(v => v.Channel)
                    .Where(v => v.ChannelId == channelId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving videos by channel {ChannelId}", channelId);
                throw;
            }
        }

        public async Task<Video?> GetVideoByExternalIdAsync(string platform, string externalId)
        {
            var normalizedId = VideoDuplicateFinder.NormalizeExternalId(externalId);
            if (normalizedId == null || string.IsNullOrWhiteSpace(platform))
            {
                return null;
            }

            try
            {
                var platformLower = platform.Trim().ToLower();
                return await _context.Videos
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(v => v.Topics)
                    .Include(v => v.Genres)
                    .Include(v => v.Channel)
                    .FirstOrDefaultAsync(v => v.ExternalId == normalizedId && v.Platform.ToLower() == platformLower);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving video with external ID {ExternalId}", externalId);
                throw;
            }
        }

        public async Task<VideoCreationResult> CreateVideoAsync(CreateVideoDto dto)
        {
            try
            {
                var video = new Video
                {
                    Title = dto.Title,
                    MediaType = MediaType.Video,
                    Link = dto.Link,
                    Notes = dto.Notes,
                    Status = dto.Status,
                    DateAdded = DateTime.UtcNow,
                    DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                    Rating = dto.Rating,
                    Description = dto.Description,
                    RelatedNotes = dto.RelatedNotes,
                    Thumbnail = dto.Thumbnail,
                    Platform = dto.Platform,
                    ChannelId = dto.ChannelId,
                    LengthInSeconds = dto.LengthInSeconds,
                    ExternalId = VideoDuplicateFinder.NormalizeExternalId(dto.ExternalId)
                };

                var existing = await FindAndMergeAsync(video);
                if (existing != null)
                {
                    return new VideoCreationResult(existing, false);
                }

                // Topics and genres the request names; a video that names none inherits its
                // channel's when it is saved.
                foreach (var topicName in dto.Topics?.Where(t => !string.IsNullOrWhiteSpace(t)) ?? Array.Empty<string>())
                {
                    var normalizedTopicName = topicName.Trim().ToLowerInvariant();
                    var existingTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == normalizedTopicName);
                    video.Topics.Add(existingTopic ?? new Topic { Name = normalizedTopicName });
                }

                foreach (var genreName in dto.Genres?.Where(g => !string.IsNullOrWhiteSpace(g)) ?? Array.Empty<string>())
                {
                    var normalizedGenreName = genreName.Trim().ToLowerInvariant();
                    var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == normalizedGenreName);
                    video.Genres.Add(existingGenre ?? new Genre { Name = normalizedGenreName });
                }

                return await AddNewAsync(video);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating video");
                throw;
            }
        }

        public async Task<Video> UpdateVideoAsync(Guid id, CreateVideoDto dto)
        {
            try
            {
                // Load tracked (with topics/genres) so EF can persist removed relationships
                // when the collections are replaced below.
                var video = await _context.Videos
                    .Include(v => v.Topics)
                    .Include(v => v.Genres)
                    .FirstOrDefaultAsync(v => v.Id == id);
                if (video == null)
                {
                    throw new ArgumentException($"Video with ID {id} not found");
                }

                // A blank id is stored as null, and an id belongs to one video per platform.
                var externalId = VideoDuplicateFinder.NormalizeExternalId(dto.ExternalId);
                if (externalId != null)
                {
                    var platformLower = dto.Platform.Trim().ToLower();
                    var takenByAnother = await _context.Videos.AnyAsync(v =>
                        v.Id != id && v.ExternalId == externalId && v.Platform.ToLower() == platformLower);
                    if (takenByAnother)
                    {
                        throw new VideoIdentityConflictException(dto.Platform, externalId);
                    }
                }

                // Update properties
                video.Title = dto.Title;
                video.Link = dto.Link;
                video.Notes = dto.Notes;
                video.Status = dto.Status;
                video.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);
                video.Rating = dto.Rating;
                video.Description = dto.Description;
                video.RelatedNotes = StoredValue.UnlessProvided(dto.RelatedNotes, video.RelatedNotes);
                video.Thumbnail = dto.Thumbnail;
                video.Platform = dto.Platform;
                // The edit form does not send the channel, so a missing one means "unchanged".
                video.ChannelId = StoredValue.UnlessProvided(dto.ChannelId, video.ChannelId);
                video.LengthInSeconds = dto.LengthInSeconds;
                video.ExternalId = externalId;

                // Clear existing topics and genres and save immediately so the removed
                // join rows are persisted before the new ones are added.
                video.Topics.Clear();
                video.Genres.Clear();
                await _context.SaveChangesAsync();

                // Add new Topics
                if (dto.Topics?.Length > 0)
                {
                    foreach (var topicName in dto.Topics.Where(t => !string.IsNullOrWhiteSpace(t)))
                    {
                        var normalizedTopicName = topicName.Trim().ToLowerInvariant();
                        var topic = await _context.Topics
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);

                        if (topic == null)
                        {
                            topic = new Topic { Name = normalizedTopicName };
                            _context.Add(topic);
                            await _context.SaveChangesAsync();
                        }

                        var trackedTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == topic.Id);
                        if (trackedTopic != null && !video.Topics.Any(t => t.Id == trackedTopic.Id))
                        {
                            video.Topics.Add(trackedTopic);
                        }
                    }
                }

                // Add new Genres
                if (dto.Genres?.Length > 0)
                {
                    foreach (var genreName in dto.Genres.Where(g => !string.IsNullOrWhiteSpace(g)))
                    {
                        var normalizedGenreName = genreName.Trim().ToLowerInvariant();
                        var genre = await _context.Genres
                            .AsNoTracking()
                            .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);

                        if (genre == null)
                        {
                            genre = new Genre { Name = normalizedGenreName };
                            _context.Add(genre);
                            await _context.SaveChangesAsync();
                        }

                        var trackedGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id);
                        if (trackedGenre != null && !video.Genres.Any(g => g.Id == trackedGenre.Id))
                        {
                            video.Genres.Add(trackedGenre);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return video;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating video with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteVideoAsync(Guid id)
        {
            try
            {
                var video = await _context.Videos.FirstOrDefaultAsync(v => v.Id == id);
                if (video == null)
                {
                    return false;
                }

                _context.Remove(video);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting video with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<YouTubePlaylist>> GetPlaylistsForVideoAsync(Guid videoId)
        {
            try
            {
                return await _context.YouTubePlaylists
                    .AsNoTracking()
                    .Where(p => p.PlaylistVideos.Any(pv => pv.VideoId == videoId))
                    .Include(p => p.Topics)
                    .Include(p => p.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving playlists for video {VideoId}", videoId);
                throw;
            }
        }

        public async Task<VideoCreationResult> SaveVideoAsync(Video video)
        {
            video.ExternalId = VideoDuplicateFinder.NormalizeExternalId(video.ExternalId);

            var existing = await FindAndMergeAsync(video);
            if (existing != null)
            {
                return new VideoCreationResult(existing, false);
            }

            return await AddNewAsync(video);
        }

        private IQueryable<Video> VideosWithDetails() =>
            _context.Videos
                .AsSplitQuery()
                .Include(v => v.Topics)
                .Include(v => v.Genres)
                .Include(v => v.Channel);

        /// <summary>
        /// Looks for the incoming video in the library. A row already known by the same id is
        /// returned untouched; a row found by its link or title gains the id and has its blank
        /// fields filled. Returns null when the video is new.
        /// </summary>
        private async Task<Video?> FindAndMergeAsync(Video incoming)
        {
            var identity = VideoIdentity.From(incoming);
            var existing = await VideoDuplicateFinder.FindExistingAsync(VideosWithDetails(), identity);
            if (existing == null)
            {
                return null;
            }

            _logger.LogInformation("Video {Title} is already in the library (ID: {Id})", existing.Title, existing.Id);

            var knownByTheSameId = !string.IsNullOrEmpty(existing.ExternalId)
                && existing.ExternalId == incoming.ExternalId
                && string.Equals(existing.Platform?.Trim(), incoming.Platform?.Trim(), StringComparison.OrdinalIgnoreCase);
            if (knownByTheSameId)
            {
                return existing;
            }

            var changed = VideoDuplicateFinder.AbsorbIdentity(existing, identity);
            changed |= VideoDuplicateFinder.AbsorbMetadata(existing, incoming);
            if (changed)
            {
                await _context.SaveChangesAsync();
            }

            return existing;
        }

        /// <summary>
        /// Saves a video that is not in the library yet. If another request saves the same video
        /// first, the unique id index rejects this one and the row that won is returned instead.
        /// </summary>
        private async Task<VideoCreationResult> AddNewAsync(Video video)
        {
            var identity = VideoIdentity.From(video);
            await InheritChannelTopicsAndGenresAsync(video);

            try
            {
                _context.Add(video);
                await _context.SaveChangesAsync();
                return new VideoCreationResult(video, true);
            }
            catch (DbUpdateException ex)
            {
                // The failed video and any new topic or genre rows are still tracked; left in
                // place they would fail the next save in the same request.
                _context.ClearChangeTracker();

                var winner = await VideoDuplicateFinder.FindExistingAsync(VideosWithDetails(), identity);
                if (winner == null)
                {
                    throw;
                }

                _logger.LogInformation(ex, "Video {Title} was saved by another request first (ID: {Id})", winner.Title, winner.Id);
                return new VideoCreationResult(winner, false);
            }
        }

        private async Task InheritChannelTopicsAndGenresAsync(Video video)
        {
            if (!video.ChannelId.HasValue)
            {
                return;
            }

            var needsTopics = video.Topics == null || !video.Topics.Any();
            var needsGenres = video.Genres == null || !video.Genres.Any();
            if (!needsTopics && !needsGenres)
            {
                return;
            }

            var channel = await _context.YouTubeChannels
                .Include(c => c.Topics)
                .Include(c => c.Genres)
                .FirstOrDefaultAsync(c => c.Id == video.ChannelId.Value);
            if (channel == null)
            {
                return;
            }

            if (needsTopics)
            {
                foreach (var topic in channel.Topics)
                {
                    var normalizedName = topic.Name.Trim().ToLowerInvariant();
                    var existingTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == normalizedName);
                    (video.Topics ??= new List<Topic>()).Add(existingTopic ?? new Topic { Name = normalizedName });
                }
            }

            if (needsGenres)
            {
                foreach (var genre in channel.Genres)
                {
                    var normalizedName = genre.Name.Trim().ToLowerInvariant();
                    var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == normalizedName);
                    (video.Genres ??= new List<Genre>()).Add(existingGenre ?? new Genre { Name = normalizedName });
                }
            }
        }
    }
}
