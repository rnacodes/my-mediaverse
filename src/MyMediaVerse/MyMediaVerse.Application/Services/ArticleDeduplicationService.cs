using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Enums;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for identifying and merging duplicate articles from different sources.
    /// Implements smart merging logic that preserves all valuable data from both sources.
    /// </summary>
    public class ArticleDeduplicationService : IArticleDeduplicationService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ArticleDeduplicationService> _logger;

        public ArticleDeduplicationService(
            IApplicationDbContext context,
            ILogger<ArticleDeduplicationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DuplicateGroupDto>> FindDuplicatesAsync()
        {
            _logger.LogInformation("Finding duplicate articles");

            var articles = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Link != null)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Link,
                    a.DateAdded,
                    a.ReadwiseDocumentId,
                    a.FullTextContent
                })
                .ToListAsync();

            var groups = articles
                .GroupBy(a => UrlNormalizer.Normalize(a.Link))
                .Where(g => g.Count() > 1)
                .Select(g => new DuplicateGroupDto
                {
                    NormalizedUrl = g.Key,
                    Articles = g.Select(a => new ArticleSummaryDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Link = a.Link,
                        DateAdded = a.DateAdded,
                        HasReaderData = !string.IsNullOrEmpty(a.ReadwiseDocumentId),
                        HasContent = !string.IsNullOrEmpty(a.FullTextContent)
                    }).ToList()
                })
                .ToList();

            _logger.LogInformation("Found {Count} duplicate groups", groups.Count);
            return groups;
        }

        public async Task<DeduplicationResultDto> FindAndMergeDuplicatesAsync()
        {
            var result = new DeduplicationResultDto
            {
                StartedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting article deduplication");

                // Find all articles grouped by normalized URL
                var articles = await _context.Articles
                    .Include(a => a.Topics)
                    .Include(a => a.Genres)
                    .Include(a => a.Highlights)
                    .Where(a => a.Link != null)
                    .ToListAsync();

                var groupedByUrl = articles
                    .GroupBy(a => UrlNormalizer.Normalize(a.Link))
                    .Where(g => g.Count() > 1)
                    .ToList();

                _logger.LogInformation("Found {Count} URL groups with duplicates", groupedByUrl.Count);

                result.GroupCount = groupedByUrl.Count;

                foreach (var group in groupedByUrl)
                {
                    var articlesToMerge = group.OrderBy(a => a.DateAdded).ToList();
                    var primary = SelectPrimaryArticle(articlesToMerge);
                    var duplicates = articlesToMerge.Where(a => a.Id != primary.Id).ToList();

                    await MergeArticlesAsync(primary, duplicates);

                    result.MergedCount += duplicates.Count;
                    result.MergedGroups.Add(new MergeGroupDto
                    {
                        PrimaryId = primary.Id,
                        DuplicateIds = duplicates.Select(d => d.Id).ToList(),
                        Url = primary.Link ?? string.Empty,
                        Title = primary.Title
                    });
                }

                await _context.SaveChangesAsync();

                result.Success = true;
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Deduplication complete. Merged {Count} articles into {Groups} groups",
                    result.MergedCount, groupedByUrl.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during article deduplication");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

        /// <summary>
        /// Selects the primary article to keep during merge.
        /// Prioritization logic:
        /// 1. Has content from Reader (FullTextContent from ReadwiseDocumentId)
        /// 2. Has ReadwiseDocumentId (Reader metadata)
        /// 3. Has more complete metadata (author, description, etc.)
        /// 4. Oldest DateAdded (first imported)
        /// </summary>
        private Article SelectPrimaryArticle(List<Article> articles)
        {
            // Priority 1: Has Reader content
            var withReaderContent = articles.FirstOrDefault(a =>
                !string.IsNullOrEmpty(a.ReadwiseDocumentId) &&
                !string.IsNullOrEmpty(a.FullTextContent));

            if (withReaderContent != null)
            {
                _logger.LogDebug("Selected article {Id} as primary (has Reader content)", withReaderContent.Id);
                return withReaderContent;
            }

            // Priority 2: Has ReadwiseDocumentId
            var withReadwise = articles.FirstOrDefault(a =>
                !string.IsNullOrEmpty(a.ReadwiseDocumentId));

            if (withReadwise != null)
            {
                _logger.LogDebug("Selected article {Id} as primary (has Reader metadata)", withReadwise.Id);
                return withReadwise;
            }

            // Priority 3: Most complete metadata
            var mostComplete = articles
                .OrderByDescending(a => GetCompletenessScore(a))
                .First();

            _logger.LogDebug("Selected article {Id} as primary (most complete metadata)", mostComplete.Id);
            return mostComplete;
        }

        /// <summary>
        /// Calculates a score representing how complete an article's metadata is.
        /// Higher score = more complete.
        /// </summary>
        private int GetCompletenessScore(Article article)
        {
            int score = 0;

            if (!string.IsNullOrEmpty(article.Author)) score += 10;
            if (!string.IsNullOrEmpty(article.Publication)) score += 10;
            if (!string.IsNullOrEmpty(article.Description)) score += 10;
            if (!string.IsNullOrEmpty(article.Thumbnail)) score += 5;
            if (article.PublicationDate.HasValue) score += 5;
            if (article.WordCount.HasValue && article.WordCount > 0) score += 5;
            if (!string.IsNullOrEmpty(article.FullTextContent)) score += 20;
            if (article.Topics.Any()) score += 5;
            if (article.Genres.Any()) score += 5;

            return score;
        }

        /// <summary>
        /// Merges duplicate articles into the primary article.
        /// Combines all metadata and relationships from duplicates.
        /// </summary>
        private async Task MergeArticlesAsync(Article primary, List<Article> duplicates)
        {
            foreach (var duplicate in duplicates)
            {
                _logger.LogInformation("Merging article {DuplicateId} into {PrimaryId}",
                    duplicate.Id, primary.Id);

                // Merge Readwise Reader data if primary doesn't have it
                if (string.IsNullOrEmpty(primary.ReadwiseDocumentId) &&
                    !string.IsNullOrEmpty(duplicate.ReadwiseDocumentId))
                {
                    primary.ReadwiseDocumentId = duplicate.ReadwiseDocumentId;
                    primary.ReaderLocation = duplicate.ReaderLocation;
                    primary.LastReaderSync = duplicate.LastReaderSync;
                    primary.IsArchived = duplicate.IsArchived;
                    primary.IsStarred = duplicate.IsStarred;
                    primary.LastSyncDate = duplicate.LastSyncDate;
                    primary.SyncStatus |= SyncStatus.ReaderSynced;
                }

                // Merge content if primary doesn't have it or duplicate has newer content
                if (string.IsNullOrEmpty(primary.FullTextContent) ||
                    (!string.IsNullOrEmpty(duplicate.FullTextContent) &&
                     duplicate.LastReaderSync > primary.LastReaderSync))
                {
                    if (!string.IsNullOrEmpty(duplicate.FullTextContent))
                    {
                        primary.FullTextContent = duplicate.FullTextContent;
                    }
                }

                // Merge metadata (prefer non-null, more recent, or longer values)
                primary.Author = MergeStringField(primary.Author, duplicate.Author);
                primary.Publication = MergeStringField(primary.Publication, duplicate.Publication);
                primary.Description = MergeStringField(primary.Description, duplicate.Description);
                primary.Thumbnail = MergeStringField(primary.Thumbnail, duplicate.Thumbnail);

                if (!primary.PublicationDate.HasValue && duplicate.PublicationDate.HasValue)
                    primary.PublicationDate = duplicate.PublicationDate;

                if ((!primary.WordCount.HasValue || primary.WordCount == 0) &&
                    duplicate.WordCount.HasValue && duplicate.WordCount > 0)
                    primary.WordCount = duplicate.WordCount;

                if ((!primary.ReadingProgress.HasValue || primary.ReadingProgress == 0) &&
                    duplicate.ReadingProgress.HasValue && duplicate.ReadingProgress > 0)
                    primary.ReadingProgress = duplicate.ReadingProgress;

                // Merge topics (avoid duplicates)
                foreach (var topic in duplicate.Topics)
                {
                    if (!primary.Topics.Any(t => t.Name == topic.Name))
                        primary.Topics.Add(topic);
                }

                // Merge genres (avoid duplicates)
                foreach (var genre in duplicate.Genres)
                {
                    if (!primary.Genres.Any(g => g.Name == genre.Name))
                        primary.Genres.Add(genre);
                }

                // Update highlights to point to primary
                foreach (var highlight in duplicate.Highlights)
                {
                    highlight.ArticleId = primary.Id;
                    highlight.Article = primary;
                }

                // Remove duplicate article
                _context.Remove(duplicate);

                _logger.LogInformation("Successfully merged article {DuplicateId} into {PrimaryId}",
                    duplicate.Id, primary.Id);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Helper method to merge string fields.
        /// Returns the longer, non-null value.
        /// </summary>
        private string? MergeStringField(string? primary, string? duplicate)
        {
            if (string.IsNullOrWhiteSpace(primary) && !string.IsNullOrWhiteSpace(duplicate))
            {
                return duplicate;
            }
            else if (!string.IsNullOrWhiteSpace(duplicate) &&
                     duplicate.Length > (primary?.Length ?? 0))
            {
                return duplicate;
            }

            return primary;
        }
    }
}
