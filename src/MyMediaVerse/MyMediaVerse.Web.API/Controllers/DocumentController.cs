using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Controller for managing Documents.
    /// Provides CRUD operations and Paperless-ngx synchronization.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentMappingService _mappingService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentService documentService,
            IDocumentMappingService mappingService,
            ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _mappingService = mappingService;
            _logger = logger;
        }

        // ============================================
        // GET Endpoints
        // ============================================

        /// <summary>
        /// Gets all documents.
        /// </summary>
        /// <returns>List of all documents</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentResponseDto>>> GetAllDocuments()
        {
            try
            {
                var documents = await _documentService.GetAllDocumentsAsync();
                var response = _mappingService.MapToResponseDtos(documents);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all documents");
                return StatusCode(500, new { error = "Failed to retrieve documents", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets a single document by ID.
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>Document details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentResponseDto>> GetDocument(Guid id)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id);
                if (document == null)
                    return NotFound($"Document with ID {id} not found.");

                var response = _mappingService.MapToResponseDto(document);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve document", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets documents by document type.
        /// </summary>
        /// <param name="documentType">Document type (e.g., "Invoice", "Receipt")</param>
        /// <returns>List of matching documents</returns>
        [HttpGet("by-type/{documentType}")]
        public async Task<ActionResult<IEnumerable<DocumentResponseDto>>> GetDocumentsByType(string documentType)
        {
            try
            {
                var documents = await _documentService.GetDocumentsByTypeAsync(documentType);
                var response = _mappingService.MapToResponseDtos(documents);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents by type: {DocumentType}", documentType);
                return StatusCode(500, new { error = "Failed to retrieve documents", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets documents by correspondent.
        /// </summary>
        /// <param name="correspondent">Correspondent name</param>
        /// <returns>List of matching documents</returns>
        [HttpGet("by-correspondent/{correspondent}")]
        public async Task<ActionResult<IEnumerable<DocumentResponseDto>>> GetDocumentsByCorrespondent(string correspondent)
        {
            try
            {
                var documents = await _documentService.GetDocumentsByCorrespondentAsync(correspondent);
                var response = _mappingService.MapToResponseDtos(documents);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents by correspondent: {Correspondent}", correspondent);
                return StatusCode(500, new { error = "Failed to retrieve documents", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets archived documents.
        /// </summary>
        /// <returns>List of archived documents</returns>
        [HttpGet("archived")]
        public async Task<ActionResult<IEnumerable<DocumentResponseDto>>> GetArchivedDocuments()
        {
            try
            {
                var documents = await _documentService.GetArchivedDocumentsAsync();
                var response = _mappingService.MapToResponseDtos(documents);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving archived documents");
                return StatusCode(500, new { error = "Failed to retrieve documents", details = ex.Message });
            }
        }

        /// <summary>
        /// Searches documents by title, correspondent, type, or description.
        /// </summary>
        /// <param name="query">Search query</param>
        /// <returns>List of matching documents</returns>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<DocumentResponseDto>>> SearchDocuments([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query is required");

                var documents = await _documentService.SearchDocumentsAsync(query);
                var response = _mappingService.MapToResponseDtos(documents);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with query: {Query}", query);
                return StatusCode(500, new { error = "Failed to search documents", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets documents within a date range.
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of documents in date range</returns>
        [HttpGet("by-date-range")]
        public async Task<ActionResult<IEnumerable<DocumentResponseDto>>> GetDocumentsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var documents = await _documentService.GetDocumentsByDateRangeAsync(startDate, endDate);
                var response = _mappingService.MapToResponseDtos(documents);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents by date range: {StartDate} - {EndDate}", startDate, endDate);
                return StatusCode(500, new { error = "Failed to retrieve documents", details = ex.Message });
            }
        }

        // ============================================
        // POST Endpoints
        // ============================================

        /// <summary>
        /// Creates a new document.
        /// </summary>
        /// <param name="dto">Document data</param>
        /// <returns>Created document</returns>
        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Document data is required");

                var document = await _documentService.CreateDocumentAsync(dto);
                var response = _mappingService.MapToResponseDto(document);

                return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                return StatusCode(500, new { error = "Failed to create document", details = ex.Message });
            }
        }

        /// <summary>
        /// Synchronizes all documents from Paperless-ngx.
        /// Creates new documents and updates existing ones.
        /// </summary>
        /// <returns>Sync result with counts</returns>
        [HttpPost("sync-paperless")]
        public async Task<ActionResult<DocumentSyncResultDto>> SyncFromPaperless()
        {
            try
            {
                _logger.LogInformation("Starting Paperless document sync via API");
                var result = await _documentService.SyncFromPaperlessAsync();

                if (!result.Success)
                {
                    return StatusCode(503, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing from Paperless");
                return StatusCode(500, new { error = "Failed to sync from Paperless", details = ex.Message });
            }
        }

        /// <summary>
        /// Synchronizes a single document from Paperless-ngx by its Paperless ID.
        /// </summary>
        /// <param name="paperlessId">Paperless document ID</param>
        /// <returns>Synced document</returns>
        [HttpPost("sync-paperless/{paperlessId}")]
        public async Task<ActionResult<DocumentResponseDto>> SyncSingleDocument(int paperlessId)
        {
            try
            {
                _logger.LogInformation("Syncing single document from Paperless: {PaperlessId}", paperlessId);
                var document = await _documentService.SyncSingleDocumentAsync(paperlessId);

                if (document == null)
                    return NotFound($"Document with Paperless ID {paperlessId} not found.");

                var response = _mappingService.MapToResponseDto(document);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing single document from Paperless: {PaperlessId}", paperlessId);
                return StatusCode(500, new { error = "Failed to sync document", details = ex.Message });
            }
        }

        // ============================================
        // PUT Endpoints
        // ============================================

        /// <summary>
        /// Updates an existing document.
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <param name="dto">Updated document data</param>
        /// <returns>Updated document</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] CreateDocumentDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Document data is required");

                var document = await _documentService.UpdateDocumentAsync(id, dto);
                var response = _mappingService.MapToResponseDto(document);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update document", details = ex.Message });
            }
        }

        // ============================================
        // DELETE Endpoints
        // ============================================

        /// <summary>
        /// Deletes a document.
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            try
            {
                var deleted = await _documentService.DeleteDocumentAsync(id);
                if (!deleted)
                    return NotFound($"Document with ID {id} not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete document", details = ex.Message });
            }
        }

        // ============================================
        // Health/Status Endpoints
        // ============================================

        /// <summary>
        /// Checks if Paperless-ngx API is available and configured.
        /// </summary>
        /// <returns>Availability status</returns>
        [HttpGet("paperless-status")]
        public async Task<ActionResult<object>> GetPaperlessStatus()
        {
            try
            {
                var isAvailable = await _documentService.IsPaperlessAvailableAsync();
                return Ok(new
                {
                    configured = true,
                    available = isAvailable,
                    message = isAvailable
                        ? "Paperless-ngx API is available and ready"
                        : "Paperless-ngx API is not available. Check the connection and credentials."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Paperless status");
                return Ok(new
                {
                    configured = false,
                    available = false,
                    message = "Paperless-ngx API client not configured. Set PAPERLESS_API_URL and PAPERLESS_API_TOKEN."
                });
            }
        }
    }
}
