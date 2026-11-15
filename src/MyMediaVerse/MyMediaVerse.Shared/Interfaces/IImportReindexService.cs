namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Fires a best-effort search reindex after an interactive (user-triggered) import or enrichment.
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

        /// <summary>
        /// Reindexes one media item by id — the single-item counterpart for imports and enrichments
        /// that touch exactly one row, where a full-library reindex would be wasteful. Never throws.
        /// </summary>
        /// <param name="mediaItemId">The media item that was created or changed.</param>
        /// <param name="importLabel">Short label used in log messages (e.g. "Open Library import").</param>
        Task ReindexItemAfterImportAsync(Guid mediaItemId, string importLabel);

        /// <summary>
        /// Reindexes just the given media items — for imports and syncs that create or change a few
        /// known rows. Skips (no-op) when <paramref name="mediaItemIds"/> is empty. Never throws.
        /// </summary>
        /// <param name="mediaItemIds">The media items that were created or changed.</param>
        /// <param name="importLabel">Short label used in log messages (e.g. "YouTube playlist sync").</param>
        Task ReindexItemsAfterImportAsync(IReadOnlyCollection<Guid> mediaItemIds, string importLabel);
    }
}
