namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Client for AI text generation using DigitalOcean Gradient AI (chat completions).
    /// </summary>
    public interface IGradientAIClient
    {
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
        /// Checks if the Gradient text-generation service is available.
        /// </summary>
        /// <returns>True if the service is configured, false otherwise</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Gets the name of the currently configured text generation model (Gradient).
        /// </summary>
        string GenerationModelName { get; }
    }
}
