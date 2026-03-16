namespace MyMediaVerse.Shared.DTOs.Readwise
{
    /// <summary>
    /// Response from Readwise books endpoint with pagination
    /// </summary>
    public class ReadwiseBooksResponse
    {
        public int count { get; set; }
        public string? next { get; set; }
        public string? previous { get; set; }
        public List<ReadwiseBookDto> results { get; set; } = new();
    }
}

