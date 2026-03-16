using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a highlight/annotation from Readwise.
    /// Can be linked to Articles, Books, or other media types.
    /// </summary>
    public class Highlight
    {
        /// <summary>
        /// Primary key for the highlight in PLB database
        /// </summary>
        [Key]
        public Guid Id { get; set; }
        
        /// <summary>
        /// External ID from Readwise API (highlight.id)
        /// Used for deduplication and sync. Null for manually-created highlights.
        /// </summary>
        public int? ReadwiseId { get; set; }
        
        /// <summary>
        /// The actual highlight text (required)
        /// Maximum 8191 characters per Readwise API spec
        /// </summary>
        [Required]
        [StringLength(8191)]
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional annotation/note attached to the highlight
        /// Can include inline tags (e.g., .philosophy)
        /// </summary>
        [StringLength(8191)]
        public string? Note { get; set; }
        
        /// <summary>
        /// Title of the source (book, article, podcast)
        /// </summary>
        [StringLength(511)]
        public string? Title { get; set; }
        
        /// <summary>
        /// Author of the source
        /// </summary>
        [StringLength(1024)]
        public string? Author { get; set; }
        
        /// <summary>
        /// Category from Readwise (books, articles, tweets, podcasts)
        /// Stored in lowercase per project standards
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; }
        
        /// <summary>
        /// Source URL (for articles, tweets, etc.)
        /// Used for linking to Article entities
        /// </summary>
        [StringLength(2047)]
        public string? SourceUrl { get; set; }
        
        /// <summary>
        /// Cover image URL from Readwise
        /// </summary>
        [StringLength(2047)]
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// Unique URL for this specific highlight (if available)
        /// </summary>
        [StringLength(4095)]
        public string? HighlightUrl { get; set; }
        
        /// <summary>
        /// Location in the source (page number, timestamp, etc.)
        /// </summary>
        public int? Location { get; set; }
        
        /// <summary>
        /// Type of location: page, location, order, offset, time_offset
        /// </summary>
        [StringLength(50)]
        public string? LocationType { get; set; }
        
        /// <summary>
        /// When the highlight was created in the original source
        /// </summary>
        public DateTime? HighlightedAt { get; set; }
        
        /// <summary>
        /// When this highlight was last updated from Readwise
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Comma-separated tags (normalized to lowercase)
        /// Example: "philosophy,stoicism,marcus-aurelius"
        /// </summary>
        [StringLength(1000)]
        public string? Tags { get; set; }
        
        /// <summary>
        /// Foreign key to Article if this highlight is from an article
        /// </summary>
        public Guid? ArticleId { get; set; }
        
        /// <summary>
        /// Navigation property to Article
        /// </summary>
        public Article? Article { get; set; }
        
        /// <summary>
        /// Foreign key to Book if this highlight is from a book
        /// </summary>
        public Guid? BookId { get; set; }
        
        /// <summary>
        /// Navigation property to Book
        /// </summary>
        public Book? Book { get; set; }
        
        /// <summary>
        /// Book ID from Readwise API (for matching)
        /// </summary>
        public int? ReadwiseBookId { get; set; }
        
        /// <summary>
        /// When the record was created in PLB
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Readwise source_type field (e.g., "instapaper", "kindle", "reader")
        /// </summary>
        [StringLength(64)]
        public string? SourceType { get; set; }
        
        /// <summary>
        /// Indicates if the highlight is a favorite in Readwise
        /// </summary>
        public bool IsFavorite { get; set; } = false;
        
        /// <summary>
        /// Color/style of the highlight (if available)
        /// </summary>
        [StringLength(50)]
        public string? Color { get; set; }
        
        /// <summary>
        /// Raw metadata from external API (Readwise, Instapaper, etc.)
        /// Stored as JSONB for flexibility and future-proofing.
        /// Can include source-specific fields that don't fit into standard columns.
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }
    }
}


