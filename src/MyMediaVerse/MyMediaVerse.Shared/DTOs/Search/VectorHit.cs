namespace MyMediaVerse.Shared.DTOs.Search
{
    /// <summary>
    /// A media item returned from a Typesense vector (semantic) query.
    /// SimilarityScore is derived from the hit's vector distance (1 - distance).
    /// </summary>
    public class MediaVectorHit
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Thumbnail { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Rating { get; set; }

        /// <summary>
        /// Similarity score in the range 0-1 (higher is more similar), computed as 1 - vector_distance.
        /// </summary>
        public double SimilarityScore { get; set; }
    }

    /// <summary>
    /// A note returned from a Typesense vector (semantic) query.
    /// SimilarityScore is derived from the hit's vector distance (1 - distance).
    /// </summary>
    public class NoteVectorHit
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VaultName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SourceUrl { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Similarity score in the range 0-1 (higher is more similar), computed as 1 - vector_distance.
        /// </summary>
        public double SimilarityScore { get; set; }
    }
}
