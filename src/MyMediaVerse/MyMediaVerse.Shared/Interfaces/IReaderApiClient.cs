using MyMediaVerse.Shared.DTOs.ReadwiseReader;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Interface for Readwise Reader API client
    /// API Documentation: https://readwise.io/reader_api
    /// </summary>
    public interface IReaderApiClient
    {
        /// <summary>
        /// Gets list of documents from Reader
        /// GET https://readwise.io/api/v3/list/
        /// </summary>
        /// <param name="updatedAfter">ISO 8601 timestamp</param>
        /// <param name="location">Filter by location: new, later, archive, feed</param>
        /// <param name="category">Filter by category: article, email, rss, highlight, note, pdf, epub, tweet, video</param>
        /// <param name="pageCursor">Cursor for pagination</param>
        Task<ReaderDocumentsResponse> GetDocumentsAsync(
            string? updatedAfter = null,
            string? location = null,
            string? category = null,
            string? pageCursor = null);
        
        /// <summary>
        /// Gets a specific document by ID with full HTML content
        /// GET https://readwise.io/api/v3/list/?id={id}
        /// </summary>
        Task<ReaderDocumentDto?> GetDocumentByIdAsync(string documentId, bool includeHtml = true);
        
        /// <summary>
        /// Creates a document in Reader
        /// POST https://readwise.io/api/v3/save/
        /// </summary>
        Task<ReaderDocumentDto?> CreateDocumentAsync(CreateReaderDocumentDto dto);
        
        /// <summary>
        /// Updates a document's location (archive, later, etc.)
        /// PATCH https://readwise.io/api/v3/save/
        /// </summary>
        Task<bool> UpdateDocumentLocationAsync(string documentId, string location);
    }
}

