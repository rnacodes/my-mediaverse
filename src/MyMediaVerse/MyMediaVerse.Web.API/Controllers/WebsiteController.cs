using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyMediaVerse.Application.Interfaces;
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
        private readonly ILogger<WebsiteController> _logger;

        public WebsiteController(
            IWebsiteService websiteService,
            IWebsiteMappingService websiteMappingService,
            IImportReindexService importReindexService,
            IWebsiteEnrichmentService enrichmentService,
            ILogger<WebsiteController> logger,
            IRssFeedService? rssFeedService = null)
        {
            _websiteService = websiteService;
            _websiteMappingService = websiteMappingService;
            _importReindexService = importReindexService;
            _enrichmentService = enrichmentService;
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

        private async Task<ActionResult<WebsiteResponseDto>> CreatedOrExistingAsync(WebsiteCreationResult result)
        {
            var response = await _websiteMappingService.MapToResponseDtoAsync(result.Website);
            return result.Created
                ? CreatedAtAction(nameof(GetWebsite), new { id = result.Website.Id }, response)
                : Ok(response);
        }
    }
}
