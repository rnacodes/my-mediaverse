using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class MixlistService : IMixlistService
    {
        private readonly IApplicationDbContext _context;
        private readonly IThumbnailStorageService _thumbnailStorage;
        private readonly ILogger<MixlistService> _logger;

        public MixlistService(
            IApplicationDbContext context,
            IThumbnailStorageService thumbnailStorage,
            ILogger<MixlistService> logger)
        {
            _context = context;
            _thumbnailStorage = thumbnailStorage;
            _logger = logger;
        }

        public async Task<IReadOnlyList<MixlistResponseDto>> GetAllMixlistsAsync()
        {
            return await _context.Mixlists
                .AsNoTracking()
                .AsSplitQuery()
                .Select(m => new MixlistResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    DateCreated = m.DateCreated,
                    Thumbnail = m.Thumbnail,
                    MediaItemIds = m.MediaItems.Select(mi => mi.Id).ToArray(),
                    MediaItems = m.MediaItems.Select(mi => new MediaItemSummary
                    {
                        Id = mi.Id,
                        Title = mi.Title,
                        Description = mi.Description,
                        MediaType = mi.MediaType,
                        Thumbnail = mi.Thumbnail
                    }).ToArray(),
                    Topics = m.Topics.Select(t => t.Name).ToArray(),
                    Genres = m.Genres.Select(g => g.Name).ToArray()
                })
                .ToListAsync();
        }

        public async Task<MixlistResponseDto?> GetMixlistAsync(Guid id)
        {
            return await _context.Mixlists
                .AsNoTracking()
                .AsSplitQuery()
                .Where(m => m.Id == id)
                .Select(m => new MixlistResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    DateCreated = m.DateCreated,
                    Thumbnail = m.Thumbnail,
                    MediaItemIds = m.MediaItems.Select(mi => mi.Id).ToArray(),
                    MediaItems = m.MediaItems.Select(mi => new MediaItemSummary
                    {
                        Id = mi.Id,
                        Title = mi.Title,
                        Description = mi.Description,
                        MediaType = mi.MediaType,
                        Thumbnail = mi.Thumbnail
                    }).ToArray(),
                    LinkedNotes = m.MixlistNotes.Select(mn => new LinkedNoteDto
                    {
                        Id = mn.Note.Id,
                        Slug = mn.Note.Slug,
                        Title = mn.Note.Title,
                        Description = mn.Note.Description,
                        VaultName = mn.Note.VaultName,
                        SourceUrl = mn.Note.SourceUrl,
                        Tags = mn.Note.Tags,
                        LinkedAt = mn.LinkedAt,
                        LinkDescription = mn.LinkDescription
                    }).ToArray(),
                    Topics = m.Topics.Select(t => t.Name).ToArray(),
                    Genres = m.Genres.Select(g => g.Name).ToArray()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<MixlistResponseDto>> SearchMixlistsAsync(string query)
        {
            var searchQuery = query.ToLower();
            return await _context.Mixlists
                .AsNoTracking()
                .AsSplitQuery()
                .Where(m =>
                    m.Name.ToLower().Contains(searchQuery) ||
                    (m.Description != null && m.Description.ToLower().Contains(searchQuery))
                )
                .Select(m => new MixlistResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    DateCreated = m.DateCreated,
                    Thumbnail = m.Thumbnail,
                    MediaItemIds = m.MediaItems.Select(mi => mi.Id).ToArray(),
                    MediaItems = m.MediaItems.Select(mi => new MediaItemSummary
                    {
                        Id = mi.Id,
                        Title = mi.Title,
                        Description = mi.Description,
                        MediaType = mi.MediaType,
                        Thumbnail = mi.Thumbnail
                    }).ToArray(),
                    Topics = m.Topics.Select(t => t.Name).ToArray(),
                    Genres = m.Genres.Select(g => g.Name).ToArray()
                })
                .ToListAsync();
        }

        public async Task<MixlistResponseDto> CreateMixlistAsync(CreateMixlistDto dto)
        {
            var mixlist = new Mixlist
            {
                Name = dto.Name,
                Description = dto.Description,
                Thumbnail = dto.Thumbnail,
                DateCreated = DateTime.UtcNow
            };

            _context.Add(mixlist);
            await _context.SaveChangesAsync();

            var topicNames = await AddTopicsAsync(mixlist, dto.Topics);
            var genreNames = await AddGenresAsync(mixlist, dto.Genres);
            await _context.SaveChangesAsync();

            return new MixlistResponseDto
            {
                Id = mixlist.Id,
                Name = mixlist.Name,
                Description = mixlist.Description,
                DateCreated = mixlist.DateCreated,
                Thumbnail = mixlist.Thumbnail,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItems = Array.Empty<MediaItemSummary>(),
                Topics = topicNames.ToArray(),
                Genres = genreNames.ToArray()
            };
        }

        public async Task<AddMediaToMixlistResult> AddMediaItemToMixlistAsync(Guid mixlistId, Guid mediaItemId)
        {
            var result = new AddMediaToMixlistResult();

            var mixlistInfo = await _context.Mixlists
                .AsNoTracking()
                .Where(m => m.Id == mixlistId)
                .Select(m => new { m.Id, m.Name, m.Description, m.Thumbnail, m.DateCreated })
                .FirstOrDefaultAsync();

            if (mixlistInfo == null)
            {
                return result;
            }

            result.MixlistFound = true;
            result.MixlistName = mixlistInfo.Name;

            var mediaItemTitle = await _context.MediaItems
                .AsNoTracking()
                .Where(m => m.Id == mediaItemId)
                .Select(m => m.Title)
                .FirstOrDefaultAsync();

            if (mediaItemTitle == null)
            {
                return result;
            }

            result.MediaItemFound = true;
            result.MediaItemTitle = mediaItemTitle;

            var alreadyInMixlist = await _context.Mixlists
                .AsNoTracking()
                .Where(m => m.Id == mixlistId)
                .SelectMany(m => m.MediaItems.Select(mi => mi.Id))
                .AnyAsync(id => id == mediaItemId);

            if (alreadyInMixlist)
            {
                result.AlreadyInMixlist = true;
                return result;
            }

            var mixlist = await _context.FindAsync<Mixlist>(mixlistId);
            var mediaItem = await _context.FindAsync<BaseMediaItem>(mediaItemId);
            mixlist!.MediaItems.Add(mediaItem!);
            await _context.SaveChangesAsync();

            return result;
        }

        public async Task<RemoveMediaFromMixlistResult> RemoveMediaItemFromMixlistAsync(Guid mixlistId, Guid mediaItemId)
        {
            var result = new RemoveMediaFromMixlistResult();

            var mixlistInfo = await _context.Mixlists
                .AsNoTracking()
                .Where(m => m.Id == mixlistId)
                .Select(m => new { m.Id, m.Name, m.Description, m.Thumbnail, m.DateCreated })
                .FirstOrDefaultAsync();

            if (mixlistInfo == null)
            {
                return result;
            }

            result.MixlistFound = true;
            result.MixlistName = mixlistInfo.Name;

            var isInMixlist = await _context.Mixlists
                .AsNoTracking()
                .Where(m => m.Id == mixlistId)
                .SelectMany(m => m.MediaItems.Select(mi => mi.Id))
                .AnyAsync(id => id == mediaItemId);

            if (!isInMixlist)
            {
                return result;
            }

            result.MediaInMixlist = true;

            var mixlist = await _context.Mixlists
                .Include(m => m.MediaItems.Where(mi => mi.Id == mediaItemId))
                .FirstAsync(m => m.Id == mixlistId);

            mixlist.MediaItems.Remove(mixlist.MediaItems.First());
            await _context.SaveChangesAsync();

            return result;
        }

        public async Task<MixlistResponseDto?> UpdateMixlistAsync(Guid id, UpdateMixlistDto dto)
        {
            var mixlist = await _context.Mixlists
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mixlist == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
                mixlist.Name = dto.Name;
            if (dto.Description != null)
                mixlist.Description = dto.Description;
            if (dto.Thumbnail != null)
                mixlist.Thumbnail = dto.Thumbnail;

            List<string> topicNames;
            if (dto.Topics != null)
            {
                mixlist.Topics.Clear();
                topicNames = await AddTopicsAsync(mixlist, dto.Topics);
            }
            else
            {
                topicNames = mixlist.Topics.Select(t => t.Name).ToList();
            }

            List<string> genreNames;
            if (dto.Genres != null)
            {
                mixlist.Genres.Clear();
                genreNames = await AddGenresAsync(mixlist, dto.Genres);
            }
            else
            {
                genreNames = mixlist.Genres.Select(g => g.Name).ToList();
            }

            await _context.SaveChangesAsync();

            return new MixlistResponseDto
            {
                Id = mixlist.Id,
                Name = mixlist.Name,
                Description = mixlist.Description,
                DateCreated = mixlist.DateCreated,
                Thumbnail = mixlist.Thumbnail,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItems = Array.Empty<MediaItemSummary>(),
                Topics = topicNames.ToArray(),
                Genres = genreNames.ToArray()
            };
        }

        public async Task<bool> DeleteMixlistAsync(Guid id)
        {
            var mixlist = await _context.Mixlists
                .Include(m => m.MediaItems)
                .Include(m => m.MixlistNotes)
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mixlist == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(mixlist.Thumbnail))
            {
                await _thumbnailStorage.DeleteAsync(mixlist.Thumbnail);
            }

            mixlist.MediaItems.Clear();
            mixlist.MixlistNotes.Clear();
            mixlist.Topics.Clear();
            mixlist.Genres.Clear();

            _context.Remove(mixlist);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<LinkNoteToMixlistResult> LinkNoteToMixlistAsync(Guid mixlistId, LinkNoteToMixlistDto dto)
        {
            var result = new LinkNoteToMixlistResult();

            var mixlistExists = await _context.Mixlists
                .AsNoTracking()
                .AnyAsync(m => m.Id == mixlistId);

            if (!mixlistExists)
            {
                return result;
            }

            result.MixlistFound = true;

            var noteExists = await _context.Notes
                .AsNoTracking()
                .AnyAsync(n => n.Id == dto.NoteId);

            if (!noteExists)
            {
                return result;
            }

            result.NoteFound = true;

            var alreadyLinked = await _context.MixlistNotes
                .AsNoTracking()
                .AnyAsync(mn => mn.MixlistId == mixlistId && mn.NoteId == dto.NoteId);

            if (alreadyLinked)
            {
                result.AlreadyLinked = true;
                return result;
            }

            var mixlistNote = new MixlistNote
            {
                MixlistId = mixlistId,
                NoteId = dto.NoteId,
                LinkedAt = DateTime.UtcNow,
                LinkDescription = dto.LinkDescription
            };

            _context.Add(mixlistNote);
            await _context.SaveChangesAsync();

            return result;
        }

        public async Task<bool> UnlinkNoteFromMixlistAsync(Guid mixlistId, Guid noteId)
        {
            var mixlistNote = await _context.MixlistNotes
                .FirstOrDefaultAsync(mn => mn.MixlistId == mixlistId && mn.NoteId == noteId);

            if (mixlistNote == null)
            {
                return false;
            }

            _context.Remove(mixlistNote);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<GetNotesForMixlistResult> GetNotesForMixlistAsync(Guid mixlistId)
        {
            var result = new GetNotesForMixlistResult();
            var mixlistExists = await _context.Mixlists
                .AsNoTracking()
                .AnyAsync(m => m.Id == mixlistId);

            if (!mixlistExists)
            {
                return result;
            }

            result.MixlistFound = true;
            result.Notes = await _context.MixlistNotes
                .AsNoTracking()
                .Where(mn => mn.MixlistId == mixlistId)
                .OrderByDescending(mn => mn.LinkedAt)
                .Select(mn => new LinkedNoteDto
                {
                    Id = mn.Note.Id,
                    Slug = mn.Note.Slug,
                    Title = mn.Note.Title,
                    Description = mn.Note.Description,
                    VaultName = mn.Note.VaultName,
                    SourceUrl = mn.Note.SourceUrl,
                    Tags = mn.Note.Tags,
                    LinkedAt = mn.LinkedAt,
                    LinkDescription = mn.LinkDescription
                })
                .ToListAsync();

            return result;
        }

        public async Task<ImportMixlistsResult> ImportMixlistsAsync(IReadOnlyList<ImportMixlistDto> importDtos)
        {
            var result = new ImportMixlistsResult();

            foreach (var dto in importDtos)
            {
                try
                {
                    var mediaItemIds = !string.IsNullOrEmpty(dto.MediaItemIds)
                        ? dto.MediaItemIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                            .Where(id => Guid.TryParse(id.Trim(), out _))
                            .Select(id => Guid.Parse(id.Trim()))
                            .ToArray()
                        : Array.Empty<Guid>();

                    var mixlist = new Mixlist
                    {
                        Name = dto.Name,
                        Description = dto.Description,
                        Thumbnail = dto.Thumbnail,
                        DateCreated = DateTime.UtcNow
                    };

                    _context.Add(mixlist);
                    await _context.SaveChangesAsync();

                    if (mediaItemIds.Any())
                    {
                        var mediaItems = await _context.MediaItems
                            .Where(m => mediaItemIds.Contains(m.Id))
                            .ToListAsync();

                        foreach (var mediaItem in mediaItems)
                        {
                            mixlist.MediaItems.Add(mediaItem);
                        }
                        await _context.SaveChangesAsync();
                    }

                    result.ImportedMixlists.Add(new
                    {
                        Id = mixlist.Id,
                        Name = mixlist.Name,
                        MediaItemCount = mixlist.MediaItems.Count,
                        Message = "Mixlist imported successfully"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import mixlist '{Name}'", dto.Name);
                    result.Errors.Add($"Failed to import mixlist '{dto.Name}': {ex.Message}");
                }
            }

            return result;
        }

        public async Task<ExportMixlistResult> ExportMixlistAsync(Guid id)
        {
            var mixlist = await _context.Mixlists
                .Include(m => m.MediaItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mixlist == null)
            {
                return new ExportMixlistResult { MixlistFound = false };
            }

            var rows = new List<object>
            {
                new
                {
                    Id = mixlist.Id,
                    Name = mixlist.Name,
                    Description = mixlist.Description ?? "",
                    DateCreated = mixlist.DateCreated.ToString("yyyy-MM-dd"),
                    Thumbnail = mixlist.Thumbnail ?? "",
                    MediaItemIds = string.Join(";", mixlist.MediaItems.Select(mi => mi.Id)),
                    MediaItemTitles = string.Join(";", mixlist.MediaItems.Select(mi => mi.Title)),
                    MediaItemTypes = string.Join(";", mixlist.MediaItems.Select(mi => mi.MediaType.ToString()))
                }
            };

            var content = WriteCsv(rows);
            var fileName = $"mixlist-{mixlist.Name.Replace(" ", "-")}-{DateTime.Now:yyyyMMdd}.csv";

            return new ExportMixlistResult
            {
                MixlistFound = true,
                Content = content,
                FileName = fileName
            };
        }

        public async Task<ExportMixlistResult> ExportAllMixlistsAsync()
        {
            var mixlists = await _context.Mixlists
                .Include(m => m.MediaItems)
                .ToListAsync();

            var rows = mixlists.Select(mixlist => (object)new
            {
                Id = mixlist.Id,
                Name = mixlist.Name,
                Description = mixlist.Description ?? "",
                DateCreated = mixlist.DateCreated.ToString("yyyy-MM-dd"),
                Thumbnail = mixlist.Thumbnail ?? "",
                MediaItemIds = string.Join(";", mixlist.MediaItems.Select(mi => mi.Id)),
                MediaItemTitles = string.Join(";", mixlist.MediaItems.Select(mi => mi.Title)),
                MediaItemTypes = string.Join(";", mixlist.MediaItems.Select(mi => mi.MediaType.ToString()))
            }).ToList();

            var content = WriteCsv(rows);
            var fileName = $"all-mixlists-{DateTime.Now:yyyyMMdd}.csv";

            return new ExportMixlistResult
            {
                MixlistFound = true,
                Content = content,
                FileName = fileName
            };
        }

        private async Task<List<string>> AddTopicsAsync(Mixlist mixlist, string[]? topics)
        {
            if (topics == null || topics.Length == 0)
            {
                return new List<string>();
            }

            var normalizedNames = topics
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLower())
                .Distinct()
                .ToList();

            if (normalizedNames.Count == 0)
            {
                return new List<string>();
            }

            var existingTopics = await _context.Topics
                .AsNoTracking()
                .Where(t => normalizedNames.Contains(t.Name))
                .ToListAsync();

            var existingNames = existingTopics.Select(t => t.Name).ToHashSet();
            var newNames = normalizedNames.Where(n => !existingNames.Contains(n)).ToList();

            if (newNames.Count > 0)
            {
                var newTopics = newNames.Select(name => new Topic { Name = name }).ToList();
                _context.AddRange(newTopics);
                await _context.SaveChangesAsync();
                existingTopics.AddRange(newTopics);
            }

            var topicIds = existingTopics.Select(t => t.Id).ToList();
            var trackedTopics = await _context.Topics
                .Where(t => topicIds.Contains(t.Id))
                .ToListAsync();

            foreach (var topic in trackedTopics)
            {
                if (!mixlist.Topics.Any(t => t.Id == topic.Id))
                {
                    mixlist.Topics.Add(topic);
                }
            }

            return normalizedNames;
        }

        private async Task<List<string>> AddGenresAsync(Mixlist mixlist, string[]? genres)
        {
            if (genres == null || genres.Length == 0)
            {
                return new List<string>();
            }

            var normalizedNames = genres
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim().ToLower())
                .Distinct()
                .ToList();

            if (normalizedNames.Count == 0)
            {
                return new List<string>();
            }

            var existingGenres = await _context.Genres
                .AsNoTracking()
                .Where(g => normalizedNames.Contains(g.Name))
                .ToListAsync();

            var existingNames = existingGenres.Select(g => g.Name).ToHashSet();
            var newNames = normalizedNames.Where(n => !existingNames.Contains(n)).ToList();

            if (newNames.Count > 0)
            {
                var newGenres = newNames.Select(name => new Genre { Name = name }).ToList();
                _context.AddRange(newGenres);
                await _context.SaveChangesAsync();
                existingGenres.AddRange(newGenres);
            }

            var genreIds = existingGenres.Select(g => g.Id).ToList();
            var trackedGenres = await _context.Genres
                .Where(g => genreIds.Contains(g.Id))
                .ToListAsync();

            foreach (var genre in trackedGenres)
            {
                if (!mixlist.Genres.Any(g => g.Id == genre.Id))
                {
                    mixlist.Genres.Add(genre);
                }
            }

            return normalizedNames;
        }

        private static byte[] WriteCsv(IEnumerable<object> rows)
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(rows);
            return Encoding.UTF8.GetBytes(writer.ToString());
        }
    }
}
