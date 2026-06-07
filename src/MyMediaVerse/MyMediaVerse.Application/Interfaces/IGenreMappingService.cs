namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// External provider whose numeric genre ids are being resolved to names.
    /// </summary>
    public enum GenreSource
    {
        Tmdb,
        ListenNotes
    }

    /// <summary>
    /// Resolves external provider genre ids (TMDB, ListenNotes) to lowercase genre names
    /// for use during media import. Maps are built from the existing TMDB/ListenNotes
    /// genre-fetch services and cached in memory.
    /// </summary>
    public interface IGenreMappingService
    {
        /// <summary>
        /// Resolves a single genre id to its lowercase name. Returns null (and logs a
        /// warning) when the id is unknown, so an import never throws on a stray genre.
        /// </summary>
        Task<string?> GetGenreNameAsync(GenreSource source, int genreId);

        /// <summary>
        /// Resolves a batch of genre ids (e.g. an import payload's genre_ids[]) to lowercase
        /// names. Unknown ids are skipped (and logged), preserving the order of resolved ids.
        /// </summary>
        Task<IReadOnlyList<string>> GetGenreNamesAsync(GenreSource source, IEnumerable<int> genreIds);
    }
}
