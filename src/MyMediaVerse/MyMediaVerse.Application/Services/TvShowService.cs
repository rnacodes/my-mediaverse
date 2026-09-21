using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.Application.Services
{
    public class TvShowService : ITvShowService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TvShowService> _logger;
        private readonly IMediaService _mediaService;

        public TvShowService(
            IApplicationDbContext context,
            ILogger<TvShowService> logger,
            IMediaService mediaService)
        {
            _context = context;
            _logger = logger;
            _mediaService = mediaService;
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

        public async Task<TvShowCreationResult> CreateTvShowAsync(CreateTvShowDto dto, bool fromTmdb = false)
        {
            try
            {
                if (dto == null)
                {
                    throw new ArgumentNullException(nameof(dto), "TV show data is required");
                }

                var existingTvShow = await FindExistingAsync(dto);
                if (existingTvShow != null)
                {
                    _logger.LogInformation("TV show already in the library: {Title} ({Year})", existingTvShow.Title, existingTvShow.FirstAirYear);
                    return new TvShowCreationResult(existingTvShow, Created: false);
                }

                var tvShow = new TvShow
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
                    OriginalName = dto.OriginalName,
                    TmdbRefreshedAt = fromTmdb ? DateTime.UtcNow : null
                };

                // Handle Topics array conversion
                await HandleTopicsAsync(tvShow, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(tvShow, dto.Genres);

                _context.Add(tvShow);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException) when (!string.IsNullOrEmpty(dto.TmdbId))
                {
                    // Two simultaneous imports of the same TMDB id can both pass the duplicate lookup
                    // before either saves. The unique index rejects the second save; return the row
                    // the first one created instead of surfacing an error.
                    _context.Remove(tvShow);
                    var winner = await GetTvShowByTmdbIdAsync(dto.TmdbId);
                    if (winner == null) throw;

                    return new TvShowCreationResult(winner, Created: false);
                }

                _logger.LogInformation("Successfully created TV show: {Title} ({Year})", tvShow.Title, tvShow.FirstAirYear);
                return new TvShowCreationResult(tvShow, Created: true);
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
                // Load tracked (with topics/genres) so EF can persist removed relationships
                // when the collections are replaced below.
                var tvShow = await _context.TvShows
                    .Include(t => t.Topics)
                    .Include(t => t.Genres)
                    .FirstOrDefaultAsync(t => t.Id == id);
                if (tvShow == null)
                {
                    throw new InvalidOperationException($"TV show with ID {id} not found.");
                }

                // Update TV show properties
                tvShow.Title = dto.Title;
                tvShow.Link = dto.Link;
                tvShow.Notes = dto.Notes;
                tvShow.Status = dto.Status;
                tvShow.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);
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

                // Clear existing topics and genres and save immediately so the removed
                // join rows are persisted before the new ones are added.
                tvShow.Topics.Clear();
                tvShow.Genres.Clear();
                await _context.SaveChangesAsync();

                // Handle Topics array conversion
                await HandleTopicsAsync(tvShow, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(tvShow, dto.Genres);

                await _context.SaveChangesAsync();

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
                // Only a show id is accepted here; any other media item is left alone.
                if (!await _context.TvShows.AnyAsync(t => t.Id == id))
                {
                    return false;
                }

                // The shared delete removes the show's episodes as media items of their own (the
                // database cascade alone would leave their base rows behind), detaches mixlists,
                // topics, and genres, and removes the show and its episodes from the search index.
                var deleted = await _mediaService.DeleteMediaItemAsync(id);
                if (deleted)
                {
                    _logger.LogInformation("Successfully deleted TV show with ID {Id}", id);
                }

                return deleted;
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

        public async Task<TvShow?> GetTvShowByTmdbIdAsync(string tmdbId)
        {
            if (string.IsNullOrWhiteSpace(tmdbId)) return null;

            return await _context.TvShows
                .Include(t => t.Topics)
                .Include(t => t.Genres)
                .FirstOrDefaultAsync(t => t.TmdbId == tmdbId);
        }

        // TMDB id first: it survives an edited title or a missing year. Title and year is the
        // fallback, and never matches a show that carries a different TMDB id (a remake can
        // share its title).
        private async Task<TvShow?> FindExistingAsync(CreateTvShowDto dto)
        {
            if (!string.IsNullOrEmpty(dto.TmdbId))
            {
                var byTmdbId = await GetTvShowByTmdbIdAsync(dto.TmdbId);
                if (byTmdbId != null) return byTmdbId;
            }

            var byTitle = await GetTvShowByTitleAndYearAsync(dto.Title, dto.FirstAirYear);
            if (byTitle == null) return null;

            var differentTmdbItem = !string.IsNullOrEmpty(dto.TmdbId)
                && !string.IsNullOrEmpty(byTitle.TmdbId)
                && byTitle.TmdbId != dto.TmdbId;
            return differentTmdbItem ? null : byTitle;
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

        // Episode methods

        public async Task<IEnumerable<TvShowEpisode>> GetEpisodesByShowIdAsync(Guid showId)
        {
            try
            {
                return await _context.TvShowEpisodes
                    .AsNoTracking()
                    .Where(e => e.ShowId == showId)
                    .Include(e => e.Show)
                    .OrderBy(e => e.SeasonNumber)
                    .ThenBy(e => e.EpisodeNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving episodes for TV show {ShowId}", showId);
                throw;
            }
        }

        public async Task<TvShowEpisode?> GetTvShowEpisodeByIdAsync(Guid id)
        {
            try
            {
                return await _context.TvShowEpisodes
                    .AsNoTracking()
                    .Include(e => e.Show)
                    .FirstOrDefaultAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV show episode with ID {Id}", id);
                throw;
            }
        }

        public async Task<TvShowEpisode> CreateTvShowEpisodeAsync(CreateTvShowEpisodeDto dto)
        {
            try
            {
                if (dto == null)
                {
                    throw new ArgumentNullException(nameof(dto), "TV show episode data is required");
                }

                // Verify parent show exists
                var showExists = await _context.TvShows.AnyAsync(s => s.Id == dto.ShowId);
                if (!showExists)
                {
                    throw new ArgumentException($"TV show with ID {dto.ShowId} not found.", nameof(dto.ShowId));
                }

                // Check for duplicate episode
                if (dto.SeasonNumber.HasValue && dto.EpisodeNumber.HasValue)
                {
                    if (await TvShowEpisodeExistsAsync(dto.ShowId, dto.SeasonNumber.Value, dto.EpisodeNumber.Value))
                    {
                        _logger.LogWarning("TV show episode already exists: S{Season}E{Episode} for show {ShowId}",
                            dto.SeasonNumber, dto.EpisodeNumber, dto.ShowId);
                        var existing = await _context.TvShowEpisodes
                            .Include(e => e.Show)
                            .FirstOrDefaultAsync(e => e.ShowId == dto.ShowId
                                && e.SeasonNumber == dto.SeasonNumber
                                && e.EpisodeNumber == dto.EpisodeNumber);
                        if (existing != null) return existing;
                    }
                }

                var episode = new TvShowEpisode
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
                    Thumbnail = dto.Thumbnail,
                    ShowId = dto.ShowId,
                    SeasonNumber = dto.SeasonNumber,
                    EpisodeNumber = dto.EpisodeNumber,
                    AirDate = dto.AirDate,
                    DurationInMinutes = dto.DurationInMinutes,
                    TmdbEpisodeId = dto.TmdbEpisodeId,
                    StillPath = dto.StillPath
                };

                _context.Add(episode);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created TV show episode: {Title} ({Identifier}) for show {ShowId}",
                    episode.Title, episode.GetEpisodeIdentifier(), episode.ShowId);
                return episode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating TV show episode");
                throw;
            }
        }

        public async Task<bool> DeleteTvShowEpisodeAsync(Guid id)
        {
            try
            {
                // Only an episode id is accepted here; any other media item is left alone.
                if (!await _context.TvShowEpisodes.AnyAsync(e => e.Id == id))
                {
                    return false;
                }

                var deleted = await _mediaService.DeleteMediaItemAsync(id);
                if (deleted)
                {
                    _logger.LogInformation("Successfully deleted TV show episode with ID {Id}", id);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting TV show episode with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> TvShowEpisodeExistsAsync(Guid showId, int seasonNumber, int episodeNumber)
        {
            try
            {
                return await _context.TvShowEpisodes
                    .AnyAsync(e => e.ShowId == showId
                        && e.SeasonNumber == seasonNumber
                        && e.EpisodeNumber == episodeNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if TV show episode exists: S{Season}E{Episode} for show {ShowId}",
                    seasonNumber, episodeNumber, showId);
                throw;
            }
        }

        private async Task HandleTopicsAsync(TvShow tvShow, string[]? topics)
        {
            if (topics == null || topics.Length == 0)
                return;

            var resolver = new TopicResolver(_context);
            foreach (var name in topics.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var topic = await resolver.GetOrCreateAsync(name.Trim().ToLowerInvariant());
                if (topic != null && !tvShow.Topics.Contains(topic))
                {
                    tvShow.Topics.Add(topic);
                }
            }
        }

        private async Task HandleGenresAsync(TvShow tvShow, string[]? genres)
        {
            if (genres == null || genres.Length == 0)
                return;

            var resolver = new GenreResolver(_context);
            foreach (var name in GenreNames.NormalizeList(genres))
            {
                var genre = await resolver.GetOrCreateAsync(name);
                if (genre != null && !tvShow.Genres.Contains(genre))
                {
                    tvShow.Genres.Add(genre);
                }
            }
        }
    }
}
