using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Enums;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.ReadwiseReader;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class ReaderService : IReaderService
    {
        // Guards against a runaway pagination loop; a run that hits it is reported as
        // incomplete via WarningMessage so the sync cursor is not advanced past it.
        internal const int MaxSyncPages = 100;

        // Pauses between Reader API calls (rate limit is 20 req/min); tests set these to 0.
        internal int PageDelayMs { get; set; } = 250;
        internal int ContentFetchDelayMs { get; set; } = 300;

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

                while (hasMore && iteration < MaxSyncPages)
                {
                    var response = await _readerClient.GetDocumentsAsync(
                        updatedAfter: updatedAfterStr,
                        location: location,
                        category: "article",
                        pageCursor: pageCursor);

                    if (response.Results.Count == 0)
                    {
                        hasMore = false;
                        break;
                    }

                    _logger.LogInformation("Processing {Count} documents (iteration {Iteration})",
                        response.Results.Count, iteration + 1);

                    foreach (var docDto in response.Results)
                    {
                        await ProcessReaderDocument(docDto, result);
                    }

                    hasMore = !string.IsNullOrEmpty(response.NextPageCursor);
                    pageCursor = response.NextPageCursor;
                    iteration++;

                    // Small delay to respect rate limits
                    if (PageDelayMs > 0) await Task.Delay(PageDelayMs);
                }

                if (hasMore)
                {
                    result.WarningMessage =
                        $"Reader sync stopped at the {MaxSyncPages}-page safety limit before reaching the end of the window; " +
                        "the remaining documents were not synced.";
                    _logger.LogWarning("{Warning}", result.WarningMessage);
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
                var htmlContent = document?.HtmlContent ?? document?.Html;
                if (document == null || string.IsNullOrEmpty(htmlContent))
                {
                    _logger.LogWarning("No HTML content available for document {DocumentId}", article.ReadwiseDocumentId);
                    return false;
                }

                // Store content directly in database
                article.FullTextContent = htmlContent;
                article.WordCount = document.WordCount;
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

        public async Task<ReaderSyncResultDto> BulkFetchArticleContentsAsync(int batchSize = 50, DateTime? updatedAfter = null)
        {
            var result = new ReaderSyncResultDto
            {
                Operation = "reader-bulk-fetch-content",
                StartedAt = DateTime.UtcNow
            };

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

                foreach (var article in articles)
                {
                    var success = await FetchAndStoreArticleContentAsync(article.Id);
                    if (success)
                    {
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.SkippedCount++;
                    }

                    // Rate limiting: wait between requests (respects 20 req/min limit)
                    if (ContentFetchDelayMs > 0) await Task.Delay(ContentFetchDelayMs);
                }

                if (result.SkippedCount > 0)
                {
                    result.WarningMessage =
                        $"{result.SkippedCount} of {articles.Count} articles had no HTML content available in Reader.";
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = true;

                _logger.LogInformation("Bulk fetch completed. Successfully fetched {Count} of {Total}",
                    result.UpdatedCount, articles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk fetch of article contents");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

        private async Task ProcessReaderDocument(ReaderDocumentDto dto, ReaderSyncResultDto result)
        {
            // Use source_url (original article URL) if available, fall back to url (Reader URL)
            var originalUrl = dto.SourceUrl ?? dto.Url;

            // Normalize URL for consistent comparison
            var normalizedUrl = UrlNormalizer.Normalize(originalUrl);

            var existing = await ArticleDuplicateFinder.FindExistingAsync(
                _context.Articles.Include(a => a.Topics),
                dto.Id,
                originalUrl);

            // Map ReaderLocation to Status - Readwise is source of truth
            var newStatus = dto.Location.ToLowerInvariant() switch
            {
                "archive" => Status.Completed,
                _ => Status.Uncharted  // new, later, feed all map to Uncharted
            };

            if (existing != null)
            {
                // Update existing article with Reader data
                existing.ReadwiseDocumentId = dto.Id;
                existing.ReaderLocation = dto.Location.ToLowerInvariant();
                existing.IsArchived = dto.Location.Equals("archive", StringComparison.OrdinalIgnoreCase);
                existing.IsStarred = dto.Favorite ?? false;
                existing.ReadingProgress = dto.ReadingProgress.HasValue
                    ? (int)(dto.ReadingProgress.Value * 100)
                    : null;
                existing.LastReaderSync = DateTime.UtcNow;

                // Readwise is source of truth for status
                existing.Status = newStatus;

                // Mark as synced with Reader
                existing.SyncStatus |= SyncStatus.ReaderSynced;

                // Fix Link if it has a Reader URL but we have the original source_url
                if (!string.IsNullOrEmpty(dto.SourceUrl) &&
                    existing.Link != null &&
                    existing.Link.Contains("read.readwise.io"))
                {
                    existing.Link = normalizedUrl;
                    _logger.LogDebug("Fixed article {ArticleId} Link from Reader URL to source URL: {SourceUrl}",
                        existing.Id, normalizedUrl);
                }

                // Update metadata fields (prefer Reader's data if more complete)
                if (!string.IsNullOrEmpty(dto.Title) &&
                    (string.IsNullOrEmpty(existing.Title) || existing.Title == "Untitled"))
                    existing.Title = dto.Title;

                if (!string.IsNullOrEmpty(dto.Summary) && string.IsNullOrEmpty(existing.Description))
                    existing.Description = dto.Summary;

                if (!string.IsNullOrEmpty(dto.Author) && string.IsNullOrEmpty(existing.Author))
                    existing.Author = dto.Author;

                if (!string.IsNullOrEmpty(dto.SiteName) && string.IsNullOrEmpty(existing.Publication))
                    existing.Publication = dto.SiteName;

                if (!string.IsNullOrEmpty(dto.ImageUrl) && string.IsNullOrEmpty(existing.Thumbnail))
                    existing.Thumbnail = dto.ImageUrl;

                if (dto.WordCount.HasValue && (!existing.WordCount.HasValue || existing.WordCount == 0))
                    existing.WordCount = dto.WordCount;

                await AddReaderTagsAsTopicsAsync(existing, dto.Tags);

                result.UpdatedCount++;

                _logger.LogDebug("Updated article {ArticleId} from Reader document {DocumentId}",
                    existing.Id, dto.Id);
            }
            else
            {
                // Create new article with normalized URL
                var article = new Article
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title ?? "Untitled",
                    Description = dto.Summary,
                    Author = dto.Author,
                    Publication = dto.SiteName,
                    Link = normalizedUrl,  // Store normalized URL
                    Thumbnail = dto.ImageUrl,
                    ReadwiseDocumentId = dto.Id,
                    ReaderLocation = dto.Location.ToLowerInvariant(),
                    IsArchived = dto.Location.Equals("archive", StringComparison.OrdinalIgnoreCase),
                    IsStarred = dto.Favorite ?? false,
                    WordCount = dto.WordCount,
                    ReadingProgress = dto.ReadingProgress.HasValue
                        ? (int)(dto.ReadingProgress.Value * 100)
                        : null,
                    PublicationDate = ParsePublishedDate(dto.PublishedDate, dto.Id),
                    LastReaderSync = DateTime.UtcNow,
                    DateAdded = DateTime.UtcNow,
                    MediaType = Domain.Entities.MediaType.Article,
                    Status = newStatus,  // Map ReaderLocation to Status
                    SyncStatus = SyncStatus.ReaderSynced  // Mark as synced from Reader
                };

                await AddReaderTagsAsTopicsAsync(article, dto.Tags);

                _context.Add(article);
                result.CreatedCount++;

                _logger.LogDebug("Created article {ArticleId} from Reader document {DocumentId}",
                    article.Id, dto.Id);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds each Reader tag as a topic on the article (find-or-create, lowercased).
        /// Additive only: topics assigned in the app are never removed by a sync.
        /// </summary>
        private async Task AddReaderTagsAsTopicsAsync(Article article, Dictionary<string, object>? tags)
        {
            if (tags == null || tags.Count == 0)
                return;

            var topicNames = tags.Keys
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            foreach (var topicName in topicNames)
            {
                if (article.Topics.Any(t => t.Name == topicName))
                    continue;

                var existingTopic = await _context.Topics
                    .FirstOrDefaultAsync(t => t.Name == topicName);

                article.Topics.Add(existingTopic ?? new Topic { Name = topicName });
            }
        }

        // A malformed date is not worth failing the document over; store null and move on.
        private DateTime? ParsePublishedDate(string? value, string documentId)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                return parsed.ToUniversalTime();

            _logger.LogWarning("Could not parse published_date '{Value}' for Reader document {DocumentId}; storing null",
                value, documentId);
            return null;
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
                result.Title = document.Title;
                result.Url = document.Url;
                result.SourceUrl = document.SourceUrl;
                result.Author = document.Author;
                result.SiteName = document.SiteName;
                result.Location = document.Location;
                result.Category = document.Category;
                result.WordCount = document.WordCount;
                result.ReadingProgress = document.ReadingProgress;

                // Check HTML content availability
                result.HasHtmlContent = !string.IsNullOrEmpty(document.HtmlContent);
                result.HasHtml = !string.IsNullOrEmpty(document.Html);

                var htmlContent = document.HtmlContent ?? document.Html;
                if (!string.IsNullOrEmpty(htmlContent))
                {
                    result.HtmlContentLength = htmlContent.Length;
                    result.HtmlContentPreview = htmlContent.Length > 500
                        ? htmlContent.Substring(0, 500) + "..."
                        : htmlContent;
                }

                // List available fields for debugging
                result.AvailableFields = new List<string>();
                if (!string.IsNullOrEmpty(document.Id)) result.AvailableFields.Add("id");
                if (!string.IsNullOrEmpty(document.Title)) result.AvailableFields.Add("title");
                if (!string.IsNullOrEmpty(document.Url)) result.AvailableFields.Add("url");
                if (!string.IsNullOrEmpty(document.SourceUrl)) result.AvailableFields.Add("source_url");
                if (!string.IsNullOrEmpty(document.Author)) result.AvailableFields.Add("author");
                if (!string.IsNullOrEmpty(document.SiteName)) result.AvailableFields.Add("site_name");
                if (!string.IsNullOrEmpty(document.Location)) result.AvailableFields.Add("location");
                if (!string.IsNullOrEmpty(document.Category)) result.AvailableFields.Add("category");
                if (document.WordCount.HasValue) result.AvailableFields.Add("word_count");
                if (document.ReadingProgress.HasValue) result.AvailableFields.Add("reading_progress");
                if (!string.IsNullOrEmpty(document.HtmlContent)) result.AvailableFields.Add("html_content");
                if (!string.IsNullOrEmpty(document.Html)) result.AvailableFields.Add("html");
                if (!string.IsNullOrEmpty(document.Content)) result.AvailableFields.Add("content");
                if (!string.IsNullOrEmpty(document.Summary)) result.AvailableFields.Add("summary");
                if (document.Tags != null && document.Tags.Count > 0) result.AvailableFields.Add("tags");

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

                var htmlContent = document.HtmlContent ?? document.Html;
                if (string.IsNullOrEmpty(htmlContent))
                {
                    return (false, $"No HTML content available for document '{readerDocumentId}'. HasHtmlContent={!string.IsNullOrEmpty(document.HtmlContent)}, HasHtml={!string.IsNullOrEmpty(document.Html)}", null);
                }

                // Store content in article
                article.FullTextContent = htmlContent;
                article.WordCount = document.WordCount;
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

                    if (response.Results.Count == 0)
                        break;

                    foreach (var doc in response.Results)
                    {
                        if (results.Count >= limit)
                            break;

                        results.Add(new ReaderArticleSummaryDto
                        {
                            ArticleId = Guid.Empty, // Not from database
                            Title = doc.Title ?? "Untitled",
                            ReadwiseDocumentId = doc.Id,
                            Status = doc.Location == "archive" ? "Completed" : "Uncharted",
                            ReaderLocation = doc.Location,
                            HasFullTextContent = false, // We don't know without fetching
                            LastReaderSync = null
                        });
                    }

                    if (string.IsNullOrEmpty(response.NextPageCursor))
                        break;

                    pageCursor = response.NextPageCursor;

                    // Small delay to respect rate limits
                    if (PageDelayMs > 0) await Task.Delay(PageDelayMs);
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

                    if (response.Results.Count == 0)
                        break;

                    foreach (var docDto in response.Results)
                    {
                        if (processedCount >= limit)
                            break;

                        await ProcessReaderDocument(docDto, result);
                        processedCount++;
                    }

                    if (string.IsNullOrEmpty(response.NextPageCursor) || processedCount >= limit)
                        break;

                    pageCursor = response.NextPageCursor;

                    // Small delay to respect rate limits
                    if (PageDelayMs > 0) await Task.Delay(PageDelayMs);
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
