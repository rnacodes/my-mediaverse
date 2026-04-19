using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class YouTubeChannelService : IYouTubeChannelService
    {
        private readonly IApplicationDbContext _context;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly IYouTubeMappingService _mappingService;
        private readonly ILogger<YouTubeChannelService> _logger;

        public YouTubeChannelService(
            IApplicationDbContext context,
            IYouTubeApiClient youTubeApiClient,
            IYouTubeMappingService mappingService,
            ILogger<YouTubeChannelService> logger)
        {
            _context = context;
            _youTubeApiClient = youTubeApiClient;
            _mappingService = mappingService;
            _logger = logger;
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

                // Handle Topics
                if (dto.Topics?.Length > 0)
                {
                    foreach (var topicName in dto.Topics.Where(t => !string.IsNullOrWhiteSpace(t)))
                    {
                        var normalizedTopicName = topicName.Trim().ToLowerInvariant();
                        var existingTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == normalizedTopicName);
                        if (existingTopic != null)
                        {
                            channel.Topics.Add(existingTopic);
                        }
                        else
                        {
                            channel.Topics.Add(new Topic { Name = normalizedTopicName });
                        }
                    }
                }

                // Handle Genres
                if (dto.Genres?.Length > 0)
                {
                    foreach (var genreName in dto.Genres.Where(g => !string.IsNullOrWhiteSpace(g)))
                    {
                        var normalizedGenreName = genreName.Trim().ToLowerInvariant();
                        var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == normalizedGenreName);
                        if (existingGenre != null)
                        {
                            channel.Genres.Add(existingGenre);
                        }
                        else
                        {
                            channel.Genres.Add(new Genre { Name = normalizedGenreName });
                        }
                    }
                }

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
                channel.DateCompleted = dto.DateCompleted;
                channel.Rating = dto.Rating;
                channel.Notes = dto.Notes;
                channel.RelatedNotes = dto.RelatedNotes;

                // Update Topics
                channel.Topics.Clear();
                if (dto.Topics?.Length > 0)
                {
                    foreach (var topicName in dto.Topics.Where(t => !string.IsNullOrWhiteSpace(t)))
                    {
                        var normalizedTopicName = topicName.Trim().ToLowerInvariant();
                        var existingTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == normalizedTopicName);
                        if (existingTopic != null)
                        {
                            channel.Topics.Add(existingTopic);
                        }
                        else
                        {
                            channel.Topics.Add(new Topic { Name = normalizedTopicName });
                        }
                    }
                }

                // Update Genres
                channel.Genres.Clear();
                if (dto.Genres?.Length > 0)
                {
                    foreach (var genreName in dto.Genres.Where(g => !string.IsNullOrWhiteSpace(g)))
                    {
                        var normalizedGenreName = genreName.Trim().ToLowerInvariant();
                        var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == normalizedGenreName);
                        if (existingGenre != null)
                        {
                            channel.Genres.Add(existingGenre);
                        }
                        else
                        {
                            channel.Genres.Add(new Genre { Name = normalizedGenreName });
                        }
                    }
                }

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
                var channel = await _context.FindAsync<YouTubeChannel>(id);
                
                if (channel == null)
                {
                    return false;
                }

                _context.Remove(channel);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted YouTube channel with ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting YouTube channel with ID {Id}", id);
                throw;
            }
        }

        public async Task<YouTubeChannel> ImportChannelFromYouTubeAsync(string channelId)
        {
            try
            {
                // Check if channel already exists
                var existingChannel = await GetChannelByExternalIdAsync(channelId);
                if (existingChannel != null)
                {
                    _logger.LogInformation("Channel {ChannelId} already exists, returning existing channel", channelId);
                    return existingChannel;
                }

                // Fetch channel data from YouTube API
                var channelDto = await _youTubeApiClient.GetChannelDetailsAsync(channelId);
                if (channelDto == null)
                {
                    throw new InvalidOperationException($"Channel {channelId} not found on YouTube");
                }

                // Map to entity
                var channel = _mappingService.MapChannelToYouTubeChannelEntity(channelDto);

                _context.Add(channel);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Imported YouTube channel {Title} with external ID {ExternalId}", channel.Title, channel.ChannelExternalId);
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing YouTube channel {ChannelId}", channelId);
                throw;
            }
        }

        public async Task<YouTubeChannel> SyncChannelMetadataAsync(Guid channelId)
        {
            try
            {
                var channel = await _context.FindAsync<YouTubeChannel>(channelId);
                if (channel == null)
                {
                    throw new ArgumentException($"Channel with ID {channelId} not found");
                }

                // Fetch latest data from YouTube API
                var channelDto = await _youTubeApiClient.GetChannelDetailsAsync(channel.ChannelExternalId);
                if (channelDto == null)
                {
                    throw new InvalidOperationException($"Channel {channel.ChannelExternalId} not found on YouTube");
                }

                // Update metadata
                channel.Title = channelDto.Snippet?.Title ?? channel.Title;
                channel.Description = channelDto.Snippet?.Description ?? channel.Description;
                channel.Thumbnail = _mappingService.MapChannelToYouTubeChannelEntity(channelDto).Thumbnail;
                channel.CustomUrl = channelDto.Snippet?.CustomUrl ?? channel.CustomUrl;
                channel.Country = channelDto.Snippet?.Country ?? channel.Country;
                channel.PublishedAt = channelDto.Snippet?.PublishedAt ?? channel.PublishedAt;

                // Update statistics
                if (channelDto.Statistics != null)
                {
                    if (long.TryParse(channelDto.Statistics.SubscriberCount, out var subscriberCount))
                        channel.SubscriberCount = subscriberCount;
                    
                    if (long.TryParse(channelDto.Statistics.VideoCount, out var videoCount))
                        channel.VideoCount = videoCount;
                    
                    if (long.TryParse(channelDto.Statistics.ViewCount, out var viewCount))
                        channel.ViewCount = viewCount;
                }

                // Update uploads playlist ID
                if (channelDto.ContentDetails?.RelatedPlaylists?.Uploads != null)
                {
                    channel.UploadsPlaylistId = channelDto.ContentDetails.RelatedPlaylists.Uploads;
                }

                channel.LastSyncedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Synced metadata for YouTube channel {Title}", channel.Title);
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing YouTube channel metadata for {ChannelId}", channelId);
                throw;
            }
        }

        public async Task<bool> ChannelExistsAsync(string externalId)
        {
            try
            {
                return await _context.YouTubeChannels
                    .AnyAsync(c => c.ChannelExternalId == externalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if channel exists with external ID {ExternalId}", externalId);
                throw;
            }
        }
    }
}

