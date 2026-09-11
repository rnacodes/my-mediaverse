using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Extensions;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// On-demand website enrichment runs: metadata fill for stubs, link health checks, and
    /// thumbnail repair. Every action fetches other people's servers on the caller's behalf, so
    /// the whole controller requires a token and sits behind the external-proxy rate limit.
    /// </summary>
    [ApiController]
    [Route("api/website/enrichment")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.ExternalProxyPolicy)]
    public class WebsiteEnrichmentController : ControllerBase
    {
        private readonly IWebsiteEnrichmentService _enrichmentService;
        private readonly IScreenshotQuota _screenshotQuota;
        private readonly IImportReindexService _importReindexService;
        private readonly WebsiteEnrichmentOptions _options;
        private readonly ILogger<WebsiteEnrichmentController> _logger;

        public WebsiteEnrichmentController(
            IWebsiteEnrichmentService enrichmentService,
            IScreenshotQuota screenshotQuota,
            IImportReindexService importReindexService,
            IOptions<WebsiteEnrichmentOptions> options,
            ILogger<WebsiteEnrichmentController> logger)
        {
            _enrichmentService = enrichmentService;
            _screenshotQuota = screenshotQuota;
            _importReindexService = importReindexService;
            _options = options.Value;
            _logger = logger;
        }

        // GET: api/website/enrichment/status
        [HttpGet("status")]
        public async Task<ActionResult<WebsiteEnrichmentStatusDto>> GetStatus()
        {
            try
            {
                return Ok(new WebsiteEnrichmentStatusDto
                {
                    PendingCount = await _enrichmentService.GetPendingCountAsync(HttpContext.RequestAborted),
                    ScreenshotQuotaRemaining = await _screenshotQuota.RemainingAsync(HttpContext.RequestAborted)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting website enrichment status");
                return StatusCode(500, new { error = "Failed to get enrichment status" });
            }
        }

        // POST: api/website/enrichment/run?limit=50
        // Returns 500 with the result body when the run itself aborted; per-website problems are
        // counts with a 200. The description feeds the search embedding, so the reindex runs last.
        [HttpPost("run")]
        public async Task<ActionResult<WebsiteEnrichmentResult>> RunEnrichment([FromQuery] int? limit = null)
        {
            var pageSize = limit ?? _options.DefaultLimit;
            if (pageSize < 1 || pageSize > _options.MaxLimit)
            {
                return BadRequest(new { error = $"limit must be between 1 and {_options.MaxLimit}" });
            }

            try
            {
                var result = await _enrichmentService.EnrichPendingAsync(pageSize, HttpContext.RequestAborted);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                if (result.TimeBudgetReached && result.PendingCount > 0)
                {
                    _logger.LogInformation(
                        "Website enrichment: reindex deferred, {Pending} still pending after a time-budgeted page",
                        result.PendingCount);
                }
                else
                {
                    await _importReindexService.ReindexAfterImportAsync(result.EnrichedCount, "website enrichment");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running website enrichment");
                return StatusCode(500, new WebsiteEnrichmentResult
                {
                    Success = false,
                    ErrorMessage = "Enrichment run failed",
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        // POST: api/website/enrichment/check-links?limit=100&olderThanDays=30
        [HttpPost("check-links")]
        public async Task<ActionResult<WebsiteLinkCheckResult>> CheckLinks([FromQuery] int? limit = null, [FromQuery] int? olderThanDays = null)
        {
            var pageSize = limit ?? _options.DefaultLinkCheckLimit;
            var days = olderThanDays ?? _options.DefaultLinkCheckOlderThanDays;

            if (pageSize < 1 || pageSize > _options.MaxLimit)
            {
                return BadRequest(new { error = $"limit must be between 1 and {_options.MaxLimit}" });
            }

            if (days < 0)
            {
                return BadRequest(new { error = "olderThanDays must be zero or greater" });
            }

            try
            {
                var result = await _enrichmentService.CheckLinksAsync(pageSize, days, HttpContext.RequestAborted);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                // Only rows whose status actually moved need to reach the index.
                await _importReindexService.ReindexAfterImportAsync(result.ChangedCount, "website link check");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running website link check");
                return StatusCode(500, new WebsiteLinkCheckResult
                {
                    Success = false,
                    ErrorMessage = "Link check run failed",
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        // POST: api/website/enrichment/repair-thumbnails?limit=50
        [HttpPost("repair-thumbnails")]
        public async Task<ActionResult<WebsiteEnrichmentResult>> RepairThumbnails([FromQuery] int? limit = null)
        {
            var pageSize = limit ?? _options.DefaultLimit;
            if (pageSize < 1 || pageSize > _options.MaxLimit)
            {
                return BadRequest(new { error = $"limit must be between 1 and {_options.MaxLimit}" });
            }

            try
            {
                var result = await _enrichmentService.RepairThumbnailsAsync(pageSize, HttpContext.RequestAborted);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                // Re-rendered and cleared thumbnails both change what search results display.
                await _importReindexService.ReindexAfterImportAsync(result.EnrichedCount + result.SkippedCount, "website thumbnail repair");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running website thumbnail repair");
                return StatusCode(500, new WebsiteEnrichmentResult
                {
                    Success = false,
                    Operation = WebsiteEnrichmentResult.ThumbnailRepairOperation,
                    ErrorMessage = "Thumbnail repair run failed",
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }
    }
}
