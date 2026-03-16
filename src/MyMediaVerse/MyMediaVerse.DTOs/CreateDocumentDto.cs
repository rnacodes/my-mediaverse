using MyMediaVerse.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for creating or updating a Document in MyMediaVerse.
    /// </summary>
    public class CreateDocumentDto
    {
        // ============================================
        // Base media item properties
        // ============================================

        [Required]
        [StringLength(500)]
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; } = MediaType.Document;

        [Url]
        [StringLength(2000)]
        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [Required]
        [JsonPropertyName("status")]
        public Status Status { get; set; } = Status.Uncharted;

        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }

        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }

        [JsonPropertyName("ownershipStatus")]
        public OwnershipStatus? OwnershipStatus { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("relatedNotes")]
        public string? RelatedNotes { get; set; }

        [Url]
        [StringLength(2000)]
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();

        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();

        // ============================================
        // Document-specific properties
        // ============================================

        [JsonPropertyName("paperlessId")]
        public int? PaperlessId { get; set; }

        [StringLength(500)]
        [JsonPropertyName("originalFileName")]
        public string? OriginalFileName { get; set; }

        [StringLength(100)]
        [JsonPropertyName("archiveSerialNumber")]
        public string? ArchiveSerialNumber { get; set; }

        [StringLength(200)]
        [JsonPropertyName("documentType")]
        public string? DocumentType { get; set; }

        [StringLength(200)]
        [JsonPropertyName("correspondent")]
        public string? Correspondent { get; set; }

        [JsonPropertyName("ocrContent")]
        public string? OcrContent { get; set; }

        [JsonPropertyName("documentDate")]
        public DateTime? DocumentDate { get; set; }

        [JsonPropertyName("pageCount")]
        public int? PageCount { get; set; }

        [StringLength(20)]
        [JsonPropertyName("fileType")]
        public string? FileType { get; set; }

        [JsonPropertyName("fileSizeBytes")]
        public long? FileSizeBytes { get; set; }

        [JsonPropertyName("paperlessTags")]
        public string[] PaperlessTags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("customFieldsJson")]
        public string? CustomFieldsJson { get; set; }

        [Url]
        [StringLength(2000)]
        [JsonPropertyName("paperlessUrl")]
        public string? PaperlessUrl { get; set; }

        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; } = false;
    }
}
