using System.Text.Json.Serialization;

namespace MyMediaVerse.Infrastructure.Models
{
    /// <summary>
    /// Typesense document model for Obsidian notes.
    /// This mirrors the schema defined in Typesense for the obsidian_notes collection.
    /// All fields must match the collection schema exactly.
    /// </summary>
    public class ObsidianNoteDocument
    {
        /// <summary>
        /// Document ID in Typesense (matches PostgreSQL Notes.Id)
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// URL-friendly identifier from Quartz
        /// </summary>
        [JsonPropertyName("slug")]
        public required string Slug { get; set; }

        /// <summary>
        /// Title of the note (searchable)
        /// </summary>
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        /// <summary>
        /// Full markdown content of the note (searchable)
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>
        /// Short excerpt or description (searchable)
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The vault this note belongs to: "general" or "programming"
        /// Facetable for filtering by vault
        /// </summary>
        [JsonPropertyName("vault_name")]
        public required string VaultName { get; set; }

        /// <summary>
        /// Full URL to the note on the Quartz static site (not indexed)
        /// </summary>
        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        /// <summary>
        /// List of tags from the note's frontmatter (facetable)
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// When the note was imported to MyMediaVerse (Unix timestamp)
        /// Used for sorting by recency
        /// </summary>
        [JsonPropertyName("date_imported")]
        public long DateImported { get; set; }

        /// <summary>
        /// Original note creation/modification date from Quartz (Unix timestamp)
        /// </summary>
        [JsonPropertyName("note_date")]
        public long? NoteDate { get; set; }

        /// <summary>
        /// Number of media items linked to this note
        /// </summary>
        [JsonPropertyName("linked_media_count")]
        public int LinkedMediaCount { get; set; }

        /// <summary>
        /// Text composed for semantic embedding. Typesense auto-embeds this via the collection's
        /// embedding field, so keyword and vector search stay sourced from one place. Serialized
        /// on write; ignored when search hits are deserialized back (no setter).
        /// </summary>
        [JsonPropertyName("embedding_source")]
        public string EmbeddingSource => string.Join("\n", new[]
        {
            Title,
            Description,
            Content,
            Tags.Count > 0 ? string.Join(", ", Tags) : null,
            VaultName,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
