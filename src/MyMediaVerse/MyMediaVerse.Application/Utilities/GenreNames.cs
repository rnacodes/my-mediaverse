namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// The single implementation of MMV's genre-name invariant: trimmed and lowercase.
    /// Every path that writes genres normalizes through here so a genre arriving from TMDB,
    /// Trakt, or a form resolves to the same row.
    /// </summary>
    public static class GenreNames
    {
        /// <summary>Returns the normalized name, or null when nothing remains.</summary>
        public static string? Normalize(string? name)
            => string.IsNullOrWhiteSpace(name) ? null : name.Trim().ToLowerInvariant();

        /// <summary>Normalizes a raw list, dropping blanks and duplicates. First-appearance order is preserved.</summary>
        public static List<string> NormalizeList(IEnumerable<string?>? names)
        {
            if (names == null) return new List<string>();

            return names
                .Select(Normalize)
                .Where(n => n != null)
                .Select(n => n!)
                .Distinct()
                .ToList();
        }
    }
}
