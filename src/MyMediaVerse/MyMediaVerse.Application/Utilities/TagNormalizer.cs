namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// The single implementation of MMV's tag invariant: trimmed, lowercase, no blanks,
    /// no duplicates. Every path that writes tags — highlight create/update/sync/bulk,
    /// note sync — must normalize through here so stored tags are byte-comparable across
    /// sources and tag lookups never miss rows over stray whitespace or casing.
    /// </summary>
    public static class TagNormalizer
    {
        /// <summary>
        /// Normalizes a raw tag list. Order of first appearance is preserved.
        /// </summary>
        public static List<string> NormalizeList(IEnumerable<string?>? tags)
        {
            if (tags == null) return new List<string>();

            return tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t!.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Normalizes and comma-joins tags for the highlight <c>Tags</c> column.
        /// Returns null when nothing remains, matching the column's null-means-untagged use.
        /// </summary>
        public static string? JoinForStorage(IEnumerable<string?>? tags)
        {
            var normalized = NormalizeList(tags);
            return normalized.Count > 0 ? string.Join(",", normalized) : null;
        }

        /// <summary>
        /// Splits a stored comma-joined tags string back into a normalized list.
        /// Tolerates legacy values written before normalization was consistent
        /// (leading spaces, empty segments).
        /// </summary>
        public static List<string> SplitStored(string? tags)
        {
            if (string.IsNullOrWhiteSpace(tags)) return new List<string>();
            return NormalizeList(tags.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
