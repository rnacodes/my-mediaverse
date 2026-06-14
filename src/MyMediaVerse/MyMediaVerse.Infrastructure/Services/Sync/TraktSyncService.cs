using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.DTOs.Trakt;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Sync
{
    public class TraktSyncService : ITraktSyncService
    {
        private readonly IApplicationDbContext _context;
        private readonly ITraktApiClient _traktClient;
        private readonly ILogger<TraktSyncService> _logger;
        private const int BatchSize = 50;
        private const int ApiDelayMs = 300;

        // Scope-lived genre lookup (lowercase name -> tracked Genre).
        private Dictionary<string, Genre>? _genreCache;

        public TraktSyncService(
            IApplicationDbContext context,
            ITraktApiClient traktClient,
            ILogger<TraktSyncService> logger)
        {
            _context = context;
            _traktClient = traktClient;
            _logger = logger;
        }

        // --- Connection & Token Management ---

        public async Task<bool> IsConnectedAsync()
        {
            var token = await _context.TraktTokens.FirstOrDefaultAsync();
            return token != null;
        }

        public async Task<TraktConnectionStatusDto> GetStatusAsync()
        {
            var token = await _context.TraktTokens.FirstOrDefaultAsync();
            return new TraktConnectionStatusDto
            {
                Connected = token != null,
                Username = token?.TraktUsername
            };
        }

        public async Task SaveTokenAsync(TraktOAuthTokenDto tokenResponse)
        {
            var existing = await _context.TraktTokens.FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.AccessToken = tokenResponse.AccessToken;
                existing.RefreshToken = tokenResponse.RefreshToken;
                existing.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(tokenResponse.CreatedAt + tokenResponse.ExpiresIn).UtcDateTime;
                existing.CreatedAt = DateTime.UtcNow;
                _context.Update(existing);
            }
            else
            {
                var token = new TraktToken
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(tokenResponse.CreatedAt + tokenResponse.ExpiresIn).UtcDateTime,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Add(token);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Trakt OAuth token saved successfully");
        }

        public async Task<string?> GetValidAccessTokenAsync()
        {
            var token = await _context.TraktTokens.FirstOrDefaultAsync();
            if (token == null)
            {
                _logger.LogWarning("No Trakt token found in database");
                return null;
            }

            // Refresh proactively if less than 1 hour remaining
            if (token.ExpiresAt < DateTime.UtcNow.AddHours(1))
            {
                _logger.LogInformation("Trakt token expiring soon, refreshing...");
                try
                {
                    var refreshed = await _traktClient.RefreshTokenAsync(token.RefreshToken);
                    token.AccessToken = refreshed.AccessToken;
                    token.RefreshToken = refreshed.RefreshToken;
                    token.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(refreshed.CreatedAt + refreshed.ExpiresIn).UtcDateTime;
                    _context.Update(token);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Trakt token refreshed successfully, new expiry: {ExpiresAt}", token.ExpiresAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh Trakt token");
                    // If refresh fails but token hasn't actually expired yet, still return it
                    if (token.ExpiresAt > DateTime.UtcNow)
                    {
                        return token.AccessToken;
                    }
                    return null;
                }
            }

            return token.AccessToken;
        }

        public async Task DisconnectAsync()
        {
            var token = await _context.TraktTokens.FirstOrDefaultAsync();
            if (token != null)
            {
                try
                {
                    await _traktClient.RevokeTokenAsync(token.AccessToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to revoke Trakt token on disconnect (continuing with local removal)");
                }

                _context.Remove(token);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Trakt account disconnected");
            }
        }

        // --- Sync Operations ---

        public async Task<TraktSyncResultDto> SyncAllAsync()
        {
            var result = new TraktSyncResultDto { StartedAt = DateTime.UtcNow };

            try
            {
                var watchedResult = await SyncWatchedAsync();
                var watchlistResult = await SyncWatchlistAsync();
                var ratingsResult = await SyncRatingsAsync();

                // Combine results
                result.Success = true;
                result.MoviesCreated = watchedResult.MoviesCreated + watchlistResult.MoviesCreated;
                result.MoviesUpdated = watchedResult.MoviesUpdated + watchlistResult.MoviesUpdated + ratingsResult.MoviesUpdated;
                result.ShowsCreated = watchedResult.ShowsCreated + watchlistResult.ShowsCreated;
                result.ShowsUpdated = watchedResult.ShowsUpdated + watchlistResult.ShowsUpdated + ratingsResult.ShowsUpdated;
                result.EpisodesCreated = watchedResult.EpisodesCreated;
                result.EpisodesUpdated = watchedResult.EpisodesUpdated;
                result.WatchlistItemsProcessed = watchlistResult.WatchlistItemsProcessed;
                result.RatingsProcessed = ratingsResult.RatingsProcessed;
                result.Errors.AddRange(watchedResult.Errors);
                result.Errors.AddRange(watchlistResult.Errors);
                result.Errors.AddRange(ratingsResult.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Trakt sync all");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        public async Task<TraktSyncResultDto> SyncWatchedAsync()
        {
            var result = new TraktSyncResultDto { StartedAt = DateTime.UtcNow };

            try
            {
                var accessToken = await GetValidAccessTokenAsync();
                if (accessToken == null)
                {
                    result.ErrorMessage = "Not connected to Trakt";
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }

                // Sync watched movies
                _logger.LogInformation("Fetching watched movies from Trakt...");
                var watchedMovies = await _traktClient.GetWatchedMoviesAsync(accessToken);
                _logger.LogInformation("Found {Count} watched movies on Trakt", watchedMovies.Count);

                await Task.Delay(ApiDelayMs);

                // Sync watched shows
                _logger.LogInformation("Fetching watched shows from Trakt...");
                var watchedShows = await _traktClient.GetWatchedShowsAsync(accessToken);
                _logger.LogInformation("Found {Count} watched shows on Trakt", watchedShows.Count);

                // Process movies
                int processedCount = 0;
                foreach (var watchedMovie in watchedMovies)
                {
                    try
                    {
                        await ProcessWatchedMovieAsync(watchedMovie, result);
                        processedCount++;

                        if (processedCount % BatchSize == 0)
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error processing movie '{watchedMovie.Movie.Title}': {ex.Message}";
                        _logger.LogWarning(ex, "Error processing watched movie: {Title}", watchedMovie.Movie.Title);
                        if (result.Errors.Count < 20) result.Errors.Add(errorMsg);
                    }
                }

                await _context.SaveChangesAsync();

                // Process shows + episodes
                processedCount = 0;
                foreach (var watchedShow in watchedShows)
                {
                    try
                    {
                        await ProcessWatchedShowAsync(watchedShow, result);
                        processedCount++;

                        if (processedCount % BatchSize == 0)
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error processing show '{watchedShow.Show.Title}': {ex.Message}";
                        _logger.LogWarning(ex, "Error processing watched show: {Title}", watchedShow.Show.Title);
                        if (result.Errors.Count < 20) result.Errors.Add(errorMsg);
                    }
                }

                await _context.SaveChangesAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Trakt watched sync");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        public async Task<TraktSyncResultDto> SyncWatchlistAsync()
        {
            var result = new TraktSyncResultDto { StartedAt = DateTime.UtcNow };

            try
            {
                var accessToken = await GetValidAccessTokenAsync();
                if (accessToken == null)
                {
                    result.ErrorMessage = "Not connected to Trakt";
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }

                _logger.LogInformation("Fetching watchlist from Trakt...");
                var watchlistMovies = await _traktClient.GetWatchlistMoviesAsync(accessToken);
                await Task.Delay(ApiDelayMs);
                var watchlistShows = await _traktClient.GetWatchlistShowsAsync(accessToken);

                _logger.LogInformation("Found {MovieCount} movies and {ShowCount} shows on Trakt watchlist",
                    watchlistMovies.Count, watchlistShows.Count);

                foreach (var item in watchlistMovies)
                {
                    try
                    {
                        await ProcessWatchlistMovieAsync(item, result);
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error processing watchlist movie '{item.Movie?.Title}': {ex.Message}";
                        _logger.LogWarning(ex, "Error processing watchlist movie: {Title}", item.Movie?.Title);
                        if (result.Errors.Count < 20) result.Errors.Add(errorMsg);
                    }
                }

                foreach (var item in watchlistShows)
                {
                    try
                    {
                        await ProcessWatchlistShowAsync(item, result);
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error processing watchlist show '{item.Show?.Title}': {ex.Message}";
                        _logger.LogWarning(ex, "Error processing watchlist show: {Title}", item.Show?.Title);
                        if (result.Errors.Count < 20) result.Errors.Add(errorMsg);
                    }
                }

                await _context.SaveChangesAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Trakt watchlist sync");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        public async Task<TraktSyncResultDto> SyncRatingsAsync()
        {
            var result = new TraktSyncResultDto { StartedAt = DateTime.UtcNow };

            try
            {
                var accessToken = await GetValidAccessTokenAsync();
                if (accessToken == null)
                {
                    result.ErrorMessage = "Not connected to Trakt";
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }

                _logger.LogInformation("Fetching ratings from Trakt...");
                var movieRatings = await _traktClient.GetRatingsMoviesAsync(accessToken);
                await Task.Delay(ApiDelayMs);
                var showRatings = await _traktClient.GetRatingsShowsAsync(accessToken);

                _logger.LogInformation("Found {MovieCount} movie ratings and {ShowCount} show ratings on Trakt",
                    movieRatings.Count, showRatings.Count);

                foreach (var rating in movieRatings)
                {
                    try
                    {
                        await ProcessMovieRatingAsync(rating, result);
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error processing movie rating '{rating.Movie?.Title}': {ex.Message}";
                        _logger.LogWarning(ex, "Error processing movie rating: {Title}", rating.Movie?.Title);
                        if (result.Errors.Count < 20) result.Errors.Add(errorMsg);
                    }
                }

                foreach (var rating in showRatings)
                {
                    try
                    {
                        await ProcessShowRatingAsync(rating, result);
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error processing show rating '{rating.Show?.Title}': {ex.Message}";
                        _logger.LogWarning(ex, "Error processing show rating: {Title}", rating.Show?.Title);
                        if (result.Errors.Count < 20) result.Errors.Add(errorMsg);
                    }
                }

                await _context.SaveChangesAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Trakt ratings sync");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        // --- Private Processing Methods ---

        private async Task ProcessWatchedMovieAsync(TraktWatchedMovieDto watchedMovie, TraktSyncResultDto result)
        {
            var tmdbId = watchedMovie.Movie.Ids.Tmdb?.ToString();
            var movie = await FindMovieAsync(tmdbId, watchedMovie.Movie.Title, watchedMovie.Movie.Year);

            if (movie != null)
            {
                // Update existing movie with Trakt data
                movie.TraktId = watchedMovie.Movie.Ids.Trakt;
                movie.TraktSlug = watchedMovie.Movie.Ids.Slug;
                movie.TraktPlays = watchedMovie.Plays;
                movie.TraktLastWatchedAt = watchedMovie.LastWatchedAt;

                if (string.IsNullOrEmpty(movie.ImdbId) && !string.IsNullOrEmpty(watchedMovie.Movie.Ids.Imdb))
                {
                    movie.ImdbId = watchedMovie.Movie.Ids.Imdb;
                }

                // Set status to Completed if not already
                if (movie.Status != Status.Completed)
                {
                    movie.Status = Status.Completed;
                    movie.DateCompleted ??= watchedMovie.LastWatchedAt;
                }

                await LinkGenresAsync(movie, watchedMovie.Movie.Genres);
                _context.Update(movie);
                result.MoviesUpdated++;
            }
            else
            {
                // Create new movie from Trakt data
                var newMovie = new Movie
                {
                    Title = watchedMovie.Movie.Title,
                    MediaType = MediaType.Movie,
                    ReleaseYear = watchedMovie.Movie.Year,
                    TmdbId = tmdbId,
                    ImdbId = watchedMovie.Movie.Ids.Imdb,
                    TraktId = watchedMovie.Movie.Ids.Trakt,
                    TraktSlug = watchedMovie.Movie.Ids.Slug,
                    TraktPlays = watchedMovie.Plays,
                    TraktLastWatchedAt = watchedMovie.LastWatchedAt,
                    Status = Status.Completed,
                    DateCompleted = watchedMovie.LastWatchedAt,
                    DateAdded = DateTime.UtcNow
                };

                _context.Add(newMovie);
                await LinkGenresAsync(newMovie, watchedMovie.Movie.Genres);
                result.MoviesCreated++;
                _logger.LogDebug("Created new movie from Trakt: {Title} ({Year})", newMovie.Title, newMovie.ReleaseYear);
            }
        }

        private async Task ProcessWatchedShowAsync(TraktWatchedShowDto watchedShow, TraktSyncResultDto result)
        {
            var tmdbId = watchedShow.Show.Ids.Tmdb?.ToString();
            var show = await FindTvShowAsync(tmdbId, watchedShow.Show.Title, watchedShow.Show.Year);

            if (show == null)
            {
                // Create new show
                show = new TvShow
                {
                    Title = watchedShow.Show.Title,
                    MediaType = MediaType.TVShow,
                    FirstAirYear = watchedShow.Show.Year,
                    TmdbId = tmdbId,
                    Status = Status.ActivelyExploring,
                    DateAdded = DateTime.UtcNow
                };
                _context.Add(show);
                await _context.SaveChangesAsync(); // Save to get the ID for episodes
                result.ShowsCreated++;
                _logger.LogDebug("Created new TV show from Trakt: {Title} ({Year})", show.Title, show.FirstAirYear);
            }
            else
            {
                result.ShowsUpdated++;
            }

            await LinkGenresAsync(show, watchedShow.Show.Genres);

            // Update show-level Trakt fields
            show.TraktId = watchedShow.Show.Ids.Trakt;
            show.TraktSlug = watchedShow.Show.Ids.Slug;
            show.TraktLastWatchedAt = watchedShow.LastWatchedAt;

            // Process episodes and calculate aggregates
            int totalEpisodePlays = 0;
            int watchedEpisodeCount = 0;

            foreach (var season in watchedShow.Seasons)
            {
                foreach (var episode in season.Episodes)
                {
                    totalEpisodePlays += episode.Plays;
                    watchedEpisodeCount++;

                    await ProcessWatchedEpisodeAsync(show.Id, season.Number, episode, result);
                }
            }

            show.TraktPlays = totalEpisodePlays;

            // Determine show status based on episode completion
            if (show.NumberOfEpisodes.HasValue && show.NumberOfEpisodes > 0 && watchedEpisodeCount >= show.NumberOfEpisodes)
            {
                if (show.Status != Status.Completed && show.Status != Status.Abandoned)
                {
                    show.Status = Status.Completed;
                    show.DateCompleted ??= watchedShow.LastWatchedAt;
                }
            }
            else if (watchedEpisodeCount > 0 && show.Status == Status.Uncharted)
            {
                show.Status = Status.ActivelyExploring;
            }

            _context.Update(show);
        }

        private async Task ProcessWatchedEpisodeAsync(Guid showId, int seasonNumber, TraktWatchedEpisodeDto episodeDto, TraktSyncResultDto result)
        {
            var existing = await _context.TvShowEpisodes
                .FirstOrDefaultAsync(e => e.ShowId == showId
                    && e.SeasonNumber == seasonNumber
                    && e.EpisodeNumber == episodeDto.Number);

            if (existing != null)
            {
                existing.TraktPlays = episodeDto.Plays;
                existing.TraktLastWatchedAt = episodeDto.LastWatchedAt;
                if (existing.Status != Status.Completed)
                {
                    existing.Status = Status.Completed;
                    existing.DateCompleted ??= episodeDto.LastWatchedAt;
                }
                _context.Update(existing);
                result.EpisodesUpdated++;
            }
            else
            {
                var episode = new TvShowEpisode
                {
                    Title = $"S{seasonNumber}E{episodeDto.Number}",
                    MediaType = MediaType.TVShow,
                    ShowId = showId,
                    SeasonNumber = seasonNumber,
                    EpisodeNumber = episodeDto.Number,
                    TraktPlays = episodeDto.Plays,
                    TraktLastWatchedAt = episodeDto.LastWatchedAt,
                    Status = Status.Completed,
                    DateCompleted = episodeDto.LastWatchedAt,
                    DateAdded = DateTime.UtcNow
                };
                _context.Add(episode);
                result.EpisodesCreated++;
            }
        }

        private async Task ProcessWatchlistMovieAsync(TraktWatchlistItemDto item, TraktSyncResultDto result)
        {
            if (item.Movie == null) return;

            var tmdbId = item.Movie.Ids.Tmdb?.ToString();
            var movie = await FindMovieAsync(tmdbId, item.Movie.Title, item.Movie.Year);

            if (movie != null)
            {
                // Update Trakt IDs but do NOT overwrite status
                movie.TraktId ??= item.Movie.Ids.Trakt;
                movie.TraktSlug ??= item.Movie.Ids.Slug;

                if (string.IsNullOrEmpty(movie.Notes) && !string.IsNullOrEmpty(item.Notes))
                {
                    movie.Notes = item.Notes;
                }

                await LinkGenresAsync(movie, item.Movie.Genres);
                _context.Update(movie);
                result.MoviesUpdated++;
            }
            else
            {
                var newMovie = new Movie
                {
                    Title = item.Movie.Title,
                    MediaType = MediaType.Movie,
                    ReleaseYear = item.Movie.Year,
                    TmdbId = tmdbId,
                    ImdbId = item.Movie.Ids.Imdb,
                    TraktId = item.Movie.Ids.Trakt,
                    TraktSlug = item.Movie.Ids.Slug,
                    Status = Status.Uncharted,
                    Notes = item.Notes,
                    DateAdded = DateTime.UtcNow
                };
                _context.Add(newMovie);
                await LinkGenresAsync(newMovie, item.Movie.Genres);
                result.MoviesCreated++;
            }

            result.WatchlistItemsProcessed++;
        }

        private async Task ProcessWatchlistShowAsync(TraktWatchlistItemDto item, TraktSyncResultDto result)
        {
            if (item.Show == null) return;

            var tmdbId = item.Show.Ids.Tmdb?.ToString();
            var show = await FindTvShowAsync(tmdbId, item.Show.Title, item.Show.Year);

            if (show != null)
            {
                show.TraktId ??= item.Show.Ids.Trakt;
                show.TraktSlug ??= item.Show.Ids.Slug;

                if (string.IsNullOrEmpty(show.Notes) && !string.IsNullOrEmpty(item.Notes))
                {
                    show.Notes = item.Notes;
                }

                await LinkGenresAsync(show, item.Show.Genres);
                _context.Update(show);
                result.ShowsUpdated++;
            }
            else
            {
                var newShow = new TvShow
                {
                    Title = item.Show.Title,
                    MediaType = MediaType.TVShow,
                    FirstAirYear = item.Show.Year,
                    TmdbId = tmdbId,
                    TraktId = item.Show.Ids.Trakt,
                    TraktSlug = item.Show.Ids.Slug,
                    Status = Status.Uncharted,
                    Notes = item.Notes,
                    DateAdded = DateTime.UtcNow
                };
                _context.Add(newShow);
                await LinkGenresAsync(newShow, item.Show.Genres);
                result.ShowsCreated++;
            }

            result.WatchlistItemsProcessed++;
        }

        private async Task ProcessMovieRatingAsync(TraktRatingItemDto ratingItem, TraktSyncResultDto result)
        {
            if (ratingItem.Movie == null) return;

            var tmdbId = ratingItem.Movie.Ids.Tmdb?.ToString();
            var movie = await FindMovieAsync(tmdbId, ratingItem.Movie.Title, ratingItem.Movie.Year);

            if (movie == null)
            {
                _logger.LogDebug("Movie not found for rating, skipping: {Title}", ratingItem.Movie.Title);
                return;
            }

            movie.TraktRating = ratingItem.Rating;
            movie.TraktId ??= ratingItem.Movie.Ids.Trakt;

            // Only set app Rating if not already set by user
            if (movie.Rating == null)
            {
                movie.Rating = MapTraktRating(ratingItem.Rating);
            }

            await LinkGenresAsync(movie, ratingItem.Movie.Genres);
            _context.Update(movie);
            result.MoviesUpdated++;
            result.RatingsProcessed++;
        }

        private async Task ProcessShowRatingAsync(TraktRatingItemDto ratingItem, TraktSyncResultDto result)
        {
            if (ratingItem.Show == null) return;

            var tmdbId = ratingItem.Show.Ids.Tmdb?.ToString();
            var show = await FindTvShowAsync(tmdbId, ratingItem.Show.Title, ratingItem.Show.Year);

            if (show == null)
            {
                _logger.LogDebug("TV show not found for rating, skipping: {Title}", ratingItem.Show.Title);
                return;
            }

            show.TraktRating = ratingItem.Rating;
            show.TraktId ??= ratingItem.Show.Ids.Trakt;

            if (show.Rating == null)
            {
                show.Rating = MapTraktRating(ratingItem.Rating);
            }

            await LinkGenresAsync(show, ratingItem.Show.Genres);
            _context.Update(show);
            result.ShowsUpdated++;
            result.RatingsProcessed++;
        }

        // --- Genre Helpers ---

        /// <summary>
        /// Links the given Trakt genre slugs to a media item, creating genre records as needed.
        /// Idempotent: a slug already linked to the item (by normalized name) is skipped, so
        /// re-importing the same media never produces duplicate links. The item's <c>Genres</c>
        /// collection must already be loaded for existing items.
        /// </summary>
        private async Task LinkGenresAsync(BaseMediaItem item, IEnumerable<string> genreSlugs)
        {
            if (genreSlugs == null) return;

            var cache = await GetGenreCacheAsync();

            foreach (var slug in genreSlugs)
            {
                var name = NormalizeGenreName(slug);
                if (string.IsNullOrEmpty(name)) continue;

                // Skip if this item is already linked to the genre.
                if (item.Genres.Any(g => g.Name == name)) continue;

                if (!cache.TryGetValue(name, out var genre))
                {
                    genre = new Genre { Name = name };
                    _context.Add(genre);
                    cache[name] = genre;
                }

                item.Genres.Add(genre);
            }
        }

        private async Task<Dictionary<string, Genre>> GetGenreCacheAsync()
        {
            if (_genreCache == null)
            {
                var existing = await _context.Genres.ToListAsync();
                _genreCache = existing.ToDictionary(g => g.Name, g => g, StringComparer.Ordinal);
            }

            return _genreCache;
        }

        // Trakt returns lowercase, hyphenated slugs (e.g. "science-fiction"). Normalize to the project's
        // lowercase convention and replace hyphens with spaces so genres unify with other sources (TMDB,
        // ListenNotes) rather than creating parallel rows.
        private static string NormalizeGenreName(string slug) =>
            string.IsNullOrWhiteSpace(slug)
                ? string.Empty
                : slug.Trim().ToLowerInvariant().Replace('-', ' ');

        // --- Lookup Helpers ---

        private async Task<Movie?> FindMovieAsync(string? tmdbId, string title, int? year)
        {
            // First try TMDB ID match
            if (!string.IsNullOrEmpty(tmdbId))
            {
                var movie = await _context.Movies
                    .Include(m => m.Genres)
                    .FirstOrDefaultAsync(m => m.TmdbId == tmdbId);
                if (movie != null) return movie;
            }

            // Fallback: title + year match
            var query = _context.Movies
                .Include(m => m.Genres)
                .Where(m => m.Title.ToLower() == title.ToLower());
            if (year.HasValue)
            {
                query = query.Where(m => m.ReleaseYear == year.Value);
            }

            return await query.FirstOrDefaultAsync();
        }

        private async Task<TvShow?> FindTvShowAsync(string? tmdbId, string title, int? year)
        {
            if (!string.IsNullOrEmpty(tmdbId))
            {
                var show = await _context.TvShows
                    .Include(s => s.Genres)
                    .FirstOrDefaultAsync(s => s.TmdbId == tmdbId);
                if (show != null) return show;
            }

            var query = _context.TvShows
                .Include(s => s.Genres)
                .Where(s => s.Title.ToLower() == title.ToLower());
            if (year.HasValue)
            {
                query = query.Where(s => s.FirstAirYear == year.Value);
            }

            return await query.FirstOrDefaultAsync();
        }

        // --- Rating Mapping ---

        private static Rating MapTraktRating(int traktRating)
        {
            return traktRating switch
            {
                <= 3 => Rating.Dislike,
                <= 5 => Rating.Neutral,
                <= 8 => Rating.Like,
                _ => Rating.SuperLike
            };
        }
    }
}
