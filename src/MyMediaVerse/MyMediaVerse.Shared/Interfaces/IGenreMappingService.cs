namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// External provider whose numeric genre ids are being resolved to names.
    /// </summary>
    public enum GenreSource
    {
        Tmdb
    }

    /// <summary>
    /// The single TMDB → library genre mapper. TMDB hands genres over in two shapes: details
    /// payloads carry names, search payloads carry only numeric ids. Both entry points apply
    /// the same rules, so a genre resolves to the same library name whichever way it arrived:
    /// lowercase, compound names split on <c>&amp;</c> ("Action &amp; Adventure" → "action",
    /// "adventure"), and TMDB's "sci-fi" spelled out as "science fiction".
    /// </summary>
    public interface IGenreMappingService
    {
        /// <summary>
        /// Maps TMDB genre names (a details payload's genres[]) to library genre names.
        /// Pure: no lookups, no I/O. Blanks and duplicates are dropped; order is preserved.
        /// </summary>
        IReadOnlyList<string> MapTmdbGenreNames(IEnumerable<string?>? tmdbGenreNames);

        /// <summary>
        /// Resolves a batch of genre ids (a search payload's genre_ids[]) to library genre
        /// names. The id map is built from TMDB's genre lists on first use and cached in memory.
        /// Unknown ids are skipped (and logged), so an import never throws on a stray genre.
        /// </summary>
        Task<IReadOnlyList<string>> GetGenreNamesAsync(GenreSource source, IEnumerable<int> genreIds);
    }
}
