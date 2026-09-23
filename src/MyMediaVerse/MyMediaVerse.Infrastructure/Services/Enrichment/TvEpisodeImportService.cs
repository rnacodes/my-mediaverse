using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Imports a TV show's episodes from TMDB: one details call for the season list, then one
    /// call per season. Episodes are upserted on (season, episode); stored rows are filled in,
    /// never overwritten, so watch history from Trakt and hand edits survive.
    /// </summary>
    public class TvEpisodeImportService : ITvEpisodeImportService
    {
        // Trakt creates episodes it has no metadata for under a placeholder title like "S1E3".
        // That is the one title an import is allowed to replace.
        private static readonly Regex PlaceholderTitle = new(@"^S\d+E\d+$", RegexOptions.Compiled);

        private readonly IApplicationDbContext _context;
        private readonly ITmdbApiClient _tmdbClient;
        private readonly ILogger<TvEpisodeImportService> _logger;

        public TvEpisodeImportService(
            IApplicationDbContext context,
            ITmdbApiClient tmdbClient,
            ILogger<TvEpisodeImportService> logger)
        {
            _context = context;
            _tmdbClient = tmdbClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<TvEpisodeImportResultDto> ImportFromTmdbAsync(
            Guid showId,
            int delayBetweenCallsMs = 250,
            CancellationToken cancellationToken = default)
        {
            var result = new TvEpisodeImportResultDto { ShowId = showId, StartedAt = DateTime.UtcNow };

            try
            {
                var show = await _context.TvShows.FirstOrDefaultAsync(s => s.Id == showId, cancellationToken);
                if (show == null)
                {
                    return Abort(result, $"TV show with ID {showId} not found.");
                }

                result.ShowTitle = show.Title;

                if (!int.TryParse(show.TmdbId, out var tmdbId))
                {
                    return Abort(result, $"'{show.Title}' has no usable TMDB id; import the show from TMDB or set its TMDB id first.");
                }

                TmdbTvShowDto details;
                try
                {
                    details = await _tmdbClient.GetTvShowDetailsAsync(tmdbId);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return Abort(result, $"TMDB no longer has id {tmdbId} for '{show.Title}'; correct or clear the show's TMDB id.");
                }

                // TMDB owns these counts and the payload is already in hand. The refresh timestamp is
                // left alone: it records when show-level data was applied, which this is not.
                if (details.NumberOfSeasons > 0) show.NumberOfSeasons = details.NumberOfSeasons;
                if (details.NumberOfEpisodes > 0) show.NumberOfEpisodes = details.NumberOfEpisodes;

                var seasons = details.Seasons.OrderBy(s => s.SeasonNumber).ToList();
                if (seasons.Count == 0)
                {
                    result.Warnings.Add($"TMDB lists no seasons for '{show.Title}'.");
                }

                // One lookup for the whole run; new rows join it so a season payload that repeats an
                // episode cannot insert it twice.
                var existing = await _context.TvShowEpisodes
                    .Where(e => e.ShowId == showId)
                    .ToListAsync(cancellationToken);
                var byNumber = new Dictionary<(int Season, int Episode), TvShowEpisode>();
                foreach (var episode in existing.Where(e => e.SeasonNumber.HasValue && e.EpisodeNumber.HasValue))
                {
                    byNumber.TryAdd((episode.SeasonNumber!.Value, episode.EpisodeNumber!.Value), episode);
                }

                _logger.LogInformation(
                    "Starting TMDB episode import for '{Title}' (TMDB ID: {TmdbId}): {SeasonCount} season(s), {ExistingCount} stored episode(s)",
                    show.Title, tmdbId, seasons.Count, existing.Count);

                foreach (var season in seasons)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Warnings.Add("The run was cancelled before every season was imported.");
                        break;
                    }

                    if (delayBetweenCallsMs > 0)
                    {
                        try
                        {
                            await Task.Delay(delayBetweenCallsMs, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Handled at the top of the loop, so the seasons already walked are still saved.
                            continue;
                        }
                    }

                    try
                    {
                        var payload = await _tmdbClient.GetTvSeasonAsync(tmdbId, season.SeasonNumber);

                        foreach (var tmdbEpisode in payload.Episodes)
                        {
                            var key = (tmdbEpisode.SeasonNumber, tmdbEpisode.EpisodeNumber);
                            if (byNumber.TryGetValue(key, out var stored))
                            {
                                if (FillEpisode(stored, tmdbEpisode)) result.UpdatedCount++;
                                else result.SkippedCount++;
                            }
                            else
                            {
                                var created = CreateEpisode(show, tmdbId, tmdbEpisode);
                                _context.Add(created);
                                byNumber[key] = created;
                                result.CreatedCount++;
                            }
                        }

                        result.SeasonsProcessed++;
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        result.FailedCount++;
                        result.AddError($"TMDB has no season {season.SeasonNumber} for '{show.Title}'.");
                        _logger.LogWarning("TMDB returned 404 for season {SeasonNumber} of {Title} (TMDB ID: {TmdbId})",
                            season.SeasonNumber, show.Title, tmdbId);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        result.FailedCount++;
                        result.AddError($"Failed to import season {season.SeasonNumber} of '{show.Title}': {ex.Message}");
                        _logger.LogWarning(ex, "Failed to import season {SeasonNumber} of {Title} (TMDB ID: {TmdbId})",
                            season.SeasonNumber, show.Title, tmdbId);
                    }
                }

                await _context.SaveChangesAsync(CancellationToken.None);
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "TMDB episode import for '{Title}' complete. Created: {Created}, updated: {Updated}, skipped: {Skipped}, failed seasons: {Failed}",
                    show.Title, result.CreatedCount, result.UpdatedCount, result.SkippedCount, result.FailedCount);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"TMDB episode import failed: {ex.Message}";
                _logger.LogError(ex, "TMDB episode import for show {ShowId} failed", showId);
            }

            return result;
        }

        private TvShowEpisode CreateEpisode(TvShow show, int showTmdbId, TmdbTvEpisodeDto tmdb)
        {
            var episode = new TvShowEpisode
            {
                // A nameless TMDB episode gets the placeholder form, so a later run can still name it.
                Title = string.IsNullOrWhiteSpace(tmdb.Name) ? $"S{tmdb.SeasonNumber}E{tmdb.EpisodeNumber}" : tmdb.Name,
                ShowId = show.Id,
                SeasonNumber = tmdb.SeasonNumber,
                EpisodeNumber = tmdb.EpisodeNumber,
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Link = $"https://www.themoviedb.org/tv/{showTmdbId}/season/{tmdb.SeasonNumber}/episode/{tmdb.EpisodeNumber}"
            };

            FillEpisode(episode, tmdb);
            return episode;
        }

        /// <summary>
        /// Copies TMDB values onto an episode where the episode has none. Only a placeholder title
        /// is replaced. Returns true when any value was set.
        /// </summary>
        private bool FillEpisode(TvShowEpisode episode, TmdbTvEpisodeDto tmdb)
        {
            var changed = false;

            if (PlaceholderTitle.IsMatch(episode.Title) && !string.IsNullOrWhiteSpace(tmdb.Name) && episode.Title != tmdb.Name)
            {
                episode.Title = tmdb.Name;
                changed = true;
            }

            if (string.IsNullOrEmpty(episode.Description) && !string.IsNullOrWhiteSpace(tmdb.Overview))
            {
                episode.Description = tmdb.Overview;
                changed = true;
            }

            if (!episode.AirDate.HasValue
                && DateTime.TryParse(tmdb.AirDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var airDate))
            {
                episode.AirDate = DateTimeNormalizer.ToUtc(airDate);
                changed = true;
            }

            if (!episode.DurationInMinutes.HasValue && tmdb.Runtime is > 0)
            {
                episode.DurationInMinutes = tmdb.Runtime;
                changed = true;
            }

            if (!episode.TmdbEpisodeId.HasValue && tmdb.Id > 0)
            {
                episode.TmdbEpisodeId = tmdb.Id;
                changed = true;
            }

            if (!string.IsNullOrEmpty(tmdb.StillPath))
            {
                if (string.IsNullOrEmpty(episode.StillPath))
                {
                    episode.StillPath = tmdb.StillPath;
                    changed = true;
                }

                if (string.IsNullOrEmpty(episode.Thumbnail))
                {
                    episode.Thumbnail = _tmdbClient.GetImageUrl(tmdb.StillPath, "w500");
                    changed = true;
                }
            }

            return changed;
        }

        private static TvEpisodeImportResultDto Abort(TvEpisodeImportResultDto result, string reason)
        {
            result.Success = false;
            result.ErrorMessage = reason;
            return result;
        }
    }
}
