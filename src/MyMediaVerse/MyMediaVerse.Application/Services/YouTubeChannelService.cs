using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class YouTubeChannelService : IYouTubeChannelService
    {
        private readonly IApplicationDbContext _context;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly IYouTubeMappingService _mappingService;
        private readonly ILogger<YouTubeChannelService> _logger;
        private readonly IMediaService _mediaService;
        private readonly YouTubeVideoResolver _videoResolver;

        public YouTubeChannelService(
            IApplicationDbContext context,
            IYouTubeApiClient youTubeApiClient,
            IYouTubeMappingService mappingService,
            ILogger<YouTubeChannelService> logger,
            IMediaService mediaService)
        {
            _context = context;
            _youTubeApiClient = youTubeApiClient;
            _mappingService = mappingService;
            _logger = logger;
            _mediaService = mediaService;
            _videoResolver = new YouTubeVideoResolver(context, youTubeApiClient, mappingService);
        }

        public async Task<IEnumerable<YouTubeChannel>> GetAllChannelsAsync()
        {
            try
            {
                return await _context.YouTubeChannels
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.Topics)
                    .Include(c => c.Genres)
                    .Include(c => c.Mixlists)
                    .Include(c => c.Videos)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all YouTube channels");
                throw;
            }
        }

        public async Task<YouTubeChannel?> GetChannelByIdAsync(Guid id)
        {
            try
            {
                return await _context.YouTubeChannels
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.Topics)
                    .Include(c => c.Genres)
                    .Include(c => c.Mixlists)
                    .Include(c => c.Videos)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving YouTube channel with ID {Id}", id);
                throw;
            }
        }

        public async Task<YouTubeChannel?> GetChannelByExternalIdAsync(string externalId)
        {
            try
            {
                return await _context.YouTubeChannels
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.Topics)
                    .Include(c => c.Genres)
                    .Include(c => c.Mixlists)
                    .Include(c => c.Videos)
                    .FirstOrDefaultAsync(c => c.ChannelExternalId == externalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving YouTube channel with external ID {ExternalId}", externalId);
                throw;
            }
        }

        public async Task<List<Video>> GetChannelVideosAsync(Guid channelId)
        {
            try
            {
                return await _context.Videos
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(v => v.Topics)
                    .Include(v => v.Genres)
                    .Where(v => v.ChannelId == channelId)
                    .OrderByDescending(v => v.DateAdded)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving videos for channel {ChannelId}", channelId);
                throw;
            }
        }

        public async Task<YouTubeChannel> CreateChannelAsync(CreateYouTubeChannelDto dto)
        {
            try
            {
                // Check if channel already exists
                var existingChannel = await GetChannelByExternalIdAsync(dto.ChannelExternalId);
                if (existingChannel != null)
                {
                    throw new InvalidOperationException($"Channel with external ID {dto.ChannelExternalId} already exists");
                }

                var channel = new YouTubeChannel
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Link = dto.Link,
                    Thumbnail = dto.Thumbnail,
                    ChannelExternalId = dto.ChannelExternalId,
                    CustomUrl = dto.CustomUrl,
                    SubscriberCount = dto.SubscriberCount,
                    VideoCount = dto.VideoCount,
                    ViewCount = dto.ViewCount,
                    UploadsPlaylistId = dto.UploadsPlaylistId,
                    Country = dto.Country,
                    PublishedAt = dto.PublishedAt,
                    MediaType = MediaType.Channel,
                    Status = dto.Status,
                    Rating = dto.Rating,
                    Notes = dto.Notes,
                    RelatedNotes = dto.RelatedNotes,
                    DateAdded = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                };

                await HandleTopicsAsync(channel, dto.Topics);

                await HandleGenresAsync(channel, dto.Genres);

                _context.Add(channel);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created YouTube channel {Title} with ID {Id}", channel.Title, channel.Id);
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating YouTube channel");
                throw;
            }
        }

        public async Task<YouTubeChannel> UpdateChannelAsync(Guid id, UpdateYouTubeChannelDto dto)
        {
            try
            {
                var channel = await _context.YouTubeChannels
                    .Include(c => c.Topics)
                    .Include(c => c.Genres)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (channel == null)
                {
                    throw new ArgumentException($"YouTube channel with ID {id} not found");
                }

                // Update properties
                channel.Title = dto.Title;
                channel.Description = dto.Description;
                channel.Link = dto.Link;
                channel.Thumbnail = dto.Thumbnail;
                channel.CustomUrl = dto.CustomUrl;
                channel.SubscriberCount = dto.SubscriberCount;
                channel.VideoCount = dto.VideoCount;
                channel.ViewCount = dto.ViewCount;
                channel.UploadsPlaylistId = dto.UploadsPlaylistId;
                channel.Country = dto.Country;
                channel.PublishedAt = dto.PublishedAt;
                channel.Status = dto.Status;
                channel.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);
                channel.Rating = dto.Rating;
                channel.Notes = dto.Notes;
                channel.RelatedNotes = dto.RelatedNotes;

                channel.Topics.Clear();
                await HandleTopicsAsync(channel, dto.Topics);

                channel.Genres.Clear();
                await HandleGenresAsync(channel, dto.Genres);

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated YouTube channel {Title} with ID {Id}", channel.Title, channel.Id);
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating YouTube channel with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteChannelAsync(Guid id)
        {
            try
            {
                // Only a channel id is accepted here; any other media item is left alone.
                if (!await _context.YouTubeChannels.AnyAsync(c => c.Id == id))
                {
                    return false;
                }

                // The shared delete detaches mixlists, topics, and genres, cleans up a stored
                // thumbnail, and removes the item from the search index. The channel's videos
                // stay in the library with their channel link cleared.
                var deleted = await _mediaService.DeleteMediaItemAsync(id);
                if (deleted)
                {
                    _logger.LogInformation("Deleted YouTube channel with ID {Id}", id);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting YouTube channel with ID {Id}", id);
                throw;
            }
        }

        public async Task<YouTubeChannelCreationResult> ImportChannelFromYouTubeAsync(string channelId)
        {
            try
            {
                // Check if channel already exists
                var existingChannel = await GetChannelByExternalIdAsync(channelId);
                if (existingChannel != null)
                {
                    _logger.LogInformation("Channel {ChannelId} already exists, returning existing channel", channelId);
                    return new YouTubeChannelCreationResult(existingChannel, false);
                }

                // Fetch channel data from YouTube API
                var channelDto = await _youTubeApiClient.GetChannelDetailsAsync(channelId);
                if (channelDto == null)
                {
                    throw new YouTubeResourceNotFoundException("channel", channelId);
                }

                // Map to entity
                var channel = _mappingService.MapChannelToYouTubeChannelEntity(channelDto);

                try
                {
                    _context.Add(channel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    // Another request imported the same channel first: drop the rejected row
                    // from tracking and hand back the one that won.
                    _context.ClearChangeTracker();

                    var winner = await GetChannelByExternalIdAsync(channelId);
                    if (winner == null)
                    {
                        throw;
                    }

                    _logger.LogInformation(ex, "Channel {ChannelId} was imported by another request first", channelId);
                    return new YouTubeChannelCreationResult(winner, false);
                }

                _logger.LogInformation("Imported YouTube channel {Title} with external ID {ExternalId}", channel.Title, channel.ChannelExternalId);
                return new YouTubeChannelCreationResult(channel, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing YouTube channel {ChannelId}", channelId);
                throw;
            }
        }

        public async Task<YouTubeChannelSyncResultDto> SyncChannelMetadataAsync(Guid channelId)
        {
            try
            {
                var result = new YouTubeChannelSyncResultDto { ChannelId = channelId, StartedAt = DateTime.UtcNow };

                var channel = await _context.FindAsync<YouTubeChannel>(channelId);
                if (channel == null)
                {
                    throw new ArgumentException($"Channel with ID {channelId} not found");
                }

                result.ChannelTitle = channel.Title;

                // Fetch latest data from YouTube API
                var channelDto = await _youTubeApiClient.GetChannelDetailsAsync(channel.ChannelExternalId);
                if (channelDto == null)
                {
                    throw new YouTubeResourceNotFoundException("channel", channel.ChannelExternalId);
                }

                // What YouTube may overwrite is decided in one place, shared with the refresh run.
                var changed = YouTubeMetadataApplier.Apply(channel, channelDto);

                channel.LastSyncedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                result.ChannelTitle = channel.Title;
                result.UpdatedCount = changed ? 1 : 0;
                result.YouTubeVideoCount = channel.VideoCount;
                result.StoredVideoCount = await _context.Videos.CountAsync(v => v.ChannelId == channel.Id);
                result.NewUploadsCount = Math.Max(0, (channel.VideoCount ?? 0) - result.StoredVideoCount);
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Synced metadata for YouTube channel {Title}: {Changed}, {Stored} of {OnYouTube} uploads stored",
                    channel.Title, changed ? "changed" : "unchanged", result.StoredVideoCount, channel.VideoCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing YouTube channel metadata for {ChannelId}", channelId);
                throw;
            }
        }

        public async Task<YouTubeChannelImportResultDto> ImportLatestUploadsAsync(Guid channelId, int count)
        {
            var result = new YouTubeChannelImportResultDto
            {
                ChannelId = channelId,
                RequestedCount = Math.Clamp(count, 1, YouTubeSyncOptions.MaxLatestUploadsCount),
                StartedAt = DateTime.UtcNow
            };

            try
            {
                var channel = await GetChannelByIdAsync(channelId);
                if (channel == null)
                {
                    throw new ArgumentException($"Channel with ID {channelId} not found");
                }

                result.ChannelTitle = channel.Title;

                // One page of the uploads playlist, newest first. YouTube returns nothing for a
                // channel that has no uploads playlist or is gone.
                var page = await _youTubeApiClient.GetChannelUploadsAsync(channel.ChannelExternalId, result.RequestedCount);

                var itemsByVideoId = new Dictionary<string, YouTubePlaylistItemDto>(StringComparer.Ordinal);
                var orderedVideoIds = new List<string>();
                foreach (var item in (page.Items ?? new List<YouTubePlaylistItemDto>())
                             .Where(item => !YouTubeVideoResolver.IsDeletedOrPrivateVideo(item)))
                {
                    var videoId = YouTubeVideoResolver.GetVideoId(item);
                    if (!string.IsNullOrEmpty(videoId) && itemsByVideoId.TryAdd(videoId, item))
                    {
                        orderedVideoIds.Add(videoId);
                    }
                }

                if (orderedVideoIds.Count == 0)
                {
                    result.Warnings.Add("YouTube listed no uploads for this channel.");
                }

                // Stored videos join the channel when they have none; linked ones are left alone.
                var stored = await _videoResolver.FindStoredVideosAsync(orderedVideoIds);
                foreach (var video in stored.Values)
                {
                    if (video.ChannelId == null)
                    {
                        video.ChannelId = channel.Id;
                        result.LinkedCount++;
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                }

                var newIds = orderedVideoIds.Where(id => !stored.ContainsKey(id)).ToList();
                if (newIds.Count > 0)
                {
                    var newVideos = await _videoResolver.BuildNewVideosAsync(newIds, itemsByVideoId);
                    var builtIds = new HashSet<string>(newVideos.Select(v => v.ExternalId!), StringComparer.Ordinal);

                    foreach (var video in newVideos)
                    {
                        video.ChannelId = channel.Id;

                        // A video that names no topics or genres takes its channel's.
                        if (video.Topics.Count == 0)
                        {
                            foreach (var topic in channel.Topics) video.Topics.Add(topic);
                        }
                        if (video.Genres.Count == 0)
                        {
                            foreach (var genre in channel.Genres) video.Genres.Add(genre);
                        }

                        _context.Add(video);
                        result.CreatedCount++;
                    }

                    foreach (var missingId in newIds.Where(id => !builtIds.Contains(id)))
                    {
                        result.FailedCount++;
                        var title = itemsByVideoId[missingId].Snippet?.Title ?? missingId;
                        result.Warnings.Add($"YouTube returned no details for \"{title}\" ({missingId}); it was not imported.");
                    }
                }

                await _context.SaveChangesAsync();
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Imported latest uploads for channel {Title}: {Created} created, {Linked} linked, {Skipped} already stored, {Failed} without details",
                    channel.Title, result.CreatedCount, result.LinkedCount, result.SkippedCount, result.FailedCount);
                return result;
            }
            catch (Exception ex) when (ex is not ArgumentException and not YouTubeQuotaExceededException and not YouTubeNotConfiguredException)
            {
                _logger.LogError(ex, "Error occurred while importing latest uploads for channel {ChannelId}", channelId);
                throw;
            }
        }

        private async Task HandleTopicsAsync(YouTubeChannel channel, string[]? topics)
        {
            if (topics == null || topics.Length == 0)
                return;

            var resolver = new TopicResolver(_context);
            foreach (var name in topics.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var topic = await resolver.GetOrCreateAsync(name.Trim().ToLowerInvariant());
                if (topic != null && !channel.Topics.Contains(topic))
                {
                    channel.Topics.Add(topic);
                }
            }
        }

        private async Task HandleGenresAsync(YouTubeChannel channel, string[]? genres)
        {
            if (genres == null || genres.Length == 0)
                return;

            var resolver = new GenreResolver(_context);
            foreach (var name in GenreNames.NormalizeList(genres))
            {
                var genre = await resolver.GetOrCreateAsync(name);
                if (genre != null && !channel.Genres.Contains(genre))
                {
                    channel.Genres.Add(genre);
                }
            }
        }
    }
}

