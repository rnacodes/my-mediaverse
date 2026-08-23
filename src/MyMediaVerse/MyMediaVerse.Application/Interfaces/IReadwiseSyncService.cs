using MyMediaVerse.DTOs;

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
        Task<ReadwiseSyncAllResultDto> SyncAllAsync(bool incremental);
    }
}
