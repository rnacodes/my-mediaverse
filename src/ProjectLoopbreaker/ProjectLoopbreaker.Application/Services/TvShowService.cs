using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectLoopbreaker.Domain.Interfaces;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.DTOs;
using ProjectLoopbreaker.Application.Interfaces;
using ProjectLoopbreaker.Application.Helpers;
using ProjectLoopbreaker.Shared.Interfaces;

namespace ProjectLoopbreaker.Application.Services
{
    public class TvShowService : ITvShowService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TvShowService> _logger;
        private readonly ITypeSenseService? _typeSenseService;

        public TvShowService(
            IApplicationDbContext context, 
            ILogger<TvShowService> logger,
            ITypeSenseService? typeSenseService = null)
        {
            _context = context;
            _logger = logger;
            _typeSenseService = typeSenseService;
        }

        public async Task<IEnumerable<TvShow>> GetAllTvShowsAsync()
        {
            try
            {
                return await _context.TvShows
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(t => t.Topics)
                    .Include(t => t.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all TV shows");
                throw;
            }
        }

        public async Task<TvShow?> GetTvShowByIdAsync(Guid id)
        {
            try
            {
                return await _context.TvShows
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(t => t.Topics)
                    .Include(t => t.Genres)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV show with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TvShow>> GetTvShowsByCreatorAsync(string creator)
        {
            try
            {
                return await _context.TvShows
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(t => t.Creator != null && t.Creator.ToLower().Contains(creator.ToLowerInvariant()))
                    .Include(t => t.Topics)
                    .Include(t => t.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV shows by creator: {Creator}", creator);
                throw;
            }
        }

        public async Task<IEnumerable<TvShow>> GetTvShowsByYearAsync(int year)
        {
            try
            {
                return await _context.TvShows
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(t => t.FirstAirYear == year)
                    .Include(t => t.Topics)
                    .Include(t => t.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV shows by year: {Year}", year);
                throw;
            }
        }

        public async Task<TvShow> CreateTvShowAsync(CreateTvShowDto dto)
        {
            try
            {
                if (dto == null)
                {
                    throw new ArgumentNullException(nameof(dto), "TV show data is required");
                }

                // Check if TV show already exists
                if (await TvShowExistsAsync(dto.Title, dto.FirstAirYear))
                {
                    _logger.LogWarning("TV show already exists: {Title} ({Year})", dto.Title, dto.FirstAirYear);
                    var existingTvShow = await GetTvShowByTitleAndYearAsync(dto.Title, dto.FirstAirYear);
                    if (existingTvShow != null)
                    {
                        return existingTvShow;
                    }
                }

                var tvShow = new TvShow
                {
                    Title = dto.Title,
                    MediaType = MediaType.TVShow,
                    Link = dto.Link,
                    Notes = dto.Notes,
                    Status = dto.Status,
                    DateAdded = DateTime.UtcNow,
                    DateCompleted = dto.DateCompleted,
                    Rating = dto.Rating,
                    OwnershipStatus = dto.OwnershipStatus,
                    Description = dto.Description,
                    RelatedNotes = dto.RelatedNotes,
                    Thumbnail = dto.Thumbnail,
                    Creator = dto.Creator,
                    Cast = dto.Cast,
                    FirstAirYear = dto.FirstAirYear,
                    LastAirYear = dto.LastAirYear,
                    NumberOfSeasons = dto.NumberOfSeasons,
                    NumberOfEpisodes = dto.NumberOfEpisodes,
                    ContentRating = dto.ContentRating,
                    TmdbId = dto.TmdbId,
                    TmdbRating = dto.TmdbRating,
                    TmdbPosterPath = dto.TmdbPosterPath,
                    Tagline = dto.Tagline,
                    Homepage = dto.Homepage,
                    OriginalLanguage = dto.OriginalLanguage,
                    OriginalName = dto.OriginalName
                };

                // Handle Topics array conversion
                await HandleTopicsAsync(tvShow, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(tvShow, dto.Genres);

                _context.Add(tvShow);
                await _context.SaveChangesAsync();

                // Index in Typesense after successful creation
                await TypesenseIndexingHelper.IndexMediaItemAsync(
                    tvShow,
                    _typeSenseService,
                    TypesenseIndexingHelper.GetTvShowFields(tvShow));

                _logger.LogInformation("Successfully created TV show: {Title} ({Year})", tvShow.Title, tvShow.FirstAirYear);
                return tvShow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating TV show");
                throw;
            }
        }

        public async Task<TvShow> UpdateTvShowAsync(Guid id, CreateTvShowDto dto)
        {
            try
            {
                var tvShow = await GetTvShowByIdAsync(id);
                if (tvShow == null)
                {
                    throw new InvalidOperationException($"TV show with ID {id} not found.");
                }

                // Update TV show properties
                tvShow.Title = dto.Title;
                tvShow.Link = dto.Link;
                tvShow.Notes = dto.Notes;
                tvShow.Status = dto.Status;
                tvShow.DateCompleted = dto.DateCompleted;
                tvShow.Rating = dto.Rating;
                tvShow.OwnershipStatus = dto.OwnershipStatus;
                tvShow.Description = dto.Description;
                tvShow.RelatedNotes = dto.RelatedNotes;
                tvShow.Thumbnail = dto.Thumbnail;
                tvShow.Creator = dto.Creator;
                tvShow.Cast = dto.Cast;
                tvShow.FirstAirYear = dto.FirstAirYear;
                tvShow.LastAirYear = dto.LastAirYear;
                tvShow.NumberOfSeasons = dto.NumberOfSeasons;
                tvShow.NumberOfEpisodes = dto.NumberOfEpisodes;
                tvShow.ContentRating = dto.ContentRating;
                tvShow.TmdbId = dto.TmdbId;
                tvShow.TmdbRating = dto.TmdbRating;
                tvShow.TmdbPosterPath = dto.TmdbPosterPath;
                tvShow.Tagline = dto.Tagline;
                tvShow.Homepage = dto.Homepage;
                tvShow.OriginalLanguage = dto.OriginalLanguage;
                tvShow.OriginalName = dto.OriginalName;

                // Clear existing topics and genres and save immediately to avoid FK conflicts
                tvShow.Topics.Clear();
                tvShow.Genres.Clear();
                // Clear change tracker and explicitly update the entity since it was retrieved with AsNoTracking
                _context.ClearChangeTracker();
                _context.Update(tvShow);
                await _context.SaveChangesAsync();

                // Handle Topics array conversion
                await HandleTopicsAsync(tvShow, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(tvShow, dto.Genres);

                await _context.SaveChangesAsync();

                // Re-index in Typesense after successful update
                await TypesenseIndexingHelper.IndexMediaItemAsync(
                    tvShow,
                    _typeSenseService,
                    TypesenseIndexingHelper.GetTvShowFields(tvShow));

                _logger.LogInformation("Successfully updated TV show: {Title} ({Year})", tvShow.Title, tvShow.FirstAirYear);
                return tvShow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating TV show with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTvShowAsync(Guid id)
        {
            try
            {
                var tvShow = await _context.FindAsync<TvShow>(id);
                if (tvShow == null)
                {
                    return false;
                }

                var tvShowId = tvShow.Id;
                var tvShowTitle = tvShow.Title;
                var tvShowYear = tvShow.FirstAirYear;

                _context.Remove(tvShow);
                await _context.SaveChangesAsync();

                // Delete from Typesense after successful deletion
                await TypesenseIndexingHelper.DeleteMediaItemAsync(tvShowId, _typeSenseService);

                _logger.LogInformation("Successfully deleted TV show: {Title} ({Year})", tvShowTitle, tvShowYear);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting TV show with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> TvShowExistsAsync(string title, int? firstAirYear = null)
        {
            try
            {
                var query = _context.TvShows.Where(t => t.Title.ToLower() == title.ToLower());
                
                if (firstAirYear.HasValue)
                {
                    query = query.Where(t => t.FirstAirYear == firstAirYear.Value);
                }
                
                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if TV show exists: {Title} ({Year})", title, firstAirYear);
                throw;
            }
        }

        public async Task<TvShow?> GetTvShowByTitleAndYearAsync(string title, int? firstAirYear = null)
        {
            try
            {
                var query = _context.TvShows
                    .Include(t => t.Topics)
                    .Include(t => t.Genres)
                    .Where(t => t.Title.ToLower() == title.ToLower());
                
                if (firstAirYear.HasValue)
                {
                    query = query.Where(t => t.FirstAirYear == firstAirYear.Value);
                }
                
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV show by title and year: {Title} ({Year})", title, firstAirYear);
                throw;
            }
        }

        private async Task HandleTopicsAsync(TvShow tvShow, string[]? topics)
        {
            if (topics == null || topics.Length == 0)
                return;

            foreach (var topicName in topics.Where(t => !string.IsNullOrWhiteSpace(t)))
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
                    _context.Add(topic);
                    await _context.SaveChangesAsync();
                }
                
                // Get tracked version and add to TV show
                var trackedTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == topic.Id);
                if (trackedTopic != null && !tvShow.Topics.Any(t => t.Id == trackedTopic.Id))
                {
                    tvShow.Topics.Add(trackedTopic);
                }
            }
        }

        private async Task HandleGenresAsync(TvShow tvShow, string[]? genres)
        {
            if (genres == null || genres.Length == 0)
                return;

            foreach (var genreName in genres.Where(g => !string.IsNullOrWhiteSpace(g)))
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
                    _context.Add(genre);
                    await _context.SaveChangesAsync();
                }
                
                // Get tracked version and add to TV show
                var trackedGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id);
                if (trackedGenre != null && !tvShow.Genres.Any(g => g.Id == trackedGenre.Id))
                {
                    tvShow.Genres.Add(trackedGenre);
                }
            }
        }
    }
}
