namespace MyMediaVerse.Shared.DTOs.Readwise
{
    /// <summary>
    /// Response from Readwise export endpoint (/api/v2/export/)
    /// This endpoint returns books with nested highlights, which is more efficient
    /// than fetching highlights and books separately.
    /// API Documentation: https://readwise.io/api_deets
    /// </summary>
    public class ReadwiseExportResponse
    {
        public int count { get; set; }
        public string? nextPageCursor { get; set; }
        public List<ReadwiseExportBookDto> results { get; set; } = new();
    }

    /// <summary>
    /// Book/source with nested highlights from the export endpoint
    /// </summary>
    public class ReadwiseExportBookDto
    {
        public int user_book_id { get; set; }
        public string title { get; set; } = string.Empty;
        public string? author { get; set; }
        public string? readable_title { get; set; }
        public string? source { get; set; }
        public string? cover_image_url { get; set; }
        public string? unique_url { get; set; }
        public string? source_url { get; set; }
        public string? asin { get; set; }
        public string? category { get; set; }
        public string? document_note { get; set; }
        public string? summary { get; set; }
        public string? readwise_url { get; set; }
        /// <summary>True when the whole source was deleted in Readwise (tombstone).</summary>
        public bool is_deleted { get; set; }
        public List<ReadwiseExportHighlightDto> highlights { get; set; } = new();
    }

    /// <summary>
    /// Highlight from the export endpoint
    /// </summary>
    public class ReadwiseExportHighlightDto
    {
        public int id { get; set; }
        public string text { get; set; } = string.Empty;
        public string? note { get; set; }
        public int? location { get; set; }
        public string? location_type { get; set; }
        public string? highlighted_at { get; set; }
        public string? url { get; set; }
        public string? color { get; set; }
        public string? updated { get; set; }
        public List<ReadwiseExportTagDto>? tags { get; set; }
        public bool is_favorite { get; set; }
        public bool is_discard { get; set; }
        /// <summary>True when the highlight was deleted in Readwise (tombstone).</summary>
        public bool is_deleted { get; set; }
    }

    /// <summary>
    /// Tag from export endpoint
    /// </summary>
    public class ReadwiseExportTagDto
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
    }
}
