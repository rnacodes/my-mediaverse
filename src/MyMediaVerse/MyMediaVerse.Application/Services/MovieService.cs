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

        public MovieService(
            IApplicationDbContext context,
            ILogger<MovieService> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task<Movie> CreateMovieAsync(CreateMovieDto dto)
        {
            try
            {
                if (dto == null)
                {
                    throw new ArgumentNullException(nameof(dto), "Movie data is required");
                }

                // Check if movie already exists
                if (await MovieExistsAsync(dto.Title, dto.ReleaseYear))
                {
                    _logger.LogWarning("Movie already exists: {Title} ({Year})", dto.Title, dto.ReleaseYear);
                    var existingMovie = await GetMovieByTitleAndYearAsync(dto.Title, dto.ReleaseYear);
                    if (existingMovie != null)
                    {
                        return existingMovie;
                    }
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
                    OriginalTitle = dto.OriginalTitle
                };

                // Handle Topics array conversion
                await HandleTopicsAsync(movie, dto.Topics);

                // Handle Genres array conversion
                await HandleGenresAsync(movie, dto.Genres);

                _context.Add(movie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created movie: {Title} ({Year})", movie.Title, movie.ReleaseYear);
                return movie;
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
                var movie = await GetMovieByIdAsync(id);
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

                // Clear existing topics and genres and save immediately to avoid FK conflicts
                movie.Topics.Clear();
                movie.Genres.Clear();
                // Clear change tracker and explicitly update the entity since it was retrieved with AsNoTracking
                _context.ClearChangeTracker();
                _context.Update(movie);
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
                var movie = await _context.FindAsync<Movie>(id);
                if (movie == null)
                {
                    return false;
                }

                var movieId = movie.Id;
                var movieTitle = movie.Title;
                var movieYear = movie.ReleaseYear;

                _context.Remove(movie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted movie: {Title} ({Year})", movieTitle, movieYear);
                return true;
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
                
                // Get tracked version and add to movie
                var trackedTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == topic.Id);
                if (trackedTopic != null && !movie.Topics.Any(t => t.Id == trackedTopic.Id))
                {
                    movie.Topics.Add(trackedTopic);
                }
            }
        }

        private async Task HandleGenresAsync(Movie movie, string[]? genres)
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
                
                // Get tracked version and add to movie
                var trackedGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id);
                if (trackedGenre != null && !movie.Genres.Any(g => g.Id == trackedGenre.Id))
                {
                    movie.Genres.Add(trackedGenre);
                }
            }
        }
    }
}
