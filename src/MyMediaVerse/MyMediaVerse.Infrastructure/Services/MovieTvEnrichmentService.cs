using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Interfaces;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services
{
    /// <summary>
    /// Service for enriching movies and TV shows from TMDB API.
    /// Processes items in batches with rate limiting to respect API guidelines.
    /// </summary>
    public class MovieTvEnrichmentService : IMovieTvEnrichmentService
    {
        private readonly IApplicationDbContext _context;
        private readonly ITmdbApiClient _tmdbClient;
        private readonly ILogger<MovieTvEnrichmentService> _logger;

        public MovieTvEnrichmentService(
            IApplicationDbContext context,
            ITmdbApiClient tmdbClient,
            ILogger<MovieTvEnrichmentService> logger)
        {
            _context = context;
            _tmdbClient = tmdbClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<int> GetMoviesNeedingEnrichmentCountAsync()
        {
            return await _context.Movies
                .Where(m => m.TmdbId == null || m.TmdbId == "")
                .CountAsync();
        }

        /// <inheritdoc />
        public async Task<int> GetTvShowsNeedingEnrichmentCountAsync()
        {
            return await _context.TvShows
                .Where(t => t.TmdbId == null || t.TmdbId == "")
                .CountAsync();
        }

        /// <inheritdoc />
        public async Task<MovieTvEnrichmentResult> EnrichMoviesWithoutTmdbDataAsync(
            int batchSize = 50,
            int delayBetweenCallsMs = 500,
            CancellationToken cancellationToken = default)
        {
            var result = new MovieTvEnrichmentResult();

            try
            {
                // Get movies that need enrichment: have no TmdbId
                var moviesToEnrich = await _context.Movies
                    .Where(m => m.TmdbId == null || m.TmdbId == "")
                    .OrderBy(m => m.DateAdded) // Process oldest first
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = moviesToEnrich.Count;

                if (moviesToEnrich.Count == 0)
                {
                    _logger.LogInformation("No movies found needing TMDB enrichment");
                    return result;
                }

                _logger.LogInformation("Starting TMDB enrichment for {Count} movies", moviesToEnrich.Count);

                foreach (var movie in moviesToEnrich)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Movie TMDB enrichment cancelled");
                        result.WasCancelled = true;
                        break;
                    }

                    try
                    {
                        _logger.LogDebug("Searching TMDB for movie: {Title}", movie.Title);

                        // Search TMDB by title
                        var searchResult = await _tmdbClient.SearchMoviesAsync(movie.Title);

                        if (searchResult.Results == null || searchResult.Results.Length == 0)
                        {
                            result.NotFoundCount++;
                            _logger.LogDebug("No TMDB match found for movie: {Title}", movie.Title);
                            continue;
                        }

                        // Use the first result (highest popularity) since we don't have year to match
                        var tmdbMatch = searchResult.Results[0];

                        // Fetch full movie details
                        var movieDetails = await _tmdbClient.GetMovieDetailsAsync(tmdbMatch.Id);

                        // Map TMDB data to entity
                        MapTmdbMovieToEntity(movie, movieDetails);
                        _context.Update(movie);
                        result.EnrichedCount++;

                        _logger.LogDebug("Successfully enriched movie: {Title} (TMDB ID: {TmdbId})",
                            movie.Title, movie.TmdbId);
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to enrich movie '{movie.Title}': {ex.Message}");
                        _logger.LogWarning(ex, "Failed to enrich movie: {Title}", movie.Title);
                    }

                    // Rate limiting: delay between API calls
                    if (delayBetweenCallsMs > 0)
                    {
                        await Task.Delay(delayBetweenCallsMs, cancellationToken);
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Movie TMDB enrichment complete. Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}",
                    result.EnrichedCount, result.NotFoundCount, result.FailedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                _logger.LogInformation("Movie TMDB enrichment was cancelled");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Movie enrichment run failed: {ex.Message}");
                _logger.LogError(ex, "Movie TMDB enrichment run failed");
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MovieTvEnrichmentResult> EnrichTvShowsWithoutTmdbDataAsync(
            int batchSize = 50,
            int delayBetweenCallsMs = 500,
            CancellationToken cancellationToken = default)
        {
            var result = new MovieTvEnrichmentResult();

            try
            {
                // Get TV shows that need enrichment: have no TmdbId
                var tvShowsToEnrich = await _context.TvShows
                    .Where(t => t.TmdbId == null || t.TmdbId == "")
                    .OrderBy(t => t.DateAdded) // Process oldest first
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = tvShowsToEnrich.Count;

                if (tvShowsToEnrich.Count == 0)
                {
                    _logger.LogInformation("No TV shows found needing TMDB enrichment");
                    return result;
                }

                _logger.LogInformation("Starting TMDB enrichment for {Count} TV shows", tvShowsToEnrich.Count);

                foreach (var tvShow in tvShowsToEnrich)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("TV show TMDB enrichment cancelled");
                        result.WasCancelled = true;
                        break;
                    }

                    try
                    {
                        _logger.LogDebug("Searching TMDB for TV show: {Title}", tvShow.Title);

                        // Search TMDB by title
                        var searchResult = await _tmdbClient.SearchTvShowsAsync(tvShow.Title);

                        if (searchResult.Results == null || searchResult.Results.Length == 0)
                        {
                            result.NotFoundCount++;
                            _logger.LogDebug("No TMDB match found for TV show: {Title}", tvShow.Title);
                            continue;
                        }

                        // Use the first result (highest popularity) since we don't have year to match
                        var tmdbMatch = searchResult.Results[0];

                        // Fetch full TV show details
                        var tvShowDetails = await _tmdbClient.GetTvShowDetailsAsync(tmdbMatch.Id);

                        // Map TMDB data to entity
                        MapTmdbTvShowToEntity(tvShow, tvShowDetails);
                        _context.Update(tvShow);
                        result.EnrichedCount++;

                        _logger.LogDebug("Successfully enriched TV show: {Title} (TMDB ID: {TmdbId})",
                            tvShow.Title, tvShow.TmdbId);
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to enrich TV show '{tvShow.Title}': {ex.Message}");
                        _logger.LogWarning(ex, "Failed to enrich TV show: {Title}", tvShow.Title);
                    }

                    // Rate limiting: delay between API calls
                    if (delayBetweenCallsMs > 0)
                    {
                        await Task.Delay(delayBetweenCallsMs, cancellationToken);
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "TV show TMDB enrichment complete. Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}",
                    result.EnrichedCount, result.NotFoundCount, result.FailedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                _logger.LogInformation("TV show TMDB enrichment was cancelled");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"TV show enrichment run failed: {ex.Message}");
                _logger.LogError(ex, "TV show TMDB enrichment run failed");
            }

            return result;
        }

        /// <summary>
        /// Maps TMDB movie details to the Movie entity, only updating fields that are null/empty.
        /// </summary>
        private void MapTmdbMovieToEntity(Movie movie, TmdbMovieDto tmdbMovie)
        {
            // Always set TmdbId as this is the primary enrichment identifier
            movie.TmdbId = tmdbMovie.Id.ToString();

            // Set TMDB rating
            if (!movie.TmdbRating.HasValue && tmdbMovie.VoteAverage > 0)
            {
                movie.TmdbRating = tmdbMovie.VoteAverage;
            }

            // Set description if not already set
            if (string.IsNullOrEmpty(movie.Description) && !string.IsNullOrEmpty(tmdbMovie.Overview))
            {
                movie.Description = tmdbMovie.Overview;
            }

            // Set runtime if not already set
            if (!movie.RuntimeMinutes.HasValue && tmdbMovie.Runtime.HasValue)
            {
                movie.RuntimeMinutes = tmdbMovie.Runtime;
            }

            // Set IMDB ID if not already set
            if (string.IsNullOrEmpty(movie.ImdbId) && !string.IsNullOrEmpty(tmdbMovie.ImdbId))
            {
                movie.ImdbId = tmdbMovie.ImdbId;
            }

            // Set backdrop path
            if (string.IsNullOrEmpty(movie.TmdbBackdropPath) && !string.IsNullOrEmpty(tmdbMovie.BackdropPath))
            {
                movie.TmdbBackdropPath = tmdbMovie.BackdropPath;
            }

            // Set poster as thumbnail if not set
            if (string.IsNullOrEmpty(movie.Thumbnail) && !string.IsNullOrEmpty(tmdbMovie.PosterPath))
            {
                movie.Thumbnail = _tmdbClient.GetImageUrl(tmdbMovie.PosterPath, "w500");
            }

            // Set tagline if not already set
            if (string.IsNullOrEmpty(movie.Tagline) && !string.IsNullOrEmpty(tmdbMovie.Tagline))
            {
                movie.Tagline = tmdbMovie.Tagline;
            }

            // Set homepage if not already set
            if (string.IsNullOrEmpty(movie.Homepage) && !string.IsNullOrEmpty(tmdbMovie.Homepage))
            {
                movie.Homepage = tmdbMovie.Homepage;
            }

            // Set original language if not already set
            if (string.IsNullOrEmpty(movie.OriginalLanguage) && !string.IsNullOrEmpty(tmdbMovie.OriginalLanguage))
            {
                movie.OriginalLanguage = tmdbMovie.OriginalLanguage;
            }

            // Set original title if not already set
            if (string.IsNullOrEmpty(movie.OriginalTitle) && !string.IsNullOrEmpty(tmdbMovie.OriginalTitle))
            {
                movie.OriginalTitle = tmdbMovie.OriginalTitle;
            }

            // Set release year if not already set
            if (!movie.ReleaseYear.HasValue && !string.IsNullOrEmpty(tmdbMovie.ReleaseDate))
            {
                if (DateTime.TryParse(tmdbMovie.ReleaseDate, out var releaseDate))
                {
                    movie.ReleaseYear = releaseDate.Year;
                }
            }
        }

        /// <summary>
        /// Maps TMDB TV show details to the TvShow entity, only updating fields that are null/empty.
        /// </summary>
        private void MapTmdbTvShowToEntity(TvShow tvShow, TmdbTvShowDto tmdbTvShow)
        {
            // Always set TmdbId as this is the primary enrichment identifier
            tvShow.TmdbId = tmdbTvShow.Id.ToString();

            // Set TMDB rating
            if (!tvShow.TmdbRating.HasValue && tmdbTvShow.VoteAverage > 0)
            {
                tvShow.TmdbRating = tmdbTvShow.VoteAverage;
            }

            // Set description if not already set
            if (string.IsNullOrEmpty(tvShow.Description) && !string.IsNullOrEmpty(tmdbTvShow.Overview))
            {
                tvShow.Description = tmdbTvShow.Overview;
            }

            // Set number of seasons if not already set
            if (!tvShow.NumberOfSeasons.HasValue && tmdbTvShow.NumberOfSeasons > 0)
            {
                tvShow.NumberOfSeasons = tmdbTvShow.NumberOfSeasons;
            }

            // Set number of episodes if not already set
            if (!tvShow.NumberOfEpisodes.HasValue && tmdbTvShow.NumberOfEpisodes > 0)
            {
                tvShow.NumberOfEpisodes = tmdbTvShow.NumberOfEpisodes;
            }

            // Set poster path
            if (string.IsNullOrEmpty(tvShow.TmdbPosterPath) && !string.IsNullOrEmpty(tmdbTvShow.PosterPath))
            {
                tvShow.TmdbPosterPath = tmdbTvShow.PosterPath;
            }

            // Set poster as thumbnail if not set
            if (string.IsNullOrEmpty(tvShow.Thumbnail) && !string.IsNullOrEmpty(tmdbTvShow.PosterPath))
            {
                tvShow.Thumbnail = _tmdbClient.GetImageUrl(tmdbTvShow.PosterPath, "w500");
            }

            // Set tagline if not already set
            if (string.IsNullOrEmpty(tvShow.Tagline) && !string.IsNullOrEmpty(tmdbTvShow.Tagline))
            {
                tvShow.Tagline = tmdbTvShow.Tagline;
            }

            // Set homepage if not already set
            if (string.IsNullOrEmpty(tvShow.Homepage) && !string.IsNullOrEmpty(tmdbTvShow.Homepage))
            {
                tvShow.Homepage = tmdbTvShow.Homepage;
            }

            // Set original language if not already set
            if (string.IsNullOrEmpty(tvShow.OriginalLanguage) && !string.IsNullOrEmpty(tmdbTvShow.OriginalLanguage))
            {
                tvShow.OriginalLanguage = tmdbTvShow.OriginalLanguage;
            }

            // Set original name if not already set
            if (string.IsNullOrEmpty(tvShow.OriginalName) && !string.IsNullOrEmpty(tmdbTvShow.OriginalName))
            {
                tvShow.OriginalName = tmdbTvShow.OriginalName;
            }

            // Set first air year if not already set
            if (!tvShow.FirstAirYear.HasValue && !string.IsNullOrEmpty(tmdbTvShow.FirstAirDate))
            {
                if (DateTime.TryParse(tmdbTvShow.FirstAirDate, out var firstAirDate))
                {
                    tvShow.FirstAirYear = firstAirDate.Year;
                }
            }

            // Set last air year if not already set
            if (!tvShow.LastAirYear.HasValue && !string.IsNullOrEmpty(tmdbTvShow.LastAirDate))
            {
                if (DateTime.TryParse(tmdbTvShow.LastAirDate, out var lastAirDate))
                {
                    tvShow.LastAirYear = lastAirDate.Year;
                }
            }
        }
    }
}
