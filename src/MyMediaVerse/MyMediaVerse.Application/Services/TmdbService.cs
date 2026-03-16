using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class TmdbService : ITmdbService
    {
        private readonly ITmdbApiClient _tmdbApiClient;
        private readonly IMovieService _movieService;
        private readonly ITvShowService _tvShowService;
        private readonly ILogger<TmdbService> _logger;

        public TmdbService(
            ITmdbApiClient tmdbApiClient,
            IMovieService movieService,
            ITvShowService tvShowService,
            ILogger<TmdbService> logger)
        {
            _tmdbApiClient = tmdbApiClient;
            _movieService = movieService;
            _tvShowService = tvShowService;
            _logger = logger;
        }

        // Search operations (return DTOs for API consumption)
        public async Task<TmdbMovieSearchResultDto> SearchMoviesAsync(string query, int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Searching movies with query: {Query}, page: {Page}", query, page);
            return await _tmdbApiClient.SearchMoviesAsync(query, page, language);
        }

        public async Task<TmdbTvSearchResultDto> SearchTvShowsAsync(string query, int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Searching TV shows with query: {Query}, page: {Page}", query, page);
            return await _tmdbApiClient.SearchTvShowsAsync(query, page, language);
        }

        public async Task<TmdbMultiSearchResultDto> SearchMultiAsync(string query, int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Searching multi with query: {Query}, page: {Page}", query, page);
            return await _tmdbApiClient.SearchMultiAsync(query, page, language);
        }

        // Detail operations (return DTOs for API consumption)
        public async Task<TmdbMovieDto> GetMovieDetailsAsync(int movieId, string language = "en-US")
        {
            _logger.LogInformation("Getting movie details for ID: {MovieId}", movieId);
            return await _tmdbApiClient.GetMovieDetailsAsync(movieId, language);
        }

        public async Task<TmdbTvShowDto> GetTvShowDetailsAsync(int tvShowId, string language = "en-US")
        {
            _logger.LogInformation("Getting TV show details for ID: {TvShowId}", tvShowId);
            return await _tmdbApiClient.GetTvShowDetailsAsync(tvShowId, language);
        }

        // Popular content operations
        public async Task<TmdbMovieSearchResultDto> GetPopularMoviesAsync(int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Getting popular movies, page: {Page}", page);
            return await _tmdbApiClient.GetPopularMoviesAsync(page, language);
        }

        public async Task<TmdbTvSearchResultDto> GetPopularTvShowsAsync(int page = 1, string language = "en-US")
        {
            _logger.LogInformation("Getting popular TV shows, page: {Page}", page);
            return await _tmdbApiClient.GetPopularTvShowsAsync(page, language);
        }

        // Genre operations
        public async Task<TmdbGenreListDto> GetMovieGenresAsync(string language = "en-US")
        {
            _logger.LogInformation("Getting movie genres");
            return await _tmdbApiClient.GetMovieGenresAsync(language);
        }

        public async Task<TmdbGenreListDto> GetTvGenresAsync(string language = "en-US")
        {
            _logger.LogInformation("Getting TV genres");
            return await _tmdbApiClient.GetTvGenresAsync(language);
        }

        // Utility operations
        public string GetImageUrl(string imagePath, string size = "w500")
        {
            return _tmdbApiClient.GetImageUrl(imagePath, size);
        }

        // Import operations (business logic - convert DTOs to Domain Entities)
        public async Task<Movie> ImportMovieAsync(int movieId, string language = "en-US")
        {
            try
            {
                _logger.LogInformation("Importing movie with ID: {MovieId}", movieId);

                // Check if movie already exists by title and year
                var movieDto = await _tmdbApiClient.GetMovieDetailsAsync(movieId, language);
                var releaseYear = !string.IsNullOrEmpty(movieDto.ReleaseDate) && DateTime.TryParse(movieDto.ReleaseDate, out var releaseDate) 
                    ? releaseDate.Year 
                    : (int?)null;
                
                var existingMovie = await _movieService.GetMovieByTitleAndYearAsync(movieDto.Title, releaseYear);
                if (existingMovie != null)
                {
                    _logger.LogInformation("Movie {Title} ({Year}) already exists", movieDto.Title, releaseYear);
                    return existingMovie;
                }

                // Create DTO for the service
                var createMovieDto = new CreateMovieDto
                {
                    Title = movieDto.Title,
                    MediaType = MediaType.Movie,
                    Link = !string.IsNullOrEmpty(movieDto.ImdbId) 
                        ? $"https://www.imdb.com/title/{movieDto.ImdbId}/" 
                        : null,
                    Notes = movieDto.Overview,
                    Description = movieDto.Overview,
                    Thumbnail = !string.IsNullOrEmpty(movieDto.PosterPath)
                        ? $"https://image.tmdb.org/t/p/w500{movieDto.PosterPath}"
                        : null,
                    Status = Status.Uncharted,
                    Rating = null, // Personal rating - leave null for imports
                    Director = null, // TMDB doesn't provide director in basic movie details
                    Cast = null, // TMDB basic movie details don't include cast - would need additional API call
                    ReleaseYear = releaseYear,
                    RuntimeMinutes = movieDto.Runtime,
                    MpaaRating = null, // Would need additional TMDB call for release info
                    ImdbId = movieDto.ImdbId,
                    TmdbId = movieDto.Id.ToString(),
                    TmdbRating = movieDto.VoteAverage > 0 ? movieDto.VoteAverage : null, // Store original TMDB rating (0-10 scale)
                    TmdbBackdropPath = movieDto.BackdropPath,
                    Tagline = movieDto.Tagline,
                    Homepage = movieDto.Homepage,
                    OriginalLanguage = movieDto.OriginalLanguage,
                    OriginalTitle = movieDto.OriginalTitle
                };

                // Save to database through domain service
                var savedMovie = await _movieService.CreateMovieAsync(createMovieDto);
                
                _logger.LogInformation("Successfully imported movie: {Title} (TMDB ID: {MovieId})", 
                    movieDto.Title, movieId);
                
                return savedMovie;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing movie with ID: {MovieId}", movieId);
                throw;
            }
        }

        public async Task<TvShow> ImportTvShowAsync(int tvShowId, string language = "en-US")
        {
            try
            {
                _logger.LogInformation("Importing TV show with ID: {TvShowId}", tvShowId);

                // Check if TV show already exists by title and year
                var tvShowDto = await _tmdbApiClient.GetTvShowDetailsAsync(tvShowId, language);
                var firstAirYear = !string.IsNullOrEmpty(tvShowDto.FirstAirDate) && DateTime.TryParse(tvShowDto.FirstAirDate, out var firstAirDate) 
                    ? firstAirDate.Year 
                    : (int?)null;
                
                var existingTvShow = await _tvShowService.GetTvShowByTitleAndYearAsync(tvShowDto.Name, firstAirYear);
                if (existingTvShow != null)
                {
                    _logger.LogInformation("TV show {Title} ({Year}) already exists", tvShowDto.Name, firstAirYear);
                    return existingTvShow;
                }

                // Create DTO for the service
                var createTvShowDto = new CreateTvShowDto
                {
                    Title = tvShowDto.Name,
                    MediaType = MediaType.TVShow,
                    Link = null, // TMDB doesn't provide direct links
                    Notes = tvShowDto.Overview,
                    Description = tvShowDto.Overview,
                    Thumbnail = !string.IsNullOrEmpty(tvShowDto.PosterPath)
                        ? $"https://image.tmdb.org/t/p/w500{tvShowDto.PosterPath}"
                        : null,
                    Status = Status.Uncharted,
                    Rating = null, // Personal rating - leave null for imports
                    Creator = null, // TMDB basic TV show details don't include creator - would need additional API call
                    Cast = null, // TMDB basic TV show details don't include cast - would need additional API call
                    FirstAirYear = firstAirYear,
                    LastAirYear = !string.IsNullOrEmpty(tvShowDto.LastAirDate) && DateTime.TryParse(tvShowDto.LastAirDate, out var lastAirDate) 
                        ? lastAirDate.Year 
                        : (int?)null,
                    NumberOfSeasons = tvShowDto.NumberOfSeasons,
                    NumberOfEpisodes = tvShowDto.NumberOfEpisodes,
                    TmdbId = tvShowDto.Id.ToString(),
                    TmdbRating = tvShowDto.VoteAverage > 0 ? tvShowDto.VoteAverage : null, // Store original TMDB rating (0-10 scale)
                    TmdbPosterPath = tvShowDto.PosterPath,
                    Tagline = tvShowDto.Tagline,
                    Homepage = tvShowDto.Homepage,
                    OriginalLanguage = tvShowDto.OriginalLanguage,
                    OriginalName = tvShowDto.OriginalName
                };

                // Save to database through domain service
                var savedTvShow = await _tvShowService.CreateTvShowAsync(createTvShowDto);
                
                _logger.LogInformation("Successfully imported TV show: {Title} (TMDB ID: {TvShowId})", 
                    tvShowDto.Name, tvShowId);
                
                return savedTvShow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing TV show with ID: {TvShowId}", tvShowId);
                throw;
            }
        }
    }
}