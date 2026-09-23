using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Imports a stored TV show's episodes from TMDB, one request per season.
    /// </summary>
    public interface ITvEpisodeImportService
    {
        /// <summary>
        /// Walks every season TMDB lists for the show (specials included) and upserts its episodes
        /// keyed on (season, episode). New episodes are created uncharted with TMDB's title,
        /// description, air date, runtime, and still; stored episodes are only filled in — a
        /// placeholder title such as <c>S1E3</c> is replaced, empty descriptive fields are set, and
        /// status, plays, watch dates, rating, notes, and topics are never touched. A season whose
        /// request fails is counted and the run continues. The show's season and episode counts are
        /// refreshed from the same details payload; its TMDB refresh timestamp is not moved.
        /// </summary>
        /// <param name="showId">The stored TV show. It must carry a numeric TMDB id.</param>
        /// <param name="delayBetweenCallsMs">Delay between TMDB requests in milliseconds (default: 250)</param>
        /// <param name="cancellationToken">Stops the run between seasons; episodes already walked are still saved.</param>
        Task<TvEpisodeImportResultDto> ImportFromTmdbAsync(
            Guid showId,
            int delayBetweenCallsMs = 250,
            CancellationToken cancellationToken = default);
    }
}
