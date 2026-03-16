using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for returning Document data in API responses.
    /// </summary>
    public class DocumentResponseDto
    {
        // ============================================
        // Base media item properties
        // ============================================

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

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

        [JsonPropertyName("originalFileName")]
        public string? OriginalFileName { get; set; }

        [JsonPropertyName("archiveSerialNumber")]
        public string? ArchiveSerialNumber { get; set; }

        [JsonPropertyName("documentType")]
        public string? DocumentType { get; set; }

        [JsonPropertyName("correspondent")]
        public string? Correspondent { get; set; }

        [JsonPropertyName("documentDate")]
        public DateTime? DocumentDate { get; set; }

        [JsonPropertyName("pageCount")]
        public int? PageCount { get; set; }

        [JsonPropertyName("fileType")]
        public string? FileType { get; set; }

        [JsonPropertyName("fileSizeBytes")]
        public long? FileSizeBytes { get; set; }

        [JsonPropertyName("formattedFileSize")]
        public string? FormattedFileSize { get; set; }

        [JsonPropertyName("paperlessTags")]
        public string[] PaperlessTags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("paperlessUrl")]
        public string? PaperlessUrl { get; set; }

        [JsonPropertyName("lastPaperlessSync")]
        public DateTime? LastPaperlessSync { get; set; }

        [JsonPropertyName("isArchived")]
        public bool IsArchived { get; set; }
    }
}
