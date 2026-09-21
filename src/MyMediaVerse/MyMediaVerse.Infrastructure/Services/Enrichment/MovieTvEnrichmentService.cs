using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Service for enriching movies and TV shows from TMDB API.
    /// Processes items in batches with rate limiting to respect API guidelines.
    /// </summary>
    public class MovieTvEnrichmentService : IMovieTvEnrichmentService
    {
        private const string TmdbImageHost = "image.tmdb.org";

        private readonly IApplicationDbContext _context;
        private readonly ITmdbApiClient _tmdbClient;
        private readonly IGenreMappingService _genreMappingService;
        private readonly ILogger<MovieTvEnrichmentService> _logger;

        public MovieTvEnrichmentService(
            IApplicationDbContext context,
            ITmdbApiClient tmdbClient,
            IGenreMappingService genreMappingService,
            ILogger<MovieTvEnrichmentService> logger)
        {
            _context = context;
            _tmdbClient = tmdbClient;
            _genreMappingService = genreMappingService;
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
                    .Include(m => m.Genres)
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

                // One resolver for the run, so items sharing a new genre resolve to one row.
                var genreResolver = new GenreResolver(_context);

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

                        // Map TMDB data to entity (fill-gaps-only; MapTmdbMovieToEntity
                        // guards every field and never overwrites a populated value).
                        MapTmdbMovieToEntity(movie, movieDetails);
                        await FillGenresAsync(movie.Genres, movieDetails.Genres, genreResolver);
                        movie.EnrichedAt = DateTime.UtcNow;
                        movie.TmdbRefreshedAt = movie.EnrichedAt;
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
                    .Include(t => t.Genres)
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

                // One resolver for the run, so items sharing a new genre resolve to one row.
                var genreResolver = new GenreResolver(_context);

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

                        // Map TMDB data to entity (fill-gaps-only; MapTmdbTvShowToEntity
                        // guards every field and never overwrites a populated value).
                        MapTmdbTvShowToEntity(tvShow, tvShowDetails);
                        await FillGenresAsync(tvShow.Genres, tvShowDetails.Genres, genreResolver);
                        tvShow.EnrichedAt = DateTime.UtcNow;
                        tvShow.TmdbRefreshedAt = tvShow.EnrichedAt;
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
        /// <inheritdoc />
        public async Task<MovieTvRefreshResultDto> RefreshStaleAsync(
            int limit = 50,
            int olderThanDays = 180,
            int delayBetweenCallsMs = 250,
            CancellationToken cancellationToken = default)
        {
            var result = new MovieTvRefreshResultDto { StartedAt = DateTime.UtcNow };

            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);

                // Each table contributes up to the limit; the merge below keeps the oldest overall.
                // Never-refreshed rows sort first explicitly: PostgreSQL puts NULLs last by default.
                var staleMovies = await StaleMovies(cutoff)
                    .Include(m => m.Genres)
                    .OrderBy(m => m.TmdbRefreshedAt != null)
                    .ThenBy(m => m.TmdbRefreshedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);
                var staleShows = await StaleTvShows(cutoff)
                    .Include(t => t.Genres)
                    .OrderBy(t => t.TmdbRefreshedAt != null)
                    .ThenBy(t => t.TmdbRefreshedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                var candidates = staleMovies.Cast<BaseMediaItem>().Concat(staleShows)
                    .OrderBy(i => StampOf(i) ?? DateTime.MinValue)
                    .Take(limit)
                    .ToList();

                _logger.LogInformation(
                    "Starting TMDB refresh for {Count} items older than {Days} days", candidates.Count, olderThanDays);

                // One resolver for the run, so items sharing a new genre resolve to one row.
                var genreResolver = new GenreResolver(_context);

                foreach (var item in candidates)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Warnings.Add("The run was cancelled before every stale item was refreshed.");
                        break;
                    }

                    var tmdbIdText = item is Movie m ? m.TmdbId : ((TvShow)item).TmdbId;
                    if (!int.TryParse(tmdbIdText, out var tmdbId))
                    {
                        result.SkippedCount++;
                        result.Warnings.Add($"'{item.Title}' has a TMDB id that is not a number ('{tmdbIdText}'); skipped.");
                        continue;
                    }

                    try
                    {
                        bool changed;
                        if (item is Movie movie)
                        {
                            var details = await _tmdbClient.GetMovieDetailsAsync(tmdbId);
                            changed = ApplyTmdbRefresh(movie, details);
                            changed |= await FillGenresAsync(movie.Genres, details.Genres, genreResolver);
                            movie.TmdbRefreshedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            var show = (TvShow)item;
                            var details = await _tmdbClient.GetTvShowDetailsAsync(tmdbId);
                            changed = ApplyTmdbRefresh(show, details);
                            changed |= await FillGenresAsync(show.Genres, details.Genres, genreResolver);
                            show.TmdbRefreshedAt = DateTime.UtcNow;
                        }

                        if (changed) result.UpdatedCount++;
                        else result.UnchangedCount++;
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Left unstamped on purpose: the item comes back every run until its id is fixed.
                        result.FailedCount++;
                        result.AddError($"TMDB no longer has id {tmdbId} for '{item.Title}'; correct or clear the item's TMDB id.");
                        _logger.LogWarning("TMDB returned 404 for {Title} (TMDB ID: {TmdbId})", item.Title, tmdbId);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        result.FailedCount++;
                        result.AddError($"Failed to refresh '{item.Title}': {ex.Message}");
                        _logger.LogWarning(ex, "Failed to refresh {Title} (TMDB ID: {TmdbId})", item.Title, tmdbId);
                    }

                    if (delayBetweenCallsMs > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(delayBetweenCallsMs, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Handled at the top of the loop, so the items already refreshed are still saved.
                        }
                    }
                }

                await _context.SaveChangesAsync(CancellationToken.None);

                result.RemainingCount =
                    await StaleMovies(cutoff).CountAsync(CancellationToken.None)
                    + await StaleTvShows(cutoff).CountAsync(CancellationToken.None);
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "TMDB refresh complete. Updated: {Updated}, unchanged: {Unchanged}, skipped: {Skipped}, failed: {Failed}, remaining: {Remaining}",
                    result.UpdatedCount, result.UnchangedCount, result.SkippedCount, result.FailedCount, result.RemainingCount);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"TMDB refresh run failed: {ex.Message}";
                _logger.LogError(ex, "TMDB refresh run failed");
            }

            return result;
        }

        private IQueryable<Movie> StaleMovies(DateTime cutoff) => _context.Movies
            .Where(m => m.TmdbId != null && m.TmdbId != "")
            .Where(m => m.TmdbRefreshedAt == null || m.TmdbRefreshedAt < cutoff);

        private IQueryable<TvShow> StaleTvShows(DateTime cutoff) => _context.TvShows
            .Where(t => t.TmdbId != null && t.TmdbId != "")
            .Where(t => t.TmdbRefreshedAt == null || t.TmdbRefreshedAt < cutoff);

        private static DateTime? StampOf(BaseMediaItem item)
            => item is Movie movie ? movie.TmdbRefreshedAt : ((TvShow)item).TmdbRefreshedAt;

        /// <summary>
        /// Applies a fresh TMDB payload to a stored movie. Values TMDB owns are overwritten;
        /// descriptive values are only filled when empty, so anything edited by hand survives.
        /// Returns true when any stored value changed.
        /// </summary>
        private static bool ApplyTmdbRefresh(Movie movie, TmdbMovieDto tmdb)
        {
            var changed = false;

            // TMDB-owned: overwrite whenever TMDB has a value.
            if (tmdb.VoteAverage > 0) changed |= Set(movie.TmdbRating, tmdb.VoteAverage, v => movie.TmdbRating = v);
            changed |= Overwrite(movie.TmdbBackdropPath, tmdb.BackdropPath, v => movie.TmdbBackdropPath = v);
            changed |= RefreshThumbnail(movie, tmdb.PosterPath);
            if (tmdb.Runtime is > 0) changed |= Set(movie.RuntimeMinutes, tmdb.Runtime, v => movie.RuntimeMinutes = v);
            changed |= Overwrite(movie.Tagline, tmdb.Tagline, v => movie.Tagline = v);
            changed |= Overwrite(movie.Homepage, tmdb.Homepage, v => movie.Homepage = v);
            changed |= Overwrite(movie.ImdbId, tmdb.ImdbId, v => movie.ImdbId = v);
            changed |= Overwrite(movie.OriginalLanguage, tmdb.OriginalLanguage, v => movie.OriginalLanguage = v);

            // Descriptive: fill only when empty.
            changed |= Fill(movie.Title, tmdb.Title, v => movie.Title = v!);
            changed |= Fill(movie.Description, tmdb.Overview, v => movie.Description = v);
            changed |= Fill(movie.Director, TmdbDetailsExtractor.GetDirector(tmdb), v => movie.Director = v);
            changed |= Fill(movie.Cast, TmdbDetailsExtractor.GetCast(tmdb.Credits), v => movie.Cast = v);
            changed |= Fill(movie.MpaaRating, TmdbDetailsExtractor.GetMpaaRating(tmdb), v => movie.MpaaRating = v);
            changed |= Fill(movie.OriginalTitle, tmdb.OriginalTitle, v => movie.OriginalTitle = v);
            changed |= Fill(movie.Link, $"https://www.themoviedb.org/movie/{tmdb.Id}", v => movie.Link = v);

            if (!movie.ReleaseYear.HasValue && DateTime.TryParse(tmdb.ReleaseDate, out var releaseDate))
            {
                movie.ReleaseYear = releaseDate.Year;
                changed = true;
            }

            return changed;
        }

        /// <summary>The TV show counterpart of the movie refresh, with the same overwrite/fill split.</summary>
        private static bool ApplyTmdbRefresh(TvShow show, TmdbTvShowDto tmdb)
        {
            var changed = false;

            // TMDB-owned: overwrite whenever TMDB has a value.
            if (tmdb.VoteAverage > 0) changed |= Set(show.TmdbRating, tmdb.VoteAverage, v => show.TmdbRating = v);
            changed |= Overwrite(show.TmdbPosterPath, tmdb.PosterPath, v => show.TmdbPosterPath = v);
            changed |= RefreshThumbnail(show, tmdb.PosterPath);
            if (tmdb.NumberOfSeasons > 0) changed |= Set(show.NumberOfSeasons, tmdb.NumberOfSeasons, v => show.NumberOfSeasons = v);
            if (tmdb.NumberOfEpisodes > 0) changed |= Set(show.NumberOfEpisodes, tmdb.NumberOfEpisodes, v => show.NumberOfEpisodes = v);
            if (DateTime.TryParse(tmdb.LastAirDate, out var lastAirDate))
            {
                changed |= Set(show.LastAirYear, lastAirDate.Year, v => show.LastAirYear = v);
            }
            changed |= Overwrite(show.Tagline, tmdb.Tagline, v => show.Tagline = v);
            changed |= Overwrite(show.Homepage, tmdb.Homepage, v => show.Homepage = v);
            changed |= Overwrite(show.OriginalLanguage, tmdb.OriginalLanguage, v => show.OriginalLanguage = v);

            // Descriptive: fill only when empty.
            changed |= Fill(show.Title, tmdb.Name, v => show.Title = v!);
            changed |= Fill(show.Description, tmdb.Overview, v => show.Description = v);
            changed |= Fill(show.Creator, TmdbDetailsExtractor.GetCreator(tmdb), v => show.Creator = v);
            changed |= Fill(show.Cast, TmdbDetailsExtractor.GetCast(tmdb.Credits), v => show.Cast = v);
            changed |= Fill(show.ContentRating, TmdbDetailsExtractor.GetContentRating(tmdb), v => show.ContentRating = v);
            changed |= Fill(show.OriginalName, tmdb.OriginalName, v => show.OriginalName = v);
            changed |= Fill(show.Link, $"https://www.themoviedb.org/tv/{tmdb.Id}", v => show.Link = v);

            if (!show.FirstAirYear.HasValue && DateTime.TryParse(tmdb.FirstAirDate, out var firstAirDate))
            {
                show.FirstAirYear = firstAirDate.Year;
                changed = true;
            }

            return changed;
        }

        // A thumbnail that points at TMDB's image host follows TMDB; any other URL was chosen by
        // hand (or uploaded) and is left alone.
        private static bool RefreshThumbnail(BaseMediaItem item, string? posterPath)
        {
            if (string.IsNullOrEmpty(posterPath)) return false;

            var isTmdbOwned = string.IsNullOrEmpty(item.Thumbnail) || item.Thumbnail.Contains(TmdbImageHost);
            return isTmdbOwned && Set(item.Thumbnail, $"https://{TmdbImageHost}/t/p/w500{posterPath}", v => item.Thumbnail = v);
        }

        private static bool Overwrite(string? current, string? incoming, Action<string?> assign)
            => !string.IsNullOrEmpty(incoming) && Set(current, incoming, assign);

        private static bool Fill(string? current, string? incoming, Action<string?> assign)
            => string.IsNullOrEmpty(current) && !string.IsNullOrEmpty(incoming) && Set(current, incoming, assign);

        private static bool Set<T>(T current, T incoming, Action<T> assign)
        {
            if (EqualityComparer<T>.Default.Equals(current, incoming)) return false;

            assign(incoming);
            return true;
        }

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

            // Credits and certification arrive with the details call (append_to_response)
            if (string.IsNullOrEmpty(movie.Director))
            {
                movie.Director = TmdbDetailsExtractor.GetDirector(tmdbMovie);
            }

            if (string.IsNullOrEmpty(movie.Cast))
            {
                movie.Cast = TmdbDetailsExtractor.GetCast(tmdbMovie.Credits);
            }

            if (string.IsNullOrEmpty(movie.MpaaRating))
            {
                movie.MpaaRating = TmdbDetailsExtractor.GetMpaaRating(tmdbMovie);
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

            // Credits and content rating arrive with the details call (append_to_response)
            if (string.IsNullOrEmpty(tvShow.Creator))
            {
                tvShow.Creator = TmdbDetailsExtractor.GetCreator(tmdbTvShow);
            }

            if (string.IsNullOrEmpty(tvShow.Cast))
            {
                tvShow.Cast = TmdbDetailsExtractor.GetCast(tmdbTvShow.Credits);
            }

            if (string.IsNullOrEmpty(tvShow.ContentRating))
            {
                tvShow.ContentRating = TmdbDetailsExtractor.GetContentRating(tmdbTvShow);
            }
        }

        /// <summary>
        /// Gives an item TMDB's genres only when it has none of its own. An item that already
        /// carries genres keeps exactly what it has, whether they were imported or hand-picked.
        /// Returns true when genres were added.
        /// </summary>
        private async Task<bool> FillGenresAsync(ICollection<Genre> itemGenres, IEnumerable<TmdbGenreDto> tmdbGenres, GenreResolver resolver)
        {
            if (itemGenres.Count > 0) return false;

            foreach (var name in _genreMappingService.MapTmdbGenreNames(tmdbGenres.Select(g => g.Name)))
            {
                var genre = await resolver.GetOrCreateAsync(name);
                if (genre != null && !itemGenres.Contains(genre))
                {
                    itemGenres.Add(genre);
                }
            }

            return itemGenres.Count > 0;
        }
    }
}
