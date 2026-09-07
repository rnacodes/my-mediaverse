using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Extensions;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebsiteController : ControllerBase
    {
        private readonly IWebsiteService _websiteService;
        private readonly IWebsiteMappingService _websiteMappingService;
        private readonly IRssFeedService? _rssFeedService;
        private readonly IImportReindexService _importReindexService;
        private readonly IWebsiteEnrichmentService _enrichmentService;
        private readonly IWebsiteBulkImportService _bulkImportService;
        private readonly IEnumerable<IBookmarkFileParser> _bookmarkParsers;
        private readonly ILogger<WebsiteController> _logger;

        private const long MaxBookmarkFileBytes = 10 * 1024 * 1024;

        public WebsiteController(
            IWebsiteService websiteService,
            IWebsiteMappingService websiteMappingService,
            IImportReindexService importReindexService,
            IWebsiteEnrichmentService enrichmentService,
            IWebsiteBulkImportService bulkImportService,
            IEnumerable<IBookmarkFileParser> bookmarkParsers,
            ILogger<WebsiteController> logger,
            IRssFeedService? rssFeedService = null)
        {
            _websiteService = websiteService;
            _websiteMappingService = websiteMappingService;
            _importReindexService = importReindexService;
            _enrichmentService = enrichmentService;
            _bulkImportService = bulkImportService;
            _bookmarkParsers = bookmarkParsers;
            _rssFeedService = rssFeedService;
            _logger = logger;
        }

        // GET: api/website
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebsiteResponseDto>>> GetAllWebsites()
        {
            try
            {
                var websites = await _websiteService.GetAllWebsitesAsync();
                var response = await _websiteMappingService.MapToResponseDtoAsync(websites);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all websites");
                return StatusCode(500, new { error = "Failed to retrieve websites" });
            }
        }

        // GET: api/website/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<WebsiteResponseDto>> GetWebsite(Guid id)
        {
            try
            {
                var website = await _websiteService.GetWebsiteByIdAsync(id);
                if (website == null)
                {
                    return NotFound(new { error = $"Website with ID {id} not found." });
                }

                var response = await _websiteMappingService.MapToResponseDtoAsync(website);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving website with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve website" });
            }
        }

        // POST: api/website
        // 201 when a new website is created; 200 with the existing website when the URL is
        // already in the library (its metadata is filled in from the request where empty).
        [HttpPost]
        public async Task<ActionResult<WebsiteResponseDto>> CreateWebsite([FromBody] CreateWebsiteDto dto)
        {
            try
            {
                var result = await _websiteService.CreateWebsiteAsync(dto);
                return await CreatedOrExistingAsync(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating website");
                return StatusCode(500, new { error = "Failed to create website" });
            }
        }

        // PUT: api/website/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<WebsiteResponseDto>> UpdateWebsite(Guid id, [FromBody] CreateWebsiteDto dto)
        {
            try
            {
                var website = await _websiteService.UpdateWebsiteAsync(id, dto);
                var response = await _websiteMappingService.MapToResponseDtoAsync(website);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating website with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update website" });
            }
        }

        // DELETE: api/website/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteWebsite(Guid id)
        {
            try
            {
                var result = await _websiteService.DeleteWebsiteAsync(id);
                if (!result)
                {
                    return NotFound(new { error = $"Website with ID {id} not found." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting website with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete website" });
            }
        }

        // POST: api/website/from-url
        // Explicit [Authorize] even though the fallback policy already requires a token: this endpoint
        // writes to the library and fetches the target page (and possibly a screenshot) per request.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("from-url")]
        public async Task<ActionResult<WebsiteResponseDto>> ImportWebsite([FromBody] ImportWebsiteDto dto)
        {
            try
            {
                var result = await _websiteService.ImportWebsiteFromUrlAsync(dto);

                if (result.Created)
                {
                    await _importReindexService.ReindexItemAfterImportAsync(result.Website.Id, "website import");
                }

                return await CreatedOrExistingAsync(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch website for import: {Url}", dto.Url);
                return BadRequest(new { error = "Failed to fetch website" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing website from URL: {Url}", dto.Url);
                return StatusCode(500, new { error = "Failed to import website" });
            }
        }

        [Authorize]
        [HttpPost("from-bookmark-file")]
        public async Task<ActionResult<WebsiteBulkImportResultDto>> ImportBookmarkFile(IFormFile? file, [FromForm] BookmarkImportOptionsDto? options = null)
        {
            var (parsed, error) = await ParseBookmarkFileAsync(file);
            if (error != null) return error;

            try
            {
                var result = await _bulkImportService.ImportAsync(
                    parsed!, options ?? new BookmarkImportOptionsDto(),
                    WebsiteBulkImportResultDto.BookmarkImportOperation, HttpContext.RequestAborted);

                return await FinishBulkImportAsync(result, "bookmark import");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing bookmark file {FileName}", file?.FileName);
                return StatusCode(500, FailedBulkImport(WebsiteBulkImportResultDto.BookmarkImportOperation, "Failed to import the bookmark file"));
            }
        }

        // POST: api/website/from-bookmark-file/preview
        [Authorize]
        [HttpPost("from-bookmark-file/preview")]
        public async Task<ActionResult<WebsiteBulkImportPreviewDto>> PreviewBookmarkFile(IFormFile? file)
        {
            var (parsed, error) = await ParseBookmarkFileAsync(file);
            if (error != null) return error;

            try
            {
                return Ok(await _bulkImportService.PreviewAsync(parsed!, HttpContext.RequestAborted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing bookmark file {FileName}", file?.FileName);
                return StatusCode(500, new { error = "Failed to preview the bookmark file" });
            }
        }

        // POST: api/website/from-url-list  { urls: "one per line", urlList?: [], options? }
        [Authorize]
        [HttpPost("from-url-list")]
        public async Task<ActionResult<WebsiteBulkImportResultDto>> ImportUrlList([FromBody] UrlListImportRequestDto request)
        {
            var parsed = ParseUrlList(request);
            if (parsed.Bookmarks.Count == 0)
            {
                return BadRequest(new { error = "No web links were found in the list." });
            }

            try
            {
                var result = await _bulkImportService.ImportAsync(
                    parsed, request.Options ?? new BookmarkImportOptionsDto(),
                    WebsiteBulkImportResultDto.UrlListImportOperation, HttpContext.RequestAborted);

                return await FinishBulkImportAsync(result, "URL list import");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing URL list");
                return StatusCode(500, FailedBulkImport(WebsiteBulkImportResultDto.UrlListImportOperation, "Failed to import the URL list"));
            }
        }

        // POST: api/website/from-url-list/preview
        [Authorize]
        [HttpPost("from-url-list/preview")]
        public async Task<ActionResult<WebsiteBulkImportPreviewDto>> PreviewUrlList([FromBody] UrlListImportRequestDto request)
        {
            try
            {
                return Ok(await _bulkImportService.PreviewAsync(ParseUrlList(request), HttpContext.RequestAborted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing URL list");
                return StatusCode(500, new { error = "Failed to preview the URL list" });
            }
        }

        // GET: api/website/export — the library as a Netscape bookmark file.
        [Authorize]
        [HttpGet("export")]
        public async Task<IActionResult> ExportBookmarks()
        {
            try
            {
                var (content, fileName) = await _websiteService.ExportBookmarksAsync();
                return File(content, "text/html", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting bookmarks");
                return StatusCode(500, new { error = "Failed to export bookmarks" });
            }
        }

        // POST: api/website/scrape-preview
        // Explicit [Authorize]: fetches an arbitrary user-supplied URL through the server.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("scrape-preview")]
        public async Task<ActionResult<WebsitePreviewDto>> ScrapePreview([FromBody] ScrapePreviewRequestDto request)
        {
            try
            {
                var preview = await _websiteService.ScrapeWebsitePreviewAsync(request.Url);
                return Ok(preview);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch website for preview: {Url}", request.Url);
                return BadRequest(new { error = "Failed to fetch website" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping preview for URL: {Url}", request.Url);
                return StatusCode(500, new { error = "Failed to scrape website" });
            }
        }

        // POST: api/website/{id}/screenshot?force=false
        // Explicit [Authorize]: spends a render on the screenshot provider and writes the thumbnail.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("{id:guid}/screenshot")]
        public async Task<ActionResult<WebsiteScreenshotResultDto>> RegenerateScreenshot(Guid id, [FromQuery] bool force = false)
        {
            try
            {
                var result = await _websiteService.RegenerateScreenshotAsync(id, force, HttpContext.RequestAborted);
                if (result == null)
                {
                    return NotFound(new { error = $"Website with ID {id} not found." });
                }

                if (result.Rendered)
                {
                    await _importReindexService.ReindexItemAfterImportAsync(id, "website screenshot");
                    result.ReindexTriggered = true;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while regenerating the screenshot for website {Id}", id);
                return StatusCode(500, new WebsiteScreenshotResultDto
                {
                    Success = false,
                    ErrorMessage = "Failed to regenerate the screenshot",
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        // POST: api/website/{id}/enrich?force=false
        // Explicit [Authorize]: fetches the page, possibly a screenshot, and the Wayback index.
        // The filled description feeds the search embedding, so the item is reindexed after.
        [Authorize]
        [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
        [HttpPost("{id:guid}/enrich")]
        public async Task<ActionResult<SingleWebsiteEnrichmentResult>> EnrichWebsite(Guid id, [FromQuery] bool force = false)
        {
            try
            {
                var result = await _enrichmentService.EnrichByIdAsync(id, force, HttpContext.RequestAborted);

                if (result.NotFound)
                {
                    return NotFound(new { error = $"Website with ID {id} not found." });
                }

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                if (result.FilledFields.Count > 0)
                {
                    await _importReindexService.ReindexItemAfterImportAsync(id, "website enrichment");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while enriching website {Id}", id);
                return StatusCode(500, new SingleWebsiteEnrichmentResult
                {
                    Success = false,
                    ErrorMessage = "Failed to enrich the website"
                });
            }
        }

        // GET: api/website/by-domain/{domain}
        [HttpGet("by-domain/{domain}")]
        public async Task<ActionResult<IEnumerable<WebsiteResponseDto>>> GetWebsitesByDomain(string domain)
        {
            try
            {
                var websites = await _websiteService.GetWebsitesByDomainAsync(domain);
                var response = await _websiteMappingService.MapToResponseDtoAsync(websites);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving websites by domain: {Domain}", domain);
                return StatusCode(500, new { error = "Failed to retrieve websites" });
            }
        }

        // GET: api/website/with-rss
        [HttpGet("with-rss")]
        public async Task<ActionResult<IEnumerable<WebsiteResponseDto>>> GetWebsitesWithRss()
        {
            try
            {
                var websites = await _websiteService.GetWebsitesWithRssFeedsAsync();
                var response = await _websiteMappingService.MapToResponseDtoAsync(websites);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving websites with RSS feeds");
                return StatusCode(500, new { error = "Failed to retrieve websites" });
            }
        }

        // GET: api/website/{id}/rss-items
        [HttpGet("{id}/rss-items")]
        public async Task<ActionResult<IEnumerable<RssFeedItemDto>>> GetRssFeedItems(Guid id, [FromQuery] int maxItems = 3)
        {
            try
            {
                if (_rssFeedService == null)
                {
                    return StatusCode(503, new { error = "RSS feed service is not available" });
                }

                var website = await _websiteService.GetWebsiteByIdAsync(id);
                if (website == null)
                {
                    return NotFound(new { error = $"Website with ID {id} not found" });
                }

                if (string.IsNullOrEmpty(website.RssFeedUrl))
                {
                    return Ok(new List<RssFeedItemDto>()); // Return empty list if no RSS feed
                }

                var items = await _rssFeedService.GetLatestFeedItemsAsync(website.RssFeedUrl, maxItems);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching RSS items for website {Id}", id);
                return StatusCode(500, new { error = "Failed to fetch RSS feed items" });
            }
        }

        /// <summary>
        /// Reads and parses an uploaded bookmark file. Returns a 400 result for a missing, empty,
        /// oversized, or unrecognized file; a parser that throws surfaces as 500 + DTO.
        /// </summary>
        private async Task<(BookmarkParseResult? Parsed, ActionResult? Error)> ParseBookmarkFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return (null, BadRequest(new { error = "No bookmark file was uploaded." }));

            if (file.Length > MaxBookmarkFileBytes)
                return (null, BadRequest(new { error = "The bookmark file must be 10 MB or smaller." }));

            string content;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync(HttpContext.RequestAborted);
            }

            var head = content.Length > 512 ? content[..512] : content;
            var parser = _bookmarkParsers.FirstOrDefault(p => p.CanParse(file.FileName, head));
            if (parser == null)
                return (null, BadRequest(new { error = "This file is not a bookmark export. Export your bookmarks as an HTML file and try again." }));

            try
            {
                return (parser.Parse(content), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bookmark parser failed for {FileName}", file.FileName);
                return (null, StatusCode(500, FailedBulkImport(WebsiteBulkImportResultDto.BookmarkImportOperation, "The bookmark file could not be read")));
            }
        }

        private static BookmarkParseResult ParseUrlList(UrlListImportRequestDto request)
        {
            var text = request.Urls ?? string.Empty;
            if (request.UrlList is { Count: > 0 })
            {
                text = text + "\n" + string.Join("\n", request.UrlList);
            }

            return UrlListParser.Parse(text);
        }

        private async Task<ActionResult<WebsiteBulkImportResultDto>> FinishBulkImportAsync(WebsiteBulkImportResultDto result, string label)
        {
            if (!result.Success)
            {
                return StatusCode(500, result);
            }

            var changed = result.CreatedCount + result.UpdatedCount;
            if (changed > 0)
            {
                await _importReindexService.ReindexAfterImportAsync(changed, label);
                result.ReindexTriggered = true;
            }

            return Ok(result);
        }

        private static WebsiteBulkImportResultDto FailedBulkImport(string operation, string message) => new()
        {
            Success = false,
            Operation = operation,
            ErrorMessage = message,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        private async Task<ActionResult<WebsiteResponseDto>> CreatedOrExistingAsync(WebsiteCreationResult result)
        {
            var response = await _websiteMappingService.MapToResponseDtoAsync(result.Website);
            return result.Created
                ? CreatedAtAction(nameof(GetWebsite), new { id = result.Website.Id }, response)
                : Ok(response);
        }
    }
}
