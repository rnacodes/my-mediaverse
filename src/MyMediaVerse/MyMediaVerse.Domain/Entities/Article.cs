using System.ComponentModel.DataAnnotations;
using MyMediaVerse.Domain.Enums;

namespace MyMediaVerse.Domain.Entities
{
    public class Article : BaseMediaItem
    {
        /// <summary>
        /// Full HTML content of the article stored directly in PostgreSQL.
        /// Fetched from Readwise Reader API.
        /// </summary>
        public string? FullTextContent { get; set; }

        /// <summary>
        /// Legacy S3 path/key - kept for backwards compatibility but no longer used.
        /// New articles store content in FullTextContent column instead.
        /// </summary>
        [StringLength(500)]
        public string? ContentStoragePath { get; set; }

        /// <summary>
        /// Indicates if the article is archived in Readwise Reader.
        /// Synced from Reader API during ongoing synchronization.
        /// </summary>
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// Indicates if the article is starred/favorited in Readwise Reader.
        /// Synced from Reader API during ongoing synchronization.
        /// </summary>
        public bool IsStarred { get; set; } = false;

        /// <summary>
        /// The last time this article was synced with Readwise.
        /// Used to track sync freshness and detect stale data.
        /// </summary>
        public DateTime? LastSyncDate { get; set; }

        /// <summary>
        /// The author of the article (if available).
        /// </summary>
        [StringLength(200)]
        public string? Author { get; set; }

        /// <summary>
        /// The publication/source name (e.g., "New York Times", "TechCrunch").
        /// </summary>
        [StringLength(200)]
        public string? Publication { get; set; }

        /// <summary>
        /// The publication date of the article.
        /// </summary>
        public DateTime? PublicationDate { get; set; }

        /// <summary>
        /// Reading progress percentage (0-100).
        /// Can be used to track how far through the article the user has read.
        /// </summary>
        [Range(0, 100)]
        public int? ReadingProgress { get; set; }

        /// <summary>
        /// Word count of the article (if available).
        /// Useful for estimating reading time.
        /// </summary>
        public int? WordCount { get; set; }

        /// <summary>
        /// External ID from Readwise Reader API (document.id)
        /// Used for syncing Reader documents
        /// </summary>
        [StringLength(100)]
        public string? ReadwiseDocumentId { get; set; }

        /// <summary>
        /// Readwise Reader location: new, later, archive, feed
        /// </summary>
        [StringLength(50)]
        public string? ReaderLocation { get; set; }

        /// <summary>
        /// Last synced from Readwise Reader
        /// </summary>
        public DateTime? LastReaderSync { get; set; }

        /// <summary>
        /// Tracks which external services have synced this article.
        /// Uses Flags to allow combining multiple sync states.
        /// </summary>
        public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;

        /// <summary>
        /// Navigation property: Highlights associated with this article
        /// </summary>
        public ICollection<Highlight> Highlights { get; set; } = new List<Highlight>();

        /// <summary>
        /// Gets the full URL to the stored content in DigitalOcean Spaces.
        /// </summary>
        public string? GetContentUrl(string bucketName, string endpoint)
        {
            if (string.IsNullOrEmpty(ContentStoragePath))
                return null;

            return $"https://{bucketName}.{endpoint}/{ContentStoragePath}";
        }

        /// <summary>
        /// Gets the estimated reading time in minutes based on word count.
        /// Assumes average reading speed of 200 words per minute.
        /// </summary>
        public int? GetEstimatedReadingTime()
        {
            if (!WordCount.HasValue || WordCount <= 0)
                return null;

            return (int)Math.Ceiling(WordCount.Value / 200.0);
        }
    }
}
