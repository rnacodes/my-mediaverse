using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Search
{
    /// <summary>
    /// Best-effort media reindex fired at the end of interactive imports and enrichments. Mirrors the
    /// swallow-and-log pattern of <see cref="SearchIndexSyncHostedService"/>: a Typesense/OpenAI hiccup
    /// logs an error and returns, leaving the rows in PostgreSQL to be indexed by the next scheduled (or
    /// N8N-driven) reindex. The bulk reindex upserts every document in place; Typesense only re-embeds
    /// the ones whose embedding text changed. When the caller knows which rows changed, the targeted
    /// methods send just those documents.
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

        public async Task ReindexItemAfterImportAsync(Guid mediaItemId, string importLabel)
        {
            try
            {
                var indexed = await _typesense.ReindexMediaItemByIdAsync(mediaItemId);
                if (indexed)
                {
                    _logger.LogInformation(
                        "Post-import reindex after {ImportLabel} completed for media item {MediaItemId}",
                        importLabel, mediaItemId);
                }
                else
                {
                    _logger.LogWarning(
                        "Post-import reindex after {ImportLabel} did not index media item {MediaItemId}; it will be indexed by the next scheduled reindex",
                        importLabel, mediaItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Post-import reindex after {ImportLabel} failed for media item {MediaItemId}; the row remains in the database and will be indexed by the next scheduled reindex",
                    importLabel, mediaItemId);
            }
        }

        public async Task ReindexItemsAfterImportAsync(IReadOnlyCollection<Guid> mediaItemIds, string importLabel)
        {
            if (mediaItemIds.Count == 0)
            {
                _logger.LogInformation(
                    "Skipping post-import reindex for {ImportLabel}: no items changed", importLabel);
                return;
            }

            try
            {
                var count = await _typesense.ReindexMediaItemsByIdsAsync(mediaItemIds);
                _logger.LogInformation(
                    "Post-import reindex after {ImportLabel} completed: indexed {Count} media items",
                    importLabel, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Post-import reindex after {ImportLabel} failed for {Requested} media items; the rows remain in the database and will be indexed by the next scheduled reindex",
                    importLabel, mediaItemIds.Count);
            }
        }
    }
}
