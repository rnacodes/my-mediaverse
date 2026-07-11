using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Search
{
    /// <summary>
    /// Best-effort media reindex fired at the end of interactive imports. Mirrors the swallow-and-log
    /// pattern of <see cref="SearchIndexSyncHostedService"/>: a Typesense/OpenAI hiccup logs an error
    /// and returns, leaving the imported rows in PostgreSQL to be indexed by the next scheduled (or
    /// N8N-driven) reindex. The bulk reindex upserts in place and skips unchanged items, so it only
    /// re-embeds the newly imported text.
    /// </summary>
    public class ImportReindexService : IImportReindexService
    {
        private readonly ITypesenseService _typesense;
        private readonly ILogger<ImportReindexService> _logger;

        public ImportReindexService(ITypesenseService typesense, ILogger<ImportReindexService> logger)
        {
            _typesense = typesense;
            _logger = logger;
        }

        public async Task ReindexAfterImportAsync(int importedCount, string importLabel)
        {
            if (importedCount <= 0)
            {
                _logger.LogInformation(
                    "Skipping post-import reindex for {ImportLabel}: no items imported", importLabel);
                return;
            }

            try
            {
                var count = await _typesense.BulkReindexAllMediaItemsAsync();
                _logger.LogInformation(
                    "Post-import reindex after {ImportLabel} completed: indexed {Count} media items",
                    importLabel, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Post-import reindex after {ImportLabel} failed; imported items remain in the database and will be indexed by the next scheduled reindex",
                    importLabel);
            }
        }
    }
}
