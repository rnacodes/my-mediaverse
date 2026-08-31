using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IReaderService
    {
        /// <summary>
        /// Syncs documents from Readwise Reader
        /// </summary>
        /// <param name="location">Filter by location (archive, new, later, feed)</param>
        /// <param name="updatedAfter">Only sync documents updated after this date (for incremental sync)</param>
        Task<ReaderSyncResultDto> SyncDocumentsAsync(string? location = null, DateTime? updatedAfter = null);

        /// <summary>
        /// Fetches and stores full HTML content for an article
        /// </summary>
        Task<bool> FetchAndStoreArticleContentAsync(Guid articleId);

        /// <summary>
        /// Bulk fetch content for archived articles missing FullTextContent.
        /// Only fetches Completed (archived) articles for archival purposes.
        /// Reports fetched articles as UpdatedCount and articles with no content available as SkippedCount.
        /// </summary>
        /// <param name="batchSize">Number of articles to fetch</param>
        /// <param name="updatedAfter">Only fetch articles synced after this date (for 7-day incremental)</param>
        Task<ReaderSyncResultDto> BulkFetchArticleContentsAsync(int batchSize = 50, DateTime? updatedAfter = null);

        /// <summary>
        /// Test endpoint: Fetches a document directly from Reader API by its document ID.
        /// Returns the raw API response for debugging purposes.
        /// </summary>
        /// <param name="readerDocumentId">The Readwise Reader document ID</param>
        /// <param name="includeHtml">Whether to request HTML content from the API</param>
        Task<ReaderDocumentTestResultDto> TestFetchDocumentByIdAsync(string readerDocumentId, bool includeHtml = true);

        /// <summary>
        /// Fetches content for a specific article using its Reader document ID.
        /// Unlike FetchAndStoreArticleContentAsync, this bypasses status checks and can be used for any article.
        /// </summary>
        /// <param name="readerDocumentId">The Readwise Reader document ID</param>
        Task<(bool success, string message, int? contentLength)> FetchContentByReaderDocumentIdAsync(string readerDocumentId);

        /// <summary>
        /// Lists articles that have Reader document IDs, useful for testing.
        /// </summary>
        /// <param name="limit">Maximum number of articles to return (default 20)</param>
        /// <param name="onlyWithoutContent">If true, only returns articles without FullTextContent</param>
        /// <param name="status">Filter by article status (e.g., "Completed" for archived articles)</param>
        Task<IEnumerable<ReaderArticleSummaryDto>> GetArticlesWithReaderDocumentIdsAsync(int limit = 20, bool onlyWithoutContent = false, string? status = null);

        /// <summary>
        /// Fetches documents directly from the Reader API (not from the database).
        /// Useful for testing and seeing what's in Readwise Reader.
        /// </summary>
        /// <param name="location">Filter by location: "new", "later", "archive", "feed"</param>
        /// <param name="limit">Maximum number of documents to return</param>
        Task<IEnumerable<ReaderArticleSummaryDto>> FetchDocumentsFromReaderApiAsync(string? location = null, int limit = 50);

        /// <summary>
        /// Syncs documents from the Reader API filtered by location, capped at <paramref name="limit"/> documents.
        /// </summary>
        /// <param name="location">Filter by location: "new", "later", "archive", "feed"</param>
        /// <param name="limit">Maximum number of documents to sync</param>
        Task<ReaderSyncResultDto> SyncDocumentsByLocationAsync(string location, int limit = 50);
    }
}
