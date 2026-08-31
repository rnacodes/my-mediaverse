using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class HighlightService : IHighlightService
    {
        private const int MaxExportPages = 100;

        private readonly IApplicationDbContext _context;
        private readonly IReadwiseApiClient _readwiseClient;
        private readonly ITypesenseService _typesenseService;
        private readonly ILogger<HighlightService> _logger;

        // Delay between export pages to respect Readwise rate limits (20 req/min for
        // list endpoints).
        internal int ExportPageDelayMs { get; set; } = 3000;

        public HighlightService(
            IApplicationDbContext context,
            IReadwiseApiClient readwiseClient,
            ITypesenseService typesenseService,
            ILogger<HighlightService> logger)
        {
            _context = context;
            _readwiseClient = readwiseClient;
            _typesenseService = typesenseService;
            _logger = logger;
        }

        /// <summary>
        /// Writes a highlight's tags and mirrors them onto its Topic links. Tags stay the
        /// user-facing field; topic links are the derived, queryable form of the same data
        /// (topics only — a highlight's genre is its linked media item's).
        /// </summary>
        private async Task ApplyTagsAsync(Highlight highlight, IEnumerable<string?>? rawTags, TopicResolver topics)
        {
            var normalized = TagNormalizer.NormalizeList(rawTags);
            highlight.Tags = normalized.Count > 0 ? string.Join(",", normalized) : null;

            foreach (var stale in highlight.Topics.Where(t => !normalized.Contains(t.Name)).ToList())
            {
                highlight.Topics.Remove(stale);
            }

            foreach (var name in normalized)
            {
                if (highlight.Topics.Any(t => t.Name == name))
                    continue;

                var topic = await topics.GetOrCreateAsync(name);
                if (topic != null)
                    highlight.Topics.Add(topic);
            }
        }

        /// <summary>
        /// One-shot (idempotent) backfill: derives Topic links from every stored tags
        /// string and re-normalizes the string itself (legacy rows can carry untrimmed
        /// tags written before normalization was consistent). Returns how many
        /// highlights changed.
        /// </summary>
        public async Task<int> BackfillHighlightTopicsAsync()
        {
            const int pageSize = 200;
            var topics = new TopicResolver(_context);
            var updated = 0;
            var lastId = Guid.Empty;

            while (true)
            {
                var page = await _context.Highlights
                    .Include(h => h.Topics)
                    .Where(h => h.Tags != null && h.Id.CompareTo(lastId) > 0)
                    .OrderBy(h => h.Id)
                    .Take(pageSize)
                    .ToListAsync();

                if (page.Count == 0)
                    break;

                foreach (var highlight in page)
                {
                    var tagsBefore = highlight.Tags;
                    var topicCountBefore = highlight.Topics.Count;

                    await ApplyTagsAsync(highlight, TagNormalizer.SplitStored(highlight.Tags), topics);

                    if (highlight.Tags != tagsBefore || highlight.Topics.Count != topicCountBefore)
                        updated++;
                }

                await _context.SaveChangesAsync();
                lastId = page[^1].Id;

                if (page.Count < pageSize)
                    break;
            }

            _logger.LogInformation("Topic backfill complete: {Count} highlights updated", updated);
            return updated;
        }

        // A malformed date is not worth failing the highlight (or the run) over; store
        // null and move on. Same policy as the Reader article sync's published_date.
        private DateTime? ParseHighlightedAt(string? value, int readwiseId)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                return parsed.ToUniversalTime();

            _logger.LogWarning("Could not parse highlighted_at '{Value}' for Readwise highlight {ReadwiseId}; storing null",
                value, readwiseId);
            return null;
        }

        /// <summary>
        /// Best-effort removal of a highlight's search document via the shared
        /// eager-delete helper — the bulk reindex's ID-diff reconcile is the backstop.
        /// </summary>
        private Task TryRemoveFromSearchIndexAsync(Guid highlightId) =>
            SearchIndexCleanup.TryDeleteAsync(
                () => _typesenseService.DeleteHighlightAsync(highlightId), _logger, "highlight", highlightId);

        public async Task<IEnumerable<Highlight>> GetAllHighlightsAsync()
        {
            return await _context.Highlights
                .AsNoTracking()
                .AsSplitQuery()
                .Include(h => h.Article)
                .Include(h => h.Book)
                .OrderByDescending(h => h.HighlightedAt ?? h.CreatedAt)
                .ToListAsync();
        }

        public async Task<Highlight?> GetHighlightByIdAsync(Guid id)
        {
            return await _context.Highlights
                .AsNoTracking()
                .AsSplitQuery()
                .Include(h => h.Article)
                .Include(h => h.Book)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<IEnumerable<Highlight>> GetHighlightsByArticleIdAsync(Guid articleId)
        {
            return await _context.Highlights
                .AsNoTracking()
                .Where(h => h.ArticleId == articleId)
                .OrderBy(h => h.Location ?? 0)
                .ToListAsync();
        }

        public async Task<IEnumerable<Highlight>> GetHighlightsByBookIdAsync(Guid bookId)
        {
            return await _context.Highlights
                .AsNoTracking()
                .Where(h => h.BookId == bookId)
                .OrderBy(h => h.Location ?? 0)
                .ToListAsync();
        }

        public async Task<IEnumerable<Highlight>> GetHighlightsByTagAsync(string tag)
        {
            // Tags are stored as a comma-joined string (trimmed + lowercased on every write
            // path).
            var wrappedTag = "," + tag.Trim().ToLowerInvariant() + ",";
            return await _context.Highlights
                .AsNoTracking()
                .Where(h => h.Tags != null && ("," + h.Tags + ",").Contains(wrappedTag))
                .OrderByDescending(h => h.HighlightedAt ?? h.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Highlight>> GetUnlinkedHighlightsAsync()
        {
            return await _context.Highlights
                .AsNoTracking()
                .Where(h => h.ArticleId == null && h.BookId == null)
                .OrderByDescending(h => h.HighlightedAt ?? h.CreatedAt)
                .ToListAsync();
        }

        public async Task<Highlight> CreateHighlightAsync(CreateHighlightDto dto)
        {
            // Clean text to prevent CSS/HTML contamination
            var cleanedText = HtmlTextCleaner.Clean(dto.Text);

            var highlight = new Highlight
            {
                Id = Guid.NewGuid(),
                Text = cleanedText,
                Note = dto.Note,
                Title = dto.Title,
                Author = dto.Author,
                Category = dto.Category?.ToLowerInvariant(),
                SourceUrl = dto.SourceUrl,
                ArticleId = dto.ArticleId,
                BookId = dto.BookId,
                Location = dto.Location,
                LocationType = dto.LocationType,
                HighlightedAt = dto.HighlightedAt,
                CreatedAt = DateTime.UtcNow
            };

            await ApplyTagsAsync(highlight, dto.Tags, new TopicResolver(_context));

            _context.Add(highlight);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created highlight {HighlightId}", highlight.Id);

            return highlight;
        }

        public async Task<Highlight> UpdateHighlightAsync(Guid id, UpdateHighlightDto dto)
        {
            var highlight = await _context.Highlights
                .Include(h => h.Article)
                .Include(h => h.Book)
                .Include(h => h.Topics)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (highlight == null)
            {
                throw new InvalidOperationException($"Highlight with ID {id} not found");
            }

            // Null = leave unchanged; empty string = clear the optional field.
            if (dto.Text != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Text))
                {
                    throw new ArgumentException("Highlight text cannot be empty.");
                }
                // Clean text to prevent CSS/HTML contamination
                highlight.Text = HtmlTextCleaner.Clean(dto.Text);
            }

            if (dto.Note != null) highlight.Note = EmptyToNull(dto.Note);
            if (dto.Title != null) highlight.Title = EmptyToNull(dto.Title);
            if (dto.Author != null) highlight.Author = EmptyToNull(dto.Author);
            if (dto.Category != null) highlight.Category = EmptyToNull(dto.Category)?.ToLowerInvariant();
            if (dto.SourceUrl != null) highlight.SourceUrl = EmptyToNull(dto.SourceUrl);
            if (dto.LocationType != null) highlight.LocationType = EmptyToNull(dto.LocationType);
            if (dto.Color != null) highlight.Color = EmptyToNull(dto.Color);
            if (dto.Location.HasValue) highlight.Location = dto.Location;
            if (dto.HighlightedAt.HasValue) highlight.HighlightedAt = dto.HighlightedAt;
            if (dto.IsFavorite.HasValue) highlight.IsFavorite = dto.IsFavorite.Value;

            if (dto.Tags != null)
            {
                // Empty list = clear tags (and their derived topic links).
                await ApplyTagsAsync(highlight, dto.Tags, new TopicResolver(_context));
            }

            highlight.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated highlight {HighlightId}", highlight.Id);

            return highlight;
        }

        public async Task<Highlight> SetHighlightLinkAsync(Guid id, Guid? articleId, Guid? bookId)
        {
            if (articleId.HasValue && bookId.HasValue)
            {
                throw new ArgumentException("A highlight can link to an article or a book, not both.");
            }

            var highlight = await _context.Highlights
                .Include(h => h.Article)
                .Include(h => h.Book)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (highlight == null)
            {
                throw new InvalidOperationException($"Highlight with ID {id} not found");
            }

            if (articleId.HasValue)
            {
                var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == articleId.Value)
                    ?? throw new InvalidOperationException($"Article with ID {articleId} not found");
                highlight.ArticleId = article.Id;
                highlight.Article = article;
                highlight.BookId = null;
                highlight.Book = null;
            }
            else if (bookId.HasValue)
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId.Value)
                    ?? throw new InvalidOperationException($"Book with ID {bookId} not found");
                highlight.BookId = book.Id;
                highlight.Book = book;
                highlight.ArticleId = null;
                highlight.Article = null;
            }
            else
            {
                highlight.ArticleId = null;
                highlight.Article = null;
                highlight.BookId = null;
                highlight.Book = null;
            }

            highlight.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated media link for highlight {HighlightId}", highlight.Id);

            return highlight;
        }

        private static string? EmptyToNull(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;

        public async Task<bool> DeleteHighlightAsync(Guid id)
        {
            var highlight = await _context.Highlights
                .FirstOrDefaultAsync(h => h.Id == id);
            if (highlight == null)
            {
                return false;
            }

            _context.Remove(highlight);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted highlight {HighlightId}", id);

            await TryRemoveFromSearchIndexAsync(id);

            return true;
        }

        public async Task<int> BulkDeleteHighlightsAsync(List<Guid> ids)
        {
            var highlights = await _context.Highlights
                .Where(h => ids.Contains(h.Id))
                .ToListAsync();

            if (highlights.Count == 0)
            {
                return 0;
            }

            foreach (var highlight in highlights)
            {
                _context.Remove(highlight);
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk deleted {Count} highlights", highlights.Count);

            // Best-effort search index cleanup after the DB delete has committed; the
            // bulk reindex's ID-diff reconcile is the backstop.
            foreach (var highlight in highlights)
            {
                await TryRemoveFromSearchIndexAsync(highlight.Id);
            }

            return highlights.Count;
        }

        public async Task<HighlightSyncResultDto> SyncHighlightsFromReadwiseAsync()
        {
            return await SyncHighlightsUsingExportAsync(null);
        }

        public async Task<HighlightSyncResultDto> SyncHighlightsIncrementalAsync(DateTime lastSyncDate)
        {
            var updatedAfter = lastSyncDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return await SyncHighlightsUsingExportAsync(updatedAfter);
        }

        /// <summary>
        /// Uses the /export/ endpoint which returns books with nested highlights.
        /// This is more efficient than fetching highlights and books separately.
        /// Also auto-links highlights to articles during import.
        /// </summary>
        private async Task<HighlightSyncResultDto> SyncHighlightsUsingExportAsync(string? updatedAfter)
        {
            var result = new HighlightSyncResultDto
            {
                StartedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting highlight sync from Readwise using export endpoint (updatedAfter: {UpdatedAfter})",
                    updatedAfter ?? "full sync");

                string? pageCursor = null;
                var hasMore = true;
                var iteration = 0;

                while (hasMore && iteration < MaxExportPages)
                {
                    _logger.LogInformation("Fetching export page {Iteration}", iteration + 1);

                    var response = await _readwiseClient.GetExportAsync(
                        updatedAfter: updatedAfter,
                        pageCursor: pageCursor);

                    if (response.results.Count == 0)
                    {
                        hasMore = false;
                        break;
                    }

                    // Process each book with its nested highlights
                    foreach (var bookDto in response.results)
                    {
                        await ProcessExportBookWithHighlightsAsync(bookDto, result);
                    }

                    hasMore = !string.IsNullOrEmpty(response.nextPageCursor);
                    pageCursor = response.nextPageCursor;
                    iteration++;

                    // Pause between pages to respect rate limits; skipped after the last page
                    if (hasMore && ExportPageDelayMs > 0)
                    {
                        await Task.Delay(ExportPageDelayMs);
                    }
                }

                if (hasMore)
                {
                    result.WarningMessage =
                        $"Sync stopped at the {MaxExportPages}-page safety limit before reaching the end of the Readwise export; some highlights were not synced.";
                    _logger.LogWarning("Highlight sync hit the {MaxPages}-page safety limit with more export pages remaining", MaxExportPages);
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = true;

                _logger.LogInformation("Completed highlight sync. Created: {Created}, Updated: {Updated}, Linked: {Linked}, Deleted: {Deleted}",
                    result.CreatedCount, result.UpdatedCount, result.LinkedCount, result.DeletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing highlights from Readwise");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

        /// <summary>
        /// Process a book with nested highlights from the export endpoint.
        /// Book data is already included, no separate API call needed.
        /// </summary>
        private async Task ProcessExportBookWithHighlightsAsync(
            Shared.DTOs.Readwise.ReadwiseExportBookDto bookDto,
            HighlightSyncResultDto result)
        {
            var removedFromDb = new List<Guid>();
            var topics = new TopicResolver(_context);

            foreach (var highlightDto in bookDto.highlights)
            {
                // Tombstones: Readwise marks deleted sources/highlights with is_deleted
                // (and hidden ones with is_discard) instead of omitting them.
                if (bookDto.is_deleted || highlightDto.is_deleted || highlightDto.is_discard)
                {
                    var doomed = await _context.Highlights
                        .FirstOrDefaultAsync(h => h.ReadwiseId == highlightDto.id);
                    if (doomed != null)
                    {
                        _context.Remove(doomed);
                        result.DeletedCount++;
                        removedFromDb.Add(doomed.Id);
                        _logger.LogInformation(
                            "Deleted highlight {HighlightId} (ReadwiseId {ReadwiseId}): removed or discarded in Readwise",
                            doomed.Id, highlightDto.id);
                    }
                    continue;
                }

                // Clean HTML/CSS from highlight text
                var cleanedText = HtmlTextCleaner.Clean(highlightDto.text);

                // Check if highlight already exists by ReadwiseId
                var existing = await _context.Highlights
                    .Include(h => h.Article)
                    .Include(h => h.Book)
                    .Include(h => h.Topics)
                    .FirstOrDefaultAsync(h => h.ReadwiseId == highlightDto.id);

                if (existing != null)
                {
                    // Update existing highlight
                    existing.Text = cleanedText;
                    existing.Note = highlightDto.note;
                    existing.Location = highlightDto.location;
                    existing.LocationType = highlightDto.location_type;
                    existing.Color = highlightDto.color;
                    existing.IsFavorite = highlightDto.is_favorite;
                    await ApplyTagsAsync(existing, highlightDto.tags?.Select(t => t.name), topics);
                    existing.UpdatedAt = DateTime.UtcNow;

                    result.UpdatedCount++;
                }
                else
                {
                    // Create new highlight with book data already available (no extra API call)
                    var highlight = new Highlight
                    {
                        Id = Guid.NewGuid(),
                        ReadwiseId = highlightDto.id,
                        Text = cleanedText,
                        Note = highlightDto.note,
                        Title = bookDto.title,
                        Author = bookDto.author,
                        Category = bookDto.category?.ToLowerInvariant(),
                        SourceUrl = bookDto.source_url,
                        ImageUrl = bookDto.cover_image_url,
                        HighlightUrl = highlightDto.url,
                        Location = highlightDto.location,
                        LocationType = highlightDto.location_type,
                        HighlightedAt = ParseHighlightedAt(highlightDto.highlighted_at, highlightDto.id),
                        ReadwiseBookId = bookDto.user_book_id,
                        Color = highlightDto.color,
                        IsFavorite = highlightDto.is_favorite,
                        SourceType = bookDto.source,
                        CreatedAt = DateTime.UtcNow
                    };

                    await ApplyTagsAsync(highlight, highlightDto.tags?.Select(t => t.name), topics);

                    // Auto-link to source media: URL(s) first, then title/title+author
                    var match = await HighlightLinkMatcher.ResolveAsync(
                        _context,
                        new[] { bookDto.source_url, bookDto.unique_url },
                        bookDto.title,
                        bookDto.author,
                        bookDto.category);

                    if (match.Article != null)
                    {
                        highlight.ArticleId = match.Article.Id;
                        highlight.Article = match.Article;
                        result.LinkedCount++;
                        _logger.LogDebug("Auto-linked highlight {HighlightId} to article {ArticleId} (title: {Title})",
                            highlight.Id, match.Article.Id, match.Article.Title);
                    }
                    else if (match.Book != null)
                    {
                        highlight.BookId = match.Book.Id;
                        highlight.Book = match.Book;
                        result.LinkedCount++;
                        _logger.LogDebug("Auto-linked highlight {HighlightId} to book {BookId}",
                            highlight.Id, match.Book.Id);
                    }
                    else if (bookDto.category?.ToLowerInvariant() == "articles")
                    {
                        // Log unlinked article highlights for debugging
                        _logger.LogDebug("Could not link highlight to article. Source URL: {SourceUrl}, Title: {Title}",
                            bookDto.source_url, bookDto.title);
                    }

                    _context.Add(highlight);
                    result.CreatedCount++;
                }
            }

            await _context.SaveChangesAsync();

            // Best-effort search index cleanup for tombstoned rows, after the DB delete has
            // committed; the next bulk reindex reconciles any misses.
            foreach (var id in removedFromDb)
            {
                await TryRemoveFromSearchIndexAsync(id);
            }
        }

        /// <summary>
        /// Cleans all existing highlights by removing HTML/CSS from their text.
        /// Returns the number of highlights that were cleaned.
        /// </summary>
        public async Task<int> CleanAllHighlightTextAsync()
        {
            _logger.LogInformation("Starting to clean HTML/CSS from all highlight text");

            var cleanedCount = 0;

            try
            {
                // Page the table instead of loading it whole
                const int pageSize = 200;

                for (var skip = 0; ; skip += pageSize)
                {
                    var highlights = await _context.Highlights
                        .OrderBy(h => h.Id)
                        .Skip(skip)
                        .Take(pageSize)
                        .ToListAsync();

                    if (highlights.Count == 0)
                    {
                        break;
                    }

                    var pageCleaned = 0;
                    foreach (var highlight in highlights)
                    {
                        if (HtmlTextCleaner.ContainsHtmlOrCss(highlight.Text))
                        {
                            var cleanedText = HtmlTextCleaner.Clean(highlight.Text);
                            if (cleanedText != highlight.Text)
                            {
                                highlight.Text = cleanedText;
                                highlight.UpdatedAt = DateTime.UtcNow;
                                pageCleaned++;
                            }
                        }
                    }

                    if (pageCleaned > 0)
                    {
                        await _context.SaveChangesAsync();
                        cleanedCount += pageCleaned;
                    }

                    if (highlights.Count < pageSize)
                    {
                        break;
                    }
                }

                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Cleaned HTML/CSS from {Count} highlights", cleanedCount);
                }
                else
                {
                    _logger.LogInformation("No highlights needed HTML/CSS cleaning");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning highlight text");
                throw;
            }

            return cleanedCount;
        }

        /// <summary>Dedup key for imported highlights: case-insensitive title + cleaned text.</summary>
        private static (string TitleKey, string Text) ImportKey(string? title, string cleanedText) =>
            ((title ?? string.Empty).Trim().ToLowerInvariant(), cleanedText);

        public async Task<BulkHighlightResultDto> BulkCreateHighlightsAsync(List<CreateHighlightDto> dtos)
        {
            var result = new BulkHighlightResultDto
            {
                StartedAt = DateTime.UtcNow
            };

            // Match-by-key upsert: re-uploading the same file must update rows in place,
            // never duplicate them.
            var titleKeys = dtos
                .Select(d => (d.Title ?? string.Empty).Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            var candidates = await _context.Highlights
                .Include(h => h.Topics)
                .Where(h => titleKeys.Contains((h.Title ?? string.Empty).Trim().ToLower()))
                .ToListAsync();
            var topics = new TopicResolver(_context);
            var existingByKey = new Dictionary<(string, string), Highlight>();
            foreach (var candidate in candidates)
            {
                existingByKey.TryAdd(ImportKey(candidate.Title, candidate.Text), candidate);
            }

            var seenThisBatch = new HashSet<(string, string)>();

            foreach (var dto in dtos)
            {
                try
                {
                    var cleanedText = HtmlTextCleaner.Clean(dto.Text);
                    var key = ImportKey(dto.Title, cleanedText);

                    // The same highlight twice in one upload: import the first, skip the rest.
                    if (!seenThisBatch.Add(key))
                    {
                        result.Skipped++;
                        continue;
                    }

                    if (existingByKey.TryGetValue(key, out var existing))
                    {
                        // Update in place. Only fields the upload actually carries are
                        // touched; links and ReadwiseId are left alone unless an explicit
                        // link was supplied.
                        if (dto.Note != null) existing.Note = dto.Note;
                        if (dto.Author != null) existing.Author = dto.Author;
                        if (dto.Category != null) existing.Category = dto.Category.ToLowerInvariant();
                        if (dto.SourceUrl != null) existing.SourceUrl = dto.SourceUrl;
                        if (dto.Tags != null) await ApplyTagsAsync(existing, dto.Tags, topics);
                        if (dto.Location.HasValue) existing.Location = dto.Location;
                        if (dto.LocationType != null) existing.LocationType = dto.LocationType;
                        if (dto.HighlightedAt.HasValue) existing.HighlightedAt = dto.HighlightedAt;
                        if (dto.ArticleId.HasValue)
                        {
                            existing.ArticleId = dto.ArticleId;
                            existing.BookId = null;
                        }
                        else if (dto.BookId.HasValue)
                        {
                            existing.BookId = dto.BookId;
                            existing.ArticleId = null;
                        }
                        existing.UpdatedAt = DateTime.UtcNow;

                        result.Updated++;
                        continue;
                    }

                    var highlight = new Highlight
                    {
                        Id = Guid.NewGuid(),
                        Text = cleanedText,
                        Note = dto.Note,
                        Title = dto.Title,
                        Author = dto.Author,
                        Category = dto.Category?.ToLowerInvariant(),
                        SourceUrl = dto.SourceUrl,
                        ArticleId = dto.ArticleId,
                        BookId = dto.BookId,
                        Location = dto.Location,
                        LocationType = dto.LocationType,
                        HighlightedAt = dto.HighlightedAt,
                        CreatedAt = DateTime.UtcNow
                    };

                    await ApplyTagsAsync(highlight, dto.Tags, topics);

                    // Auto-link only when the caller didn't supply an explicit link
                    if (highlight.ArticleId == null && highlight.BookId == null)
                    {
                        var match = await HighlightLinkMatcher.ResolveAsync(
                            _context,
                            new[] { dto.SourceUrl },
                            dto.Title,
                            dto.Author,
                            dto.Category);

                        if (match.Article != null)
                        {
                            highlight.ArticleId = match.Article.Id;
                            highlight.Article = match.Article;
                            result.Linked++;
                        }
                        else if (match.Book != null)
                        {
                            highlight.BookId = match.Book.Id;
                            highlight.Book = match.Book;
                            result.Linked++;
                        }
                    }

                    _context.Add(highlight);
                    result.Created++;
                }
                catch (Exception ex)
                {
                    var preview = dto.Text.Length > 50 ? dto.Text[..50] + "..." : dto.Text;
                    result.Errors.Add($"Failed to create highlight '{preview}': {ex.Message}");
                    _logger.LogWarning(ex, "Failed to create highlight in bulk operation");
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Fatal: nothing from this batch was persisted, whatever the counters say.
                _logger.LogError(ex, "Failed to save bulk highlights to database");
                result.Success = false;
                result.ErrorMessage = $"Database save failed: {ex.Message}";
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }

            result.Success = true;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Bulk import: created {Created}, updated {Updated}, skipped {Skipped}, linked {Linked}, errors {Errors}",
                result.Created, result.Updated, result.Skipped, result.Linked, result.Errors.Count);

            return result;
        }
    }
}

