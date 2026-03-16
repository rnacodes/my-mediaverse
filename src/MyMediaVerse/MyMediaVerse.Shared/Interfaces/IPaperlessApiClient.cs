using MyMediaVerse.Shared.DTOs.Paperless;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Client interface for Paperless-ngx REST API integration.
    /// Paperless-ngx is a document management system that provides OCR, tagging, and full-text search.
    /// </summary>
    public interface IPaperlessApiClient
    {
        // ============================================
        // Document Operations
        // ============================================

        /// <summary>
        /// Gets a paginated list of all documents from Paperless-ngx.
        /// </summary>
        /// <param name="page">Page number (1-indexed)</param>
        /// <param name="pageSize">Number of documents per page (default 25, max 100)</param>
        /// <returns>Paginated list of documents</returns>
        Task<PaperlessDocumentListResponseDto> GetDocumentsAsync(int page = 1, int pageSize = 25);

        /// <summary>
        /// Gets a single document by its ID.
        /// </summary>
        /// <param name="id">The Paperless document ID</param>
        /// <returns>Document details or null if not found</returns>
        Task<PaperlessDocumentDto?> GetDocumentByIdAsync(int id);

        /// <summary>
        /// Searches documents using Paperless-ngx full-text search.
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Results per page</param>
        /// <returns>Search results</returns>
        Task<PaperlessDocumentListResponseDto> SearchDocumentsAsync(string query, int page = 1, int pageSize = 25);

        /// <summary>
        /// Downloads the original document file content.
        /// </summary>
        /// <param name="id">The Paperless document ID</param>
        /// <returns>File content as byte array</returns>
        Task<byte[]> GetDocumentContentAsync(int id);

        /// <summary>
        /// Gets the document thumbnail image.
        /// </summary>
        /// <param name="id">The Paperless document ID</param>
        /// <returns>Thumbnail image as byte array</returns>
        Task<byte[]> GetDocumentThumbnailAsync(int id);

        /// <summary>
        /// Constructs the URL to preview a document in Paperless-ngx.
        /// </summary>
        /// <param name="id">The Paperless document ID</param>
        /// <returns>Preview URL</returns>
        string GetDocumentPreviewUrl(int id);

        /// <summary>
        /// Constructs the URL to download a document from Paperless-ngx.
        /// </summary>
        /// <param name="id">The Paperless document ID</param>
        /// <returns>Download URL</returns>
        string GetDocumentDownloadUrl(int id);

        /// <summary>
        /// Uploads a new document to Paperless-ngx for processing.
        /// The document will be queued for OCR and classification.
        /// </summary>
        /// <param name="fileStream">The file content stream</param>
        /// <param name="fileName">Original filename</param>
        /// <param name="title">Optional title (defaults to filename)</param>
        /// <param name="correspondentId">Optional correspondent ID</param>
        /// <param name="documentTypeId">Optional document type ID</param>
        /// <param name="tagIds">Optional list of tag IDs</param>
        /// <returns>Task ID for tracking upload processing</returns>
        Task<string> UploadDocumentAsync(
            Stream fileStream,
            string fileName,
            string? title = null,
            int? correspondentId = null,
            int? documentTypeId = null,
            List<int>? tagIds = null);

        /// <summary>
        /// Updates document metadata in Paperless-ngx.
        /// </summary>
        /// <param name="id">The Paperless document ID</param>
        /// <param name="title">New title (null to keep existing)</param>
        /// <param name="correspondentId">New correspondent ID (null to keep existing)</param>
        /// <param name="documentTypeId">New document type ID (null to keep existing)</param>
        /// <param name="tagIds">New tag IDs (null to keep existing)</param>
        /// <returns>Updated document</returns>
        Task<PaperlessDocumentDto> UpdateDocumentAsync(
            int id,
            string? title = null,
            int? correspondentId = null,
            int? documentTypeId = null,
            List<int>? tagIds = null);

        /// <summary>
        /// Deletes a document from Paperless-ngx.
        /// </summary>
        /// <param name="id">The Paperless document ID</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteDocumentAsync(int id);

        // ============================================
        // Tag Operations
        // ============================================

        /// <summary>
        /// Gets all tags from Paperless-ngx.
        /// </summary>
        /// <returns>List of all tags</returns>
        Task<List<PaperlessTagDto>> GetTagsAsync();

        /// <summary>
        /// Gets a single tag by ID.
        /// </summary>
        /// <param name="id">The tag ID</param>
        /// <returns>Tag details or null if not found</returns>
        Task<PaperlessTagDto?> GetTagByIdAsync(int id);

        /// <summary>
        /// Creates a new tag in Paperless-ngx.
        /// </summary>
        /// <param name="name">Tag name</param>
        /// <param name="color">Optional hex color code</param>
        /// <returns>Created tag</returns>
        Task<PaperlessTagDto> CreateTagAsync(string name, string? color = null);

        // ============================================
        // Document Type Operations
        // ============================================

        /// <summary>
        /// Gets all document types from Paperless-ngx.
        /// </summary>
        /// <returns>List of all document types</returns>
        Task<List<PaperlessDocumentTypeDto>> GetDocumentTypesAsync();

        /// <summary>
        /// Gets a single document type by ID.
        /// </summary>
        /// <param name="id">The document type ID</param>
        /// <returns>Document type details or null if not found</returns>
        Task<PaperlessDocumentTypeDto?> GetDocumentTypeByIdAsync(int id);

        // ============================================
        // Correspondent Operations
        // ============================================

        /// <summary>
        /// Gets all correspondents from Paperless-ngx.
        /// </summary>
        /// <returns>List of all correspondents</returns>
        Task<List<PaperlessCorrespondentDto>> GetCorrespondentsAsync();

        /// <summary>
        /// Gets a single correspondent by ID.
        /// </summary>
        /// <param name="id">The correspondent ID</param>
        /// <returns>Correspondent details or null if not found</returns>
        Task<PaperlessCorrespondentDto?> GetCorrespondentByIdAsync(int id);

        // ============================================
        // Health & Utility
        // ============================================

        /// <summary>
        /// Checks if Paperless-ngx API is available and configured.
        /// </summary>
        /// <returns>True if API is reachable and authenticated</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Gets the base URL of the Paperless-ngx instance.
        /// </summary>
        string GetBaseUrl();
    }
}
