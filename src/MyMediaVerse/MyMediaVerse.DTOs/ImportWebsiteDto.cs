using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for importing a website by URL.
    /// The backend will scrape metadata automatically.
    /// </summary>
    public class ImportWebsiteDto
    {
        /// <summary>
        /// The URL to import and scrape.
        /// </summary>
        [Required]
        [Url]
        public required string Url { get; set; }
        
        /// <summary>
        /// Optional: Override the scraped title.
        /// </summary>
        public string? TitleOverride { get; set; }
        
        /// <summary>
        /// Optional: Additional notes to attach to the website.
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Optional: Topics to associate with this website.
        /// </summary>
        public List<string>? Topics { get; set; }
        
        /// <summary>
        /// Optional: Genres to associate with this website.
        /// </summary>
        public List<string>? Genres { get; set; }
    }
}


