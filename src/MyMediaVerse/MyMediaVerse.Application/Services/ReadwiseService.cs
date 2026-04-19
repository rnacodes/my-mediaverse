using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class ReadwiseService : IReadwiseService
    {
        private readonly IApplicationDbContext _context;
        private readonly IReadwiseApiClient _readwiseClient;
        private readonly ILogger<ReadwiseService> _logger;

        public ReadwiseService(
            IApplicationDbContext context,
            IReadwiseApiClient readwiseClient,
            ILogger<ReadwiseService> logger)
        {
            _context = context;
            _readwiseClient = readwiseClient;
            _logger = logger;
        }

        public async Task<bool> ValidateConnectionAsync()
        {
            _logger.LogInformation("Validating Readwise API connection");
            return await _readwiseClient.ValidateTokenAsync();
        }

        public async Task<ReadwiseSyncResultDto> SyncBooksAsync(string? category = null)
        {
            var result = new ReadwiseSyncResultDto
            {
                StartedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting book sync from Readwise (category: {Category})", 
                    category ?? "all");

                var page = 1;
                var hasMore = true;

                while (hasMore)
                {
                    _logger.LogInformation("Fetching books page {Page}", page);

                    var response = await _readwiseClient.GetBooksAsync(
                        category: category,
                        page: page);
                    
                    if (response.results.Count == 0)
                    {
                        hasMore = false;
                        break;
                    }

                    foreach (var bookDto in response.results)
                    {
                        await ProcessBookDto(bookDto, result);
                    }

                    hasMore = !string.IsNullOrEmpty(response.next);
                    page++;

                    // Safety check
                    if (page > 1000)
                    {
                        _logger.LogWarning("Stopped book sync after 1000 pages");
                        break;
                    }

                    // Small delay to respect rate limits (20 req/min for books)
                    await Task.Delay(3000);
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = true;

                _logger.LogInformation("Completed book sync. Created: {Created}, Updated: {Updated}",
                    result.BooksCreated, result.BooksUpdated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing books from Readwise");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<int> LinkHighlightsToMediaAsync()
        {
            _logger.LogInformation("Starting to link highlights to media items");

            var linkedCount = 0;

            try
            {
                // Get all highlights that don't have ArticleId or BookId set yet
                var unlinkedHighlights = await _context.Highlights
                    .Where(h => h.ArticleId == null && h.BookId == null)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} unlinked highlights", unlinkedHighlights.Count);

                foreach (var highlight in unlinkedHighlights)
                {
                    Domain.Entities.Article? article = null;

                    // Try to match by source URL first (for articles) - using multiple matching strategies
                    if (!string.IsNullOrEmpty(highlight.SourceUrl))
                    {
                        var normalizedSourceUrl = UrlNormalizer.Normalize(highlight.SourceUrl);

                        // Try exact normalized match first
                        article = await _context.Articles
                            .FirstOrDefaultAsync(a =>
                                a.Link != null &&
                                EF.Functions.ILike(a.Link, normalizedSourceUrl));

                        // If no match, try partial URL match (without protocol)
                        if (article == null)
                        {
                            var urlWithoutProtocol = normalizedSourceUrl
                                .Replace("https://", "")
                                .Replace("http://", "");
                            article = await _context.Articles
                                .FirstOrDefaultAsync(a =>
                                    a.Link != null &&
                                    (EF.Functions.ILike(a.Link, $"%{urlWithoutProtocol}") ||
                                     EF.Functions.ILike(a.Link, $"%{urlWithoutProtocol}/")));
                        }
                    }

                    // Fallback: Try to match by title for article highlights if URL match failed
                    if (article == null &&
                        highlight.Category?.ToLowerInvariant() == "articles" &&
                        !string.IsNullOrEmpty(highlight.Title))
                    {
                        article = await _context.Articles
                            .FirstOrDefaultAsync(a =>
                                EF.Functions.ILike(a.Title, highlight.Title));

                        if (article != null)
                        {
                            _logger.LogDebug("Linked highlight {HighlightId} to article {ArticleId} by title match (URL match failed)",
                                highlight.Id, article.Id);
                        }
                    }

                    if (article != null)
                    {
                        highlight.ArticleId = article.Id;
                        linkedCount++;
                        _logger.LogDebug("Linked highlight {HighlightId} to article {ArticleId}",
                            highlight.Id, article.Id);
                        continue;
                    }

                    // Try to match books by title and author
                    if (!string.IsNullOrEmpty(highlight.Title) &&
                        !string.IsNullOrEmpty(highlight.Author) &&
                        highlight.Category == "books")
                    {
                        var book = await _context.Books
                            .FirstOrDefaultAsync(b =>
                                b.Title.ToLower() == highlight.Title.ToLower() &&
                                b.Author.ToLower() == highlight.Author.ToLower());

                        if (book != null)
                        {
                            highlight.BookId = book.Id;
                            linkedCount++;
                            _logger.LogDebug("Linked highlight {HighlightId} to book {BookId} by title/author",
                                highlight.Id, book.Id);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully linked {Count} highlights to media items", linkedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking highlights to media");
            }

            return linkedCount;
        }

        public async Task<bool> ExportHighlightToReadwiseAsync(Guid highlightId)
        {
            try
            {
                var highlight = await _context.Highlights
                    .Include(h => h.Article)
                    .Include(h => h.Book)
                    .FirstOrDefaultAsync(h => h.Id == highlightId);

                if (highlight == null)
                {
                    _logger.LogWarning("Highlight {HighlightId} not found for export", highlightId);
                    return false;
                }

                var dto = new Shared.DTOs.Readwise.CreateReadwiseHighlightDto
                {
                    text = highlight.Text,
                    title = highlight.Title ?? highlight.Article?.Title ?? highlight.Book?.Title,
                    author = highlight.Author ?? highlight.Article?.Author ?? highlight.Book?.Author,
                    source_url = highlight.SourceUrl ?? highlight.Article?.Link,
                    note = highlight.Note,
                    category = highlight.Category,
                    highlighted_at = highlight.HighlightedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    location = highlight.Location,
                    location_type = highlight.LocationType
                };

                var success = await _readwiseClient.CreateHighlightsAsync(new List<Shared.DTOs.Readwise.CreateReadwiseHighlightDto> { dto });

                if (success)
                {
                    _logger.LogInformation("Successfully exported highlight {HighlightId} to Readwise", highlightId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting highlight {HighlightId} to Readwise", highlightId);
                return false;
            }
        }

        private async Task ProcessBookDto(
            MyMediaVerse.Shared.DTOs.Readwise.ReadwiseBookDto bookDto,
            ReadwiseSyncResultDto result)
        {
            // This method primarily tracks book metadata for linking purposes
            // We don't create Book entities automatically, but we could update existing ones
            
            // Check if we have a book that matches by title and author
            if (!string.IsNullOrEmpty(bookDto.title) && !string.IsNullOrEmpty(bookDto.author))
            {
                var existingBook = await _context.Books
                    .FirstOrDefaultAsync(b => 
                        b.Title.ToLower() == bookDto.title.ToLower() &&
                        b.Author.ToLower() == bookDto.author.ToLower());

                if (existingBook != null)
                {
                    // Update the ReadwiseBookId if not already set
                    if (existingBook.ReadwiseBookId != bookDto.id)
                    {
                        existingBook.ReadwiseBookId = bookDto.id;
                        existingBook.LastReadwiseSync = DateTime.UtcNow;
                        result.BooksUpdated++;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Updated book {BookId} with Readwise ID {ReadwiseId}",
                            existingBook.Id, bookDto.id);
                    }
                }
            }

            // Note: We intentionally don't auto-create books here to avoid cluttering
            // the library with books the user may not want to track
        }
    }
}

