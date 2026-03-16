using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Join entity for self-referential many-to-many relationship between media items.
    /// Represents a saved "related" connection between two media items.
    /// The relationship is directional: SourceMediaItem relates to RelatedMediaItem.
    /// </summary>
    public class MediaItemRelation
    {
        public Guid SourceMediaItemId { get; set; }
        public BaseMediaItem SourceMediaItem { get; set; } = null!;

        public Guid RelatedMediaItemId { get; set; }
        public BaseMediaItem RelatedMediaItem { get; set; } = null!;

        /// <summary>
        /// When this relationship was created/saved.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// How this relationship was created.
        /// </summary>
        public RelationSource Source { get; set; } = RelationSource.ManuallyAdded;

        /// <summary>
        /// The similarity score at the time the AI recommendation was saved.
        /// Only populated for AiRecommended source.
        /// </summary>
        public double? SimilarityScore { get; set; }

        /// <summary>
        /// Optional note explaining why these items are related.
        /// </summary>
        [StringLength(500)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// Indicates how a media item relation was created.
    /// </summary>
    public enum RelationSource
    {
        /// <summary>
        /// Saved from an AI-generated recommendation.
        /// </summary>
        AiRecommended,

        /// <summary>
        /// Manually added by the user.
        /// </summary>
        ManuallyAdded
    }
}
