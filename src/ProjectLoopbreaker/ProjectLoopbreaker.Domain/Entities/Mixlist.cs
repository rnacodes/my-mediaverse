using System.ComponentModel.DataAnnotations;

namespace ProjectLoopbreaker.Domain.Entities
{
    public class Mixlist
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Changed to Guid for consistency
        
        [Required]
        [StringLength(200)]
        public required string Name { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Url]
        [StringLength(2000)]
        public string? Thumbnail { get; set; }
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        
        // Navigation property for many-to-many relationship
        public ICollection<BaseMediaItem> MediaItems { get; set; } = new List<BaseMediaItem>();

        // Navigation property for many-to-many relationship with notes
        public ICollection<MixlistNote> MixlistNotes { get; set; } = new List<MixlistNote>();
    }
}
