using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for managing Obsidian notes from Quartz vaults.
    /// </summary>
    public class NoteService : INoteService
    {
        private readonly IApplicationDbContext _context;
        private readonly IQuartzApiClient _quartzClient;
        private readonly IConfiguration _configuration;
        private readonly ITypesenseService _typesenseService;
        private readonly ILogger<NoteService> _logger;

        public NoteService(
            IApplicationDbContext context,
            IQuartzApiClient quartzClient,
            IConfiguration configuration,
            ITypesenseService typesenseService,
            ILogger<NoteService> logger)
        {
            _context = context;
            _quartzClient = quartzClient;
            _configuration = configuration;
            _typesenseService = typesenseService;
            _logger = logger;
        }

        // ============================================
        // CRUD Operations
        // ============================================

        public async Task<Note?> GetByIdAsync(Guid id)
        {
            return await _context.Notes
                .Include(n => n.MediaItemNotes)
                    .ThenInclude(min => min.MediaItem)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Note?> GetBySlugAndVaultAsync(string slug, string vaultName)
        {
            var normalizedSlug = slug.ToLowerInvariant();
            return await _context.Notes
                .Include(n => n.MediaItemNotes)
                    .ThenInclude(min => min.MediaItem)
                .FirstOrDefaultAsync(n => n.Slug == normalizedSlug && n.VaultName == vaultName.ToLower());
        }

        public async Task<List<Note>> GetAllAsync(string? vaultName = null)
        {
            var query = _context.Notes
                .Include(n => n.MediaItemNotes)
                    .ThenInclude(min => min.MediaItem)
                .AsQueryable();

            if (!string.IsNullOrEmpty(vaultName))
            {
                query = query.Where(n => n.VaultName == vaultName.ToLower());
            }

            return await query
                .OrderByDescending(n => n.DateImported)
                .ToListAsync();
        }

        public async Task<Note> CreateAsync(CreateNoteDto dto)
        {
            var note = new Note
            {
                Slug = dto.Slug.ToLower(),
                Title = dto.Title,
                Content = dto.Content,
                Description = dto.Description,
                VaultName = dto.VaultName.ToLower(),
                SourceUrl = dto.SourceUrl,
                Tags = dto.Tags ?? new List<string>(),
                NoteDate = dto.NoteDate,
                DateImported = DateTime.UtcNow,
                ContentHash = ComputeContentHash(dto.Content)
            };

            _context.Add(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created note {Id} ({Title}) in vault {VaultName}", note.Id, note.Title, note.VaultName);
            return note;
        }

        public async Task<Note> UpdateAsync(Guid id, UpdateNoteDto dto)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                throw new KeyNotFoundException($"Note with ID {id} not found.");
            }

            if (dto.Title != null) note.Title = dto.Title;
            if (dto.Content != null)
            {
                note.Content = dto.Content;
                note.ContentHash = ComputeContentHash(dto.Content);
            }
            if (dto.Description != null)
            {
                note.Description = dto.Description;
                // Mark as manually edited so AI won't overwrite during sync
                note.IsDescriptionManual = true;
            }
            if (dto.Tags != null) note.Tags = dto.Tags;
            if (dto.NoteDate.HasValue) note.NoteDate = dto.NoteDate;

            note.LastSyncedAt = DateTime.UtcNow;

            _context.Update(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated note {Id} ({Title})", note.Id, note.Title);
            return note;
        }

        public async Task DeleteAsync(Guid id)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                throw new KeyNotFoundException($"Note with ID {id} not found.");
            }

            _context.Remove(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted note {Id}", id);

         try
            {
                await _typesenseService.DeleteNoteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove note {Id} from the search index; it will be removed on the next reindex", id);
            }
        }

        public async Task<int> BulkDeleteAsync(List<Guid> ids)
        {
            var notes = await _context.Notes
                .Where(n => ids.Contains(n.Id))
                .ToListAsync();

            if (notes.Count == 0)
            {
                return 0;
            }

            foreach (var note in notes)
            {
                _context.Remove(note);
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk deleted {Count} notes", notes.Count);

            // Best-effort search index cleanup, mirroring DeleteAsync; the next bulk reindex
            // reconciles any misses.
            foreach (var note in notes)
            {
                try
                {
                    await _typesenseService.DeleteNoteAsync(note.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove note {Id} from the search index; it will be removed on the next reindex", note.Id);
                }
            }

            return notes.Count;
        }

        // ============================================
        // Linking Operations
        // ============================================

        public async Task LinkToMediaItemAsync(Guid noteId, Guid mediaItemId, string? description = null)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (note == null)
            {
                throw new KeyNotFoundException($"Note with ID {noteId} not found.");
            }

            var mediaItem = await _context.MediaItems.FirstOrDefaultAsync(m => m.Id == mediaItemId);
            if (mediaItem == null)
            {
                throw new KeyNotFoundException($"Media item with ID {mediaItemId} not found.");
            }

            // Check if link already exists
            var existingLink = await _context.MediaItemNotes
                .FirstOrDefaultAsync(min => min.NoteId == noteId && min.MediaItemId == mediaItemId);

            if (existingLink != null)
            {
                _logger.LogWarning("Link between note {NoteId} and media item {MediaItemId} already exists", noteId, mediaItemId);
                return;
            }

            var link = new MediaItemNote
            {
                NoteId = noteId,
                MediaItemId = mediaItemId,
                LinkDescription = description,
                LinkedAt = DateTime.UtcNow
            };

            _context.Add(link);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Linked note {NoteId} to media item {MediaItemId}", noteId, mediaItemId);
        }

        public async Task UnlinkFromMediaItemAsync(Guid noteId, Guid mediaItemId)
        {
            var link = await _context.MediaItemNotes
                .FirstOrDefaultAsync(min => min.NoteId == noteId && min.MediaItemId == mediaItemId);

            if (link == null)
            {
                throw new KeyNotFoundException($"Link between note {noteId} and media item {mediaItemId} not found.");
            }

            _context.Remove(link);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Unlinked note {NoteId} from media item {MediaItemId}", noteId, mediaItemId);
        }

        public async Task<List<LinkedNoteDto>> GetNotesForMediaItemAsync(Guid mediaItemId)
        {
            var links = await _context.MediaItemNotes
                .Include(min => min.Note)
                .Where(min => min.MediaItemId == mediaItemId)
                .OrderByDescending(min => min.LinkedAt)
                .ToListAsync();

            return links.Select(link => new LinkedNoteDto
            {
                Id = link.Note.Id,
                Slug = link.Note.Slug,
                Title = link.Note.Title,
                Description = link.Note.Description,
                VaultName = link.Note.VaultName,
                SourceUrl = link.Note.SourceUrl,
                Tags = link.Note.Tags,
                LinkedAt = link.LinkedAt,
                LinkDescription = link.LinkDescription
            }).ToList();
        }

        public async Task<List<LinkedMediaItemDto>> GetMediaItemsForNoteAsync(Guid noteId)
        {
            var links = await _context.MediaItemNotes
                .Include(min => min.MediaItem)
                .Where(min => min.NoteId == noteId)
                .OrderByDescending(min => min.LinkedAt)
                .ToListAsync();

            return links.Select(link => new LinkedMediaItemDto
            {
                Id = link.MediaItem.Id,
                Title = link.MediaItem.Title,
                MediaType = link.MediaItem.MediaType.ToString(),
                Thumbnail = link.MediaItem.Thumbnail,
                LinkedAt = link.LinkedAt,
                LinkDescription = link.LinkDescription
            }).ToList();
        }

        // ============================================
        // Sync Operations
        // ============================================

        public async Task<NoteSyncResultDto> SyncFromQuartzVaultAsync(string vaultName, string vaultUrl, string? authToken = null, bool removeOrphans = false)
        {
            var startedAt = DateTime.UtcNow;
            var result = new NoteSyncResultDto
            {
                VaultName = vaultName.ToLower(),
                StartedAt = startedAt,
                SyncedAt = startedAt
            };

            _logger.LogInformation("Starting sync for vault {VaultName} from {VaultUrl}", vaultName, vaultUrl);

            try
            {
                await SyncVaultCoreAsync(vaultName, vaultUrl, authToken, removeOrphans, result);
                result.CompletedAt = DateTime.UtcNow;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Vault authentication failed while syncing vault {VaultName}", vaultName);
                result.Success = false;
                result.ErrorMessage = $"Vault authentication failed: {ex.Message}";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to reach vault {VaultName}", vaultName);
                result.Success = false;
                result.ErrorMessage = $"Failed to reach the vault: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing vault {VaultName}", vaultName);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private async Task SyncVaultCoreAsync(string vaultName, string vaultUrl, string? authToken, bool removeOrphans, NoteSyncResultDto result)
        {
            var contentIndex = await _quartzClient.GetContentIndexAsync(vaultUrl, authToken);
            result.TotalProcessed = contentIndex.Count;

            foreach (var (slug, noteDto) in contentIndex)
            {
                try
                {
                    // Debug logging for tags deserialization
                    _logger.LogDebug("Processing note {Slug}: Tags count = {TagsCount}, Tags = [{Tags}]",
                        slug,
                        noteDto.Tags?.Count ?? 0,
                        noteDto.Tags != null ? string.Join(", ", noteDto.Tags) : "null");

                    var normalizedSlug = slug.ToLowerInvariant();

                    var existingNote = await _context.Notes
                        .FirstOrDefaultAsync(n => n.Slug == normalizedSlug && n.VaultName == vaultName.ToLower());

                    var contentHash = ComputeContentHash(noteDto.Content);

                    if (existingNote == null)
                    {
                        // Create new note
                        var note = new Note
                        {
                            Slug = normalizedSlug,
                            Title = noteDto.Title,
                            Content = noteDto.Content,
                            Description = noteDto.Description,
                            VaultName = vaultName.ToLower(),
                            SourceUrl = $"{vaultUrl.TrimEnd('/')}/{slug}",
                            Tags = NormalizeTags(noteDto.Tags),
                            NoteDate = ParseDate(noteDto.Date),
                            DateImported = DateTime.UtcNow,
                            LastSyncedAt = DateTime.UtcNow,
                            ContentHash = contentHash
                        };

                        _context.Add(note);
                        await _context.SaveChangesAsync();

                        result.CreatedCount++;
                    }
                    else if (existingNote.ContentHash != contentHash)
                    {
                        // Update existing note
                        existingNote.Title = noteDto.Title;
                        existingNote.Content = noteDto.Content;
                        existingNote.Tags = NormalizeTags(noteDto.Tags);
                        existingNote.NoteDate = ParseDate(noteDto.Date);
                        existingNote.LastSyncedAt = DateTime.UtcNow;
                        existingNote.ContentHash = contentHash;

                        // Content changed, so any AI summary is now stale. Clear it (and reset the
                        // synced Description unless the user hand-edited it) so the batch regen,
                        // which selects notes where AiDescription == null, picks this note up again.
                        existingNote.AiDescription = null;
                        existingNote.AiDescriptionGeneratedAt = null;
                        if (!existingNote.IsDescriptionManual)
                        {
                            existingNote.Description = noteDto.Description;
                        }

                        _context.Update(existingNote);
                        await _context.SaveChangesAsync();

                        result.UpdatedCount++;
                    }
                    else
                    {
                        // Content unchanged, but still update tags in case they were missed in a previous sync
                        var normalizedTags = NormalizeTags(noteDto.Tags);
                        var tagsChanged = !TagsAreEqual(existingNote.Tags, normalizedTags);
                        if (tagsChanged)
                        {
                            _logger.LogDebug("Updating tags for unchanged note {Slug}: [{OldTags}] -> [{NewTags}]",
                                slug,
                                string.Join(", ", existingNote.Tags ?? new List<string>()),
                                string.Join(", ", normalizedTags));
                            existingNote.Tags = normalizedTags;
                        }
                        existingNote.LastSyncedAt = DateTime.UtcNow;
                        _context.Update(existingNote);
                        result.SkippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing note {Slug} from vault {VaultName}", slug, vaultName);
                    result.FailedCount++;
                    result.Errors.Add($"Failed to sync note '{slug}': {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Sync completed for vault {VaultName}: {Created} created, {Updated} updated, {Skipped} unchanged, {Failed} failed",
                vaultName, result.CreatedCount, result.UpdatedCount, result.SkippedCount, result.FailedCount);

            if (removeOrphans)
            {
                await RemoveOrphanedNotesAsync(vaultName.ToLower(), contentIndex.Keys, result);
            }

            if (result.FailedCount > 0)
            {
                AppendWarning(result, $"{result.FailedCount} of {result.TotalProcessed} notes failed to sync; see errors for detail.");
            }
        }

        private static void AppendWarning(NoteSyncResultDto result, string message)
        {
            result.WarningMessage = string.IsNullOrWhiteSpace(result.WarningMessage)
                ? message
                : $"{result.WarningMessage} {message}";
        }

        // Deletes notes whose slug is no longer present in the vault's published content index.
        // Runs only after a successful index fetch, so a vault outage can never look like a mass deletion.
        private async Task RemoveOrphanedNotesAsync(string vaultName, IEnumerable<string> publishedSlugs, NoteSyncResultDto result)
        {
            var publishedSet = publishedSlugs.Select(s => s.ToLowerInvariant()).ToHashSet();

            var vaultNotes = await _context.Notes
                .Where(n => n.VaultName == vaultName)
                .ToListAsync();

            var orphans = vaultNotes.Where(n => !publishedSet.Contains(n.Slug.ToLowerInvariant())).ToList();

            if (publishedSet.Count == 0 && orphans.Count > 0)
            {
                // An empty published index while notes exist in the database usually means
                // the vault publish itself is broken, so log at error level for alerting.
                _logger.LogError(
                    "Orphan removal skipped for vault {VaultName}: content index is empty but {Count} notes exist in the database",
                    vaultName, orphans.Count);
                AppendWarning(result, $"Orphan removal skipped: the published content index is empty but {orphans.Count} notes exist in the database. Check the vault publish.");
                return;
            }

            foreach (var orphan in orphans)
            {
                _context.Remove(orphan);
                result.OrphansRemoved++;
                result.RemovedSlugs.Add(orphan.Slug);
                _logger.LogInformation("Removing orphaned note {Id} ({Slug}) from vault {VaultName}", orphan.Id, orphan.Slug, vaultName);
            }

            if (orphans.Count == 0)
            {
                return;
            }

            await _context.SaveChangesAsync();

            // Best-effort search index cleanup, mirroring DeleteAsync; the next bulk reindex reconciles any misses.
            foreach (var orphan in orphans)
            {
                try
                {
                    await _typesenseService.DeleteNoteAsync(orphan.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove orphaned note {Id} from the search index; it will be removed on the next reindex", orphan.Id);
                }
            }

            _logger.LogInformation("Removed {Count} orphaned note(s) from vault {VaultName}", orphans.Count, vaultName);
        }

        public async Task<List<NoteSyncResultDto>> SyncAllVaultsAsync(bool removeOrphans = false)
        {
            var results = new List<NoteSyncResultDto>();

            var generalVaultUrl = Environment.GetEnvironmentVariable("OBSIDIAN_GENERAL_VAULT_URL") ??
                _configuration["ObsidianNoteSync:GeneralVaultUrl"];
            var generalVaultAuth = Environment.GetEnvironmentVariable("OBSIDIAN_GENERAL_VAULT_AUTH_TOKEN") ??
                _configuration["ObsidianNoteSync:GeneralVaultAuthToken"];

            if (!string.IsNullOrEmpty(generalVaultUrl))
            {
                var result = await SyncVaultGuardedAsync("general", generalVaultUrl, generalVaultAuth, removeOrphans);
                results.Add(result);
            }

            var programmingVaultUrl = Environment.GetEnvironmentVariable("OBSIDIAN_PROGRAMMING_VAULT_URL") ??
                _configuration["ObsidianNoteSync:ProgrammingVaultUrl"];
            var programmingVaultAuth = Environment.GetEnvironmentVariable("OBSIDIAN_PROGRAMMING_VAULT_AUTH_TOKEN") ??
                _configuration["ObsidianNoteSync:ProgrammingVaultAuthToken"];

            if (!string.IsNullOrEmpty(programmingVaultUrl))
            {
                var result = await SyncVaultGuardedAsync("programming", programmingVaultUrl, programmingVaultAuth, removeOrphans);
                results.Add(result);
            }

            return results;
        }

        // Guards the sync-all loop: a failure in one vault is reported in that vault's
        // result instead of aborting the sync of the remaining vaults.
        private async Task<NoteSyncResultDto> SyncVaultGuardedAsync(string vaultName, string vaultUrl, string? authToken, bool removeOrphans)
        {
            try
            {
                return await SyncFromQuartzVaultAsync(vaultName, vaultUrl, authToken, removeOrphans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error syncing vault {VaultName}", vaultName);
                var startedAt = DateTime.UtcNow;
                return new NoteSyncResultDto
                {
                    VaultName = vaultName.ToLower(),
                    Success = false,
                    ErrorMessage = ex.Message,
                    StartedAt = startedAt,
                    SyncedAt = startedAt
                };
            }
        }

        public async Task<NoteSyncStatusDto> GetSyncStatusAsync()
        {
            var generalVaultUrl = Environment.GetEnvironmentVariable("OBSIDIAN_GENERAL_VAULT_URL") ??
                _configuration["ObsidianNoteSync:GeneralVaultUrl"];
            var generalVaultAuth = Environment.GetEnvironmentVariable("OBSIDIAN_GENERAL_VAULT_AUTH_TOKEN") ??
                _configuration["ObsidianNoteSync:GeneralVaultAuthToken"];
            var programmingVaultUrl = Environment.GetEnvironmentVariable("OBSIDIAN_PROGRAMMING_VAULT_URL") ??
                _configuration["ObsidianNoteSync:ProgrammingVaultUrl"];
            var programmingVaultAuth = Environment.GetEnvironmentVariable("OBSIDIAN_PROGRAMMING_VAULT_AUTH_TOKEN") ??
                _configuration["ObsidianNoteSync:ProgrammingVaultAuthToken"];

            var enabled = bool.TryParse(
                Environment.GetEnvironmentVariable("OBSIDIAN_SYNC_ENABLED") ?? _configuration["ObsidianNoteSync:Enabled"],
                out var e) && e;

            var intervalHours = int.TryParse(
                Environment.GetEnvironmentVariable("OBSIDIAN_SYNC_INTERVAL_HOURS") ?? _configuration["ObsidianNoteSync:IntervalHours"],
                out var i) ? i : 6;

            var lastSyncGeneral = await _context.Notes
                .Where(n => n.VaultName == "general")
                .MaxAsync(n => (DateTime?)n.LastSyncedAt);

            var lastSyncProgramming = await _context.Notes
                .Where(n => n.VaultName == "programming")
                .MaxAsync(n => (DateTime?)n.LastSyncedAt);

            var totalGeneral = await _context.Notes.CountAsync(n => n.VaultName == "general");
            var totalProgramming = await _context.Notes.CountAsync(n => n.VaultName == "programming");

            // The background worker only honors the ObsidianNoteSync config section,
            // so report that value (not the legacy OBSIDIAN_SYNC_ENABLED variable) here.
            var backgroundSyncEnabled = bool.TryParse(_configuration["ObsidianNoteSync:Enabled"], out var bg) && bg;

            var lastSyncTime = lastSyncGeneral > lastSyncProgramming ? lastSyncGeneral : lastSyncProgramming;

            return new NoteSyncStatusDto
            {
                Enabled = enabled,
                BackgroundSyncEnabled = backgroundSyncEnabled,
                IntervalHours = intervalHours,
                GeneralVaultUrl = generalVaultUrl,
                ProgrammingVaultUrl = programmingVaultUrl,
                GeneralVaultConfigured = !string.IsNullOrEmpty(generalVaultUrl),
                ProgrammingVaultConfigured = !string.IsNullOrEmpty(programmingVaultUrl),
                GeneralVaultHasAuth = !string.IsNullOrEmpty(generalVaultAuth),
                ProgrammingVaultHasAuth = !string.IsNullOrEmpty(programmingVaultAuth),
                LastSyncGeneral = lastSyncGeneral,
                LastSyncProgramming = lastSyncProgramming,
                LastSyncTime = lastSyncTime,
                TotalNotesGeneral = totalGeneral,
                TotalNotesProgramming = totalProgramming
            };
        }

        // ============================================
        // Helper Methods
        // ============================================

        private static string? ComputeContentHash(string? content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes);
        }

        private static DateTime? ParseDate(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return null;

            if (DateTime.TryParse(dateStr, out var date))
            {
                return date.ToUniversalTime();
            }

            return null;
        }

        /// <summary>
        /// Normalizes frontmatter tags to MMV's lowercase-tags invariant: trims, lowercases,
        /// drops blanks, and de-duplicates. Owned here rather than inherited from Quartz's slugTag.
        /// </summary>
        private static List<string> NormalizeTags(IEnumerable<string>? tags)
        {
            if (tags == null) return new List<string>();

            return tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLower())
                .Distinct()
                .ToList();
        }

        private static bool TagsAreEqual(List<string>? tags1, List<string>? tags2)
        {
            var list1 = tags1 ?? new List<string>();
            var list2 = tags2 ?? new List<string>();

            if (list1.Count != list2.Count) return false;

            return list1.OrderBy(t => t).SequenceEqual(list2.OrderBy(t => t));
        }
    }
}
