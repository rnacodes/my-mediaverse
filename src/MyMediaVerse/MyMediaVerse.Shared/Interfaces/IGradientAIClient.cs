namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Client for AI operations using multiple providers:
    /// - OpenAI for embeddings (text-embedding-3-large with configurable dimensions)
    /// - DigitalOcean Gradient AI for text generation (chat completions)
    /// </summary>
    public interface IGradientAIClient
    {
        /// <summary>
        /// Generates a vector embedding for the given text using OpenAI.
        /// Uses text-embedding-3-large with configurable dimensions (default: 1024).
        /// </summary>
        /// <param name="text">The text to generate an embedding for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A float array representing the embedding vector</returns>
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates vector embeddings for multiple texts in a single batch request using OpenAI.
        /// More efficient than calling GenerateEmbeddingAsync multiple times.
        /// </summary>
        /// <param name="texts">The list of texts to generate embeddings for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of float arrays representing the embedding vectors</returns>
        Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates text using DigitalOcean Gradient AI (chat completions).
        /// Used for generating note descriptions and other AI-powered content.
        /// </summary>
        /// <param name="prompt">The user prompt to send to the model</param>
        /// <param name="systemPrompt">Optional system prompt to set context</param>
        /// <param name="maxTokens">Maximum tokens in the response (default: 500)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The generated text response</returns>
        Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt = "",
            int maxTokens = 500,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if AI services are available (either OpenAI embeddings or Gradient text generation).
        /// </summary>
        /// <returns>True if at least one service is available, false otherwise</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Gets the name of the currently configured embedding model (OpenAI).
        /// </summary>
        string EmbeddingModelName { get; }

        /// <summary>
        /// Gets the configured embedding dimensions (default: 1024).
        /// </summary>
        int EmbeddingDimensions { get; }

        /// <summary>
        /// Gets the name of the currently configured text generation model (Gradient).
        /// </summary>
        string GenerationModelName { get; }
    }
}
