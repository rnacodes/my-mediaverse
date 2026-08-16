using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service for managing Obsidian notes from Quartz vaults.
    /// </summary>
    public interface INoteService
    {
        // ============================================
        // CRUD Operations
        // ============================================

        /// <summary>
        /// Gets a note by its ID.
        /// </summary>
        Task<Note?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a note by its vault name and slug.
        /// </summary>
        Task<Note?> GetBySlugAndVaultAsync(string slug, string vaultName);

        /// <summary>
        /// Gets all notes, optionally filtered by vault name.
        /// </summary>
        Task<List<Note>> GetAllAsync(string? vaultName = null);

        /// <summary>
        /// Creates a new note manually.
        /// </summary>
        Task<Note> CreateAsync(CreateNoteDto dto);

        /// <summary>
        /// Updates an existing note.
        /// </summary>
        Task<Note> UpdateAsync(Guid id, UpdateNoteDto dto);

        /// <summary>
        /// Deletes a note.
        /// </summary>
        Task DeleteAsync(Guid id);

        // ============================================
        // Linking Operations
        // ============================================

        /// <summary>
        /// Links a note to a media item.
        /// </summary>
        Task LinkToMediaItemAsync(Guid noteId, Guid mediaItemId, string? description = null);

        /// <summary>
        /// Unlinks a note from a media item.
        /// </summary>
        Task UnlinkFromMediaItemAsync(Guid noteId, Guid mediaItemId);

        /// <summary>
        /// Gets all notes linked to a media item.
        /// </summary>
        Task<List<LinkedNoteDto>> GetNotesForMediaItemAsync(Guid mediaItemId);

        /// <summary>
        /// Gets all media items linked to a note.
        /// </summary>
        Task<List<LinkedMediaItemDto>> GetMediaItemsForNoteAsync(Guid noteId);

        // ============================================
        // Sync Operations
        // ============================================

        /// <summary>
        /// Syncs notes from a specific Quartz vault.
        /// </summary>
        /// <param name="vaultName">The vault name ("general" or "programming")</param>
        /// <param name="vaultUrl">The base URL of the Quartz vault</param>
        /// <param name="authToken">Optional authentication token</param>
        /// <param name="removeOrphans">Also delete notes that no longer exist in the published vault</param>
        Task<NoteSyncResultDto> SyncFromQuartzVaultAsync(string vaultName, string vaultUrl, string? authToken = null, bool removeOrphans = false);

        /// <summary>
        /// Syncs notes from all configured vaults.
        /// </summary>
        /// <param name="removeOrphans">Also delete notes that no longer exist in the published vaults</param>
        Task<List<NoteSyncResultDto>> SyncAllVaultsAsync(bool removeOrphans = false);

        /// <summary>
        /// Gets the current sync configuration status.
        /// </summary>
        Task<NoteSyncStatusDto> GetSyncStatusAsync();
    }
}
