namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Fires a best-effort search reindex after an import, sync or enrichment, covering only the items
    /// it changed. The full-library reindex is deliberately not here: it lives on
    /// <see cref="ITypesenseService.BulkReindexAllMediaItemsAsync"/> (the Admin page and the scheduled sweep).
    /// </summary>
    public interface IImportReindexService
    {
        /// <summary>
        /// Reindexes one media item by id — the single-item counterpart for imports and enrichments
        /// that touch exactly one row, where a batch call would be overkill. Never throws.
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
