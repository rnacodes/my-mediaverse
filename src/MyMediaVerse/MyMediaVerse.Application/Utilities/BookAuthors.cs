namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Stores a book's full author list in the single Author column and recovers the
    /// primary (first-listed) author for matching. Sources like Google Books return every
    /// credited author, and all of them are kept rather than only the first.
    /// </summary>
    public static class BookAuthors
    {
        /// <summary>Matches the Author column's max length.</summary>
        public const int MaxLength = 300;

        public const string Separator = ", ";

        /// <summary>
        /// Joins the authors in source order, skipping blanks and repeats. Names that would
        /// push the value past <see cref="MaxLength"/> are left off whole rather than cut
        /// mid-name. Returns null when there are no usable names.
        /// </summary>
        public static string? Join(IEnumerable<string?>? authors)
        {
            if (authors == null) return null;

            var names = new List<string>();
            var length = 0;

            foreach (var raw in authors)
            {
                var name = raw?.Trim();
                if (string.IsNullOrEmpty(name)) continue;
                if (names.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;

                var added = names.Count == 0 ? name.Length : Separator.Length + name.Length;
                if (length + added > MaxLength)
                {
                    if (names.Count == 0) return name[..MaxLength];
                    break;
                }

                names.Add(name);
                length += added;
            }

            return names.Count == 0 ? null : string.Join(Separator, names);
        }

        /// <summary>
        /// The first-listed author of a stored Author value ("A, B" → "A"). Used wherever
        /// books are matched by author, so a multi-author row still matches a source that
        /// only provides the primary author.
        /// </summary>
        public static string Primary(string author)
        {
            var trimmed = author.Trim();
            var index = trimmed.IndexOf(Separator, StringComparison.Ordinal);
            return index < 0 ? trimmed : trimmed[..index].Trim();
        }
    }
}
