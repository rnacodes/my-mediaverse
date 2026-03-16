using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service interface for managing Documents in MyMediaVerse.
    /// Handles CRUD operations and Paperless-ngx synchronization.
    /// </summary>
    public interface IDocumentService
    {
        // ============================================
        // Basic CRUD operations
        // ============================================

        /// <summary>
        /// Gets all documents with their topics and genres.
        /// </summary>
        Task<IEnumerable<Document>> GetAllDocumentsAsync();

        /// <summary>
        /// Gets a single document by its MyMediaVerse ID.
        /// </summary>
        Task<Document?> GetDocumentByIdAsync(Guid id);

        /// <summary>
        /// Gets a document by its Paperless-ngx ID.
        /// </summary>
        Task<Document?> GetDocumentByPaperlessIdAsync(int paperlessId);

        /// <summary>
        /// Creates a new document.
        /// </summary>
        Task<Document> CreateDocumentAsync(CreateDocumentDto dto);

        /// <summary>
        /// Updates an existing document.
        /// </summary>
        Task<Document> UpdateDocumentAsync(Guid id, CreateDocumentDto dto);

        /// <summary>
        /// Deletes a document.
        /// </summary>
        Task<bool> DeleteDocumentAsync(Guid id);

        // ============================================
        // Query operations
        // ============================================

        /// <summary>
        /// Gets all documents of a specific type (e.g., "Invoice", "Receipt").
        /// </summary>
        Task<IEnumerable<Document>> GetDocumentsByTypeAsync(string documentType);

        /// <summary>
        /// Gets all documents from a specific correspondent.
        /// </summary>
        Task<IEnumerable<Document>> GetDocumentsByCorrespondentAsync(string correspondent);

        /// <summary>
        /// Gets all archived documents.
        /// </summary>
        Task<IEnumerable<Document>> GetArchivedDocumentsAsync();

        /// <summary>
        /// Searches documents by title, content, correspondent, or type.
        /// </summary>
        Task<IEnumerable<Document>> SearchDocumentsAsync(string query);

        /// <summary>
        /// Gets documents within a date range based on document date.
        /// </summary>
        Task<IEnumerable<Document>> GetDocumentsByDateRangeAsync(DateTime startDate, DateTime endDate);

        // ============================================
        // Paperless-ngx sync operations
        // ============================================

        /// <summary>
        /// Synchronizes all documents from Paperless-ngx.
        /// Creates new documents and updates existing ones based on PaperlessId.
        /// </summary>
        Task<DocumentSyncResultDto> SyncFromPaperlessAsync();

        /// <summary>
        /// Synchronizes a single document from Paperless-ngx by its ID.
        /// </summary>
        Task<Document?> SyncSingleDocumentAsync(int paperlessId);

        /// <summary>
        /// Checks if Paperless-ngx API is available.
        /// </summary>
        Task<bool> IsPaperlessAvailableAsync();
    }
}
