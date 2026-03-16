namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for returning Website data in API responses.
    /// </summary>
    public class WebsiteResponseDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? Link { get; set; }
        public string? Thumbnail { get; set; }
        public string? RssFeedUrl { get; set; }
        public string? Domain { get; set; }
        public string? Author { get; set; }
        public string? Publication { get; set; }
        public DateTime? LastCheckedDate { get; set; }
        public DateTime DateAdded { get; set; }
        public string Status { get; set; } = "Uncharted";
        public string? Rating { get; set; }
        public string? Notes { get; set; }
        public List<string> Topics { get; set; } = new();
        public List<string> Genres { get; set; } = new();
        public string MediaType { get; set; } = "Website";

        // Archive fields for future ArchiveBox integration
        public string? ArchiveUrl { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? ArchiveStatus { get; set; }
        public string? WaybackUrl { get; set; }
    }
}


