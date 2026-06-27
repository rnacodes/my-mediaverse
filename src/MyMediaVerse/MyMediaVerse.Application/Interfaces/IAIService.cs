using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service for AI-powered operations including note description generation.
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
