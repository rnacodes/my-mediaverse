using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    public abstract class BaseMediaItem
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Primary Key
        
        [Required]
        [StringLength(500)]
        public required string Title { get; set; }
        
        [Required]
        public MediaType MediaType { get; set; }
        
        [Url]
        [StringLength(2000)]
        public string? Link { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        
        [Required]
        public Status Status { get; set; } = Status.Uncharted;
        
        public DateTime? DateCompleted { get; set; } // Nullable to allow for items that haven't been consumed yet
        
        public Rating? Rating { get; set; }
        
        public OwnershipStatus? OwnershipStatus { get; set; }
        
        public string? Description { get; set; }
        
        // Navigation properties for many-to-many relationships
        public ICollection<Topic> Topics { get; set; } = new List<Topic>();
        public ICollection<Genre> Genres { get; set; } = new List<Genre>();
        
        // Add a field for one or more strings that would allow me to paste in links to my related Obsidian notes or other documents.
        public string? RelatedNotes { get; set; } // This can store links to Obsidian notes or other documents
        
        /// <summary>
        /// Thumbnail image URL for the media item.
        /// This field stores the FULL URL to the thumbnail image, not just a path.
        /// For media imported from external APIs (e.g., TMDb for movies/TV shows, ListenNotes for podcasts),
        /// this should contain the complete, publicly accessible URL constructed from the API's image path.
        /// Example: "https://image.tmdb.org/t/p/w500/path-to-poster.jpg"
        /// </summary>
        [Url]
        [StringLength(2000)]
        public string? Thumbnail { get; set; }
        
        // Navigation property for many-to-many relationship with Mixlists
        public ICollection<Mixlist> Mixlists { get; set; } = new List<Mixlist>();

        // Navigation properties for self-referential many-to-many relationship (saved related items)
        /// <summary>
        /// Media items that this item is related TO (outgoing relationships).
        /// These are items the user has saved as related from this item's perspective.
        /// </summary>
        public ICollection<MediaItemRelation> RelatedToItems { get; set; } = new List<MediaItemRelation>();

        /// <summary>
        /// Media items that are related TO THIS item (incoming relationships).
        /// These are relationships where other items link to this one.
        /// </summary>
        public ICollection<MediaItemRelation> RelatedFromItems { get; set; } = new List<MediaItemRelation>();
    }

    public enum MediaType
        {
        Article,
        Book,
        Channel,
        Document,
        Movie,
        Music,
        Other,
        Playlist,
        Podcast,
        TVShow,
        Video,
        VideoGame,
        Website
    }

    public enum Status
    {
        Uncharted,
        ActivelyExploring,
        Completed,
        Abandoned
    }

    public enum Rating
    {
        SuperLike,
        Like,
        Neutral,
        Dislike
    }

    public enum OwnershipStatus
    {
        Own,
        Rented,
        Streamed
    }
}

