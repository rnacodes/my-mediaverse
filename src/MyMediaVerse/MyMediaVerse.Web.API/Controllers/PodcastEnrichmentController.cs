using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PodcastEnrichmentController : ControllerBase
    {
        private readonly IPodcastEnrichmentService _enrichmentService;
        private readonly ILogger<PodcastEnrichmentController> _logger;

        public PodcastEnrichmentController(
            IPodcastEnrichmentService enrichmentService,
            ILogger<PodcastEnrichmentController> logger)
        {
            _enrichmentService = enrichmentService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the count of podcast series that need ListenNotes enrichment (have no ExternalId).
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<PodcastEnrichmentStatusDto>> GetStatus()
        {
            try
            {
                var pendingCount = await _enrichmentService.GetPodcastsNeedingEnrichmentCountAsync();

                return Ok(new PodcastEnrichmentStatusDto
                {
                    PodcastsNeedingEnrichment = pendingCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting podcast enrichment status");
                return StatusCode(500, new { error = "Failed to get enrichment status", details = ex.Message });
            }
        }

        /// <summary>
        /// Triggers an on-demand podcast ListenNotes enrichment run.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run")]
        public async Task<ActionResult<PodcastEnrichmentResult>> RunEnrichment(
            [FromBody] RunPodcastEnrichmentRequest? request = null)
        {
            try
            {
                var batchSize = request?.BatchSize ?? 25;
                var delayMs = request?.DelayBetweenCallsMs ?? 1500;

                // Validate parameters
                if (batchSize < 1 || batchSize > 100)
                {
                    return BadRequest(new { error = "BatchSize must be between 1 and 100" });
                }

                if (delayMs < 500 || delayMs > 30000)
                {
                    return BadRequest(new { error = "DelayBetweenCallsMs must be between 500 and 30000" });
                }

                _logger.LogInformation(
                    "Starting on-demand podcast ListenNotes enrichment. BatchSize: {BatchSize}, Delay: {Delay}ms",
                    batchSize, delayMs);

                var result = await _enrichmentService.EnrichPodcastsWithoutListenNotesDataAsync(
                    batchSize: batchSize,
                    delayBetweenCallsMs: delayMs);

                _logger.LogInformation(
                    "On-demand podcast enrichment completed. Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}",
                    result.EnrichedCount, result.NotFoundCount, result.FailedCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running on-demand podcast enrichment");
                return StatusCode(500, new { error = "Podcast enrichment run failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Runs enrichment for all podcasts until complete or limit reached.
        /// Use with caution - ListenNotes has strict rate limits.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run-all")]
        public async Task<ActionResult<PodcastEnrichmentRunAllResult>> RunEnrichmentAll(
            [FromBody] RunPodcastEnrichmentAllRequest? request = null)
        {
            try
            {
                var batchSize = request?.BatchSize ?? 25;
                var delayMs = request?.DelayBetweenCallsMs ?? 1500;
                var maxPodcasts = request?.MaxPodcasts ?? 100; // More conservative default due to API limits
                var pauseBetweenBatchesSeconds = request?.PauseBetweenBatchesSeconds ?? 60;

                // Validate parameters
                if (batchSize < 1 || batchSize > 50)
                {
                    return BadRequest(new { error = "BatchSize must be between 1 and 50" });
                }

                if (maxPodcasts < 1 || maxPodcasts > 500)
                {
                    return BadRequest(new { error = "MaxPodcasts must be between 1 and 500" });
                }

                _logger.LogInformation(
                    "Starting full podcast ListenNotes enrichment. BatchSize: {BatchSize}, MaxPodcasts: {MaxPodcasts}",
                    batchSize, maxPodcasts);

                var totalEnriched = 0;
                var totalNotFound = 0;
                var totalFailed = 0;
                var totalProcessed = 0;
                var allErrors = new List<string>();
                var batchesRun = 0;

                var pendingCount = await _enrichmentService.GetPodcastsNeedingEnrichmentCountAsync();

                while (pendingCount > 0 && totalProcessed < maxPodcasts)
                {
                    var result = await _enrichmentService.EnrichPodcastsWithoutListenNotesDataAsync(
                        batchSize: Math.Min(batchSize, maxPodcasts - totalProcessed),
                        delayBetweenCallsMs: delayMs);

                    totalEnriched += result.EnrichedCount;
                    totalNotFound += result.NotFoundCount;
                    totalFailed += result.FailedCount;
                    totalProcessed += result.TotalProcessed;
                    allErrors.AddRange(result.Errors.Take(5));
                    batchesRun++;

                    if (result.TotalProcessed == 0)
                    {
                        break; // No more podcasts to process
                    }

                    // Get updated count
                    pendingCount = await _enrichmentService.GetPodcastsNeedingEnrichmentCountAsync();

                    // Pause between batches if there are more to process
                    if (pendingCount > 0 && totalProcessed < maxPodcasts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(pauseBetweenBatchesSeconds));
                    }
                }

                _logger.LogInformation(
                    "Full podcast enrichment completed. Batches: {Batches}, Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}, Remaining: {Remaining}",
                    batchesRun, totalEnriched, totalNotFound, totalFailed, pendingCount);

                return Ok(new PodcastEnrichmentRunAllResult
                {
                    TotalProcessed = totalProcessed,
                    TotalEnriched = totalEnriched,
                    TotalNotFound = totalNotFound,
                    TotalFailed = totalFailed,
                    BatchesRun = batchesRun,
                    RemainingPodcasts = pendingCount,
                    Errors = allErrors.Take(20).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running full podcast enrichment");
                return StatusCode(500, new { error = "Full enrichment run failed", details = ex.Message });
            }
        }
    }

    public class PodcastEnrichmentStatusDto
    {
        public int PodcastsNeedingEnrichment { get; set; }
    }

    public class RunPodcastEnrichmentRequest
    {
        /// <summary>
        /// Number of podcasts to process in this run (1-100, default: 25)
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Delay between API calls in milliseconds (500-30000, default: 1500)
        /// </summary>
        public int? DelayBetweenCallsMs { get; set; }
    }

    public class RunPodcastEnrichmentAllRequest
    {
        /// <summary>
        /// Number of podcasts per batch (1-50, default: 25)
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Delay between API calls in milliseconds (default: 1500)
        /// </summary>
        public int? DelayBetweenCallsMs { get; set; }

        /// <summary>
        /// Maximum total podcasts to process (1-500, default: 100)
        /// </summary>
        public int? MaxPodcasts { get; set; }

        /// <summary>
        /// Pause in seconds between batches (default: 60)
        /// </summary>
        public int? PauseBetweenBatchesSeconds { get; set; }
    }

    public class PodcastEnrichmentRunAllResult
    {
        public int TotalProcessed { get; set; }
        public int TotalEnriched { get; set; }
        public int TotalNotFound { get; set; }
        public int TotalFailed { get; set; }
        public int BatchesRun { get; set; }
        public int RemainingPodcasts { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
