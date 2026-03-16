using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.Paperless
{
    /// <summary>
    /// Represents a document from Paperless-ngx API
    /// </summary>
    public class PaperlessDocumentDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("correspondent")]
        public int? Correspondent { get; set; }

        [JsonPropertyName("document_type")]
        public int? DocumentType { get; set; }

        [JsonPropertyName("storage_path")]
        public int? StoragePath { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("modified")]
        public DateTime Modified { get; set; }

        [JsonPropertyName("added")]
        public DateTime Added { get; set; }

        [JsonPropertyName("archive_serial_number")]
        public string? ArchiveSerialNumber { get; set; }

        [JsonPropertyName("original_file_name")]
        public string? OriginalFileName { get; set; }

        [JsonPropertyName("archived_file_name")]
        public string? ArchivedFileName { get; set; }

        [JsonPropertyName("tags")]
        public List<int> Tags { get; set; } = new();

        [JsonPropertyName("custom_fields")]
        public List<PaperlessCustomFieldValueDto> CustomFields { get; set; } = new();

        [JsonPropertyName("owner")]
        public int? Owner { get; set; }

        [JsonPropertyName("notes")]
        public List<PaperlessNoteDto> Notes { get; set; } = new();

        [JsonPropertyName("page_count")]
        public int? PageCount { get; set; }
    }

    /// <summary>
    /// Paginated list response from Paperless-ngx
    /// </summary>
    public class PaperlessDocumentListResponseDto
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("all")]
        public List<int>? All { get; set; }

        [JsonPropertyName("results")]
        public List<PaperlessDocumentDto> Results { get; set; } = new();
    }

    /// <summary>
    /// Custom field value from Paperless-ngx
    /// </summary>
    public class PaperlessCustomFieldValueDto
    {
        [JsonPropertyName("field")]
        public int Field { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    /// <summary>
    /// Note attached to a document in Paperless-ngx
    /// </summary>
    public class PaperlessNoteDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
    }
}
