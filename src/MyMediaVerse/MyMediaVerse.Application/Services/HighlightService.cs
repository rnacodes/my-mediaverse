using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class HighlightService : IHighlightService
    {
        private readonly IApplicationDbContext _context;
        private readonly IReadwiseApiClient _readwiseClient;
        private readonly ILogger<HighlightService> _logger;

        public HighlightService(
            IApplicationDbContext context,
            IReadwiseApiClient readwiseClient,
            ILogger<HighlightService> logger)
        {
            _context = context;
            _readwiseClient = readwiseClient;
            _logger = logger;
        }

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
            var normalizedTag = tag.ToLowerInvariant();
            return await _context.Highlights
                .AsNoTracking()
                .Where(h => h.Tags != null && h.Tags.Contains(normalizedTag))
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
                Tags = dto.Tags != null ? string.Join(",", dto.Tags.Select(t => t.ToLowerInvariant())) : null,
                Location = dto.Location,
                LocationType = dto.LocationType,
                HighlightedAt = dto.HighlightedAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(highlight);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created highlight {HighlightId}", highlight.Id);

            return highlight;
        }

        public async Task<Highlight> UpdateHighlightAsync(Guid id, CreateHighlightDto dto)
        {
            var highlight = await _context.Highlights
                .Include(h => h.Article)
                .Include(h => h.Book)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (highlight == null)
            {
                throw new InvalidOperationException($"Highlight with ID {id} not found");
            }

            // Clean text to prevent CSS/HTML contamination
            highlight.Text = HtmlTextCleaner.Clean(dto.Text);
            highlight.Note = dto.Note;
            highlight.Tags = dto.Tags != null ? string.Join(",", dto.Tags.Select(t => t.ToLowerInvariant())) : null;
            highlight.ArticleId = dto.ArticleId;
            highlight.BookId = dto.BookId;
            highlight.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated highlight {HighlightId}", highlight.Id);

            return highlight;
        }

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

            return true;
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

                while (hasMore && iteration < 100) // Safety limit
                {
                    _logger.LogInformation("Fetching export page {Iteration}", iteration + 1);

                    var response = await _readwiseClient.GetExportAsync(
                        updatedAfter: updatedAfter,
                        pageCursor: pageCursor);

                    if (response.results.Count == 0)
                    {
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

                    // Small delay to respect rate limits (20 req/min for list endpoints)
                    await Task.Delay(3000);
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = true;

                _logger.LogInformation("Completed highlight sync. Created: {Created}, Updated: {Updated}, Linked: {Linked}",
                    result.CreatedCount, result.UpdatedCount, result.LinkedCount);
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
            foreach (var highlightDto in bookDto.highlights)
            {
                // Clean HTML/CSS from highlight text
                var cleanedText = HtmlTextCleaner.Clean(highlightDto.text);

                // Check if highlight already exists by ReadwiseId
                var existing = await _context.Highlights
                    .Include(h => h.Article)
                    .Include(h => h.Book)
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
                    existing.Tags = highlightDto.tags != null
                        ? string.Join(",", highlightDto.tags.Select(t => t.name.ToLowerInvariant()))
                        : null;
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
                        HighlightedAt = !string.IsNullOrEmpty(highlightDto.highlighted_at)
                            ? DateTime.Parse(highlightDto.highlighted_at, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime()
                            : null,
                        ReadwiseBookId = bookDto.user_book_id,
                        Tags = highlightDto.tags != null
                            ? string.Join(",", highlightDto.tags.Select(t => t.name.ToLowerInvariant()))
                            : null,
                        Color = highlightDto.color,
                        IsFavorite = highlightDto.is_favorite,
                        SourceType = bookDto.source,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Auto-link to article by URL (using multiple matching strategies)
                    // Try source_url first, then unique_url as fallback
                    var urlsToTry = new List<string>();
                    if (!string.IsNullOrEmpty(bookDto.source_url))
                        urlsToTry.Add(bookDto.source_url);
                    if (!string.IsNullOrEmpty(bookDto.unique_url) && bookDto.unique_url != bookDto.source_url)
                        urlsToTry.Add(bookDto.unique_url);

                    Article? article = null;
                    foreach (var urlToTry in urlsToTry)
                    {
                        var normalizedUrl = UrlNormalizer.Normalize(urlToTry);

                        // Try exact normalized match first
                        article = await _context.Articles
                            .FirstOrDefaultAsync(a =>
                                a.Link != null &&
                                EF.Functions.ILike(a.Link, normalizedUrl));

                        // If no match, try partial URL match (without protocol)
                        if (article == null)
                        {
                            var urlWithoutProtocol = normalizedUrl
                                .Replace("https://", "")
                                .Replace("http://", "");
                            article = await _context.Articles
                                .FirstOrDefaultAsync(a =>
                                    a.Link != null &&
                                    (EF.Functions.ILike(a.Link, $"%{urlWithoutProtocol}") ||
                                     EF.Functions.ILike(a.Link, $"%{urlWithoutProtocol}/")));
                        }

                        if (article != null)
                            break;
                    }

                    // Fallback: Try to match by title if URL matching failed
                    if (article == null &&
                        bookDto.category?.ToLowerInvariant() == "articles" &&
                        !string.IsNullOrEmpty(bookDto.title))
                    {
                        article = await _context.Articles
                            .FirstOrDefaultAsync(a =>
                                EF.Functions.ILike(a.Title, bookDto.title));

                        if (article != null)
                        {
                            _logger.LogDebug("Auto-linked highlight {HighlightId} to article {ArticleId} by title match (URL match failed)",
                                highlight.Id, article.Id);
                        }
                    }

                    if (article != null)
                    {
                        highlight.ArticleId = article.Id;
                        highlight.Article = article;
                        result.LinkedCount++;
                        _logger.LogDebug("Auto-linked highlight {HighlightId} to article {ArticleId} (title: {Title})",
                            highlight.Id, article.Id, article.Title);
                    }
                    else if ((urlsToTry.Count > 0 || !string.IsNullOrEmpty(bookDto.title)) && bookDto.category?.ToLowerInvariant() == "articles")
                    {
                        // Log unlinked article highlights for debugging
                        _logger.LogDebug("Could not link highlight to article. Source URL: {SourceUrl}, Title: {Title}",
                            bookDto.source_url, bookDto.title);
                    }

                    // Auto-link to book by title and author if category is "books"
                    if (highlight.ArticleId == null &&
                        bookDto.category?.ToLowerInvariant() == "books" &&
                        !string.IsNullOrEmpty(bookDto.title) &&
                        !string.IsNullOrEmpty(bookDto.author))
                    {
                        var book = await _context.Books
                            .FirstOrDefaultAsync(b =>
                                b.Title.ToLower() == bookDto.title.ToLower() &&
                                b.Author != null && b.Author.ToLower() == bookDto.author.ToLower());
                        if (book != null)
                        {
                            highlight.BookId = book.Id;
                            highlight.Book = book;
                            result.LinkedCount++;
                            _logger.LogDebug("Auto-linked highlight {HighlightId} to book {BookId}",
                                highlight.Id, book.Id);
                        }
                    }

                    _context.Add(highlight);
                    result.CreatedCount++;
                }
            }

            await _context.SaveChangesAsync();
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
                var highlights = await _context.Highlights.ToListAsync();

                foreach (var highlight in highlights)
                {
                    if (HtmlTextCleaner.ContainsHtmlOrCss(highlight.Text))
                    {
                        var cleanedText = HtmlTextCleaner.Clean(highlight.Text);
                        if (cleanedText != highlight.Text)
                        {
                            highlight.Text = cleanedText;
                            highlight.UpdatedAt = DateTime.UtcNow;
                            cleanedCount++;
                        }
                    }
                }

                if (cleanedCount > 0)
                {
                    await _context.SaveChangesAsync();
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

        public async Task<BulkHighlightResultDto> BulkCreateHighlightsAsync(List<CreateHighlightDto> dtos)
        {
            var result = new BulkHighlightResultDto();

            foreach (var dto in dtos)
            {
                try
                {
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
                        Tags = dto.Tags != null ? string.Join(",", dto.Tags.Select(t => t.ToLowerInvariant())) : null,
                        Location = dto.Location,
                        LocationType = dto.LocationType,
                        HighlightedAt = dto.HighlightedAt,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Auto-link to article by URL if sourceUrl is provided and no articleId set
                    if (highlight.ArticleId == null && !string.IsNullOrEmpty(dto.SourceUrl))
                    {
                        var normalizedUrl = UrlNormalizer.Normalize(dto.SourceUrl);
                        var urlWithoutProtocol = normalizedUrl
                            .Replace("https://", "")
                            .Replace("http://", "");

                        var article = await _context.Articles
                            .FirstOrDefaultAsync(a =>
                                a.Link != null &&
                                EF.Functions.ILike(a.Link, normalizedUrl));

                        if (article == null)
                        {
                            article = await _context.Articles
                                .FirstOrDefaultAsync(a =>
                                    a.Link != null &&
                                    (EF.Functions.ILike(a.Link, $"%{urlWithoutProtocol}") ||
                                     EF.Functions.ILike(a.Link, $"%{urlWithoutProtocol}/")));
                        }

                        if (article != null)
                        {
                            highlight.ArticleId = article.Id;
                            highlight.Article = article;
                            result.Linked++;
                        }
                    }

                    // Auto-link to book by title + author if category is "books" and no bookId set
                    if (highlight.ArticleId == null && highlight.BookId == null &&
                        dto.Category?.ToLowerInvariant() == "books" &&
                        !string.IsNullOrEmpty(dto.Title) && !string.IsNullOrEmpty(dto.Author))
                    {
                        var book = await _context.Books
                            .FirstOrDefaultAsync(b =>
                                b.Title.ToLower() == dto.Title.ToLower() &&
                                b.Author != null && b.Author.ToLower() == dto.Author.ToLower());

                        if (book != null)
                        {
                            highlight.BookId = book.Id;
                            highlight.Book = book;
                            result.Linked++;
                        }
                    }

                    // Fallback: try title match for articles if no link yet
                    if (highlight.ArticleId == null && highlight.BookId == null &&
                        dto.Category?.ToLowerInvariant() == "articles" &&
                        !string.IsNullOrEmpty(dto.Title))
                    {
                        var article = await _context.Articles
                            .FirstOrDefaultAsync(a =>
                                EF.Functions.ILike(a.Title, dto.Title));

                        if (article != null)
                        {
                            highlight.ArticleId = article.Id;
                            highlight.Article = article;
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
                _logger.LogError(ex, "Failed to save bulk highlights to database");
                result.Errors.Add($"Database save failed: {ex.Message}");
                return result;
            }

            _logger.LogInformation("Bulk created {Created} highlights, linked {Linked}, errors {Errors}",
                result.Created, result.Linked, result.Errors.Count);

            return result;
        }
    }
}

