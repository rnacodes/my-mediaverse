using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Request body for previewing a website's scraped metadata before saving it.
    /// </summary>
    public class ScrapePreviewRequestDto
    {
        [Required]
        [Url]
        public required string Url { get; set; }
    }
}
