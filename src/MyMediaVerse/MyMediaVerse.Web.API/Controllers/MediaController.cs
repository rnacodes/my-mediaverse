using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure; // To access the DbContext
using MyMediaVerse.DTOs;
using Microsoft.EntityFrameworkCore; // For ToListAsync, etc.
using System.Globalization; // For CultureInfo
using System.Text; // For Encoding
using System.IO; // For StringWriter
using CsvHelper; // For CsvHelper

namespace MyMediaVerse.Web.API.Controllers
{
[ApiController]
[Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly Infrastructure.Data.MediaLibraryDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public MediaController(Infrastructure.Data.MediaLibraryDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: api/media
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<MediaItemResponseDto>>> GetAllMedia()
        {
            try
            {
                var mediaItems = await _context.MediaItems
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(m => m.Mixlists)
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .ToListAsync();
                    
                var response = mediaItems.Select(item => new MediaItemResponseDto
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
                }).ToList();
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                Console.WriteLine($"ERROR in GetAllMedia: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return StatusCode(500, new { 
                    error = "Failed to retrieve media items", 
                    details = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }

        // POST: api/media
        [HttpPost]
        public async Task<IActionResult> AddMediaItem([FromBody] CreateMediaItemDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Media item data is null.");
            }

            // Create the appropriate concrete type based on MediaType
            BaseMediaItem mediaItem = dto.MediaType switch
            {
                MediaType.Article => await CreateArticleAsync(dto),
                MediaType.Podcast => await CreatePodcastAsync(dto),
                MediaType.Video => await CreateVideoAsync(dto),
                MediaType.Movie => await CreateMovieAsync(dto),
                MediaType.TVShow => await CreateTvShowAsync(dto),
                MediaType.Book => await CreateBookAsync(dto),
                MediaType.Channel => await CreateYouTubeChannelAsync(dto),
                // For any other types, return an error
                _ => throw new NotSupportedException($"Media type '{dto.MediaType}' is not yet supported. Please implement a concrete class for this media type.")
            };

            _context.Add(mediaItem);
            await _context.SaveChangesAsync();

            // Reload the entity with includes to properly serialize topics and genres
            var createdMediaItem = await _context.MediaItems
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .Include(m => m.Mixlists)
                .FirstOrDefaultAsync(m => m.Id == mediaItem.Id);

            var response = new MediaItemResponseDto
            {
                Id = createdMediaItem!.Id,
                Title = createdMediaItem.Title,
                MediaType = createdMediaItem.MediaType,
                Link = createdMediaItem.Link,
                Notes = createdMediaItem.Notes,
                DateAdded = createdMediaItem.DateAdded,
                Status = createdMediaItem.Status,
                DateCompleted = createdMediaItem.DateCompleted,
                Rating = createdMediaItem.Rating,
                OwnershipStatus = createdMediaItem.OwnershipStatus,
                Description = createdMediaItem.Description,
                RelatedNotes = createdMediaItem.RelatedNotes,
                Thumbnail = createdMediaItem.Thumbnail,
                Topics = createdMediaItem.Topics.Select(t => t.Name).ToArray(),
                Genres = createdMediaItem.Genres.Select(g => g.Name).ToArray(),
                MixlistIds = createdMediaItem.Mixlists.Select(m => m.Id).ToArray()
            };

            // Return the created item, including its new ID
            return CreatedAtAction(nameof(GetMediaItem), new { id = mediaItem.Id }, response);
        }

        /// <summary>
        /// Helper method to add Topics to a media item ensuring proper EF Core change tracking
        /// OPTIMIZED: Batches database operations to prevent N+1 queries
        /// </summary>
        private async Task AddTopicsToMediaItemAsync(BaseMediaItem mediaItem, string[] topicNames)
        {
            if (topicNames == null || topicNames.Length == 0)
                return;

            // Normalize all topic names at once
            var normalizedTopicNames = topicNames
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (!normalizedTopicNames.Any())
                return;

            // ✅ Single query to fetch all existing topics
            var existingTopics = await _context.Topics
                .AsNoTracking()
                .Where(t => normalizedTopicNames.Contains(t.Name))
                .ToListAsync();

            var existingTopicNames = existingTopics.Select(t => t.Name).ToHashSet();
            var newTopicNames = normalizedTopicNames.Except(existingTopicNames).ToList();

            // ✅ Batch create all new topics at once
            if (newTopicNames.Any())
            {
                var newTopics = newTopicNames.Select(name => new Topic { Name = name }).ToList();
                _context.Topics.AddRange(newTopics);
                await _context.SaveChangesAsync(); // Only 1 round trip for all new topics
                existingTopics.AddRange(newTopics);
            }

            // ✅ Load all topics into tracking context and add to media item
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

        /// <summary>
        /// Helper method to add Genres to a media item ensuring proper EF Core change tracking
        /// OPTIMIZED: Batches database operations to prevent N+1 queries
        /// </summary>
        private async Task AddGenresToMediaItemAsync(BaseMediaItem mediaItem, string[] genreNames)
        {
            if (genreNames == null || genreNames.Length == 0)
                return;

            // Normalize all genre names at once
            var normalizedGenreNames = genreNames
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            if (!normalizedGenreNames.Any())
                return;

            // ✅ Single query to fetch all existing genres
            var existingGenres = await _context.Genres
                .AsNoTracking()
                .Where(g => normalizedGenreNames.Contains(g.Name))
                .ToListAsync();

            var existingGenreNames = existingGenres.Select(g => g.Name).ToHashSet();
            var newGenreNames = normalizedGenreNames.Except(existingGenreNames).ToList();

            // ✅ Batch create all new genres at once
            if (newGenreNames.Any())
            {
                var newGenres = newGenreNames.Select(name => new Genre { Name = name }).ToList();
                _context.Genres.AddRange(newGenres);
                await _context.SaveChangesAsync(); // Only 1 round trip for all new genres
                existingGenres.AddRange(newGenres);
            }

            // ✅ Load all genres into tracking context and add to media item
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

        private async Task<PodcastSeries> CreatePodcastAsync(CreateMediaItemDto dto)
        {
            var podcast = new PodcastSeries
            {
                Title = dto.Title,
                MediaType = MediaType.Podcast,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = dto.DateCompleted?.ToUniversalTime(),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail
            };

            // Use helper methods to add Topics and Genres with proper change tracking
            await AddTopicsToMediaItemAsync(podcast, dto.Topics);
            await AddGenresToMediaItemAsync(podcast, dto.Genres);

            return podcast;
        }

        private async Task<Video> CreateVideoAsync(CreateMediaItemDto dto)
        {
            var video = new Video
            {
                Title = dto.Title,
                MediaType = dto.MediaType,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = dto.DateCompleted?.ToUniversalTime(),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail,
                Platform = "YouTube", // Default platform for videos created via MediaController
                ChannelId = null, // Will be set by frontend or YouTube import
                VideoType = VideoType.Series // Default to Series for now
            };

            // Use helper methods to add Topics and Genres with proper change tracking
            await AddTopicsToMediaItemAsync(video, dto.Topics);
            await AddGenresToMediaItemAsync(video, dto.Genres);

            return video;
        }

        private async Task<Article> CreateArticleAsync(CreateMediaItemDto dto)
        {
            var article = new Article
            {
                Title = dto.Title,
                MediaType = dto.MediaType,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = dto.DateCompleted?.ToUniversalTime(),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail,
                ReadingProgress = 0,
                IsStarred = false,
                IsArchived = false
            };

            // Use helper methods to add Topics and Genres with proper change tracking
            await AddTopicsToMediaItemAsync(article, dto.Topics);
            await AddGenresToMediaItemAsync(article, dto.Genres);

            return article;
        }

        private async Task<YouTubeChannel> CreateYouTubeChannelAsync(CreateMediaItemDto dto)
        {
            var channel = new YouTubeChannel
            {
                Title = dto.Title,
                MediaType = MediaType.Channel,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = dto.DateCompleted?.ToUniversalTime(),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail,
                ChannelExternalId = "", // This should be set when importing from YouTube API
                LastSyncedAt = DateTime.UtcNow
            };

            // Use helper methods to add Topics and Genres with proper change tracking
            await AddTopicsToMediaItemAsync(channel, dto.Topics);
            await AddGenresToMediaItemAsync(channel, dto.Genres);

            return channel;
        }

        private async Task<Movie> CreateMovieAsync(CreateMediaItemDto dto)
        {
            var movie = new Movie
            {
                Title = dto.Title,
                MediaType = MediaType.Movie,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = dto.DateCompleted?.ToUniversalTime(),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail
            };

            // Use helper methods to add Topics and Genres with proper change tracking
            await AddTopicsToMediaItemAsync(movie, dto.Topics);
            await AddGenresToMediaItemAsync(movie, dto.Genres);

            return movie;
        }

        private async Task<TvShow> CreateTvShowAsync(CreateMediaItemDto dto)
        {
            var tvShow = new TvShow
            {
                Title = dto.Title,
                MediaType = MediaType.TVShow,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = dto.DateCompleted?.ToUniversalTime(),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail
            };

            // Use helper methods to add Topics and Genres with proper change tracking
            await AddTopicsToMediaItemAsync(tvShow, dto.Topics);
            await AddGenresToMediaItemAsync(tvShow, dto.Genres);

            return tvShow;
        }

        private async Task<Book> CreateBookAsync(CreateMediaItemDto dto)
        {
            var book = new Book
            {
                Title = dto.Title,
                Author = "", // Will be set by frontend or book import
                MediaType = MediaType.Book,
                Link = dto.Link,
                Notes = dto.Notes,
                Status = dto.Status,
                DateAdded = DateTime.UtcNow,
                DateCompleted = dto.DateCompleted?.ToUniversalTime(),
                Rating = dto.Rating,
                OwnershipStatus = dto.OwnershipStatus,
                Description = dto.Description,
                RelatedNotes = dto.RelatedNotes,
                Thumbnail = dto.Thumbnail
            };

            // Use helper methods to add Topics and Genres with proper change tracking
            await AddTopicsToMediaItemAsync(book, dto.Topics);
            await AddGenresToMediaItemAsync(book, dto.Genres);

            return book;
        }

        // GET: api/media/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<MediaItemResponseDto>> GetMediaItem(Guid id)
        {
            try
            {
                var mediaItem = await _context.MediaItems
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(m => m.Mixlists)
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mediaItem == null)
                {
                    return NotFound($"Media item with ID {id} not found.");
                }

                var response = new MediaItemResponseDto
                {
                    Id = mediaItem.Id,
                    Title = mediaItem.Title,
                    MediaType = mediaItem.MediaType,
                    Link = mediaItem.Link,
                    Notes = mediaItem.Notes,
                    DateAdded = mediaItem.DateAdded,
                    Status = mediaItem.Status,
                    DateCompleted = mediaItem.DateCompleted,
                    Rating = mediaItem.Rating,
                    OwnershipStatus = mediaItem.OwnershipStatus,
                    Description = mediaItem.Description,
                    RelatedNotes = mediaItem.RelatedNotes,
                    Thumbnail = mediaItem.Thumbnail,
                    Topics = mediaItem.Topics.Select(t => t.Name).ToArray(),
                    Genres = mediaItem.Genres.Select(g => g.Name).ToArray(),
                    MixlistIds = mediaItem.Mixlists.Select(m => m.Id).ToArray()
                };

                // Add website-specific properties if applicable
                if (mediaItem is Website website)
                {
                    response.RssFeedUrl = website.RssFeedUrl;
                    response.Domain = website.Domain;
                    response.Author = website.Author;
                    response.Publication = website.Publication;
                    response.LastCheckedDate = website.LastCheckedDate;
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve media item", details = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // PUT: api/media/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMediaItem(Guid id, [FromBody] CreateMediaItemDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Media item data is null.");
            }

            var existingItem = await _context.MediaItems
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (existingItem == null)
            {
                return NotFound($"Media item with ID {id} not found.");
            }

            try
            {
                // Update basic properties
                existingItem.Title = dto.Title;
                existingItem.MediaType = dto.MediaType;
                existingItem.Link = dto.Link;
                existingItem.Notes = dto.Notes;
                existingItem.Status = dto.Status;
                existingItem.DateCompleted = dto.DateCompleted;
                existingItem.Rating = dto.Rating;
                existingItem.OwnershipStatus = dto.OwnershipStatus;
                existingItem.Description = dto.Description;
                existingItem.RelatedNotes = dto.RelatedNotes;
                existingItem.Thumbnail = dto.Thumbnail;

                // Clear existing topics and genres and save immediately
                existingItem.Topics.Clear();
                existingItem.Genres.Clear();
                await _context.SaveChangesAsync();

                // Process topics: ensure they exist first, then create associations
                if (dto.Topics?.Length > 0)
                {
                    foreach (var topicName in dto.Topics.Where(t => !string.IsNullOrWhiteSpace(t)))
                    {
                        var normalizedTopicName = topicName.Trim().ToLowerInvariant();
                        
                        // Check if topic exists using AsNoTracking to avoid tracking conflicts
                        var topic = await _context.Topics
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);
                        
                        if (topic == null)
                        {
                            // Create new topic and save immediately
                            topic = new Topic { Name = normalizedTopicName };
                            _context.Topics.Add(topic);
                            await _context.SaveChangesAsync();
                        }
                        
                        // Now attach the topic to the media item using a fresh query
                        var trackedTopic = await _context.Topics.FindAsync(topic.Id);
                        if (trackedTopic != null && !existingItem.Topics.Any(t => t.Id == trackedTopic.Id))
                        {
                            existingItem.Topics.Add(trackedTopic);
                        }
                    }
                }

                // Process genres: ensure they exist first, then create associations
                if (dto.Genres?.Length > 0)
                {
                    foreach (var genreName in dto.Genres.Where(g => !string.IsNullOrWhiteSpace(g)))
                    {
                        var normalizedGenreName = genreName.Trim().ToLowerInvariant();
                        
                        // Check if genre exists using AsNoTracking to avoid tracking conflicts
                        var genre = await _context.Genres
                            .AsNoTracking()
                            .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);
                        
                        if (genre == null)
                        {
                            // Create new genre and save immediately
                            genre = new Genre { Name = normalizedGenreName };
                            _context.Genres.Add(genre);
                            await _context.SaveChangesAsync();
                        }
                        
                        // Now attach the genre to the media item using a fresh query
                        var trackedGenre = await _context.Genres.FindAsync(genre.Id);
                        if (trackedGenre != null && !existingItem.Genres.Any(g => g.Id == trackedGenre.Id))
                        {
                            existingItem.Genres.Add(trackedGenre);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Reload with mixlists to return complete DTO
                var updatedItem = await _context.MediaItems
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .Include(m => m.Mixlists)
                    .FirstOrDefaultAsync(m => m.Id == id);

                var response = new MediaItemResponseDto
                {
                    Id = updatedItem!.Id,
                    Title = updatedItem.Title,
                    MediaType = updatedItem.MediaType,
                    Link = updatedItem.Link,
                    Notes = updatedItem.Notes,
                    DateAdded = updatedItem.DateAdded,
                    Status = updatedItem.Status,
                    DateCompleted = updatedItem.DateCompleted,
                    Rating = updatedItem.Rating,
                    OwnershipStatus = updatedItem.OwnershipStatus,
                    Description = updatedItem.Description,
                    RelatedNotes = updatedItem.RelatedNotes,
                    Thumbnail = updatedItem.Thumbnail,
                    Topics = updatedItem.Topics.Select(t => t.Name).ToArray(),
                    Genres = updatedItem.Genres.Select(g => g.Name).ToArray(),
                    MixlistIds = updatedItem.Mixlists.Select(m => m.Id).ToArray()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update media item", details = ex.Message });
            }
        }

        // DELETE: api/media/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMediaItem(Guid id)
        {
            try
            {
                var mediaItem = await _context.MediaItems
                    .Include(m => m.Mixlists)
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mediaItem == null)
                {
                    return NotFound($"Media item with ID {id} not found.");
                }

                // Delete thumbnail from S3 if it exists
                if (!string.IsNullOrEmpty(mediaItem.Thumbnail))
                {
                    await DeleteThumbnailFromS3(mediaItem.Thumbnail);
                }

                // Remove from all mixlists
                mediaItem.Mixlists.Clear();
                mediaItem.Topics.Clear();
                mediaItem.Genres.Clear();

                _context.MediaItems.Remove(mediaItem);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete media item", details = ex.Message });
            }
        }

        // DELETE: api/media/bulk
        [HttpDelete("bulk")]
        public async Task<IActionResult> BulkDeleteMediaItems([FromBody] BulkDeleteRequest request)
        {
            try
            {
                if (request.Ids == null || !request.Ids.Any())
                {
                    return BadRequest("No media IDs provided for deletion.");
                }

                var mediaItems = await _context.MediaItems
                    .Include(m => m.Mixlists)
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .Where(m => request.Ids.Contains(m.Id))
                    .ToListAsync();

                if (!mediaItems.Any())
                {
                    return NotFound("No media items found with the provided IDs.");
                }

                var deletedCount = 0;
                var thumbnailsDeletionErrors = new List<string>();

                foreach (var mediaItem in mediaItems)
                {
                    // Delete thumbnail from S3 if it exists
                    if (!string.IsNullOrEmpty(mediaItem.Thumbnail))
                    {
                        try
                        {
                            await DeleteThumbnailFromS3(mediaItem.Thumbnail);
                        }
                        catch (Exception ex)
                        {
                            thumbnailsDeletionErrors.Add($"Failed to delete thumbnail for '{mediaItem.Title}': {ex.Message}");
                        }
                    }

                    // Remove from all mixlists
                    mediaItem.Mixlists.Clear();
                    mediaItem.Topics.Clear();
                    mediaItem.Genres.Clear();

                    _context.MediaItems.Remove(mediaItem);
                    deletedCount++;
                }

                await _context.SaveChangesAsync();

                var response = new
                {
                    message = $"Successfully deleted {deletedCount} media item{(deletedCount != 1 ? "s" : "")}",
                    deletedCount = deletedCount,
                    thumbnailsDeletionErrors = thumbnailsDeletionErrors.Any() ? thumbnailsDeletionErrors : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to bulk delete media items", details = ex.Message });
            }
        }

        private async Task DeleteThumbnailFromS3(string thumbnailUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(thumbnailUrl))
                    return;

                // Call the UploadController's delete endpoint
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(Request.Scheme + "://" + Request.Host);
                
                var response = await httpClient.DeleteAsync($"/api/upload/thumbnail?url={Uri.EscapeDataString(thumbnailUrl)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to delete thumbnail: {thumbnailUrl}. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the entire operation if thumbnail deletion fails
                Console.WriteLine($"Error deleting thumbnail {thumbnailUrl}: {ex.Message}");
            }
        }

        // GET: api/media/search?query={query}
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BaseMediaItem>>> SearchMedia([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            try
            {
                var lowerQuery = query.ToLowerInvariant();
                var results = await _context.MediaItems
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
                    .Take(100) // Limit results for performance
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Search failed", details = ex.Message });
            }
        }

        // GET: api/media/by-topic/{topicId}
        [HttpGet("by-topic/{topicId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<MediaItemResponseDto>>> GetMediaByTopic(Guid topicId)
        {
            try
            {
                var mediaItems = await _context.MediaItems
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(m => m.Topics.Any(t => t.Id == topicId))
                    .Include(m => m.Mixlists)
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .ToListAsync();

                var response = mediaItems.Select(item => new MediaItemResponseDto
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
                    Topics = item.Topics.Select(t => t.Name).ToArray(),
                    Genres = item.Genres.Select(g => g.Name).ToArray(),
                    MixlistIds = item.Mixlists.Select(m => m.Id).ToArray()
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve media by topic", details = ex.Message });
            }
        }

        // GET: api/media/by-genre/{genreId}
        [HttpGet("by-genre/{genreId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<MediaItemResponseDto>>> GetMediaByGenre(Guid genreId)
        {
            try
            {
                var mediaItems = await _context.MediaItems
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(m => m.Genres.Any(g => g.Id == genreId))
                    .Include(m => m.Mixlists)
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .ToListAsync();

                var response = mediaItems.Select(item => new MediaItemResponseDto
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
                    Topics = item.Topics.Select(t => t.Name).ToArray(),
                    Genres = item.Genres.Select(g => g.Name).ToArray(),
                    MixlistIds = item.Mixlists.Select(m => m.Id).ToArray()
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve media by genre", details = ex.Message });
            }
        }

        // GET: api/media/by-type/{mediaType}
        [HttpGet("by-type/{mediaType}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<MediaItemResponseDto>>> GetMediaByType(string mediaType)
        {
            try
            {
                if (!Enum.TryParse<MediaType>(mediaType, true, out var parsedMediaType))
                {
                    return BadRequest($"Invalid media type: {mediaType}");
                }

                var mediaItems = await _context.MediaItems
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(m => m.MediaType == parsedMediaType)
                    .Include(m => m.Mixlists)
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .ToListAsync();

                var response = mediaItems.Select(item => new MediaItemResponseDto
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
                    Topics = item.Topics.Select(t => t.Name).ToArray(),
                    Genres = item.Genres.Select(g => g.Name).ToArray(),
                    MixlistIds = item.Mixlists.Select(m => m.Id).ToArray()
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve media by type", details = ex.Message });
            }
        }

        // GET: api/media/{id}/export
        [HttpGet("{id:guid}/export")]
        [AllowAnonymous]
        public async Task<IActionResult> ExportMediaItem(Guid id)
        {
            try
            {
                var mediaItem = await _context.MediaItems
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .Include(m => m.Mixlists)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mediaItem == null)
                {
                    return NotFound($"Media item with ID {id} not found.");
                }

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
                using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
                
                csv.WriteRecords(csvData);
                
                var csvContent = writer.ToString();
                var fileName = $"media-item-{mediaItem.Title.Replace(" ", "-")}-{DateTime.Now:yyyyMMdd}.csv";
                
                return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export media item", details = ex.Message });
            }
        }

        // GET: api/media/export
        [HttpGet("export")]
        [AllowAnonymous]
        public async Task<IActionResult> ExportAllMedia()
        {
            try
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
                using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
                
                csv.WriteRecords(csvData);
                
                var csvContent = writer.ToString();
                var fileName = $"all-media-{DateTime.Now:yyyyMMdd}.csv";
                
                return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export media items", details = ex.Message });
            }
        }
    }

    public class BulkDeleteRequest
    {
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }
}