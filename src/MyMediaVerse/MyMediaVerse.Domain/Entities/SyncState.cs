using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Per-source sync bookkeeping for the single-user app: one row per external source
    /// (e.g. "readwise"), holding the last successful sync time plus an optional free-form
    /// value for source-specific cursors or settings.
    /// </summary>
    public class SyncState
    {
        /// <summary>Source identifier, e.g. "readwise". Lower-case, stable.</summary>
        [Key]
        [StringLength(100)]
        public required string Key { get; set; }

        /// <summary>
        /// UTC timestamp captured at the start of the most recent run that completed fully
        /// (no errors, no truncation). Null until the first successful run.
        /// </summary>
        public DateTime? LastSuccessfulSyncAt { get; set; }

        /// <summary>Optional source-specific value (page token, etag, small JSON, ...).</summary>
        [StringLength(4000)]
        public string? Value { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
