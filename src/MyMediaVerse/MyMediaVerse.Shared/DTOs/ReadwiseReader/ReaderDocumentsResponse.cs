namespace MyMediaVerse.Shared.DTOs.ReadwiseReader
{
    /// <summary>
    /// Response from Readwise Reader list endpoint
    /// </summary>
    public class ReaderDocumentsResponse
    {
        public int count { get; set; }
        public string? nextPageCursor { get; set; }
        public List<ReaderDocumentDto> results { get; set; } = new();
    }
}

