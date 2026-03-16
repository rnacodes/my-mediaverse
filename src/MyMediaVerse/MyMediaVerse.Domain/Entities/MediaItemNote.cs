using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Join entity for many-to-many relationship between media items and notes.
    /// Allows tracking additional metadata about the relationship.
    /// </summary>
    public class MediaItemNote
    {
        public Guid MediaItemId { get; set; }
        public BaseMediaItem MediaItem { get; set; } = null!;

        public Guid NoteId { get; set; }
        public Note Note { get; set; } = null!;

        /// <summary>
        /// When this link was created.
        /// </summary>
        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional description explaining why this note is linked to the media item.
        /// </summary>
        [StringLength(500)]
        public string? LinkDescription { get; set; }
    }
}
