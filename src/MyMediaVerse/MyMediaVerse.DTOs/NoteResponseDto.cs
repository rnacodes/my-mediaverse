using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// DTO for returning Note data in API responses.
    /// </summary>
    public class NoteResponseDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("vaultName")]
        public string VaultName { get; set; } = string.Empty;

        [JsonPropertyName("sourceUrl")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("noteDate")]
        public DateTime? NoteDate { get; set; }

        [JsonPropertyName("dateImported")]
        public DateTime DateImported { get; set; }

        [JsonPropertyName("lastSyncedAt")]
        public DateTime? LastSyncedAt { get; set; }

        // AI-generated fields

        [JsonPropertyName("aiDescription")]
        public string? AiDescription { get; set; }

        [JsonPropertyName("aiDescriptionGeneratedAt")]
        public DateTime? AiDescriptionGeneratedAt { get; set; }

        [JsonPropertyName("isDescriptionManual")]
        public bool IsDescriptionManual { get; set; }

        [JsonPropertyName("linkedMediaItems")]
        public List<LinkedMediaItemDto> LinkedMediaItems { get; set; } = new();
    }

    /// <summary>
    /// Simplified DTO for media items linked to a note.
    /// </summary>
    public class LinkedMediaItemDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; } = string.Empty;

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("linkedAt")]
        public DateTime LinkedAt { get; set; }

        [JsonPropertyName("linkDescription")]
        public string? LinkDescription { get; set; }
    }

    /// <summary>
    /// Simplified DTO for notes linked to a media item.
    /// </summary>
    public class LinkedNoteDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("vaultName")]
        public string VaultName { get; set; } = string.Empty;

        [JsonPropertyName("sourceUrl")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("linkedAt")]
        public DateTime LinkedAt { get; set; }

        [JsonPropertyName("linkDescription")]
        public string? LinkDescription { get; set; }
    }

    /// <summary>
    /// DTO for creating a note manually.
    /// </summary>
    public class CreateNoteDto
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("vaultName")]
        public string VaultName { get; set; } = string.Empty;

        [JsonPropertyName("sourceUrl")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("noteDate")]
        public DateTime? NoteDate { get; set; }
    }

    /// <summary>
    /// DTO for updating a note.
    /// </summary>
    public class UpdateNoteDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("noteDate")]
        public DateTime? NoteDate { get; set; }
    }

    /// <summary>
    /// DTO for linking a note to a media item.
    /// </summary>
    public class LinkNoteToMediaDto
    {
        [JsonPropertyName("mediaItemId")]
        public Guid MediaItemId { get; set; }

        [JsonPropertyName("linkDescription")]
        public string? LinkDescription { get; set; }
    }

    /// <summary>
    /// DTO for linking a note to a mixlist.
    /// </summary>
    public class LinkNoteToMixlistDto
    {
        [JsonPropertyName("noteId")]
        public Guid NoteId { get; set; }

        [JsonPropertyName("linkDescription")]
        public string? LinkDescription { get; set; }
    }

    /// <summary>
    /// DTO for note sync operation results.
    /// </summary>
    public class NoteSyncResultDto
    {
        [JsonPropertyName("totalProcessed")]
        public int TotalProcessed { get; set; }

        [JsonPropertyName("imported")]
        public int Imported { get; set; }

        [JsonPropertyName("updated")]
        public int Updated { get; set; }

        [JsonPropertyName("unchanged")]
        public int Unchanged { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        /// <summary>
        /// Combined count of notes imported or updated by this sync.
        /// </summary>
        [JsonPropertyName("importedCount")]
        public int ImportedCount => Imported + Updated;

        /// <summary>
        /// Notes deleted because they no longer exist in the published vault
        /// (only populated when the sync is called with removeOrphans).
        /// </summary>
        [JsonPropertyName("orphansRemoved")]
        public int OrphansRemoved { get; set; }

        [JsonPropertyName("removedSlugs")]
        public List<string> RemovedSlugs { get; set; } = new();

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("vaultName")]
        public string VaultName { get; set; } = string.Empty;

        [JsonPropertyName("syncedAt")]
        public DateTime SyncedAt { get; set; }
    }

    /// <summary>
    /// DTO for note sync configuration status.
    /// </summary>
    public class NoteSyncStatusDto
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether the background sync worker is enabled (the ObsidianNoteSync config
        /// section the worker actually honors).
        /// </summary>
        [JsonPropertyName("backgroundSyncEnabled")]
        public bool BackgroundSyncEnabled { get; set; }

        [JsonPropertyName("intervalHours")]
        public int IntervalHours { get; set; }

        [JsonPropertyName("generalVaultConfigured")]
        public bool GeneralVaultConfigured { get; set; }

        [JsonPropertyName("programmingVaultConfigured")]
        public bool ProgrammingVaultConfigured { get; set; }

        /// <summary>
        /// The most recent sync time across all vaults.
        /// </summary>
        [JsonPropertyName("lastSyncTime")]
        public DateTime? LastSyncTime { get; set; }

        [JsonPropertyName("generalVaultUrl")]
        public string? GeneralVaultUrl { get; set; }

        [JsonPropertyName("programmingVaultUrl")]
        public string? ProgrammingVaultUrl { get; set; }

        [JsonPropertyName("generalVaultHasAuth")]
        public bool GeneralVaultHasAuth { get; set; }

        [JsonPropertyName("programmingVaultHasAuth")]
        public bool ProgrammingVaultHasAuth { get; set; }

        [JsonPropertyName("lastSyncGeneral")]
        public DateTime? LastSyncGeneral { get; set; }

        [JsonPropertyName("lastSyncProgramming")]
        public DateTime? LastSyncProgramming { get; set; }

        [JsonPropertyName("totalNotesGeneral")]
        public int TotalNotesGeneral { get; set; }

        [JsonPropertyName("totalNotesProgramming")]
        public int TotalNotesProgramming { get; set; }
    }
}
