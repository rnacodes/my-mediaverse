using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Bulk website intake. Each bookmark is normalized and keyed exactly like the single-URL
    /// paths, so a URL that is already in the library (in any scheme or tracking-parameter
    /// variant) is matched rather than duplicated; a match gains any topics the import carries
    /// and nothing else. New rows are saved as stubs (title, URL, domain, date, topics) with
    /// <c>EnrichedAt</c> left null so the enrichment run picks them up.
    /// </summary>
    public class WebsiteBulkImportService : IWebsiteBulkImportService
    {
        private const int SaveBatchSize = 100;
        private const int KeyLookupChunk = 500;
        private const int SampleSize = 10;
        private const int TitleMaxLength = 500;

        private readonly IApplicationDbContext _context;
        private readonly ILogger<WebsiteBulkImportService> _logger;

        public WebsiteBulkImportService(IApplicationDbContext context, ILogger<WebsiteBulkImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private sealed record Candidate(ParsedBookmark Bookmark, string Link, string Key, string Domain);

        public async Task<WebsiteBulkImportPreviewDto> PreviewAsync(BookmarkParseResult parsed, CancellationToken cancellationToken = default)
        {
            var preview = new WebsiteBulkImportPreviewDto
            {
                TotalCount = parsed.Bookmarks.Count,
                NonWebLinkCount = parsed.NonWebLinkCount,
                Folders = parsed.Folders.ToList()
            };

            var (candidates, invalid) = Normalize(parsed.Bookmarks);
            preview.InvalidCount = invalid;
            preview.ValidCount = candidates.Count;

            var existingKeys = await FindExistingKeysAsync(candidates.Select(c => c.Key).Distinct().ToList(), cancellationToken);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var candidate in candidates)
            {
                if (!seen.Add(candidate.Key))
                {
                    preview.DuplicateInFileCount++;
                    continue;
                }

                var inLibrary = existingKeys.Contains(candidate.Key);
                if (inLibrary) preview.AlreadyInLibraryCount++;
                else preview.NewCount++;

                if (preview.Sample.Count < SampleSize)
                {
                    preview.Sample.Add(new BookmarkPreviewItemDto
                    {
                        Url = candidate.Link,
                        Title = candidate.Bookmark.Title,
                        FolderPath = candidate.Bookmark.FolderPath.Count > 0 ? string.Join("/", candidate.Bookmark.FolderPath) : null,
                        AlreadyInLibrary = inLibrary
                    });
                }
            }

            return preview;
        }

        public async Task<WebsiteBulkImportResultDto> ImportAsync(
            BookmarkParseResult parsed,
            BookmarkImportOptionsDto options,
            string operation,
            CancellationToken cancellationToken = default)
        {
            var result = new WebsiteBulkImportResultDto
            {
                StartedAt = DateTime.UtcNow,
                Operation = operation,
                TotalProcessed = parsed.Bookmarks.Count,
                NonWebLinkCount = parsed.NonWebLinkCount,
                FoldersFound = parsed.Folders.Count
            };

            try
            {
                var (candidates, invalid) = Normalize(parsed.Bookmarks);
                result.FailedCount += invalid;
                if (invalid > 0)
                {
                    result.Errors.Add($"{invalid} entr{(invalid == 1 ? "y" : "ies")} had a URL that could not be read and were not imported.");
                }

                var existing = await LoadExistingAsync(candidates.Select(c => c.Key).Distinct().ToList(), cancellationToken);
                var topics = new TopicResolver(_context);
                var genres = new GenreResolver(_context);
                var extraTopics = NormalizeNames(options.ExtraTopics);
                var extraGenres = NormalizeNames(options.ExtraGenres);
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var pendingSaves = 0;

                foreach (var candidate in candidates)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    if (!seen.Add(candidate.Key))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    try
                    {
                        var topicNames = TopicNamesFor(candidate.Bookmark, options, extraTopics);

                        if (existing.TryGetValue(candidate.Key, out var website))
                        {
                            var changed = WebsiteDuplicateFinder.FillIdentity(website);
                            changed |= await AddTagsAsync(website, topicNames, extraGenres, topics, genres);

                            if (changed) result.UpdatedCount++;
                            else result.SkippedCount++;
                        }
                        else
                        {
                            website = new Website
                            {
                                Title = TitleFor(candidate),
                                Link = candidate.Link,
                                UrlKey = candidate.Key,
                                Domain = candidate.Domain,
                                MediaType = MediaType.Website,
                                Status = options.DefaultStatus,
                                DateAdded = candidate.Bookmark.AddedAt.HasValue
                                    ? DateTime.SpecifyKind(candidate.Bookmark.AddedAt.Value, DateTimeKind.Utc)
                                    : DateTime.UtcNow,
                                EnrichedAt = null
                            };

                            await AddTagsAsync(website, topicNames, extraGenres, topics, genres);
                            _context.Add(website);
                            existing[candidate.Key] = website;
                            result.CreatedCount++;
                        }

                        if (++pendingSaves >= SaveBatchSize)
                        {
                            await _context.SaveChangesAsync(cancellationToken);
                            pendingSaves = 0;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"{candidate.Link}: {ex.Message}");
                        _logger.LogWarning(ex, "Bookmark import failed for {Url}", candidate.Link);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                result.TopicsCreatedCount = topics.CreatedCount;
                result.PendingEnrichmentCount = await _context.Websites.CountAsync(w => w.EnrichedAt == null, cancellationToken);
                result.WarningMessage = BuildWarning(result);
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Bookmark import ({Operation}) complete. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Failed: {Failed}, Topics created: {Topics}",
                    operation, result.CreatedCount, result.UpdatedCount, result.SkippedCount, result.FailedCount, result.TopicsCreatedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                result.WarningMessage = "The import was canceled; websites saved before that point were kept.";
                result.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Import failed: {ex.Message}";
                result.CompletedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Bookmark import ({Operation}) failed", operation);
            }

            return result;
        }

        #region Helpers

        private static (List<Candidate> Candidates, int Invalid) Normalize(IReadOnlyList<ParsedBookmark> bookmarks)
        {
            var candidates = new List<Candidate>(bookmarks.Count);
            var invalid = 0;

            foreach (var bookmark in bookmarks)
            {
                if (!UrlNormalizer.IsValid(bookmark.Url))
                {
                    invalid++;
                    continue;
                }

                var link = UrlNormalizer.Normalize(bookmark.Url);
                var key = UrlNormalizer.GetComparisonKey(link);
                if (string.IsNullOrEmpty(key) || link.Length > 2000)
                {
                    invalid++;
                    continue;
                }

                candidates.Add(new Candidate(bookmark, link, key, UrlNormalizer.ExtractDomain(link)));
            }

            return (candidates, invalid);
        }

        /// <summary>
        /// Keys already in the library: one query per 500 keys, plus the legacy rows saved before
        /// the key column existed (matched in memory by their stored link).
        /// </summary>
        private async Task<HashSet<string>> FindExistingKeysAsync(List<string> keys, CancellationToken cancellationToken)
        {
            var existing = await LoadExistingAsync(keys, cancellationToken);
            return new HashSet<string>(existing.Keys, StringComparer.Ordinal);
        }

        private async Task<Dictionary<string, Website>> LoadExistingAsync(List<string> keys, CancellationToken cancellationToken)
        {
            var existing = new Dictionary<string, Website>(StringComparer.Ordinal);
            if (keys.Count == 0)
                return existing;

            var wanted = new HashSet<string>(keys, StringComparer.Ordinal);

            foreach (var chunk in keys.Chunk(KeyLookupChunk))
            {
                var rows = await _context.Websites
                    .Include(w => w.Topics)
                    .Include(w => w.Genres)
                    .Where(w => w.UrlKey != null && chunk.Contains(w.UrlKey))
                    .ToListAsync(cancellationToken);

                foreach (var row in rows)
                {
                    existing.TryAdd(row.UrlKey!, row);
                }
            }

            var legacy = await _context.Websites
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .Where(w => w.UrlKey == null && w.Link != null)
                .ToListAsync(cancellationToken);

            foreach (var row in legacy)
            {
                var key = UrlNormalizer.GetComparisonKey(row.Link);
                if (!string.IsNullOrEmpty(key) && wanted.Contains(key))
                {
                    existing.TryAdd(key, row);
                }
            }

            return existing;
        }

        private static List<string> TopicNamesFor(ParsedBookmark bookmark, BookmarkImportOptionsDto options, List<string> extraTopics)
        {
            var names = new List<string>();
            if (options.FoldersAsTopics) names.AddRange(bookmark.FolderPath);
            if (options.TagsAsTopics) names.AddRange(bookmark.Tags);
            var normalized = NormalizeNames(names);
            normalized.AddRange(extraTopics);
            return normalized.Distinct(StringComparer.Ordinal).ToList();
        }

        private static List<string> NormalizeNames(IEnumerable<string>? names) =>
            (names ?? Enumerable.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim().ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToList();

        private static async Task<bool> AddTagsAsync(
            Website website, List<string> topicNames, List<string> genreNames, TopicResolver topics, GenreResolver genres)
        {
            var changed = false;

            foreach (var name in topicNames)
            {
                if (website.Topics.Any(t => t.Name == name)) continue;
                var topic = await topics.GetOrCreateAsync(name);
                if (topic == null) continue;
                website.Topics.Add(topic);
                changed = true;
            }

            foreach (var name in genreNames)
            {
                if (website.Genres.Any(g => g.Name == name)) continue;
                var genre = await genres.GetOrCreateAsync(name);
                if (genre == null) continue;
                website.Genres.Add(genre);
                changed = true;
            }

            return changed;
        }

        private static string TitleFor(Candidate candidate)
        {
            var title = candidate.Bookmark.Title?.Trim();
            if (string.IsNullOrEmpty(title))
                title = string.IsNullOrEmpty(candidate.Domain) ? candidate.Link : candidate.Domain;

            return title.Length > TitleMaxLength ? title[..TitleMaxLength] : title;
        }

        private static string? BuildWarning(WebsiteBulkImportResultDto result)
        {
            var parts = new List<string>();
            if (result.NonWebLinkCount > 0)
                parts.Add($"{result.NonWebLinkCount} entr{(result.NonWebLinkCount == 1 ? "y was" : "ies were")} not web links and were ignored.");
            if (result.FailedCount > 0)
                parts.Add($"{result.FailedCount} entr{(result.FailedCount == 1 ? "y" : "ies")} could not be imported.");
            if (result.WasCancelled)
                parts.Add("The import was canceled; websites saved before that point were kept.");
            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        #endregion
    }
}
