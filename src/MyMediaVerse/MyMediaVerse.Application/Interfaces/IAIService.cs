using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service for AI-powered operations including note description generation
    /// and embedding generation for semantic search and recommendations.
    /// </summary>
    public interface IAIService
    {
        #region Note Description Generation

        /// <summary>
        /// Generates an AI description for a specific note.
        /// </summary>
        /// <param name="noteId">The ID of the note to generate a description for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The generated description, or null if generation failed</returns>
        Task<string?> GenerateNoteDescriptionAsync(Guid noteId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates AI descriptions for a batch of notes that don't have descriptions.
        /// Only processes notes where IsDescriptionManual is false.
        /// </summary>
        /// <param name="batchSize">Maximum number of notes to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result containing counts of processed, successful, and failed notes</returns>
        Task<AIBatchResultDto> GenerateNoteDescriptionsBatchAsync(int batchSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of notes that need AI description generation.
        /// </summary>
        /// <returns>Count of notes with no AiDescription and IsDescriptionManual=false</returns>
        Task<int> GetNotesNeedingDescriptionCountAsync();

        #endregion

        #region Embedding Generation

        /// <summary>
        /// Generates an embedding for a specific media item.
        /// </summary>
        /// <param name="mediaItemId">The ID of the media item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if embedding was generated successfully</returns>
        Task<bool> GenerateMediaItemEmbeddingAsync(Guid mediaItemId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates an embedding for a specific note.
        /// </summary>
        /// <param name="noteId">The ID of the note</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if embedding was generated successfully</returns>
        Task<bool> GenerateNoteEmbeddingAsync(Guid noteId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates embeddings for a batch of media items that don't have embeddings.
        /// </summary>
        /// <param name="batchSize">Maximum number of items to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result containing counts of processed, successful, and failed items</returns>
        Task<AIBatchResultDto> GenerateMediaItemEmbeddingsBatchAsync(int batchSize = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates embeddings for a batch of notes that don't have embeddings.
        /// </summary>
        /// <param name="batchSize">Maximum number of notes to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result containing counts of processed, successful, and failed notes</returns>
        Task<AIBatchResultDto> GenerateNoteEmbeddingsBatchAsync(int batchSize = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of media items that need embedding generation.
        /// </summary>
        /// <returns>Count of media items with no embedding</returns>
        Task<int> GetMediaItemsNeedingEmbeddingCountAsync();

        /// <summary>
        /// Gets the count of notes that need embedding generation.
        /// </summary>
        /// <returns>Count of notes with no embedding</returns>
        Task<int> GetNotesNeedingEmbeddingCountAsync();

        #endregion

        #region Status

        /// <summary>
        /// Gets the current status of AI services.
        /// </summary>
        /// <returns>Status information including availability and pending items</returns>
        Task<AIStatusDto> GetStatusAsync();

        /// <summary>
        /// Checks if the AI service is available and properly configured.
        /// </summary>
        /// <returns>True if the service is available</returns>
        Task<bool> IsAvailableAsync();

        #endregion
    }
}
