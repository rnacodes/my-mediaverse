namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Where an incremental sync window came from.
    /// </summary>
    public enum SyncWindowSource
    {
        /// <summary>Derived from the persisted last-successful-sync cursor.</summary>
        Cursor,
        /// <summary>No cursor yet; the default look-back window was used.</summary>
        Default
    }

    /// <summary>
    /// Resolved incremental window: sync everything updated at or after <see cref="Since"/>.
    /// </summary>
    public record IncrementalSyncWindow(DateTime Since, SyncWindowSource Source);

    /// <summary>
    /// Persists per-source sync bookkeeping (last successful run, optional cursor value)
    /// so incremental syncs pick up exactly where the previous successful run left off.
    /// </summary>
    public interface ISyncStateService
    {
        /// <summary>
        /// Legacy key from when Reader documents and Readwise highlights shared one cursor.
        /// Only read to seed the two per-source keys below on their first run.
        /// </summary>
        const string ReadwiseKey = "readwise";

        /// <summary>Cursor for the Readwise Reader (documents, v3 API) sync step.</summary>
        const string ReadwiseReaderKey = "readwise-reader";

        /// <summary>Cursor for the Readwise highlights (v2 API) sync step.</summary>
        const string ReadwiseHighlightsKey = "readwise-highlights";

        /// <summary>Returns the last fully-successful run time for the source, or null if none.</summary>
        Task<DateTime?> GetLastSuccessfulSyncAsync(string key);

        /// <summary>
        /// Resolves the incremental window for the source: the cursor minus <paramref name="overlap"/>
        /// when a cursor exists, otherwise <paramref name="now"/> minus <paramref name="defaultLookBack"/>.
        /// The overlap guards against clock skew and edits that landed mid-run.
        /// </summary>
        Task<IncrementalSyncWindow> GetIncrementalWindowAsync(string key, DateTime now, TimeSpan defaultLookBack, TimeSpan overlap);

        /// <summary>
        /// Records a fully-successful run. <paramref name="runStartedAt"/> should be captured
        /// before the run begins so changes made during the run are not skipped next time.
        /// Never moves the cursor backwards.
        /// </summary>
        Task MarkSyncSucceededAsync(string key, DateTime runStartedAt);
    }
}
