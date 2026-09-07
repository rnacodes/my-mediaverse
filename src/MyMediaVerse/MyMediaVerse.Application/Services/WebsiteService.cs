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
    /// Service for managing Website media items. A website's identity is its normalized URL:
    /// every creating path runs through <see cref="WebsiteDuplicateFinder"/>, so saving a URL
    /// that is already in the library enriches the existing row instead of adding a second one.
    /// </summary>
    public class WebsiteService : IWebsiteService
    {
        private readonly IApplicationDbContext _context;
        private readonly IWebsiteScraperService _scraperService;
        private readonly ITypesenseService _typesenseService;
        private readonly IThumbnailStorageService _thumbnailStorage;
        private readonly IWebsiteScreenshotService? _screenshotService;
        private readonly IScreenshotQuota? _screenshotQuota;
        private readonly ILogger<WebsiteService> _logger;

        public WebsiteService(
            IApplicationDbContext context,
            IWebsiteScraperService scraperService,
            ITypesenseService typesenseService,
            IThumbnailStorageService thumbnailStorage,
            ILogger<WebsiteService> logger,
            IWebsiteScreenshotService? screenshotService = null,
            IScreenshotQuota? screenshotQuota = null)
        {
            _context = context;
            _scraperService = scraperService;
            _typesenseService = typesenseService;
            _thumbnailStorage = thumbnailStorage;
            _screenshotService = screenshotService;
            _screenshotQuota = screenshotQuota;
            _logger = logger;
        }

        /// <summary>Tracked query with the tag collections loaded, for paths that write to the row.</summary>
        private IQueryable<Website> TrackedWithTags => _context.Websites
            .Include(w => w.Topics)
            .Include(w => w.Genres);

        public async Task<IEnumerable<Website>> GetAllWebsitesAsync()
        {
            return await _context.Websites
                .AsNoTracking()
                .AsSplitQuery()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .OrderByDescending(w => w.DateAdded)
                .ToListAsync();
        }

        public async Task<Website?> GetWebsiteByIdAsync(Guid id)
        {
            return await _context.Websites
                .AsNoTracking()
                .AsSplitQuery()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<WebsiteCreationResult> CreateWebsiteAsync(CreateWebsiteDto dto)
        {
            var link = NormalizeLinkOrThrow(dto.Url);

            var incoming = new Website
            {
                Title = dto.Title,
                Description = dto.Description,
                Link = link,
                UrlKey = UrlNormalizer.GetComparisonKey(link),
                Domain = UrlNormalizer.ExtractDomain(link),
                Thumbnail = dto.Thumbnail,
                RssFeedUrl = dto.RssFeedUrl,
                Notes = dto.Notes,
                Author = dto.Author,
                Publication = dto.Publication,
                MediaType = MediaType.Website,
                Status = dto.Status ?? Status.Uncharted,
                Rating = dto.Rating,
                DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted),
                DateAdded = DateTime.UtcNow,
                LastCheckedDate = DateTime.UtcNow
            };

            await HandleTopicsAsync(incoming, dto.Topics);
            await HandleGenresAsync(incoming, dto.Genres);

            var existing = await WebsiteDuplicateFinder.FindExistingAsync(TrackedWithTags, link);
            if (existing != null)
            {
                if (WebsiteDuplicateFinder.AbsorbMetadata(existing, incoming))
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Website already exists for {Url} (ID: {Id}); returning existing row",
                    link, existing.Id);
                return new WebsiteCreationResult(existing, Created: false);
            }

            _context.Add(incoming);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created website with ID: {Id}, Title: {Title}", incoming.Id, incoming.Title);
            return new WebsiteCreationResult(incoming, Created: true);
        }

        public async Task<Website> UpdateWebsiteAsync(Guid id, CreateWebsiteDto dto)
        {
            var website = await TrackedWithTags.FirstOrDefaultAsync(w => w.Id == id);
            if (website == null)
                throw new KeyNotFoundException($"Website with ID {id} not found.");

            var link = NormalizeLinkOrThrow(dto.Url);
            var key = UrlNormalizer.GetComparisonKey(link);

            if (key != website.UrlKey)
            {
                var other = await WebsiteDuplicateFinder.FindExistingAsync(_context.Websites, link);
                if (other != null && other.Id != id)
                {
                    throw new InvalidOperationException(
                        $"Another website already uses this URL (ID: {other.Id}).");
                }
            }

            website.Title = dto.Title;
            website.Description = dto.Description;
            website.Link = link;
            website.UrlKey = key;
            website.Domain = UrlNormalizer.ExtractDomain(link);
            website.Thumbnail = dto.Thumbnail;
            website.RssFeedUrl = dto.RssFeedUrl;
            website.Notes = dto.Notes;
            website.Author = dto.Author;
            website.Publication = dto.Publication;
            website.LastCheckedDate = DateTime.UtcNow;

            // Null means "leave as is" so callers that never sent these fields keep their values.
            if (dto.Status.HasValue) website.Status = dto.Status.Value;
            if (dto.Rating.HasValue) website.Rating = dto.Rating;
            if (dto.DateCompleted.HasValue) website.DateCompleted = DateTimeNormalizer.ToUtc(dto.DateCompleted);

            website.Topics.Clear();
            await HandleTopicsAsync(website, dto.Topics);

            website.Genres.Clear();
            await HandleGenresAsync(website, dto.Genres);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated website with ID: {Id}", id);
            return website;
        }

        public async Task<bool> DeleteWebsiteAsync(Guid id)
        {
            var website = await _context.Websites
                .Include(w => w.Mixlists)
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (website == null)
                return false;

            // Storage skips URLs outside our bucket, so provider-hosted images are left alone.
            if (!string.IsNullOrEmpty(website.Thumbnail))
            {
                await _thumbnailStorage.DeleteAsync(website.Thumbnail);
            }

            website.Mixlists.Clear();
            website.Topics.Clear();
            website.Genres.Clear();

            _context.Remove(website);
            await _context.SaveChangesAsync();

            // Eager search-index cleanup so the deleted website stops appearing in search immediately.
            // Best effort: the next bulk reindex reconciles anything this misses.
            await SearchIndexCleanup.TryDeleteAsync(
                () => _typesenseService.DeleteMediaItemAsync(id), _logger, "website", id);

            _logger.LogInformation("Deleted website with ID: {Id}", id);
            return true;
        }

        public async Task<WebsiteCreationResult> ImportWebsiteFromUrlAsync(ImportWebsiteDto dto)
        {
            _logger.LogInformation("Importing website from URL: {Url}", dto.Url);

            // Probe before scraping so a known URL costs no fetch and no screenshot.
            var existing = await WebsiteDuplicateFinder.FindExistingAsync(TrackedWithTags, dto.Url);
            if (existing != null)
            {
                if (WebsiteDuplicateFinder.FillIdentity(existing))
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Website already exists for {Url} (ID: {Id}); skipping scrape",
                    dto.Url, existing.Id);
                return new WebsiteCreationResult(existing, Created: false);
            }

            var scrapedData = await _scraperService.ScrapeWebsiteAsync(dto.Url);

            // Get thumbnail - use scraped image or capture screenshot if not available
            var thumbnail = scrapedData.ImageUrl;
            if (string.IsNullOrEmpty(thumbnail) && _screenshotService != null)
            {
                _logger.LogInformation("No og:image found, capturing screenshot for: {Url}", dto.Url);
                try
                {
                    thumbnail = await _screenshotService.CaptureScreenshotAsync(dto.Url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to capture screenshot for URL: {Url}. Continuing without thumbnail.", dto.Url);
                }
            }

            var createDto = new CreateWebsiteDto
            {
                Url = dto.Url,
                Title = dto.TitleOverride ?? scrapedData.Title ?? scrapedData.Domain ?? "Untitled Website",
                Description = scrapedData.Description,
                Thumbnail = thumbnail,
                RssFeedUrl = scrapedData.RssFeedUrl,
                Notes = dto.Notes,
                Topics = dto.Topics,
                Genres = dto.Genres,
                Author = scrapedData.Author,
                Publication = scrapedData.Publication
            };

            return await CreateWebsiteAsync(createDto);
        }

        public async Task<WebsitePreviewDto> ScrapeWebsitePreviewAsync(string url)
        {
            _logger.LogInformation("Scraping preview for URL: {Url}", url);
            var scraped = await _scraperService.ScrapeWebsiteAsync(url);

            var existing = await WebsiteDuplicateFinder.FindExistingAsync(_context.Websites.AsNoTracking(), url);
            return WebsitePreviewDto.FromScraped(scraped, existing?.Id, existing?.Title);
        }

        public async Task<WebsiteScreenshotResultDto?> RegenerateScreenshotAsync(Guid id, bool force, CancellationToken cancellationToken = default)
        {
            var result = new WebsiteScreenshotResultDto { StartedAt = DateTime.UtcNow };

            var website = await _context.Websites.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
            if (website == null)
                return null;

            result.Thumbnail = website.Thumbnail;

            if (!string.IsNullOrEmpty(website.Thumbnail) && !force)
            {
                result.Skipped = true;
                result.WarningMessage = "The website already has a thumbnail; pass force=true to replace it.";
                return Complete(result);
            }

            if (_screenshotService == null)
            {
                result.WarningMessage = "Screenshots are not configured.";
                return Complete(result);
            }

            if (_screenshotQuota != null && await _screenshotQuota.RemainingAsync(cancellationToken) <= 0)
            {
                result.WarningMessage = "The monthly screenshot quota has been reached; try again next month or switch the screenshot provider.";
                return Complete(result);
            }

            var newThumbnail = await _screenshotService.CaptureScreenshotAsync(website.Link ?? string.Empty, cancellationToken);
            if (newThumbnail == null)
            {
                result.WarningMessage = "No usable screenshot could be rendered for this page.";
                return Complete(result);
            }

            var previous = website.Thumbnail;
            website.Thumbnail = newThumbnail;
            website.LastCheckedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Storage ignores URLs outside our bucket, so a provider-hosted og:image is left alone.
            if (!string.IsNullOrEmpty(previous) && previous != newThumbnail)
            {
                await _thumbnailStorage.DeleteAsync(previous);
            }

            result.Rendered = true;
            result.Thumbnail = newThumbnail;
            _logger.LogInformation("Regenerated screenshot for website {Id}: {Thumbnail}", id, newThumbnail);
            return Complete(result);

            static WebsiteScreenshotResultDto Complete(WebsiteScreenshotResultDto r)
            {
                r.CompletedAt = DateTime.UtcNow;
                return r;
            }
        }

        public async Task<IEnumerable<Website>> GetWebsitesByDomainAsync(string domain)
        {
            var normalizedDomain = domain.ToLowerInvariant();
            return await _context.Websites
                .AsNoTracking()
                .AsSplitQuery()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .Where(w => w.Domain != null && w.Domain.ToLower() == normalizedDomain)
                .OrderByDescending(w => w.DateAdded)
                .ToListAsync();
        }

        public async Task<IEnumerable<Website>> GetWebsitesWithRssFeedsAsync()
        {
            return await _context.Websites
                .AsNoTracking()
                .AsSplitQuery()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .Where(w => w.RssFeedUrl != null && w.RssFeedUrl != "")
                .OrderByDescending(w => w.DateAdded)
                .ToListAsync();
        }

        private static string NormalizeLinkOrThrow(string? url)
        {
            if (!UrlNormalizer.IsValid(url))
                throw new ArgumentException("A valid http or https URL is required.");

            return UrlNormalizer.Normalize(url);
        }

        private async Task HandleTopicsAsync(Website website, IEnumerable<string>? topics)
        {
            if (topics == null) return;

            var resolver = new TopicResolver(_context);
            foreach (var name in topics.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var topic = await resolver.GetOrCreateAsync(name.Trim().ToLowerInvariant());
                if (topic != null && !website.Topics.Contains(topic))
                {
                    website.Topics.Add(topic);
                }
            }
        }

        private async Task HandleGenresAsync(Website website, IEnumerable<string>? genres)
        {
            if (genres == null) return;

            var resolver = new GenreResolver(_context);
            foreach (var name in genres.Where(g => !string.IsNullOrWhiteSpace(g)))
            {
                var normalizedGenreName = name.Trim().ToLowerInvariant();
                if (website.Genres.Any(g => g.Name == normalizedGenreName)) continue;

                var genre = await resolver.GetOrCreateAsync(normalizedGenreName);
                if (genre != null)
                {
                    website.Genres.Add(genre);
                }
            }
        }

        public async Task<(byte[] Content, string FileName)> ExportBookmarksAsync()
        {
            var websites = await _context.Websites
                .AsNoTracking()
                .Include(w => w.Topics)
                .OrderBy(w => w.DateAdded)
                .ToListAsync();

            var html = NetscapeBookmarkWriter.Write(websites);
            return (System.Text.Encoding.UTF8.GetBytes(html), NetscapeBookmarkWriter.FileName(DateTime.UtcNow));
        }
    }
}
