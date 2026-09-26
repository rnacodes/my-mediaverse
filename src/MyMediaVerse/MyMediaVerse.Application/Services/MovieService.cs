using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.Application.Services
{
    public class MovieService : IMovieService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<MovieService> _logger;
        private readonly IMediaService _mediaService;

        public MovieService(
            IApplicationDbContext context,
            ILogger<MovieService> logger,
            IMediaService mediaService)
        {
            _context = context;
            _logger = logger;
            _mediaService = mediaService;
        }

        public async Task<IEnumerable<Movie>> GetAllMoviesAsync()
        {
            try
            {
                return await _context.Movies
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all movies");
                throw;
            }
        }

        public async Task<Movie?> GetMovieByIdAsync(Guid id)
        {
            try
            {
                return await _context.Movies
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving movie with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Movie>> GetMoviesByDirectorAsync(string director)
        {
            try
            {
                return await _context.Movies
                    .Where(m => m.Director != null && m.Director.ToLower().Contains(director.ToLower()))
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving movies by director: {Director}", director);
                throw;
            }
        }

        public async Task<IEnumerable<Movie>> GetMoviesByYearAsync(int year)
        {
            try
            {
                return await _context.Movies
                    .Where(m => m.ReleaseYear == year)
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving movies by year: {Year}", year);
                throw;
            }
        }

        public async Task<MovieCreationResult> CreateMovieAsync(CreateMovieDto dto, bool fromTmdb = false)
        {
            try
            {
                if (dto == null)
                {
                    throw new ArgumentNullException(nameof(dto), "Movie data is required");
                }

                var existingMovie = await FindExistingAsync(dto);
                if (existingMovie != null)
                {
                    _logger.LogInformation("Movie already in the library: {Title} ({Year})", existingMovie.Title, existingMovie.ReleaseYear);
                    return new MovieCreationResult(existingMovie, Created: false);
                }

                var movie = new Movie
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
                    Thumbnail = dto.Thumbnail,
                    Director = dto.Director,
                    Cast = dto.Cast,
                    ReleaseYear = dto.ReleaseYear,
                    RuntimeMinutes = dto.RuntimeMinutes,
                    MpaaRating = dto.MpaaRating,
                    ImdbId = dto.ImdbId,
                    TmdbId = dto.TmdbId,
                    TmdbRating = dto.TmdbRating,
                    TmdbBackdropPath = dto.TmdbBackdropPath,
                    Tagline = dto.Tagline,
                    Homepage = dto.Homepage,
                    OriginalLanguage = dto.OriginalLanguage,
                    OriginalTitle = dto.OriginalTitle,
                    TmdbRefreshedAt = fromTmdb ? DateTime.UtcNow : null
                };

                // Handle Topics array conversion
                await HandleTopicsAsync(movie, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(movie, dto.Genres);

                _context.Add(movie);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException) when (!string.IsNullOrEmpty(dto.TmdbId))
                {
                    // Two simultaneous imports of the same TMDB id can both pass the duplicate lookup
                    // before either saves. The unique index rejects the second save; return the row
                    // the first one created instead of surfacing an error.
                    _context.Remove(movie);
                    var winner = await GetMovieByTmdbIdAsync(dto.TmdbId);
                    if (winner == null) throw;

                    return new MovieCreationResult(winner, Created: false);
                }

                _logger.LogInformation("Successfully created movie: {Title} ({Year})", movie.Title, movie.ReleaseYear);
                return new MovieCreationResult(movie, Created: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating movie");
                throw;
            }
        }

        public async Task<Movie> UpdateMovieAsync(Guid id, CreateMovieDto dto)
        {
            try
            {
                var movie = await _context.Movies
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (movie == null)
                {
                    throw new InvalidOperationException($"Movie with ID {id} not found.");
                }

                // Update movie properties
                movie.Title = dto.Title;
                movie.Link = dto.Link;
                movie.Notes = dto.Notes;
                movie.Status = dto.Status;
                movie.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);
                movie.Rating = dto.Rating;
                movie.OwnershipStatus = dto.OwnershipStatus;
                movie.Description = dto.Description;
                movie.RelatedNotes = dto.RelatedNotes;
                movie.Thumbnail = dto.Thumbnail;
                movie.Director = dto.Director;
                movie.Cast = dto.Cast;
                movie.ReleaseYear = dto.ReleaseYear;
                movie.RuntimeMinutes = dto.RuntimeMinutes;
                movie.MpaaRating = dto.MpaaRating;
                movie.ImdbId = dto.ImdbId;
                movie.TmdbId = dto.TmdbId;
                movie.TmdbRating = dto.TmdbRating;
                movie.TmdbBackdropPath = dto.TmdbBackdropPath;
                movie.Tagline = dto.Tagline;
                movie.Homepage = dto.Homepage;
                movie.OriginalLanguage = dto.OriginalLanguage;
                movie.OriginalTitle = dto.OriginalTitle;

                movie.Topics.Clear();
                movie.Genres.Clear();
                await _context.SaveChangesAsync();

                // Handle Topics array conversion
                await HandleTopicsAsync(movie, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(movie, dto.Genres);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated movie: {Title} ({Year})", movie.Title, movie.ReleaseYear);
                return movie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating movie with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteMovieAsync(Guid id)
        {
            try
            {
                // Only a movie id is accepted here; any other media item is left alone.
                if (!await _context.Movies.AnyAsync(m => m.Id == id))
                {
                    return false;
                }

                // The shared delete detaches mixlists, topics, and genres, cleans up a stored
                // thumbnail, and removes the item from the search index.
                var deleted = await _mediaService.DeleteMediaItemAsync(id);
                if (deleted)
                {
                    _logger.LogInformation("Successfully deleted movie with ID {Id}", id);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting movie with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> MovieExistsAsync(string title, int? releaseYear = null)
        {
            try
            {
                var query = _context.Movies.Where(m => m.Title.ToLower() == title.ToLower());
                
                if (releaseYear.HasValue)
                {
                    query = query.Where(m => m.ReleaseYear == releaseYear.Value);
                }
                
                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if movie exists: {Title} ({Year})", title, releaseYear);
                throw;
            }
        }

        public async Task<Movie?> GetMovieByTmdbIdAsync(string tmdbId)
        {
            if (string.IsNullOrWhiteSpace(tmdbId)) return null;

            return await _context.Movies
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.TmdbId == tmdbId);
        }

        // TMDB id first: it survives an edited title or a missing year. Title and year is the
        // fallback, and never matches a movie that carries a different TMDB id (two films can
        // share a title and a year).
        private async Task<Movie?> FindExistingAsync(CreateMovieDto dto)
        {
            if (!string.IsNullOrEmpty(dto.TmdbId))
            {
                var byTmdbId = await GetMovieByTmdbIdAsync(dto.TmdbId);
                if (byTmdbId != null) return byTmdbId;
            }

            var byTitle = await GetMovieByTitleAndYearAsync(dto.Title, dto.ReleaseYear);
            if (byTitle == null) return null;

            var differentTmdbItem = !string.IsNullOrEmpty(dto.TmdbId)
                && !string.IsNullOrEmpty(byTitle.TmdbId)
                && byTitle.TmdbId != dto.TmdbId;
            return differentTmdbItem ? null : byTitle;
        }

        public async Task<Movie?> GetMovieByTitleAndYearAsync(string title, int? releaseYear = null)
        {
            try
            {
                var query = _context.Movies
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .Where(m => m.Title.ToLower() == title.ToLower());
                
                if (releaseYear.HasValue)
                {
                    query = query.Where(m => m.ReleaseYear == releaseYear.Value);
                }
                
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving movie by title and year: {Title} ({Year})", title, releaseYear);
                throw;
            }
        }

        private async Task HandleTopicsAsync(Movie movie, string[]? topics)
        {
            if (topics == null || topics.Length == 0)
                return;

            var resolver = new TopicResolver(_context);
            foreach (var name in topics.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var topic = await resolver.GetOrCreateAsync(name.Trim().ToLowerInvariant());
                if (topic != null && !movie.Topics.Contains(topic))
                {
                    movie.Topics.Add(topic);
                }
            }
        }

        private async Task HandleGenresAsync(Movie movie, string[]? genres)
        {
            if (genres == null || genres.Length == 0)
                return;

            var resolver = new GenreResolver(_context);
            foreach (var name in GenreNames.NormalizeList(genres))
            {
                var genre = await resolver.GetOrCreateAsync(name);
                if (genre != null && !movie.Genres.Contains(genre))
                {
                    movie.Genres.Add(genre);
                }
            }
        }
    }
}
