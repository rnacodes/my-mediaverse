namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Repository interface for vector similarity searches using PostgreSQL pgvector.
    /// Provides optimized database-level similarity queries.
    /// </summary>
    public interface IVectorSearchRepository
    {
        /// <summary>
        /// Finds media items similar to the given embedding using pgvector cosine distance.
        /// </summary>
        /// <param name="embedding">The query embedding vector</param>
        /// <param name="excludeId">Optional ID to exclude from results (e.g., the source item)</param>
        /// <param name="mediaTypeFilter">Optional media type filter (e.g., "Book", "Movie")</param>
        /// <param name="limit">Maximum number of results</param>
        /// <returns>List of similar items with similarity scores</returns>
        Task<List<VectorSearchResult>> FindSimilarMediaItemsAsync(
            float[] embedding,
            Guid? excludeId = null,
            string? mediaTypeFilter = null,
            int limit = 10);

        /// <summary>
        /// Finds notes similar to the given embedding using pgvector cosine distance.
        /// </summary>
        /// <param name="embedding">The query embedding vector</param>
        /// <param name="excludeId">Optional ID to exclude from results</param>
        /// <param name="vaultFilter">Optional vault name filter</param>
        /// <param name="limit">Maximum number of results</param>
        /// <returns>List of similar notes with similarity scores</returns>
        Task<List<VectorSearchNoteResult>> FindSimilarNotesAsync(
            float[] embedding,
            Guid? excludeId = null,
            string? vaultFilter = null,
            int limit = 10);

        /// <summary>
        /// Gets the embedding for a media item.
        /// </summary>
        Task<float[]?> GetMediaItemEmbeddingAsync(Guid id);

        /// <summary>
        /// Gets the embedding for a note.
        /// </summary>
        Task<float[]?> GetNoteEmbeddingAsync(Guid id);

        /// <summary>
        /// Checks if pgvector extension is available in the database.
        /// </summary>
        Task<bool> IsPgVectorAvailableAsync();

        /// <summary>
        /// Checks if any media items have embeddings stored.
        /// </summary>
        Task<bool> HasAnyMediaEmbeddingsAsync();
    }

    /// <summary>
    /// Result from a vector similarity search for media items.
    /// </summary>
    public class VectorSearchResult
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Thumbnail { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Rating { get; set; }

        /// <summary>
        /// Cosine similarity score (0 to 1, where 1 is identical).
        /// Calculated as: 1 - cosine_distance
        /// </summary>
        public double SimilarityScore { get; set; }
    }

    /// <summary>
    /// Result from a vector similarity search for notes.
    /// </summary>
    public class VectorSearchNoteResult
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VaultName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SourceUrl { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Cosine similarity score (0 to 1, where 1 is identical).
        /// </summary>
        public double SimilarityScore { get; set; }
    }
}
