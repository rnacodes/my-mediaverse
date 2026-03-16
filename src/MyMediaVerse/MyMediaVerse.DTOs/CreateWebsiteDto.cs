using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for creating a new Website media item.
    /// Can be used for manual creation or populated from scraped data.
    /// </summary>
    public class CreateWebsiteDto
    {
        /// <summary>
        /// The URL of the website (required).
        /// </summary>
        [Required]
        [Url]
        public required string Url { get; set; }
        
        /// <summary>
        /// The title of the website (required).
        /// </summary>
        [Required]
        [StringLength(500)]
        public required string Title { get; set; }
        
        /// <summary>
        /// Description of the website content.
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// URL of the thumbnail/preview image.
        /// </summary>
        [Url]
        public string? Thumbnail { get; set; }
        
        /// <summary>
        /// RSS feed URL, if available.
        /// </summary>
        [Url]
        public string? RssFeedUrl { get; set; }
        
        /// <summary>
        /// Personal notes about this website.
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Topic names associated with this website.
        /// </summary>
        public List<string>? Topics { get; set; }
        
        /// <summary>
        /// Genre names associated with this website.
        /// </summary>
        public List<string>? Genres { get; set; }
        
        /// <summary>
        /// Author of the website content.
        /// </summary>
        public string? Author { get; set; }
        
        /// <summary>
        /// Publication/site name.
        /// </summary>
        public string? Publication { get; set; }
    }
}


