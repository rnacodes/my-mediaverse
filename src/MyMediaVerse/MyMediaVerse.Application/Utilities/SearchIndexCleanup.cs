using Microsoft.Extensions.Logging;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// The shared eager-delete pattern for search documents: every delete path makes a
    /// best-effort index cleanup so search stops showing the row immediately, and a
    /// failure is logged and swallowed — the next bulk reindex's ID-diff reconcile is
    /// always the guaranteed backstop. Deletion must never fail because Typesense was
    /// unreachable.
    /// </summary>
    public static class SearchIndexCleanup
    {
        public static async Task TryDeleteAsync(Func<Task> deleteFromIndex, ILogger logger, string kind, Guid id)
        {
            try
            {
                await deleteFromIndex();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to remove {Kind} {Id} from the search index; it will be removed on the next reindex",
                    kind, id);
            }
        }
    }
}
