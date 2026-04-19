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
        private readonly ITypeSenseService? _typesenseService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NoteService> _logger;

        public NoteService(
            IApplicationDbContext context,
            IQuartzApiClient quartzClient,
            ITypeSenseService? typesenseService,
            IConfiguration configuration,
            ILogger<NoteService> logger)
        {
            _context = context;
            _quartzClient = quartzClient;
            _typesenseService = typesenseService;
            _configuration = configuration;
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
            return await _context.Notes
                .Include(n => n.MediaItemNotes)
                    .ThenInclude(min => min.MediaItem)
                .FirstOrDefaultAsync(n => n.Slug == slug && n.VaultName == vaultName.ToLower());
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

            // Index in Typesense
            await IndexNoteInTypesenseAsync(note);

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

            // Update in Typesense
            await IndexNoteInTypesenseAsync(note);

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

            // Remove from Typesense
            if (_typesenseService != null)
            {
                try
                {
                    await _typesenseService.DeleteNoteAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete note {Id} from Typesense", id);
                }
            }

            _logger.LogInformation("Deleted note {Id}", id);
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

            // Update Typesense with new linked count
            await IndexNoteInTypesenseAsync(note);

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

            // Update Typesense with new linked count
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == noteId);
            if (note != null)
            {
                await IndexNoteInTypesenseAsync(note);
            }

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

        public async Task<NoteSyncResultDto> SyncFromQuartzVaultAsync(string vaultName, string vaultUrl, string? authToken = null)
        {
            var result = new NoteSyncResultDto
            {
                VaultName = vaultName.ToLower(),
                SyncedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting sync for vault {VaultName} from {VaultUrl}", vaultName, vaultUrl);

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

                        var existingNote = await _context.Notes
                            .FirstOrDefaultAsync(n => n.Slug == slug && n.VaultName == vaultName.ToLower());

                        var contentHash = ComputeContentHash(noteDto.Content);

                        if (existingNote == null)
                        {
                            // Create new note
                            var note = new Note
                            {
                                Slug = slug,
                                Title = noteDto.Title,
                                Content = noteDto.Content,
                                Description = noteDto.Description,
                                VaultName = vaultName.ToLower(),
                                SourceUrl = $"{vaultUrl.TrimEnd('/')}/{slug}",
                                Tags = noteDto.Tags ?? new List<string>(),
                                NoteDate = ParseDate(noteDto.Date),
                                DateImported = DateTime.UtcNow,
                                LastSyncedAt = DateTime.UtcNow,
                                ContentHash = contentHash
                            };

                            _context.Add(note);
                            await _context.SaveChangesAsync();
                            await IndexNoteInTypesenseAsync(note);

                            result.Imported++;
                        }
                        else if (existingNote.ContentHash != contentHash)
                        {
                            // Update existing note
                            existingNote.Title = noteDto.Title;
                            existingNote.Content = noteDto.Content;
                            existingNote.Description = noteDto.Description;
                            existingNote.Tags = noteDto.Tags ?? new List<string>();
                            existingNote.NoteDate = ParseDate(noteDto.Date);
                            existingNote.LastSyncedAt = DateTime.UtcNow;
                            existingNote.ContentHash = contentHash;

                            _context.Update(existingNote);
                            await _context.SaveChangesAsync();
                            await IndexNoteInTypesenseAsync(existingNote);

                            result.Updated++;
                        }
                        else
                        {
                            // Content unchanged, but still update tags in case they were missed in a previous sync
                            var tagsChanged = !TagsAreEqual(existingNote.Tags, noteDto.Tags);
                            if (tagsChanged)
                            {
                                _logger.LogDebug("Updating tags for unchanged note {Slug}: [{OldTags}] -> [{NewTags}]",
                                    slug,
                                    string.Join(", ", existingNote.Tags ?? new List<string>()),
                                    string.Join(", ", noteDto.Tags ?? new List<string>()));
                                existingNote.Tags = noteDto.Tags ?? new List<string>();
                            }
                            existingNote.LastSyncedAt = DateTime.UtcNow;
                            _context.Update(existingNote);
                            result.Unchanged++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing note {Slug} from vault {VaultName}", slug, vaultName);
                        result.Failed++;
                        result.Errors.Add($"Failed to sync note '{slug}': {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Sync completed for vault {VaultName}: {Imported} imported, {Updated} updated, {Unchanged} unchanged, {Failed} failed",
                    vaultName, result.Imported, result.Updated, result.Unchanged, result.Failed);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Authentication error syncing vault {VaultName}", vaultName);
                result.Errors.Add($"Authentication error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing vault {VaultName}", vaultName);
                result.Errors.Add($"Sync error: {ex.Message}");
            }

            return result;
        }

        public async Task<List<NoteSyncResultDto>> SyncAllVaultsAsync()
        {
            var results = new List<NoteSyncResultDto>();

            var generalVaultUrl = Environment.GetEnvironmentVariable("OBSIDIAN_GENERAL_VAULT_URL") ??
                _configuration["ObsidianNoteSync:GeneralVaultUrl"];
            var generalVaultAuth = Environment.GetEnvironmentVariable("OBSIDIAN_GENERAL_VAULT_AUTH_TOKEN") ??
                _configuration["ObsidianNoteSync:GeneralVaultAuthToken"];

            if (!string.IsNullOrEmpty(generalVaultUrl))
            {
                var result = await SyncFromQuartzVaultAsync("general", generalVaultUrl, generalVaultAuth);
                results.Add(result);
            }

            var programmingVaultUrl = Environment.GetEnvironmentVariable("OBSIDIAN_PROGRAMMING_VAULT_URL") ??
                _configuration["ObsidianNoteSync:ProgrammingVaultUrl"];
            var programmingVaultAuth = Environment.GetEnvironmentVariable("OBSIDIAN_PROGRAMMING_VAULT_AUTH_TOKEN") ??
                _configuration["ObsidianNoteSync:ProgrammingVaultAuthToken"];

            if (!string.IsNullOrEmpty(programmingVaultUrl))
            {
                var result = await SyncFromQuartzVaultAsync("programming", programmingVaultUrl, programmingVaultAuth);
                results.Add(result);
            }

            return results;
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

            return new NoteSyncStatusDto
            {
                Enabled = enabled,
                IntervalHours = intervalHours,
                GeneralVaultUrl = generalVaultUrl,
                ProgrammingVaultUrl = programmingVaultUrl,
                GeneralVaultHasAuth = !string.IsNullOrEmpty(generalVaultAuth),
                ProgrammingVaultHasAuth = !string.IsNullOrEmpty(programmingVaultAuth),
                LastSyncGeneral = lastSyncGeneral,
                LastSyncProgramming = lastSyncProgramming,
                TotalNotesGeneral = totalGeneral,
                TotalNotesProgramming = totalProgramming
            };
        }

        // ============================================
        // Helper Methods
        // ============================================

        private async Task IndexNoteInTypesenseAsync(Note note)
        {
            if (_typesenseService == null) return;

            try
            {
                var linkedCount = await _context.MediaItemNotes.CountAsync(min => min.NoteId == note.Id);

                await _typesenseService.IndexNoteAsync(
                    note.Id,
                    note.Slug,
                    note.Title,
                    note.Content,
                    note.Description,
                    note.VaultName,
                    note.SourceUrl,
                    note.Tags,
                    note.DateImported,
                    note.NoteDate,
                    linkedCount
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index note {Id} in Typesense", note.Id);
            }
        }

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

        private static bool TagsAreEqual(List<string>? tags1, List<string>? tags2)
        {
            var list1 = tags1 ?? new List<string>();
            var list2 = tags2 ?? new List<string>();

            if (list1.Count != list2.Count) return false;

            return list1.OrderBy(t => t).SequenceEqual(list2.OrderBy(t => t));
        }
    }
}
