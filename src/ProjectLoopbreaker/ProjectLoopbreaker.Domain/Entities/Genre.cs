using System.ComponentModel.DataAnnotations;

namespace ProjectLoopbreaker.Domain.Entities
{
    public class Genre
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }
        
        // Navigation property for many-to-many relationship with media items
        public ICollection<BaseMediaItem> MediaItems { get; set; } = new List<BaseMediaItem>();

        // Navigation property for many-to-many relationship with mixlists
        public ICollection<Mixlist> Mixlists { get; set; } = new List<Mixlist>();
    }
}
