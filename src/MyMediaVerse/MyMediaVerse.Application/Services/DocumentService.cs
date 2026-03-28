using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for managing Documents in MyMediaVerse.
    /// Handles CRUD operations and Paperless-ngx synchronization.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<DocumentService> _logger;
        private readonly IPaperlessApiClient? _paperlessClient;
        private readonly IDocumentMappingService _mappingService;

        public DocumentService(
            IApplicationDbContext context,
            ILogger<DocumentService> logger,
            IDocumentMappingService mappingService,
            IPaperlessApiClient? paperlessClient = null)
        {
            _context = context;
            _logger = logger;
            _mappingService = mappingService;
            _paperlessClient = paperlessClient;
        }

        // ============================================
        // Basic CRUD operations
        // ============================================

        public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
        {
            try
            {
                return await _context.Documents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .OrderByDescending(d => d.DateAdded)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all documents");
                throw;
            }
        }

        public async Task<Document?> GetDocumentByIdAsync(Guid id)
        {
            try
            {
                return await _context.Documents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .FirstOrDefaultAsync(d => d.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving document with ID {Id}", id);
                throw;
            }
        }

        public async Task<Document?> GetDocumentByPaperlessIdAsync(int paperlessId)
        {
            try
            {
                return await _context.Documents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .FirstOrDefaultAsync(d => d.PaperlessId == paperlessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving document with Paperless ID {PaperlessId}", paperlessId);
                throw;
            }
        }

        public async Task<Document> CreateDocumentAsync(CreateDocumentDto dto)
        {
            try
            {
                var document = new Document
                {
                    Title = dto.Title,
                    MediaType = MediaType.Document,
                    Link = dto.Link,
                    Notes = dto.Notes,
                    Status = dto.Status,
                    DateAdded = DateTime.UtcNow,
                    DateCompleted = dto.DateCompleted,
                    Rating = dto.Rating,
                    OwnershipStatus = dto.OwnershipStatus,
                    Description = dto.Description,
                    RelatedNotes = dto.RelatedNotes,
                    Thumbnail = dto.Thumbnail,
                    PaperlessId = dto.PaperlessId,
                    OriginalFileName = dto.OriginalFileName,
                    ArchiveSerialNumber = dto.ArchiveSerialNumber,
                    DocumentType = dto.DocumentType,
                    Correspondent = dto.Correspondent,
                    OcrContent = dto.OcrContent,
                    DocumentDate = dto.DocumentDate,
                    PageCount = dto.PageCount,
                    FileType = dto.FileType,
                    FileSizeBytes = dto.FileSizeBytes,
                    CustomFieldsJson = dto.CustomFieldsJson,
                    PaperlessUrl = dto.PaperlessUrl,
                    IsArchived = dto.IsArchived,
                    LastPaperlessSync = dto.PaperlessId.HasValue ? DateTime.UtcNow : null
                };

                // Set Paperless tags
                document.SetPaperlessTags(dto.PaperlessTags);

                // Handle Topics
                await HandleTopicsAsync(document, dto.Topics);

                // Handle Genres
                await HandleGenresAsync(document, dto.Genres);

                _context.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created document: {Title}", document.Title);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating document");
                throw;
            }
        }

        public async Task<Document> UpdateDocumentAsync(Guid id, CreateDocumentDto dto)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (document == null)
                {
                    throw new InvalidOperationException($"Document with ID {id} not found.");
                }

                // Update document properties
                document.Title = dto.Title;
                document.Link = dto.Link;
                document.Notes = dto.Notes;
                document.Status = dto.Status;
                document.DateCompleted = dto.DateCompleted;
                document.Rating = dto.Rating;
                document.OwnershipStatus = dto.OwnershipStatus;
                document.Description = dto.Description;
                document.RelatedNotes = dto.RelatedNotes;
                document.Thumbnail = dto.Thumbnail;
                document.PaperlessId = dto.PaperlessId;
                document.OriginalFileName = dto.OriginalFileName;
                document.ArchiveSerialNumber = dto.ArchiveSerialNumber;
                document.DocumentType = dto.DocumentType;
                document.Correspondent = dto.Correspondent;
                document.OcrContent = dto.OcrContent;
                document.DocumentDate = dto.DocumentDate;
                document.PageCount = dto.PageCount;
                document.FileType = dto.FileType;
                document.FileSizeBytes = dto.FileSizeBytes;
                document.CustomFieldsJson = dto.CustomFieldsJson;
                document.PaperlessUrl = dto.PaperlessUrl;
                document.IsArchived = dto.IsArchived;
                document.SetPaperlessTags(dto.PaperlessTags);

                // Clear existing topics and genres
                document.Topics.Clear();
                document.Genres.Clear();
                await _context.SaveChangesAsync();

                // Handle Topics
                await HandleTopicsAsync(document, dto.Topics);

                // Handle Genres
                await HandleGenresAsync(document, dto.Genres);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated document: {Title}", document.Title);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating document with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(Guid id)
        {
            try
            {
                var document = await _context.FindAsync<Document>(id);
                if (document == null)
                {
                    return false;
                }

                var documentId = document.Id;
                var documentTitle = document.Title;

                _context.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted document: {Title}", documentTitle);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting document with ID {Id}", id);
                throw;
            }
        }

        // ============================================
        // Query operations
        // ============================================

        public async Task<IEnumerable<Document>> GetDocumentsByTypeAsync(string documentType)
        {
            try
            {
                return await _context.Documents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(d => d.DocumentType != null && EF.Functions.ILike(d.DocumentType, documentType))
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .OrderByDescending(d => d.DocumentDate ?? d.DateAdded)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving documents by type: {DocumentType}", documentType);
                throw;
            }
        }

        public async Task<IEnumerable<Document>> GetDocumentsByCorrespondentAsync(string correspondent)
        {
            try
            {
                return await _context.Documents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(d => d.Correspondent != null && EF.Functions.ILike(d.Correspondent, $"%{correspondent}%"))
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .OrderByDescending(d => d.DocumentDate ?? d.DateAdded)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving documents by correspondent: {Correspondent}", correspondent);
                throw;
            }
        }

        public async Task<IEnumerable<Document>> GetArchivedDocumentsAsync()
        {
            try
            {
                return await _context.Documents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(d => d.IsArchived)
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .OrderByDescending(d => d.DateAdded)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving archived documents");
                throw;
            }
        }

        public async Task<IEnumerable<Document>> SearchDocumentsAsync(string query)
        {
            try
            {
                return await _context.Documents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(d =>
                        EF.Functions.ILike(d.Title, $"%{query}%") ||
                        (d.Correspondent != null && EF.Functions.ILike(d.Correspondent, $"%{query}%")) ||
                        (d.DocumentType != null && EF.Functions.ILike(d.DocumentType, $"%{query}%")) ||
                        (d.Description != null && EF.Functions.ILike(d.Description, $"%{query}%")) ||
                        (d.OriginalFileName != null && EF.Functions.ILike(d.OriginalFileName, $"%{query}%")))
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .OrderByDescending(d => d.DateAdded)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching documents for query: {Query}", query);
                throw;
            }
        }

        public async Task<IEnumerable<Document>> GetDocumentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.Documents
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(d => d.DocumentDate >= startDate && d.DocumentDate <= endDate)
                    .Include(d => d.Topics)
                    .Include(d => d.Genres)
                    .OrderByDescending(d => d.DocumentDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving documents by date range: {StartDate} - {EndDate}", startDate, endDate);
                throw;
            }
        }

        // ============================================
        // Paperless-ngx sync operations
        // ============================================

        public async Task<DocumentSyncResultDto> SyncFromPaperlessAsync()
        {
            var result = new DocumentSyncResultDto
            {
                SyncStartTime = DateTime.UtcNow
            };

            if (_paperlessClient == null)
            {
                result.Success = false;
                result.ErrorMessage = "Paperless API client not configured. Set PAPERLESS_API_URL and PAPERLESS_API_TOKEN environment variables.";
                _logger.LogWarning("Paperless sync failed: API client not configured");
                return result;
            }

            try
            {
                _logger.LogInformation("Starting Paperless document sync");

                // Check if Paperless is available
                if (!await _paperlessClient.IsAvailableAsync())
                {
                    result.Success = false;
                    result.ErrorMessage = "Paperless API is not available. Check the connection and credentials.";
                    return result;
                }

                // Build lookup dictionaries for tags, document types, and correspondents
                var tags = await _paperlessClient.GetTagsAsync();
                var documentTypes = await _paperlessClient.GetDocumentTypesAsync();
                var correspondents = await _paperlessClient.GetCorrespondentsAsync();

                var tagLookup = _mappingService.BuildTagLookup(tags);
                var documentTypeLookup = _mappingService.BuildDocumentTypeLookup(documentTypes);
                var correspondentLookup = _mappingService.BuildCorrespondentLookup(correspondents);

                var paperlessBaseUrl = _paperlessClient.GetBaseUrl();

                // Fetch all documents from Paperless with pagination
                var page = 1;
                var hasMore = true;

                while (hasMore)
                {
                    var paperlessDocuments = await _paperlessClient.GetDocumentsAsync(page, 100);

                    foreach (var pDoc in paperlessDocuments.Results)
                    {
                        try
                        {
                            var existingDoc = await GetDocumentByPaperlessIdAsync(pDoc.Id);

                            var dto = await _mappingService.MapFromPaperlessAsync(
                                pDoc, tagLookup, documentTypeLookup, correspondentLookup, paperlessBaseUrl);

                            if (existingDoc == null)
                            {
                                // Create new document
                                await CreateDocumentAsync(dto);
                                result.AddedCount++;
                                _logger.LogDebug("Added document from Paperless: {Title} (ID: {Id})", pDoc.Title, pDoc.Id);
                            }
                            else
                            {
                                // Update existing document
                                await UpdateDocumentAsync(existingDoc.Id, dto);
                                result.UpdatedCount++;
                                _logger.LogDebug("Updated document from Paperless: {Title} (ID: {Id})", pDoc.Title, pDoc.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            result.ErrorCount++;
                            result.Errors.Add(new DocumentSyncError
                            {
                                PaperlessId = pDoc.Id,
                                DocumentTitle = pDoc.Title,
                                ErrorMessage = ex.Message,
                                ErrorType = ex.GetType().Name
                            });
                            _logger.LogWarning(ex, "Failed to sync document from Paperless: {Title} (ID: {Id})", pDoc.Title, pDoc.Id);
                        }
                    }

                    hasMore = paperlessDocuments.Next != null;
                    page++;
                }

                result.Success = true;
                result.TotalProcessed = result.AddedCount + result.UpdatedCount + result.ErrorCount;
                result.SyncEndTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "Paperless sync completed. Added: {Added}, Updated: {Updated}, Errors: {Errors}",
                    result.AddedCount, result.UpdatedCount, result.ErrorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Paperless sync");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.SyncEndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<Document?> SyncSingleDocumentAsync(int paperlessId)
        {
            if (_paperlessClient == null)
            {
                _logger.LogWarning("Cannot sync single document: Paperless API client not configured");
                return null;
            }

            try
            {
                var pDoc = await _paperlessClient.GetDocumentByIdAsync(paperlessId);
                if (pDoc == null)
                {
                    _logger.LogWarning("Document not found in Paperless: {PaperlessId}", paperlessId);
                    return null;
                }

                // Build lookup dictionaries
                var tags = await _paperlessClient.GetTagsAsync();
                var documentTypes = await _paperlessClient.GetDocumentTypesAsync();
                var correspondents = await _paperlessClient.GetCorrespondentsAsync();

                var tagLookup = _mappingService.BuildTagLookup(tags);
                var documentTypeLookup = _mappingService.BuildDocumentTypeLookup(documentTypes);
                var correspondentLookup = _mappingService.BuildCorrespondentLookup(correspondents);

                var dto = await _mappingService.MapFromPaperlessAsync(
                    pDoc, tagLookup, documentTypeLookup, correspondentLookup, _paperlessClient.GetBaseUrl());

                var existingDoc = await GetDocumentByPaperlessIdAsync(paperlessId);

                if (existingDoc == null)
                {
                    return await CreateDocumentAsync(dto);
                }
                else
                {
                    return await UpdateDocumentAsync(existingDoc.Id, dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing single document from Paperless: {PaperlessId}", paperlessId);
                throw;
            }
        }

        public async Task<bool> IsPaperlessAvailableAsync()
        {
            if (_paperlessClient == null)
                return false;

            return await _paperlessClient.IsAvailableAsync();
        }

        // ============================================
        // Private helpers
        // ============================================

        private async Task HandleTopicsAsync(Document document, string[] topics)
        {
            if (topics?.Length > 0)
            {
                foreach (var topicName in topics.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    var normalizedTopicName = topicName.Trim().ToLowerInvariant();
                    var existingTopic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == normalizedTopicName);
                    if (existingTopic != null)
                    {
                        document.Topics.Add(existingTopic);
                    }
                    else
                    {
                        document.Topics.Add(new Topic { Name = normalizedTopicName });
                    }
                }
            }
        }

        private async Task HandleGenresAsync(Document document, string[] genres)
        {
            if (genres?.Length > 0)
            {
                foreach (var genreName in genres.Where(g => !string.IsNullOrWhiteSpace(g)))
                {
                    var normalizedGenreName = genreName.Trim().ToLowerInvariant();
                    var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == normalizedGenreName);
                    if (existingGenre != null)
                    {
                        document.Genres.Add(existingGenre);
                    }
                    else
                    {
                        document.Genres.Add(new Genre { Name = normalizedGenreName });
                    }
                }
            }
        }

    }
}
