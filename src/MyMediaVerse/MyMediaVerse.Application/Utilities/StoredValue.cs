namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Update rule for columns an edit request may not carry: identities and values that come
    /// from the item's source (a TMDB id, a feed URL, a catalog rating). A form only sends the
    /// fields it shows, so a value it leaves out means "unchanged", never "erase".
    /// </summary>
    public static class StoredValue
    {
        /// <summary>Returns the incoming text, or the stored one when the incoming is null or blank.</summary>
        public static string? UnlessProvided(string? incoming, string? stored) =>
            string.IsNullOrWhiteSpace(incoming) ? stored : incoming;

        /// <summary>Returns the incoming value, or the stored one when the incoming is null.</summary>
        public static T? UnlessProvided<T>(T? incoming, T? stored) where T : struct =>
            incoming ?? stored;
    }
}
