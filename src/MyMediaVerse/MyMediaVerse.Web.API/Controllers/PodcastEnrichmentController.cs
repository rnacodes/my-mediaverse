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
        private readonly IImportReindexService _importReindexService;
        private readonly ILogger<PodcastEnrichmentController> _logger;

        public PodcastEnrichmentController(
            IPodcastEnrichmentService enrichmentService,
            IImportReindexService importReindexService,
            ILogger<PodcastEnrichmentController> logger)
        {
            _enrichmentService = enrichmentService;
            _importReindexService = importReindexService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the count of podcast series waiting to be filled from their feed.
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
                return StatusCode(500, new { error = "Failed to get enrichment status" });
            }
        }

        /// <summary>
        /// Triggers an on-demand podcast feed-fill run. 200 with the reporting-contract result when the
        /// run completed (failures included); 500 with the same body when the run itself aborted.
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
                    "Starting on-demand podcast enrichment. BatchSize: {BatchSize}, Delay: {Delay}ms",
                    batchSize, delayMs);

                var result = await _enrichmentService.EnrichPendingPodcastsAsync(
                    batchSize: batchSize,
                    delayBetweenCallsMs: delayMs,
                    cancellationToken: HttpContext.RequestAborted);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                await ReindexAsync(result);

                _logger.LogInformation(
                    "On-demand podcast enrichment completed. Filled: {Enriched}, unchanged: {Unchanged}, no feed: {NotFound}, failed: {Failed}",
                    result.EnrichedCount, result.UnchangedCount, result.NotFoundCount, result.FailedCount);

                return Ok(result);
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running on-demand podcast enrichment");
                return StatusCode(500, new { error = "Podcast enrichment run failed" });
            }
        }

        /// <summary>
        /// Runs enrichment for all podcasts until complete or limit reached.
        /// Stops early when a batch fills nothing, since the remaining series cannot be filled yet.
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
                var maxPodcasts = request?.MaxPodcasts ?? 100;
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
                    "Starting full podcast enrichment. BatchSize: {BatchSize}, MaxPodcasts: {MaxPodcasts}",
                    batchSize, maxPodcasts);

                var totalEnriched = 0;
                var totalUnchanged = 0;
                var totalNotFound = 0;
                var totalFailed = 0;
                var totalProcessed = 0;
                var allErrors = new List<string>();
                var batchesRun = 0;

                var pendingCount = await _enrichmentService.GetPodcastsNeedingEnrichmentCountAsync();

                while (pendingCount > 0 && totalProcessed < maxPodcasts)
                {
                    var result = await _enrichmentService.EnrichPendingPodcastsAsync(
                        batchSize: Math.Min(batchSize, maxPodcasts - totalProcessed),
                        delayBetweenCallsMs: delayMs,
                        cancellationToken: HttpContext.RequestAborted);

                    totalEnriched += result.EnrichedCount;
                    totalUnchanged += result.UnchangedCount;
                    totalNotFound += result.NotFoundCount;
                    totalFailed += result.FailedCount;
                    totalProcessed += result.TotalProcessed;
                    allErrors.AddRange(result.Errors.Take(5));
                    batchesRun++;

                    if (result.WasCancelled)
                    {
                        break;
                    }

                    // Nothing left that can be filled right now.
                    if (result.TotalProcessed == 0 || result.EnrichedCount == 0)
                    {
                        pendingCount = result.PendingCount;
                        break;
                    }

                    pendingCount = result.PendingCount;

                    // Pause between batches if there are more to process
                    if (pendingCount > 0 && totalProcessed < maxPodcasts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(pauseBetweenBatchesSeconds), HttpContext.RequestAborted);
                    }
                }

                // One reindex for the whole run rather than one per batch.
                var reindexTriggered = false;
                if (totalEnriched > 0)
                {
                    await _importReindexService.ReindexAfterImportAsync(totalEnriched, "podcast enrichment");
                    reindexTriggered = true;
                }

                _logger.LogInformation(
                    "Full podcast enrichment completed. Batches: {Batches}, Filled: {Enriched}, unchanged: {Unchanged}, no feed: {NotFound}, failed: {Failed}, remaining: {Remaining}",
                    batchesRun, totalEnriched, totalUnchanged, totalNotFound, totalFailed, pendingCount);

                return Ok(new PodcastEnrichmentRunAllResult
                {
                    TotalProcessed = totalProcessed,
                    TotalEnriched = totalEnriched,
                    TotalUnchanged = totalUnchanged,
                    TotalNotFound = totalNotFound,
                    TotalFailed = totalFailed,
                    BatchesRun = batchesRun,
                    RemainingPodcasts = pendingCount,
                    ReindexTriggered = reindexTriggered,
                    Errors = allErrors.Take(20).ToList()
                });
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running full podcast enrichment");
                return StatusCode(500, new { error = "Full enrichment run failed" });
            }
        }

        private async Task ReindexAsync(PodcastEnrichmentResult result)
        {
            if (result.EnrichedCount <= 0)
            {
                return;
            }

            await _importReindexService.ReindexAfterImportAsync(result.EnrichedCount, "podcast enrichment");
            result.ReindexTriggered = true;
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
        public int TotalUnchanged { get; set; }
        public int TotalNotFound { get; set; }
        public int TotalFailed { get; set; }
        public int BatchesRun { get; set; }
        public int RemainingPodcasts { get; set; }
        public bool ReindexTriggered { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
