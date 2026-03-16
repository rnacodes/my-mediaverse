using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a website that has been saved to the media library.
    /// Websites can be scraped for metadata and may include RSS feed tracking.
    /// </summary>
    public class Website : BaseMediaItem
    {
        /// <summary>
        /// URL of the RSS feed associated with this website, if discovered during scraping.
        /// </summary>
        [Url]
        [StringLength(2000)]
        public string? RssFeedUrl { get; set; }
        
        /// <summary>
        /// The last time the website was checked or scraped for updates.
        /// Useful for tracking freshness of metadata.
        /// </summary>
        public DateTime? LastCheckedDate { get; set; }
        
        /// <summary>
        /// The domain name extracted from the URL (e.g., "theverge.com").
        /// Useful for grouping and filtering websites by source.
        /// </summary>
        [StringLength(200)]
        public string? Domain { get; set; }
        
        /// <summary>
        /// The author or creator of the website content, if available from metadata.
        /// </summary>
        [StringLength(200)]
        public string? Author { get; set; }
        
        /// <summary>
        /// The publication or website name (e.g., "The Verge", "TechCrunch").
        /// Extracted from metadata during scraping.
        /// </summary>
        [StringLength(200)]
        public string? Publication { get; set; }

        /// <summary>
        /// URL to the archived snapshot of this website (e.g., from ArchiveBox).
        /// Used for permanent storage and offline access.
        /// </summary>
        [Url]
        [StringLength(2000)]
        public string? ArchiveUrl { get; set; }

        /// <summary>
        /// When the website was archived.
        /// </summary>
        public DateTime? ArchivedAt { get; set; }

        /// <summary>
        /// Status of the archival process: "pending", "archived", "failed".
        /// </summary>
        [StringLength(50)]
        public string? ArchiveStatus { get; set; }

        /// <summary>
        /// URL to the Internet Archive Wayback Machine snapshot, if available.
        /// Provides an additional layer of archival redundancy.
        /// </summary>
        [Url]
        [StringLength(2000)]
        public string? WaybackUrl { get; set; }
    }
}


