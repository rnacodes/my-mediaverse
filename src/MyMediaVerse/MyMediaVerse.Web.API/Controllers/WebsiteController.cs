using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebsiteController : ControllerBase
    {
        private readonly IWebsiteService _websiteService;
        private readonly IWebsiteMappingService _websiteMappingService;
        private readonly IRssFeedService? _rssFeedService;
        private readonly ILogger<WebsiteController> _logger;

        public WebsiteController(
            IWebsiteService websiteService,
            IWebsiteMappingService websiteMappingService,
            ILogger<WebsiteController> logger,
            IRssFeedService? rssFeedService = null)
        {
            _websiteService = websiteService;
            _websiteMappingService = websiteMappingService;
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
                return StatusCode(500, new { error = "Failed to retrieve websites", details = ex.Message });
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
                    return NotFound($"Website with ID {id} not found.");
                }

                var response = await _websiteMappingService.MapToResponseDtoAsync(website);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving website with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve website", details = ex.Message });
            }
        }

        // POST: api/website
        [HttpPost]
        public async Task<ActionResult<WebsiteResponseDto>> CreateWebsite([FromBody] CreateWebsiteDto dto)
        {
            try
            {
                var website = await _websiteService.CreateWebsiteAsync(dto);
                var response = await _websiteMappingService.MapToResponseDtoAsync(website);
                return CreatedAtAction(nameof(GetWebsite), new { id = website.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating website");
                return StatusCode(500, new { error = "Failed to create website", details = ex.Message });
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating website with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update website", details = ex.Message });
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
                    return NotFound($"Website with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting website with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete website", details = ex.Message });
            }
        }

        // POST: api/website/import
        [HttpPost("import")]
        public async Task<ActionResult<WebsiteResponseDto>> ImportWebsite([FromBody] ImportWebsiteDto dto)
        {
            try
            {
                var website = await _websiteService.ImportWebsiteFromUrlAsync(dto);
                var response = await _websiteMappingService.MapToResponseDtoAsync(website);
                return CreatedAtAction(nameof(GetWebsite), new { id = website.Id }, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(new { error = "Failed to fetch website", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while importing website from URL: {Url}", dto.Url);
                return StatusCode(500, new { error = "Failed to import website", details = ex.Message });
            }
        }

        // POST: api/website/scrape-preview
        [HttpPost("scrape-preview")]
        public async Task<ActionResult<ScrapedWebsiteDataDto>> ScrapePreview([FromBody] string url)
        {
            try
            {
                var scrapedData = await _websiteService.ScrapeWebsitePreviewAsync(url);
                return Ok(scrapedData);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(new { error = "Failed to fetch website", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scraping preview for URL: {Url}", url);
                return StatusCode(500, new { error = "Failed to scrape website", details = ex.Message });
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
                return StatusCode(500, new { error = "Failed to retrieve websites", details = ex.Message });
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
                return StatusCode(500, new { error = "Failed to retrieve websites", details = ex.Message });
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
                return StatusCode(500, new { error = "Failed to fetch RSS feed items", details = ex.Message });
            }
        }
    }
}

