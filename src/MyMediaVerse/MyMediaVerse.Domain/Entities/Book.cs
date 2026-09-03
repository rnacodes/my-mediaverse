using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    public class Book : BaseMediaItem
    {
        [Required]
        [StringLength(300)]
        public required string Author { get; set; }
        
        [StringLength(17)]
        public string? ISBN { get; set; }
        
        [StringLength(20)]
        public string? ASIN { get; set; }

        /// <summary>
        /// Readwise's stable per-book id (user_book_id from the Readwise export API).
        /// Fill-only external id: written when a highlight sync creates or matches this book,
        /// never overwritten once set. Primary join key for highlight↔book linking.
        /// </summary>
        public int? ReadwiseBookId { get; set; }

        /// <summary>
        /// Goodreads "Book Id" from the library export CSV.
        /// Fill-only external id: written on Goodreads import, never overwritten once set.
        /// </summary>
        public long? GoodreadsBookId { get; set; }

        /// <summary>
        /// Google Books volume id. Fill-only external id: written on Google Books import
        /// or when enrichment resolves this book, never overwritten once set.
        /// </summary>
        [StringLength(40)]
        public string? GoogleVolumeId { get; set; }

        /// <summary>
        /// Open Library work key (e.g. "/works/OL45883W"). Fill-only external id:
        /// written on Open Library import, never overwritten once set.
        /// </summary>
        [StringLength(40)]
        public string? OpenLibraryKey { get; set; }


        [Required]
        public BookFormat Format { get; set; } = BookFormat.Digital;
        
        public bool PartOfSeries { get; set; } = false;
        
        /// <summary>
        /// User's Goodreads rating from 1-5 scale. This is separate from the PLB Rating enum.
        /// Conversion: 5 = SuperLike, 4 = Like, 3 = Neutral, 1-2 = Dislike
        /// </summary>
        [Range(1, 5)]
        public decimal? GoodreadsRating { get; set; }

        /// <summary>
        /// Goodreads community average rating (1-5 scale)
        /// </summary>
        [Range(1, 5)]
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// Year the book was published (this edition)
        /// </summary>
        public int? YearPublished { get; set; }

        /// <summary>
        /// Original publication year of the work
        /// </summary>
        public int? OriginalPublicationYear { get; set; }

        /// <summary>
        /// Date the user finished reading the book
        /// </summary>
        public DateTime? DateRead { get; set; }

        /// <summary>
        /// User's personal review of the book
        /// </summary>
        [StringLength(10000)]
        public string? MyReview { get; set; }

        /// <summary>
        /// Publisher name
        /// </summary>
        [StringLength(500)]
        public string? Publisher { get; set; }

        /// <summary>
        /// Goodreads bookshelves/tags (stored as JSON array)
        /// </summary>
        public List<string> GoodreadsTags { get; set; } = new List<string>();

        /// <summary>
        /// UTC timestamp of the last successful metadata enrichment (Google Books).
        /// Set only when enrichment actually populated a previously-empty field; enrichment
        /// is fill-gaps-only and never overwrites user-edited/populated values.
        /// </summary>
        public DateTime? EnrichedAt { get; set; }

        /// <summary>
        /// Navigation property: Highlights associated with this book
        /// </summary>
        public ICollection<Highlight> Highlights { get; set; } = new List<Highlight>();
    }
    
    public enum BookFormat
    {
        Digital,
        Physical
    }
}


