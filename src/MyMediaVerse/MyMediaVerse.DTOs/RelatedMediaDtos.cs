using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Request to save a related media item.
    /// </summary>
    public class SaveRelatedMediaDto
    {
        [JsonPropertyName("relatedMediaItemId")]
        public Guid RelatedMediaItemId { get; set; }

        /// <summary>
        /// How the relationship was created: "AiRecommended" or "ManuallyAdded"
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = "ManuallyAdded";

        /// <summary>
        /// The similarity score at the time of saving (for AI recommendations).
        /// </summary>
        [JsonPropertyName("similarityScore")]
        public double? SimilarityScore { get; set; }

        /// <summary>
        /// Optional note explaining why these items are related.
        /// </summary>
        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// Response for a saved related media relationship.
    /// </summary>
    public class RelatedMediaResponseDto
    {
        [JsonPropertyName("sourceMediaItemId")]
        public Guid SourceMediaItemId { get; set; }

        [JsonPropertyName("relatedMediaItemId")]
        public Guid RelatedMediaItemId { get; set; }

        [JsonPropertyName("relatedMediaItem")]
        public RelatedMediaItemSummaryDto? RelatedMediaItem { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("similarityScore")]
        public double? SimilarityScore { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// Summary of a related media item for display.
    /// </summary>
    public class RelatedMediaItemSummaryDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public string? Rating { get; set; }
    }
}
