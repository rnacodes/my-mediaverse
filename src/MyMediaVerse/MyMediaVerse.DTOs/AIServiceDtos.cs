using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Result DTO for batch AI operations (description generation, embedding generation).
    /// </summary>
    public class AIBatchResultDto
    {
        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed { get; set; }

        [JsonPropertyName("successCount")]
        public int SuccessCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        [JsonPropertyName("skippedCount")]
        public int SkippedCount { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("processedAt")]
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("durationMs")]
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// Status DTO for AI service availability and pending work.
    /// </summary>
    public class AIStatusDto
    {
        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }

        [JsonPropertyName("generationModel")]
        public string GenerationModel { get; set; } = string.Empty;

        [JsonPropertyName("generationProvider")]
        public string GenerationProvider { get; set; } = "DigitalOcean";

        [JsonPropertyName("pendingNoteDescriptions")]
        public int PendingNoteDescriptions { get; set; }

        [JsonPropertyName("lastDescriptionGenerationRun")]
        public DateTime? LastDescriptionGenerationRun { get; set; }

        [JsonPropertyName("statusMessage")]
        public string StatusMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result DTO for single note description generation.
    /// </summary>
    public class NoteDescriptionResultDto
    {
        [JsonPropertyName("noteId")]
        public Guid NoteId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("generatedDescription")]
        public string? GeneratedDescription { get; set; }

        [JsonPropertyName("generatedAt")]
        public DateTime? GeneratedAt { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request DTO for generating description for a specific note.
    /// </summary>
    public class GenerateNoteDescriptionRequestDto
    {
        [JsonPropertyName("noteId")]
        public Guid NoteId { get; set; }

        [JsonPropertyName("forceRegenerate")]
        public bool ForceRegenerate { get; set; } = false;
    }

    /// <summary>
    /// Request DTO for batch operations.
    /// </summary>
    public class AIBatchRequestDto
    {
        [JsonPropertyName("batchSize")]
        public int BatchSize { get; set; } = 20;
    }
}
