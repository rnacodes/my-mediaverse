using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Join entity for many-to-many relationship between mixlists and notes.
    /// Allows tracking additional metadata about the relationship.
    /// </summary>
    public class MixlistNote
    {
        public Guid MixlistId { get; set; }
        public Mixlist Mixlist { get; set; } = null!;

        public Guid NoteId { get; set; }
        public Note Note { get; set; } = null!;

        /// <summary>
        /// When this link was created.
        /// </summary>
        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional description explaining why this note is linked to the mixlist.
        /// </summary>
        [StringLength(500)]
        public string? LinkDescription { get; set; }
    }
}
