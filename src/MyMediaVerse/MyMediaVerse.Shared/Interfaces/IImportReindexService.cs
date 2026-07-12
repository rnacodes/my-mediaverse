namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Fires a best-effort, full-library search reindex after an interactive (user-triggered) import
    /// completes, so freshly imported items are searchable immediately without a manual "reindex"
    /// click. Failures are swallowed and logged — a search hiccup must never fail an import whose
    /// rows are already committed to PostgreSQL, and the next scheduled reindex will pick them up.
    /// </summary>
    public interface IImportReindexService
    {
        /// <summary>
        /// Reindexes the media collection when <paramref name="importedCount"/> is greater than zero,
        /// and skips (no-op) when the import changed nothing. Never throws.
        /// </summary>
        /// <param name="importedCount">Number of items the import created or updated.</param>
        /// <param name="importLabel">Short label used in log messages (e.g. "Goodreads CSV").</param>
        Task ReindexAfterImportAsync(int importedCount, string importLabel);
    }
}
