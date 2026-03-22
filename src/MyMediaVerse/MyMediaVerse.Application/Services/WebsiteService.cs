using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for managing Website media items.
    /// </summary>
    public class WebsiteService : IWebsiteService
    {
        private readonly IApplicationDbContext _context;
        private readonly IWebsiteScraperService _scraperService;
        private readonly IWebsiteScreenshotService? _screenshotService;
        private readonly ILogger<WebsiteService> _logger;

        public WebsiteService(
            IApplicationDbContext context,
            IWebsiteScraperService scraperService,
            ILogger<WebsiteService> logger,
            IWebsiteScreenshotService? screenshotService = null)
        {
            _context = context;
            _scraperService = scraperService;
            _screenshotService = screenshotService;
            _logger = logger;
        }

        public async Task<IEnumerable<Website>> GetAllWebsitesAsync()
        {
            return await _context.MediaItems
                .OfType<Website>()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .OrderByDescending(w => w.DateAdded)
                .ToListAsync();
        }

        public async Task<Website?> GetWebsiteByIdAsync(Guid id)
        {
            return await _context.MediaItems
                .OfType<Website>()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<Website> CreateWebsiteAsync(CreateWebsiteDto dto)
        {
            var website = new Website
            {
                Title = dto.Title,
                Description = dto.Description,
                Link = dto.Url,
                Thumbnail = dto.Thumbnail,
                RssFeedUrl = dto.RssFeedUrl,
                Notes = dto.Notes,
                Author = dto.Author,
                Publication = dto.Publication,
                MediaType = MediaType.Website,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                LastCheckedDate = DateTime.UtcNow
            };

            // Extract domain from URL
            if (Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri))
            {
                website.Domain = uri.Host.StartsWith("www.") ? uri.Host[4..] : uri.Host;
            }

            // Handle Topics
            if (dto.Topics?.Any() == true)
            {
                foreach (var topicName in dto.Topics)
                {
                    var normalizedTopicName = topicName.ToLowerInvariant();
                    var topic = await _context.Topics
                        .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);

                    if (topic == null)
                    {
                        topic = new Topic { Name = normalizedTopicName };
                        _context.Add(topic);
                    }

                    website.Topics.Add(topic);
                }
            }

            // Handle Genres
            if (dto.Genres?.Any() == true)
            {
                foreach (var genreName in dto.Genres)
                {
                    var normalizedGenreName = genreName.ToLowerInvariant();
                    var genre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);

                    if (genre == null)
                    {
                        genre = new Genre { Name = normalizedGenreName };
                        _context.Add(genre);
                    }

                    website.Genres.Add(genre);
                }
            }

            _context.Add(website);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created website with ID: {Id}, Title: {Title}", website.Id, website.Title);
            return website;
        }

        public async Task<Website> UpdateWebsiteAsync(Guid id, CreateWebsiteDto dto)
        {
            var website = await GetWebsiteByIdAsync(id);
            if (website == null)
                throw new KeyNotFoundException($"Website with ID {id} not found.");

            // Update basic properties
            website.Title = dto.Title;
            website.Description = dto.Description;
            website.Link = dto.Url;
            website.Thumbnail = dto.Thumbnail;
            website.RssFeedUrl = dto.RssFeedUrl;
            website.Notes = dto.Notes;
            website.Author = dto.Author;
            website.Publication = dto.Publication;
            website.LastCheckedDate = DateTime.UtcNow;

            // Update domain
            if (Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri))
            {
                website.Domain = uri.Host.StartsWith("www.") ? uri.Host[4..] : uri.Host;
            }

            // Update Topics
            website.Topics.Clear();
            if (dto.Topics?.Any() == true)
            {
                foreach (var topicName in dto.Topics)
                {
                    var normalizedTopicName = topicName.ToLowerInvariant();
                    var topic = await _context.Topics
                        .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);

                    if (topic == null)
                    {
                        topic = new Topic { Name = normalizedTopicName };
                        _context.Add(topic);
                    }

                    website.Topics.Add(topic);
                }
            }

            // Update Genres
            website.Genres.Clear();
            if (dto.Genres?.Any() == true)
            {
                foreach (var genreName in dto.Genres)
                {
                    var normalizedGenreName = genreName.ToLowerInvariant();
                    var genre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name == normalizedGenreName);

                    if (genre == null)
                    {
                        genre = new Genre { Name = normalizedGenreName };
                        _context.Add(genre);
                    }

                    website.Genres.Add(genre);
                }
            }

            _context.Update(website);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated website with ID: {Id}", id);
            return website;
        }

        public async Task<bool> DeleteWebsiteAsync(Guid id)
        {
            var website = await GetWebsiteByIdAsync(id);
            if (website == null)
                return false;

            var websiteId = website.Id;

            _context.Remove(website);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted website with ID: {Id}", id);
            return true;
        }

        public async Task<Website> ImportWebsiteFromUrlAsync(ImportWebsiteDto dto)
        {
            _logger.LogInformation("Importing website from URL: {Url}", dto.Url);

            // Scrape the website
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

            // Create the website using scraped data
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

        public async Task<ScrapedWebsiteDataDto> ScrapeWebsitePreviewAsync(string url)
        {
            _logger.LogInformation("Scraping preview for URL: {Url}", url);
            return await _scraperService.ScrapeWebsiteAsync(url);
        }

        public async Task<IEnumerable<Website>> GetWebsitesByDomainAsync(string domain)
        {
            var normalizedDomain = domain.ToLowerInvariant();
            return await _context.MediaItems
                .OfType<Website>()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .Where(w => w.Domain != null && w.Domain.ToLower() == normalizedDomain)
                .OrderByDescending(w => w.DateAdded)
                .ToListAsync();
        }

        public async Task<IEnumerable<Website>> GetWebsitesWithRssFeedsAsync()
        {
            return await _context.MediaItems
                .OfType<Website>()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .Where(w => w.RssFeedUrl != null && w.RssFeedUrl != "")
                .OrderByDescending(w => w.DateAdded)
                .ToListAsync();
        }

        public async Task<Website?> GetWebsiteByUrlAsync(string url)
        {
            return await _context.MediaItems
                .OfType<Website>()
                .Include(w => w.Topics)
                .Include(w => w.Genres)
                .FirstOrDefaultAsync(w => w.Link == url);
        }
    }
}

