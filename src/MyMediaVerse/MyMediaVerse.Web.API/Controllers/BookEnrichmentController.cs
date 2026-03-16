using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookEnrichmentController : ControllerBase
    {
        private readonly IBookDescriptionEnrichmentService _enrichmentService;
        private readonly ILogger<BookEnrichmentController> _logger;

        public BookEnrichmentController(
            IBookDescriptionEnrichmentService enrichmentService,
            ILogger<BookEnrichmentController> logger)
        {
            _enrichmentService = enrichmentService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the count of books that need description enrichment (have ISBN but no description).
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<BookEnrichmentStatusDto>> GetStatus()
        {
            try
            {
                var pendingCount = await _enrichmentService.GetBooksNeedingEnrichmentCountAsync();

                return Ok(new BookEnrichmentStatusDto
                {
                    BooksNeedingEnrichment = pendingCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book enrichment status");
                return StatusCode(500, new { error = "Failed to get enrichment status", details = ex.Message });
            }
        }

        /// <summary>
        /// Enriches a single book by its ID.
        /// </summary>
        /// <param name="id">The media ID of the book to enrich</param>
        [HttpPost("{id:guid}")]
        public async Task<ActionResult<SingleBookEnrichmentResult>> EnrichSingleBook(Guid id)
        {
            try
            {
                _logger.LogInformation("Starting single book enrichment for ID: {BookId}", id);

                var result = await _enrichmentService.EnrichBookByIdAsync(id);

                if (result.NotFound)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                _logger.LogInformation(
                    "Single book enrichment completed for {Title}. Success: {Success}",
                    result.BookTitle, result.Success);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running single book enrichment for ID: {BookId}", id);
                return StatusCode(500, new { error = "Enrichment failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Triggers an on-demand book description enrichment run.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run")]
        public async Task<ActionResult<BookDescriptionEnrichmentResult>> RunEnrichment(
            [FromBody] RunEnrichmentRequest? request = null)
        {
            try
            {
                var batchSize = request?.BatchSize ?? 50;
                var delayMs = request?.DelayBetweenCallsMs ?? 1000;

                // Validate parameters
                if (batchSize < 1 || batchSize > 500)
                {
                    return BadRequest(new { error = "BatchSize must be between 1 and 500" });
                }

                if (delayMs < 100 || delayMs > 10000)
                {
                    return BadRequest(new { error = "DelayBetweenCallsMs must be between 100 and 10000" });
                }

                _logger.LogInformation(
                    "Starting on-demand book description enrichment. BatchSize: {BatchSize}, Delay: {Delay}ms",
                    batchSize, delayMs);

                var result = await _enrichmentService.EnrichBooksWithoutDescriptionsAsync(
                    batchSize: batchSize,
                    delayBetweenCallsMs: delayMs);

                _logger.LogInformation(
                    "On-demand enrichment completed. Enriched: {Enriched}, Failed: {Failed}",
                    result.EnrichedCount, result.FailedCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running on-demand book enrichment");
                return StatusCode(500, new { error = "Enrichment run failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Runs enrichment for all books without descriptions until complete or limit reached.
        /// Use with caution for large libraries - this can take a long time.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run-all")]
        public async Task<ActionResult<BookEnrichmentRunAllResult>> RunEnrichmentAll(
            [FromBody] RunEnrichmentAllRequest? request = null)
        {
            try
            {
                var batchSize = request?.BatchSize ?? 50;
                var delayMs = request?.DelayBetweenCallsMs ?? 1000;
                var maxBooks = request?.MaxBooks ?? 1000; // Safety limit
                var pauseBetweenBatchesSeconds = request?.PauseBetweenBatchesSeconds ?? 30;

                // Validate parameters
                if (batchSize < 1 || batchSize > 200)
                {
                    return BadRequest(new { error = "BatchSize must be between 1 and 200" });
                }

                if (maxBooks < 1 || maxBooks > 10000)
                {
                    return BadRequest(new { error = "MaxBooks must be between 1 and 10000" });
                }

                _logger.LogInformation(
                    "Starting full book description enrichment. BatchSize: {BatchSize}, MaxBooks: {MaxBooks}",
                    batchSize, maxBooks);

                var totalEnriched = 0;
                var totalFailed = 0;
                var totalProcessed = 0;
                var allErrors = new List<string>();
                var batchesRun = 0;

                var pendingCount = await _enrichmentService.GetBooksNeedingEnrichmentCountAsync();

                while (pendingCount > 0 && totalProcessed < maxBooks)
                {
                    var result = await _enrichmentService.EnrichBooksWithoutDescriptionsAsync(
                        batchSize: Math.Min(batchSize, maxBooks - totalProcessed),
                        delayBetweenCallsMs: delayMs);

                    totalEnriched += result.EnrichedCount;
                    totalFailed += result.FailedCount;
                    totalProcessed += result.TotalProcessed;
                    allErrors.AddRange(result.Errors.Take(10)); // Limit errors collected
                    batchesRun++;

                    if (result.TotalProcessed == 0)
                    {
                        break; // No more books to process
                    }

                    // Get updated count
                    pendingCount = await _enrichmentService.GetBooksNeedingEnrichmentCountAsync();

                    // Pause between batches if there are more to process
                    if (pendingCount > 0 && totalProcessed < maxBooks)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(pauseBetweenBatchesSeconds));
                    }
                }

                _logger.LogInformation(
                    "Full enrichment completed. Batches: {Batches}, Enriched: {Enriched}, Failed: {Failed}, Remaining: {Remaining}",
                    batchesRun, totalEnriched, totalFailed, pendingCount);

                return Ok(new BookEnrichmentRunAllResult
                {
                    TotalProcessed = totalProcessed,
                    TotalEnriched = totalEnriched,
                    TotalFailed = totalFailed,
                    BatchesRun = batchesRun,
                    RemainingBooks = pendingCount,
                    Errors = allErrors.Take(20).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running full book enrichment");
                return StatusCode(500, new { error = "Full enrichment run failed", details = ex.Message });
            }
        }
    }

    public class BookEnrichmentStatusDto
    {
        public int BooksNeedingEnrichment { get; set; }
    }

    public class RunEnrichmentRequest
    {
        /// <summary>
        /// Number of books to process in this run (1-500, default: 50)
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Delay between API calls in milliseconds (100-10000, default: 1000)
        /// </summary>
        public int? DelayBetweenCallsMs { get; set; }
    }

    public class RunEnrichmentAllRequest
    {
        /// <summary>
        /// Number of books per batch (1-200, default: 50)
        /// </summary>
        public int? BatchSize { get; set; }

        /// <summary>
        /// Delay between API calls in milliseconds (default: 1000)
        /// </summary>
        public int? DelayBetweenCallsMs { get; set; }

        /// <summary>
        /// Maximum total books to process (1-10000, default: 1000)
        /// </summary>
        public int? MaxBooks { get; set; }

        /// <summary>
        /// Pause in seconds between batches (default: 30)
        /// </summary>
        public int? PauseBetweenBatchesSeconds { get; set; }
    }

    public class BookEnrichmentRunAllResult
    {
        public int TotalProcessed { get; set; }
        public int TotalEnriched { get; set; }
        public int TotalFailed { get; set; }
        public int BatchesRun { get; set; }
        public int RemainingBooks { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
