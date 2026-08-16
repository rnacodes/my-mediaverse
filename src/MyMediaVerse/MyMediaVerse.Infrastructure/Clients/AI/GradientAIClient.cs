using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Clients.AI
{
    /// <summary>
    /// HTTP client for AI text generation using DigitalOcean Gradient AI (chat completions).
    /// </summary>
    public class GradientAIClient : IGradientAIClient
    {
        private readonly HttpClient _httpClient; // For Gradient/DigitalOcean text generation
        private readonly ILogger<GradientAIClient> _logger;
        private readonly string _generationModel;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly bool _isConfigured;

        public string GenerationModelName => _generationModel;

        public GradientAIClient(HttpClient httpClient, ILogger<GradientAIClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Gradient/DigitalOcean configuration for text generation
            _generationModel = Environment.GetEnvironmentVariable("GRADIENT_GENERATION_MODEL") ?? "gpt-4-turbo";

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Check if Gradient API key is configured (for text generation)
            var gradientApiKey = Environment.GetEnvironmentVariable("GRADIENT_API_KEY");
            _isConfigured = !string.IsNullOrEmpty(gradientApiKey);

            if (!_isConfigured)
            {
                _logger.LogWarning("Gradient AI API key not configured. Text generation will be disabled.");
            }
            else
            {
                _logger.LogInformation("AI client initialized - Generation: Gradient {GenerationModel}", _generationModel);
            }
        }

        /// <inheritdoc />
        public async Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt = "",
            int maxTokens = 500,
            CancellationToken cancellationToken = default)
        {
            if (!_isConfigured)
            {
                throw new InvalidOperationException("Gradient AI is not configured. Set the GRADIENT_API_KEY environment variable.");
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
            }

            try
            {
                var messages = new List<ChatMessage>();

                if (!string.IsNullOrWhiteSpace(systemPrompt))
                {
                    messages.Add(new ChatMessage { Role = "system", Content = systemPrompt });
                }

                messages.Add(new ChatMessage { Role = "user", Content = prompt });

                var request = new ChatCompletionRequest
                {
                    Model = _generationModel,
                    Messages = messages.ToArray(),
                    MaxTokens = maxTokens,
                    Temperature = 0.7
                };

                var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("Generating text response using model {Model} (max {MaxTokens} tokens)",
                    _generationModel, maxTokens);

                var response = await _httpClient.PostAsync("chat/completions", httpContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gradient AI chat completion request failed with status {Status}: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Gradient AI chat completion request failed: {response.StatusCode} - {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, _jsonOptions);

                if (chatResponse?.Choices == null || chatResponse.Choices.Length == 0)
                {
                    throw new InvalidOperationException("No response returned from Gradient AI.");
                }

                var generatedText = chatResponse.Choices[0].Message?.Content ?? string.Empty;

                _logger.LogDebug("Successfully generated text response ({Length} chars)", generatedText.Length);

                return generatedText.Trim();
            }
            catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
            {
                _logger.LogError(ex, "Error generating text from Gradient AI");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsAvailableAsync()
        {
            if (!_isConfigured)
            {
                return false;
            }

            try
            {
                var gradientResponse = await _httpClient.GetAsync("models");
                if (!gradientResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gradient AI availability check failed with status {Status}", gradientResponse.StatusCode);
                }

                return gradientResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI availability check failed");
                return false;
            }
        }

        #region Request/Response DTOs

        private class ChatCompletionRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }
        }

        private class ChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private class ChatCompletionResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("choices")]
            public ChatChoice[] Choices { get; set; } = Array.Empty<ChatChoice>();

            [JsonPropertyName("usage")]
            public UsageInfo? Usage { get; set; }
        }

        private class ChatChoice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("message")]
            public ChatMessage? Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private class UsageInfo
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }

        #endregion
    }
}
