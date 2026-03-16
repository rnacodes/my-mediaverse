namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service interface for generating media recommendations using vector similarity.
    /// Uses PostgreSQL with pgvector extension for similarity searches.
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// Gets media items similar to the specified item using vector similarity.
        /// Uses the item's embedding stored in PostgreSQL.
        /// </summary>
        /// <param name="mediaItemId">The ID of the source media item</param>
        /// <param name="count">Maximum number of recommendations to return</param>
        /// <param name="mediaTypeFilter">Optional filter to limit results to specific media types</param>
        /// <returns>List of similar media items with similarity scores</returns>
        Task<List<SimilarItemResult>> GetSimilarMediaItemsAsync(
            Guid mediaItemId,
            int count = 10,
            string? mediaTypeFilter = null);

        /// <summary>
        /// Gets notes similar to the specified note using vector similarity.
        /// </summary>
        /// <param name="noteId">The ID of the source note</param>
        /// <param name="count">Maximum number of recommendations</param>
        /// <param name="vaultFilter">Optional filter to limit to specific vault</param>
        /// <returns>List of similar notes with similarity scores</returns>
        Task<List<SimilarNoteResult>> GetSimilarNotesAsync(
            Guid noteId,
            int count = 10,
            string? vaultFilter = null);

        /// <summary>
        /// Searches for media items matching a "vibe" description.
        /// Generates an embedding for the description and finds similar items.
        /// </summary>
        /// <param name="vibeDescription">Natural language description of desired content (e.g., "dark atmospheric sci-fi")</param>
        /// <param name="count">Maximum number of results</param>
        /// <param name="mediaTypeFilter">Optional media type filter</param>
        /// <returns>List of matching media items with similarity scores</returns>
        Task<List<SimilarItemResult>> SearchByVibeAsync(
            string vibeDescription,
            int count = 20,
            string? mediaTypeFilter = null);

        /// <summary>
        /// Gets personalized recommendations based on user's liked items.
        /// Aggregates embeddings from liked items to find similar content.
        /// </summary>
        /// <param name="count">Maximum number of recommendations</param>
        /// <param name="excludeExplored">Whether to exclude items already explored</param>
        /// <returns>List of recommended media items</returns>
        Task<List<SimilarItemResult>> GetPersonalizedRecommendationsAsync(
            int count = 20,
            bool excludeExplored = true);

        /// <summary>
        /// Gets media items that are related to a specific note.
        /// Useful for "find media mentioned in this note" features.
        /// </summary>
        /// <param name="noteId">The note to find related media for</param>
        /// <param name="count">Maximum number of results</param>
        /// <returns>List of related media items</returns>
        Task<List<SimilarItemResult>> GetMediaRelatedToNoteAsync(
            Guid noteId,
            int count = 10);

        /// <summary>
        /// Gets notes that are related to a specific media item.
        /// Useful for "find notes about this topic" features.
        /// </summary>
        /// <param name="mediaItemId">The media item to find related notes for</param>
        /// <param name="count">Maximum number of results</param>
        /// <returns>List of related notes</returns>
        Task<List<SimilarNoteResult>> GetNotesRelatedToMediaAsync(
            Guid mediaItemId,
            int count = 10);

        /// <summary>
        /// Checks if recommendation features are available.
        /// Requires AI service for embedding generation and pgvector extension in database.
        /// </summary>
        /// <returns>True if recommendation features are available</returns>
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Result object for similar media item queries.
    /// </summary>
    public class SimilarItemResult
    {
        /// <summary>
        /// The ID of the similar media item.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Title of the media item.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type of media (Book, Movie, etc.)
        /// </summary>
        public string MediaType { get; set; } = string.Empty;

        /// <summary>
        /// Description of the media item.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Thumbnail URL.
        /// </summary>
        public string? Thumbnail { get; set; }

        /// <summary>
        /// Similarity score (0-1, higher is more similar).
        /// Calculated as 1 - cosine_distance.
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// Current exploration status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// User rating if any.
        /// </summary>
        public string? Rating { get; set; }
    }

    /// <summary>
    /// Result object for similar note queries.
    /// </summary>
    public class SimilarNoteResult
    {
        /// <summary>
        /// The ID of the similar note.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Title of the note.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The vault this note belongs to.
        /// </summary>
        public string VaultName { get; set; } = string.Empty;

        /// <summary>
        /// Note description (AI-generated or manual).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// URL to the note on the Quartz site.
        /// </summary>
        public string? SourceUrl { get; set; }

        /// <summary>
        /// Tags associated with the note.
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Similarity score (0-1, higher is more similar).
        /// </summary>
        public double SimilarityScore { get; set; }
    }
}
