using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;

namespace MyMediaVerse.Application.Services
{
    public class MediaService : IMediaService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<MediaService> _logger;
        private readonly IThumbnailStorageService _thumbnailStorage;

        public MediaService(
            IApplicationDbContext context,
            ILogger<MediaService> logger,
            IThumbnailStorageService thumbnailStorage)
        {
            _context = context;
            _logger = logger;
            _thumbnailStorage = thumbnailStorage;
        }

        public async Task<IEnumerable<MediaItemResponseDto>> GetAllMediaAsync()
        {
            var mediaItems = await _context.MediaItems
                .AsNoTracking()
                .AsSplitQuery()
                .Include(m => m.Mixlists)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .ToListAsync();

            return mediaItems.Select(MapToResponseDto).ToList();
        }

        public async Task<MediaItemResponseDto?> GetMediaItemAsync(Guid id)
        {
            var mediaItem = await _context.MediaItems
                .AsNoTracking()
                .AsSplitQuery()
                .Include(m => m.Mixlists)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mediaItem == null)
                return null;

            return MapToResponseDto(mediaItem);
        }

        public async Task<IEnumerable<MediaItemResponseDto>> SearchMediaAsync(string query)
        {
            var lowerQuery = query.ToLowerInvariant();
            var mediaItems = await _context.MediaItems
                .AsNoTracking()
                .AsSplitQuery()
                .Where(m => m.Title.ToLower().Contains(lowerQuery) ||
                           (m.Description != null && m.Description.ToLower().Contains(lowerQuery)) ||
                           (m.Topics.Any(t => t.Name.ToLower().Contains(lowerQuery))) ||
                           (m.Genres.Any(g => g.Name.ToLower().Contains(lowerQuery))) ||
                           m.MediaType.ToString().ToLower().Contains(lowerQuery))
                .Include(m => m.Mixlists)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .Take(100)
                .ToListAsync();

            return mediaItems.Select(MapToResponseDto).ToList();
        }

        public async Task<IEnumerable<MediaItemResponseDto>> GetMediaByTopicAsync(Guid topicId)
        {
            var mediaItems = await _context.MediaItems
                .AsNoTracking()
                .AsSplitQuery()
                .Where(m => m.Topics.Any(t => t.Id == topicId))
                .Include(m => m.Mixlists)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .ToListAsync();

            return mediaItems.Select(MapToResponseDto).ToList();
        }

        public async Task<IEnumerable<MediaItemResponseDto>> GetMediaByGenreAsync(Guid genreId)
        {
            var mediaItems = await _context.MediaItems
                .AsNoTracking()
                .AsSplitQuery()
                .Where(m => m.Genres.Any(g => g.Id == genreId))
                .Include(m => m.Mixlists)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .ToListAsync();

            return mediaItems.Select(MapToResponseDto).ToList();
        }

        public async Task<IEnumerable<MediaItemResponseDto>> GetMediaByTypeAsync(string mediaType)
        {
            if (!Enum.TryParse<MediaType>(mediaType, true, out var parsedMediaType))
                throw new ArgumentException($"Invalid media type: {mediaType}");

            var mediaItems = await _context.MediaItems
                .AsNoTracking()
                .AsSplitQuery()
                .Where(m => m.MediaType == parsedMediaType)
                .Include(m => m.Mixlists)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .ToListAsync();

            return mediaItems.Select(MapToResponseDto).ToList();
        }

        public async Task<MediaItemResponseDto> CreateMediaItemAsync(CreateMediaItemDto dto)
        {
            if (dto.MediaType == MediaType.Article)
            {
                var existingArticle = await ArticleDuplicateFinder.FindExistingAsync(_context.Articles, null, dto.Link);
                if (existingArticle != null)
                {
                    throw new InvalidOperationException(
                        $"An article with this URL already exists (ID: {existingArticle.Id}).");
                }
            }

            BaseMediaItem mediaItem = dto.MediaType switch
            {
                MediaType.Article => CreateArticle(dto),
                MediaType.Podcast => CreatePodcast(dto),
                MediaType.Video => CreateVideo(dto),
                MediaType.Movie => CreateMovie(dto),
                MediaType.TVShow => CreateTvShow(dto),
                MediaType.Book => throw new NotSupportedException("Books must be created via POST /api/book."),
                MediaType.Channel => CreateYouTubeChannel(dto),
                _ => throw new NotSupportedException($"Media type '{dto.MediaType}' is not yet supported. Please implement a concrete class for this media type.")
            };

            await AddTopicsToMediaItemAsync(mediaItem, dto.Topics);
            await AddGenresToMediaItemAsync(mediaItem, dto.Genres);

            _context.Add(mediaItem);
            await _context.SaveChangesAsync();

            // Reload the entity with includes to properly serialize topics and genres
            var createdMediaItem = await _context.MediaItems
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .Include(m => m.Mixlists)
                .FirstOrDefaultAsync(m => m.Id == mediaItem.Id);

            return MapToResponseDto(createdMediaItem!);
        }

        public async Task<MediaItemResponseDto> UpdateMediaItemAsync(Guid id, CreateMediaItemDto dto)
        {
            var existingItem = await _context.MediaItems
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingItem == null)
                throw new KeyNotFoundException($"Media item with ID {id} not found.");

            // Update basic properties
            existingItem.Title = dto.Title;
            existingItem.MediaType = dto.MediaType;
            existingItem.Link = dto.Link;
            existingItem.Notes = dto.Notes;
            existingItem.Status = dto.Status;
            existingItem.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);
            existingItem.Rating = dto.Rating;
            existingItem.OwnershipStatus = dto.OwnershipStatus;
            existingItem.Description = dto.Description;
            existingItem.RelatedNotes = dto.RelatedNotes;
            existingItem.Thumbnail = dto.Thumbnail;

            existingItem.Topics.Clear();
            existingItem.Genres.Clear();

            await AddTopicsToMediaItemAsync(existingItem, dto.Topics ?? Array.Empty<string>());
            await AddGenresToMediaItemAsync(existingItem, dto.Genres ?? Array.Empty<string>());

            await _context.SaveChangesAsync();

            // Reload with mixlists to return complete DTO
            var updatedItem = await _context.MediaItems
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .Include(m => m.Mixlists)
                .FirstOrDefaultAsync(m => m.Id == id);

            return MapToResponseDto(updatedItem!);
        }

        public async Task<bool> DeleteMediaItemAsync(Guid id)
        {
            var mediaItem = await _context.MediaItems
                .Include(m => m.Mixlists)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mediaItem == null)
                return false;

            if (!string.IsNullOrEmpty(mediaItem.Thumbnail))
            {
                await _thumbnailStorage.DeleteAsync(mediaItem.Thumbnail);
            }

            mediaItem.Mixlists.Clear();
            mediaItem.Topics.Clear();
            mediaItem.Genres.Clear();

            _context.Remove(mediaItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(int deletedCount, List<string> thumbnailErrors)> BulkDeleteMediaItemsAsync(List<Guid> ids)
        {
            var mediaItems = await _context.MediaItems
                .Include(m => m.Mixlists)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .Where(m => ids.Contains(m.Id))
                .ToListAsync();

            var deletedCount = 0;
            var thumbnailErrors = new List<string>();

            foreach (var mediaItem in mediaItems)
            {
                if (!string.IsNullOrEmpty(mediaItem.Thumbnail))
                {
                    try
                    {
                        await _thumbnailStorage.DeleteAsync(mediaItem.Thumbnail);
                    }
                    catch (Exception ex)
                    {
                        thumbnailErrors.Add($"Failed to delete thumbnail for '{mediaItem.Title}': {ex.Message}");
                    }
                }

                mediaItem.Mixlists.Clear();
                mediaItem.Topics.Clear();
                mediaItem.Genres.Clear();

                _context.Remove(mediaItem);
                deletedCount++;
            }

            await _context.SaveChangesAsync();

            return (deletedCount, thumbnailErrors);
        }

        public async Task<(byte[] content, string fileName)?> ExportMediaItemAsync(Guid id)
        {
            var mediaItem = await _context.MediaItems
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .Include(m => m.Mixlists)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mediaItem == null)
                return null;

            var csvData = new List<object>
            {
                new
                {
                    Id = mediaItem.Id,
                    Title = mediaItem.Title,
                    MediaType = mediaItem.MediaType.ToString(),
                    Link = mediaItem.Link ?? "",
                    Notes = mediaItem.Notes ?? "",
                    DateAdded = mediaItem.DateAdded.ToString("yyyy-MM-dd"),
                    Status = mediaItem.Status.ToString(),
                    DateCompleted = mediaItem.DateCompleted?.ToString("yyyy-MM-dd") ?? "",
                    Rating = mediaItem.Rating?.ToString() ?? "",
                    OwnershipStatus = mediaItem.OwnershipStatus?.ToString() ?? "",
                    Description = mediaItem.Description ?? "",
                    RelatedNotes = mediaItem.RelatedNotes ?? "",
                    Thumbnail = mediaItem.Thumbnail ?? "",
                    Topics = string.Join(";", mediaItem.Topics.Select(t => t.Name)),
                    Genres = string.Join(";", mediaItem.Genres.Select(g => g.Name)),
                    MixlistIds = string.Join(";", mediaItem.Mixlists.Select(m => m.Id))
                }
            };

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteRecords(csvData);

            var csvContent = writer.ToString();
            var fileName = $"media-item-{mediaItem.Title.Replace(" ", "-")}-{DateTime.Now:yyyyMMdd}.csv";

            return (Encoding.UTF8.GetBytes(csvContent), fileName);
        }

        public async Task<(byte[] content, string fileName)> ExportAllMediaAsync()
        {
            var mediaItems = await _context.MediaItems
                .AsNoTracking()
                .AsSplitQuery()
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .Include(m => m.Mixlists)
                .ToListAsync();

            var csvData = mediaItems.Select(item => new
            {
                Id = item.Id,
                Title = item.Title,
                MediaType = item.MediaType.ToString(),
                Link = item.Link ?? "",
                Notes = item.Notes ?? "",
                DateAdded = item.DateAdded.ToString("yyyy-MM-dd"),
                Status = item.Status.ToString(),
                DateCompleted = item.DateCompleted?.ToString("yyyy-MM-dd") ?? "",
                Rating = item.Rating?.ToString() ?? "",
                OwnershipStatus = item.OwnershipStatus?.ToString() ?? "",
                Description = item.Description ?? "",
                RelatedNotes = item.RelatedNotes ?? "",
                Thumbnail = item.Thumbnail ?? "",
                Topics = string.Join(";", item.Topics.Select(t => t.Name)),
                Genres = string.Join(";", item.Genres.Select(g => g.Name)),
                MixlistIds = string.Join(";", item.Mixlists.Select(m => m.Id))
            }).ToList();

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteRecords(csvData);

            var csvContent = writer.ToString();
            var fileName = $"all-media-{DateTime.Now:yyyyMMdd}.csv";

            return (Encoding.UTF8.GetBytes(csvContent), fileName);
        }

        // --- Private helpers ---

        private static MediaItemResponseDto MapToResponseDto(BaseMediaItem item)
        {
            var response = new MediaItemResponseDto
            {
                Id = item.Id,
                Title = item.Title,
                MediaType = item.MediaType,
                Link = item.Link,
                Notes = item.Notes,
                DateAdded = item.DateAdded,
                Status = item.Status,
                DateCompleted = item.DateCompleted,
                Rating = item.Rating,
                OwnershipStatus = item.OwnershipStatus,
                Description = item.Description,
                RelatedNotes = item.RelatedNotes,
                Thumbnail = item.Thumbnail,
                Topics = item.Topics?.Select(t => t.Name).ToArray() ?? Array.Empty<string>(),
                Genres = item.Genres?.Select(g => g.Name).ToArray() ?? Array.Empty<string>(),
                MixlistIds = item.Mixlists?.Select(m => m.Id).ToArray() ?? Array.Empty<Guid>()
            };

            // Add website-specific properties if applicable
            if (item is Website website)
            {
                response.RssFeedUrl = website.RssFeedUrl;
                response.Domain = website.Domain;
                response.Author = website.Author;
                response.Publication = website.Publication;
                response.LastCheckedDate = website.LastCheckedDate;
            }

            return response;
        }

        private async Task AddTopicsToMediaItemAsync(BaseMediaItem mediaItem, string[] topicNames)
        {
            if (topicNames == null || topicNames.Length == 0)
                return;

            var normalizedTopicNames = topicNames
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (!normalizedTopicNames.Any())
                return;

            var existingTopics = await _context.Topics
                .AsNoTracking()
                .Where(t => normalizedTopicNames.Contains(t.Name))
                .ToListAsync();

            var existingTopicNames = existingTopics.Select(t => t.Name).ToHashSet();
            var newTopicNames = normalizedTopicNames.Except(existingTopicNames).ToList();

            if (newTopicNames.Any())
            {
                var newTopics = newTopicNames.Select(name => new Topic { Name = name }).ToList();
                _context.AddRange(newTopics);
                await _context.SaveChangesAsync();
                existingTopics.AddRange(newTopics);
            }

            var topicIds = existingTopics.Select(t => t.Id).ToList();
            var trackedTopics = await _context.Topics
                .Where(t => topicIds.Contains(t.Id))
                .ToListAsync();

            foreach (var topic in trackedTopics)
            {
                if (!mediaItem.Topics.Any(t => t.Id == topic.Id))
                {
                    mediaItem.Topics.Add(topic);
                }
            }
        }

        private async Task AddGenresToMediaItemAsync(BaseMediaItem mediaItem, string[] genreNames)
        {
            if (genreNames == null || genreNames.Length == 0)
                return;

            var normalizedGenreNames = genreNames
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (!normalizedGenreNames.Any())
                return;

            var existingGenres = await _context.Genres
                .AsNoTracking()
                .Where(g => normalizedGenreNames.Contains(g.Name))
                .ToListAsync();

            var existingGenreNames = existingGenres.Select(g => g.Name).ToHashSet();
            var newGenreNames = normalizedGenreNames.Except(existingGenreNames).ToList();

            if (newGenreNames.Any())
            {
                var newGenres = newGenreNames.Select(name => new Genre { Name = name }).ToList();
                _context.AddRange(newGenres);
                await _context.SaveChangesAsync();
                existingGenres.AddRange(newGenres);
            }

            var genreIds = existingGenres.Select(g => g.Id).ToList();
            var trackedGenres = await _context.Genres
                .Where(g => genreIds.Contains(g.Id))
                .ToListAsync();

            foreach (var genre in trackedGenres)
            {
                if (!mediaItem.Genres.Any(g => g.Id == genre.Id))
                {
                    mediaItem.Genres.Add(genre);
                }
            }
        }

        private static Article CreateArticle(CreateMediaItemDto dto)
        {
            return new Article
            {
                Title = dto.Title,
                MediaType = dto.MediaType,
                Link = string.IsNullOrWhiteSpace(dto.Link) ? dto.Link : UrlNormalizer.Normalize(dto.Link),
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail,
                ReadingProgress = 0,
                IsStarred = false,
                IsArchived = false
            };
        }

        private static PodcastSeries CreatePodcast(CreateMediaItemDto dto)
        {
            return new PodcastSeries
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
                Thumbnail = dto.Thumbnail
            };
        }

        private static Video CreateVideo(CreateMediaItemDto dto)
        {
            return new Video
            {
                Title = dto.Title,
                MediaType = dto.MediaType,
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
                Platform = "YouTube",
                ChannelId = null
            };
        }

        private static Movie CreateMovie(CreateMediaItemDto dto)
        {
            return new Movie
            {
                Title = dto.Title,
                MediaType = MediaType.Movie,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail
            };
        }

        private static TvShow CreateTvShow(CreateMediaItemDto dto)
        {
            return new TvShow
            {
                Title = dto.Title,
                MediaType = MediaType.TVShow,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail
            };
        }

        private static YouTubeChannel CreateYouTubeChannel(CreateMediaItemDto dto)
        {
            return new YouTubeChannel
            {
                Title = dto.Title,
                MediaType = MediaType.Channel,
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
                ChannelExternalId = "",
                LastSyncedAt = DateTime.UtcNow
            };
        }

    }
}
