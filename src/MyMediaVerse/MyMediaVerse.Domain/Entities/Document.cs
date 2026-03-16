using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a document managed by Paperless-ngx and tracked in MyMediaVerse.
    /// Documents can include PDFs, scanned papers, receipts, contracts, etc.
    /// </summary>
    public class Document : BaseMediaItem
    {
        /// <summary>
        /// External ID from Paperless-ngx (document.id).
        /// Used for syncing documents between systems.
        /// </summary>
        public int? PaperlessId { get; set; }

        /// <summary>
        /// Original filename of the uploaded document.
        /// </summary>
        [StringLength(500)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// Archive serial number from Paperless-ngx.
        /// Unique identifier assigned by Paperless for physical document organization.
        /// </summary>
        [StringLength(100)]
        public string? ArchiveSerialNumber { get; set; }

        /// <summary>
        /// Document type category from Paperless-ngx (e.g., "Invoice", "Receipt", "Contract").
        /// </summary>
        [StringLength(200)]
        public string? DocumentType { get; set; }

        /// <summary>
        /// Correspondent (sender/source) from Paperless-ngx (e.g., "Amazon", "IRS", "Bank").
        /// </summary>
        [StringLength(200)]
        public string? Correspondent { get; set; }

        /// <summary>
        /// OCR-extracted full text content from Paperless-ngx.
        /// Stored locally for full-text search capability without API calls.
        /// </summary>
        public string? OcrContent { get; set; }

        /// <summary>
        /// Date the document was created/dated (from Paperless).
        /// This is the date on the document itself, not when it was added.
        /// </summary>
        public DateTime? DocumentDate { get; set; }

        /// <summary>
        /// Number of pages in the document.
        /// </summary>
        public int? PageCount { get; set; }

        /// <summary>
        /// File type/extension (pdf, png, jpg, etc.).
        /// </summary>
        [StringLength(20)]
        public string? FileType { get; set; }

        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long? FileSizeBytes { get; set; }

        /// <summary>
        /// Tags from Paperless-ngx stored as comma-separated values.
        /// These are stored separately from Topics/Genres for reference.
        /// </summary>
        public string? PaperlessTagsCsv { get; set; }

        /// <summary>
        /// Custom fields from Paperless-ngx stored as JSON.
        /// </summary>
        public string? CustomFieldsJson { get; set; }

        /// <summary>
        /// When the document was last synced from Paperless-ngx.
        /// </summary>
        public DateTime? LastPaperlessSync { get; set; }

        /// <summary>
        /// URL to view/download the document in Paperless-ngx.
        /// </summary>
        [Url]
        [StringLength(2000)]
        public string? PaperlessUrl { get; set; }

        /// <summary>
        /// Whether this document is archived/finalized in Paperless-ngx.
        /// </summary>
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// Gets the Paperless tags as a list.
        /// </summary>
        public List<string> GetPaperlessTags()
        {
            if (string.IsNullOrEmpty(PaperlessTagsCsv))
                return new List<string>();

            return PaperlessTagsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
        }

        /// <summary>
        /// Sets the Paperless tags from a list.
        /// </summary>
        public void SetPaperlessTags(IEnumerable<string> tags)
        {
            PaperlessTagsCsv = tags?.Any() == true
                ? string.Join(",", tags.Select(t => t.Trim()))
                : null;
        }

        /// <summary>
        /// Gets the human-readable file size.
        /// </summary>
        public string? GetFormattedFileSize()
        {
            if (!FileSizeBytes.HasValue)
                return null;

            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = FileSizeBytes.Value;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
