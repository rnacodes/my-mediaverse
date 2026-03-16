using System;

namespace MyMediaVerse.Domain.Enums
{
    /// <summary>
    /// Tracks which external services have synced each article.
    /// Uses Flags attribute to allow combining multiple statuses.
    /// </summary>
    [Flags]
    public enum SyncStatus
    {
        /// <summary>
        /// Article was created locally, not synced from any external service
        /// </summary>
        LocalOnly = 0,

        /// <summary>
        /// Article has been synced with Readwise (highlights only)
        /// </summary>
        ReadwiseSynced = 2,

        /// <summary>
        /// Article has been synced with Readwise Reader (document metadata and content)
        /// </summary>
        ReaderSynced = 4,

        /// <summary>
        /// Article is fully synced across all available services
        /// </summary>
        FullySynced = ReadwiseSynced | ReaderSynced
    }
}
