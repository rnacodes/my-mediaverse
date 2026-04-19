using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Enums;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class ReaderService : IReaderService
    {
        private readonly IApplicationDbContext _context;
        private readonly IReaderApiClient _readerClient;
        private readonly ILogger<ReaderService> _logger;

        public ReaderService(
            IApplicationDbContext context,
            IReaderApiClient readerClient,
            ILogger<ReaderService> logger)
        {
            _context = context;
            _readerClient = readerClient;
            _logger = logger;
        }

        public async Task<ReaderSyncResultDto> SyncDocumentsAsync(string? location = null, DateTime? updatedAfter = null)
        {
            var result = new ReaderSyncResultDto
            {
                StartedAt = DateTime.UtcNow
            };

            try
            {
                // Format updatedAfter for the API (ISO 8601 format)
                string? updatedAfterStr = updatedAfter?.ToString("yyyy-MM-ddTHH:mm:ssZ");

                _logger.LogInformation("Starting Reader document sync (location: {Location}, updatedAfter: {UpdatedAfter})",
                    location ?? "all", updatedAfterStr ?? "none");

                string? pageCursor = null;
                var hasMore = true;
                var iteration = 0;

                while (hasMore && iteration < 100) // Safety limit
                {
                    var response = await _readerClient.GetDocumentsAsync(
                        updatedAfter: updatedAfterStr,
                        location: location,
                        category: "article",
                        pageCursor: pageCursor);

                    if (response.results.Count == 0)
                    {
                        break;
                    }

                    _logger.LogInformation("Processing {Count} documents (iteration {Iteration})", 
                        response.results.Count, iteration + 1);

                    foreach (var docDto in response.results)
                    {
                        await ProcessReaderDocument(docDto, result);
                    }

                    hasMore = !string.IsNullOrEmpty(response.nextPageCursor);
                    pageCursor = response.nextPageCursor;
                    iteration++;

                    // Small delay to respect rate limits
                    await Task.Delay(250);
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = true;

                _logger.LogInformation("Completed Reader sync. Created: {Created}, Updated: {Updated}",
                    result.CreatedCount, result.UpdatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing documents from Reader");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<bool> FetchAndStoreArticleContentAsync(Guid articleId)
        {
            try
            {
                var article = await _context.Articles
                    .FirstOrDefaultAsync(a => a.Id == articleId);
                if (article == null || string.IsNullOrEmpty(article.ReadwiseDocumentId))
                {
                    _logger.LogWarning("Article {ArticleId} not found or missing Reader document ID", articleId);
                    return false;
                }

                _logger.LogInformation("Fetching content for article {ArticleId} (Reader doc: {DocId})",
                    articleId, article.ReadwiseDocumentId);

                // Fetch document with HTML content
                var document = await _readerClient.GetDocumentByIdAsync(article.ReadwiseDocumentId, includeHtml: true);

                // Check for html_content (from withHtmlContent=true) or fall back to html
                var htmlContent = document?.html_content ?? document?.html;
                if (document == null || string.IsNullOrEmpty(htmlContent))
                {
                    _logger.LogWarning("No HTML content available for document {DocumentId}", article.ReadwiseDocumentId);
                    return false;
                }

                // Store content directly in database
                article.FullTextContent = htmlContent;
                article.WordCount = document.word_count;
                article.LastReaderSync = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully stored content for article {ArticleId} in database ({Size} chars)",
                    articleId, htmlContent.Length);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching and storing content for article {ArticleId}", articleId);
                return false;
            }
        }

        public async Task<int> BulkFetchArticleContentsAsync(int batchSize = 50, DateTime? updatedAfter = null)
        {
            try
            {
                // Get archived articles with Reader document ID but no content
                // Only fetch content for Completed (archived) articles - for archival purposes
                var baseQuery = _context.Articles
                    .Where(a => a.ReadwiseDocumentId != null
                             && a.FullTextContent == null
                             && a.Status == Status.Completed);  // Only archived articles

                if (updatedAfter.HasValue)
                {
                    // Include articles with null LastReaderSync (never synced) or synced after the date
                    baseQuery = baseQuery.Where(a =>
                        a.LastReaderSync == null || a.LastReaderSync >= updatedAfter.Value);
                }

                var articles = await baseQuery
                    .OrderBy(a => a.DateAdded)  // Consistent ordering for pagination
                    .Take(batchSize)
                    .ToListAsync();

                _logger.LogInformation("Starting bulk content fetch for {Count} archived articles", articles.Count);

                var successCount = 0;

                foreach (var article in articles)
                {
                    var success = await FetchAndStoreArticleContentAsync(article.Id);
                    if (success)
                    {
                        successCount++;
                    }

                    // Rate limiting: wait 300ms between requests (respects 20 req/min limit)
                    await Task.Delay(300);
                }

                _logger.LogInformation("Bulk fetch completed. Successfully fetched {Count} of {Total}",
                    successCount, articles.Count);

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk fetch of article contents");
                return 0;
            }
        }

        private async Task ProcessReaderDocument(
            MyMediaVerse.Shared.DTOs.ReadwiseReader.ReaderDocumentDto dto,
            ReaderSyncResultDto result)
        {
            // Use source_url (original article URL) if available, fall back to url (Reader URL)
            var originalUrl = dto.source_url ?? dto.url;

            // Normalize URL for consistent comparison
            var normalizedUrl = UrlNormalizer.Normalize(originalUrl);

            // Check if article exists by Reader document ID OR normalized URL
            var existing = await _context.Articles
                .FirstOrDefaultAsync(a =>
                    a.ReadwiseDocumentId == dto.id ||
                    (a.Link != null && EF.Functions.ILike(a.Link, normalizedUrl)));

            // Map ReaderLocation to Status - Readwise is source of truth
            var newStatus = dto.location.ToLowerInvariant() switch
            {
                "archive" => Status.Completed,
                _ => Status.Uncharted  // new, later, feed all map to Uncharted
            };

            if (existing != null)
            {
                // Update existing article with Reader data
                existing.ReadwiseDocumentId = dto.id;
                existing.ReaderLocation = dto.location.ToLowerInvariant();
                existing.IsArchived = dto.location.Equals("archive", StringComparison.OrdinalIgnoreCase);
                existing.IsStarred = dto.favorite ?? false;
                existing.ReadingProgress = dto.reading_progress.HasValue
                    ? (int)(dto.reading_progress.Value * 100)
                    : null;
                existing.LastReaderSync = DateTime.UtcNow;

                // Readwise is source of truth for status
                existing.Status = newStatus;

                // Mark as synced with Reader
                existing.SyncStatus |= SyncStatus.ReaderSynced;

                // Fix Link if it has a Reader URL but we have the original source_url
                if (!string.IsNullOrEmpty(dto.source_url) &&
                    existing.Link != null &&
                    existing.Link.Contains("read.readwise.io"))
                {
                    existing.Link = normalizedUrl;
                    _logger.LogDebug("Fixed article {ArticleId} Link from Reader URL to source URL: {SourceUrl}",
                        existing.Id, normalizedUrl);
                }

                // Update metadata fields (prefer Reader's data if more complete)
                if (!string.IsNullOrEmpty(dto.title) &&
                    (string.IsNullOrEmpty(existing.Title) || existing.Title == "Untitled"))
                    existing.Title = dto.title;

                if (!string.IsNullOrEmpty(dto.summary) && string.IsNullOrEmpty(existing.Description))
                    existing.Description = dto.summary;

                if (!string.IsNullOrEmpty(dto.author) && string.IsNullOrEmpty(existing.Author))
                    existing.Author = dto.author;

                if (!string.IsNullOrEmpty(dto.site_name) && string.IsNullOrEmpty(existing.Publication))
                    existing.Publication = dto.site_name;

                if (!string.IsNullOrEmpty(dto.image_url) && string.IsNullOrEmpty(existing.Thumbnail))
                    existing.Thumbnail = dto.image_url;

                if (dto.word_count.HasValue && (!existing.WordCount.HasValue || existing.WordCount == 0))
                    existing.WordCount = dto.word_count;

                result.UpdatedCount++;

                _logger.LogDebug("Updated article {ArticleId} from Reader document {DocumentId}",
                    existing.Id, dto.id);
            }
            else
            {
                // Create new article with normalized URL
                var article = new Article
                {
                    Id = Guid.NewGuid(),
                    Title = dto.title ?? "Untitled",
                    Description = dto.summary,
                    Author = dto.author,
                    Publication = dto.site_name,
                    Link = normalizedUrl,  // Store normalized URL
                    Thumbnail = dto.image_url,
                    ReadwiseDocumentId = dto.id,
                    ReaderLocation = dto.location.ToLowerInvariant(),
                    IsArchived = dto.location.Equals("archive", StringComparison.OrdinalIgnoreCase),
                    IsStarred = dto.favorite ?? false,
                    WordCount = dto.word_count,
                    ReadingProgress = dto.reading_progress.HasValue 
                        ? (int)(dto.reading_progress.Value * 100) 
                        : null,
                    PublicationDate = !string.IsNullOrEmpty(dto.published_date)
                        ? DateTime.Parse(dto.published_date, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime()
                        : null,
                    LastReaderSync = DateTime.UtcNow,
                    DateAdded = DateTime.UtcNow,
                    MediaType = Domain.Entities.MediaType.Article,
                    Status = newStatus,  // Map ReaderLocation to Status
                    SyncStatus = SyncStatus.ReaderSynced  // Mark as synced from Reader
                };

                _context.Add(article);
                result.CreatedCount++;

                _logger.LogDebug("Created article {ArticleId} from Reader document {DocumentId}",
                    article.Id, dto.id);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ReaderDocumentTestResultDto> TestFetchDocumentByIdAsync(string readerDocumentId, bool includeHtml = true)
        {
            var result = new ReaderDocumentTestResultDto
            {
                DocumentId = readerDocumentId
            };

            try
            {
                _logger.LogInformation("Testing fetch for Reader document {DocumentId} (includeHtml: {IncludeHtml})",
                    readerDocumentId, includeHtml);

                // Fetch document from Reader API
                var document = await _readerClient.GetDocumentByIdAsync(readerDocumentId, includeHtml);

                if (document == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Document with ID '{readerDocumentId}' not found in Reader API";
                    return result;
                }

                // Populate result with API response data
                result.Success = true;
                result.Title = document.title;
                result.Url = document.url;
                result.SourceUrl = document.source_url;
                result.Author = document.author;
                result.SiteName = document.site_name;
                result.Location = document.location;
                result.Category = document.category;
                result.WordCount = document.word_count;
                result.ReadingProgress = document.reading_progress;

                // Check HTML content availability
                result.HasHtmlContent = !string.IsNullOrEmpty(document.html_content);
                result.HasHtml = !string.IsNullOrEmpty(document.html);

                var htmlContent = document.html_content ?? document.html;
                if (!string.IsNullOrEmpty(htmlContent))
                {
                    result.HtmlContentLength = htmlContent.Length;
                    result.HtmlContentPreview = htmlContent.Length > 500
                        ? htmlContent.Substring(0, 500) + "..."
                        : htmlContent;
                }

                // List available fields for debugging
                result.AvailableFields = new List<string>();
                if (!string.IsNullOrEmpty(document.id)) result.AvailableFields.Add("id");
                if (!string.IsNullOrEmpty(document.title)) result.AvailableFields.Add("title");
                if (!string.IsNullOrEmpty(document.url)) result.AvailableFields.Add("url");
                if (!string.IsNullOrEmpty(document.source_url)) result.AvailableFields.Add("source_url");
                if (!string.IsNullOrEmpty(document.author)) result.AvailableFields.Add("author");
                if (!string.IsNullOrEmpty(document.site_name)) result.AvailableFields.Add("site_name");
                if (!string.IsNullOrEmpty(document.location)) result.AvailableFields.Add("location");
                if (!string.IsNullOrEmpty(document.category)) result.AvailableFields.Add("category");
                if (document.word_count.HasValue) result.AvailableFields.Add("word_count");
                if (document.reading_progress.HasValue) result.AvailableFields.Add("reading_progress");
                if (!string.IsNullOrEmpty(document.html_content)) result.AvailableFields.Add("html_content");
                if (!string.IsNullOrEmpty(document.html)) result.AvailableFields.Add("html");
                if (!string.IsNullOrEmpty(document.content)) result.AvailableFields.Add("content");
                if (!string.IsNullOrEmpty(document.summary)) result.AvailableFields.Add("summary");

                // Check if article exists in database
                var article = await _context.Articles
                    .FirstOrDefaultAsync(a => a.ReadwiseDocumentId == readerDocumentId);

                if (article != null)
                {
                    result.FoundInDatabase = true;
                    result.ArticleId = article.Id;
                    result.ArticleStatus = article.Status.ToString();
                    result.ArticleHasContent = !string.IsNullOrEmpty(article.FullTextContent);
                }

                _logger.LogInformation("Test fetch result for {DocumentId}: HasHtmlContent={HasHtml}, DbFound={Found}",
                    readerDocumentId, result.HasHtmlContent || result.HasHtml, result.FoundInDatabase);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing fetch for document {DocumentId}", readerDocumentId);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public async Task<(bool success, string message, int? contentLength)> FetchContentByReaderDocumentIdAsync(string readerDocumentId)
        {
            try
            {
                _logger.LogInformation("Fetching content for Reader document {DocumentId}", readerDocumentId);

                // Find article by Reader document ID
                var article = await _context.Articles
                    .FirstOrDefaultAsync(a => a.ReadwiseDocumentId == readerDocumentId);

                if (article == null)
                {
                    return (false, $"No article found in database with Reader document ID '{readerDocumentId}'", null);
                }

                // Fetch document with HTML content from Reader API
                var document = await _readerClient.GetDocumentByIdAsync(readerDocumentId, includeHtml: true);

                if (document == null)
                {
                    return (false, $"Document '{readerDocumentId}' not found in Reader API", null);
                }

                var htmlContent = document.html_content ?? document.html;
                if (string.IsNullOrEmpty(htmlContent))
                {
                    return (false, $"No HTML content available for document '{readerDocumentId}'. HasHtmlContent={!string.IsNullOrEmpty(document.html_content)}, HasHtml={!string.IsNullOrEmpty(document.html)}", null);
                }

                // Store content in article
                article.FullTextContent = htmlContent;
                article.WordCount = document.word_count;
                article.LastReaderSync = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully stored content for article {ArticleId} ({Length} chars)",
                    article.Id, htmlContent.Length);

                return (true, $"Successfully stored {htmlContent.Length} chars of content for article '{article.Title}'", htmlContent.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching content for document {DocumentId}", readerDocumentId);
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public async Task<IEnumerable<ReaderArticleSummaryDto>> GetArticlesWithReaderDocumentIdsAsync(int limit = 20, bool onlyWithoutContent = false, string? status = null)
        {
            var query = _context.Articles
                .Where(a => a.ReadwiseDocumentId != null);

            if (onlyWithoutContent)
            {
                query = query.Where(a => a.FullTextContent == null);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<Status>(status, ignoreCase: true, out var statusEnum))
            {
                query = query.Where(a => a.Status == statusEnum);
            }

            var articles = await query
                .OrderByDescending(a => a.LastReaderSync ?? a.DateAdded)
                .Take(limit)
                .Select(a => new ReaderArticleSummaryDto
                {
                    ArticleId = a.Id,
                    Title = a.Title,
                    ReadwiseDocumentId = a.ReadwiseDocumentId,
                    Status = a.Status.ToString(),
                    ReaderLocation = a.ReaderLocation,
                    HasFullTextContent = a.FullTextContent != null,
                    ContentLength = a.FullTextContent != null ? a.FullTextContent.Length : null,
                    LastReaderSync = a.LastReaderSync
                })
                .ToListAsync();

            return articles;
        }

        public async Task<IEnumerable<ReaderArticleSummaryDto>> FetchDocumentsFromReaderApiAsync(string? location = null, int limit = 50)
        {
            try
            {
                _logger.LogInformation("Fetching documents directly from Reader API (location: {Location}, limit: {Limit})",
                    location ?? "all", limit);

                var results = new List<ReaderArticleSummaryDto>();
                string? pageCursor = null;

                // Fetch pages until we have enough results
                while (results.Count < limit)
                {
                    var response = await _readerClient.GetDocumentsAsync(
                        location: location,
                        category: "article",
                        pageCursor: pageCursor);

                    if (response.results.Count == 0)
                        break;

                    foreach (var doc in response.results)
                    {
                        if (results.Count >= limit)
                            break;

                        results.Add(new ReaderArticleSummaryDto
                        {
                            ArticleId = Guid.Empty, // Not from database
                            Title = doc.title ?? "Untitled",
                            ReadwiseDocumentId = doc.id,
                            Status = doc.location == "archive" ? "Completed" : "Uncharted",
                            ReaderLocation = doc.location,
                            HasFullTextContent = false, // We don't know without fetching
                            LastReaderSync = null
                        });
                    }

                    if (string.IsNullOrEmpty(response.nextPageCursor))
                        break;

                    pageCursor = response.nextPageCursor;

                    // Small delay to respect rate limits
                    await Task.Delay(250);
                }

                _logger.LogInformation("Fetched {Count} documents from Reader API", results.Count);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documents from Reader API");
                throw;
            }
        }

        public async Task<ReaderSyncResultDto> SyncDocumentsByLocationAsync(string location, int limit = 50)
        {
            var result = new ReaderSyncResultDto
            {
                StartedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Syncing {Limit} documents from Reader with location: {Location}", limit, location);

                string? pageCursor = null;
                var processedCount = 0;

                while (processedCount < limit)
                {
                    var response = await _readerClient.GetDocumentsAsync(
                        location: location,
                        category: "article",
                        pageCursor: pageCursor);

                    if (response.results.Count == 0)
                        break;

                    foreach (var docDto in response.results)
                    {
                        if (processedCount >= limit)
                            break;

                        await ProcessReaderDocument(docDto, result);
                        processedCount++;
                    }

                    if (string.IsNullOrEmpty(response.nextPageCursor) || processedCount >= limit)
                        break;

                    pageCursor = response.nextPageCursor;

                    // Small delay to respect rate limits
                    await Task.Delay(250);
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = true;

                _logger.LogInformation("Synced {Count} documents by location. Created: {Created}, Updated: {Updated}",
                    processedCount, result.CreatedCount, result.UpdatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing documents by location");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

    }
}

