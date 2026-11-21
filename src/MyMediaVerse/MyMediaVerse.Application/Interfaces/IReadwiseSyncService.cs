using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Orchestrates the unified Readwise sync (Reader documents + highlights) and maintains
    /// the persisted sync cursor used for incremental runs.
    /// </summary>
    public interface IReadwiseSyncService
    {
        /// <param name="incremental">
        /// When true, only items updated since the last fully-successful run are synced
        /// (falling back to a default look-back window until one is recorded).
        /// When false, everything is synced.
        /// </param>
        /// <returns>The run's report, plus the ids of the articles the Reader step created or changed in a
        /// searchable field and the stub books the highlights step created.</returns>
        Task<ChangedItemsResult<ReadwiseSyncAllResultDto>> SyncAllAsync(bool incremental);
    }
}
