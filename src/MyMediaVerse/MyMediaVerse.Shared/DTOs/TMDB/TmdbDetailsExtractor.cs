namespace MyMediaVerse.Shared.DTOs.TMDB
{
    /// <summary>
    /// Derives the library's flat credit and rating fields from a TMDB details payload fetched
    /// with <c>append_to_response</c>. Every method returns null when TMDB has nothing to offer,
    /// so callers can tell "no data" apart from an empty string.
    /// </summary>
    public static class TmdbDetailsExtractor
    {
        public const int MaxDirectorLength = 100;
        public const int MaxCreatorLength = 100;
        public const int MaxCastLength = 500;
        public const int MaxCastMembers = 8;

        private const string RatingCountry = "US";
        private const string Separator = ", ";

        public static string? GetDirector(TmdbMovieDto movie)
        {
            var directors = movie.Credits?.Crew
                .Where(c => string.Equals(c.Job, "Director", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Name);

            return JoinNames(directors, MaxDirectorLength);
        }

        public static string? GetCreator(TmdbTvShowDto show)
            => JoinNames(show.CreatedBy.Select(c => c.Name), MaxCreatorLength);

        public static string? GetCast(TmdbCreditsDto? credits)
        {
            var cast = credits?.Cast
                .OrderBy(c => c.Order)
                .Take(MaxCastMembers)
                .Select(c => c.Name);

            return JoinNames(cast, MaxCastLength);
        }

        /// <summary>US certification; a country can list several releases, so the first non-empty one wins.</summary>
        public static string? GetMpaaRating(TmdbMovieDto movie)
        {
            return movie.ReleaseDates?.Results
                .Where(r => string.Equals(r.Iso31661, RatingCountry, StringComparison.OrdinalIgnoreCase))
                .SelectMany(r => r.ReleaseDates)
                .Select(d => d.Certification?.Trim())
                .FirstOrDefault(c => !string.IsNullOrEmpty(c));
        }

        public static string? GetContentRating(TmdbTvShowDto show)
        {
            return show.ContentRatings?.Results
                .Where(r => string.Equals(r.Iso31661, RatingCountry, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Rating?.Trim())
                .FirstOrDefault(r => !string.IsNullOrEmpty(r));
        }

        // Joins whole names only: a name that would push the value past the column cap is
        // dropped along with everything after it, rather than being cut mid-name.
        private static string? JoinNames(IEnumerable<string>? names, int maxLength)
        {
            if (names == null) return null;

            var kept = new List<string>();
            var length = 0;

            foreach (var name in names.Select(n => n?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct())
            {
                var added = name!.Length + (kept.Count > 0 ? Separator.Length : 0);
                if (length + added > maxLength) break;

                kept.Add(name);
                length += added;
            }

            return kept.Count > 0 ? string.Join(Separator, kept) : null;
        }
    }
}
