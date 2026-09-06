using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookEnrichmentController : ControllerBase
    {
        private readonly IBookDescriptionEnrichmentService _enrichmentService;
        private readonly IBookRatingEnrichmentService _ratingEnrichmentService;
        private readonly IImportReindexService _importReindexService;
        private readonly ILogger<BookEnrichmentController> _logger;

        public BookEnrichmentController(
            IBookDescriptionEnrichmentService enrichmentService,
            IBookRatingEnrichmentService ratingEnrichmentService,
            IImportReindexService importReindexService,
            ILogger<BookEnrichmentController> logger)
        {
            _enrichmentService = enrichmentService;
            _ratingEnrichmentService = ratingEnrichmentService;
            _importReindexService = importReindexService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the count of books that need description enrichment: no description, and a lookup key
        /// (ISBN, or title plus a real author).
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
                return StatusCode(500, new { error = "Failed to get enrichment status" });
            }
        }

        /// <summary>
        /// Enriches a single book by its ID. The description is part of the search embedding, so a
        /// successful enrichment reindexes that one item.
        /// </summary>
        /// <param name="id">The media ID of the book to enrich</param>
        [HttpPost("{id:guid}")]
        public async Task<ActionResult<SingleBookEnrichmentResult>> EnrichSingleBook(Guid id)
        {
            try
            {
                _logger.LogInformation("Starting single book enrichment for ID: {BookId}", id);

                var result = await _enrichmentService.EnrichBookByIdAsync(id, HttpContext.RequestAborted);

                if (result.NotFound)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                _logger.LogInformation(
                    "Single book enrichment completed for {Title}. Success: {Success}, Filled: {Filled}",
                    result.BookTitle, result.Success, string.Join(", ", result.FilledFields));

                if (result.FilledFields.Count > 0)
                {
                    await _importReindexService.ReindexItemAfterImportAsync(id, "book enrichment");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running single book enrichment for ID: {BookId}", id);
                return StatusCode(500, new { error = "Enrichment failed" });
            }
        }

        /// <summary>
        /// Triggers an on-demand book description enrichment run. Returns 500 with the result body
        /// when the run itself aborted; per-book misses are reported as counts with a 200.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run")]
        public async Task<ActionResult<BookDescriptionEnrichmentResult>> RunEnrichment(
            [FromBody] RunEnrichmentRequest? request = null)
        {
            var batchSize = request?.BatchSize ?? 50;
            var delayMs = request?.DelayBetweenCallsMs ?? 1000;

            if (batchSize < 1 || batchSize > 500)
            {
                return BadRequest(new { error = "BatchSize must be between 1 and 500" });
            }

            if (delayMs < 100 || delayMs > 10000)
            {
                return BadRequest(new { error = "DelayBetweenCallsMs must be between 100 and 10000" });
            }

            var startedAt = DateTime.UtcNow;
            try
            {
                _logger.LogInformation(
                    "Starting on-demand book description enrichment. BatchSize: {BatchSize}, Delay: {Delay}ms",
                    batchSize, delayMs);

                var result = await _enrichmentService.EnrichBooksWithoutDescriptionsAsync(
                    batchSize: batchSize,
                    delayBetweenCallsMs: delayMs,
                    cancellationToken: HttpContext.RequestAborted);

                _logger.LogInformation(
                    "On-demand enrichment completed. Success: {Success}, Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}",
                    result.Success, result.EnrichedCount, result.NotFoundCount, result.FailedCount);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                await _importReindexService.ReindexAfterImportAsync(result.EnrichedCount, "book enrichment");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running on-demand book enrichment");
                return StatusCode(500, new BookDescriptionEnrichmentResult
                {
                    Success = false,
                    ErrorMessage = "Enrichment run failed unexpectedly",
                    StartedAt = startedAt
                });
            }
        }

        /// <summary>
        /// Runs enrichment for all books without descriptions until complete or limit reached.
        /// Use with caution for large libraries - this can take a long time. Stops early if the
        /// client disconnects so an abandoned request does not keep spending Google Books quota.
        /// </summary>
        /// <param name="request">Optional parameters for the enrichment run</param>
        [HttpPost("run-all")]
        public async Task<ActionResult<BookEnrichmentRunAllResult>> RunEnrichmentAll(
            [FromBody] RunEnrichmentAllRequest? request = null)
        {
            var batchSize = request?.BatchSize ?? 50;
            var delayMs = request?.DelayBetweenCallsMs ?? 1000;
            var maxBooks = request?.MaxBooks ?? 1000; // Safety limit
            var pauseBetweenBatchesSeconds = request?.PauseBetweenBatchesSeconds ?? 30;

            if (batchSize < 1 || batchSize > 200)
            {
                return BadRequest(new { error = "BatchSize must be between 1 and 200" });
            }

            if (maxBooks < 1 || maxBooks > 10000)
            {
                return BadRequest(new { error = "MaxBooks must be between 1 and 10000" });
            }

            var cancellationToken = HttpContext.RequestAborted;
            var summary = new BookEnrichmentRunAllResult { StartedAt = DateTime.UtcNow };

            try
            {
                _logger.LogInformation(
                    "Starting full book description enrichment. BatchSize: {BatchSize}, MaxBooks: {MaxBooks}",
                    batchSize, maxBooks);

                var pendingCount = await _enrichmentService.GetBooksNeedingEnrichmentCountAsync();

                while (pendingCount > 0 && summary.TotalProcessed < maxBooks && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _enrichmentService.EnrichBooksWithoutDescriptionsAsync(
                        batchSize: Math.Min(batchSize, maxBooks - summary.TotalProcessed),
                        delayBetweenCallsMs: delayMs,
                        cancellationToken: cancellationToken);

                    summary.TotalEnriched += result.EnrichedCount;
                    summary.TotalNotFound += result.NotFoundCount;
                    summary.TotalFailed += result.FailedCount;
                    summary.TotalProcessed += result.TotalProcessed;
                    summary.Errors.AddRange(result.Errors.Take(10)); // Limit errors collected
                    summary.BatchesRun++;

                    if (!result.Success)
                    {
                        summary.Success = false;
                        summary.ErrorMessage = result.ErrorMessage;
                        break;
                    }

                    if (result.WasCancelled)
                    {
                        summary.WasCancelled = true;
                        break;
                    }

                    if (result.TotalProcessed == 0)
                    {
                        break; // No more books to process
                    }

                    // Get updated count
                    pendingCount = await _enrichmentService.GetBooksNeedingEnrichmentCountAsync();

                    // Pause between batches if there are more to process
                    if (pendingCount > 0 && summary.TotalProcessed < maxBooks)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(pauseBetweenBatchesSeconds), cancellationToken);
                    }
                }

                summary.RemainingBooks = pendingCount;
                summary.Errors = summary.Errors.Take(20).ToList();

                if (summary.TotalFailed > 0)
                {
                    summary.WarningMessage = $"{summary.TotalFailed} of {summary.TotalProcessed} book lookups failed";
                }

                _logger.LogInformation(
                    "Full enrichment completed. Success: {Success}, Batches: {Batches}, Enriched: {Enriched}, NotFound: {NotFound}, Failed: {Failed}, Remaining: {Remaining}",
                    summary.Success, summary.BatchesRun, summary.TotalEnriched, summary.TotalNotFound, summary.TotalFailed, pendingCount);

                if (!summary.Success)
                {
                    return StatusCode(500, summary);
                }

                summary.CompletedAt = DateTime.UtcNow;

                await _importReindexService.ReindexAfterImportAsync(summary.TotalEnriched, "book enrichment");

                return Ok(summary);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The client went away mid-run. Whatever was saved stays saved; nothing to return to.
                _logger.LogInformation(
                    "Full enrichment stopped because the request was aborted after {Batches} batches ({Enriched} enriched)",
                    summary.BatchesRun, summary.TotalEnriched);
                summary.WasCancelled = true;
                summary.CompletedAt = DateTime.UtcNow;
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running full book enrichment");
                summary.Success = false;
                summary.ErrorMessage = "Full enrichment run failed unexpectedly";
                return StatusCode(500, summary);
            }
        }

        /// <summary>
        /// Derives the MMV Rating enum from the raw GoodreadsRating stored at import time, for every
        /// book with a real 1-5 Goodreads rating. Goodreads CSV import stores only the raw rating, so
        /// this is the step that populates the app Rating.
        /// </summary>
        [HttpPost("convert-ratings")]
        public async Task<ActionResult<BookRatingConversionResult>> ConvertGoodreadsRatings()
        {
            var startedAt = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Starting Goodreads rating conversion run");

                var result = await _ratingEnrichmentService.ConvertGoodreadsRatingsAsync(HttpContext.RequestAborted);

                _logger.LogInformation(
                    "Goodreads rating conversion completed. Success: {Success}, Candidates: {Candidates}, Converted: {Converted}",
                    result.Success, result.TotalCandidates, result.Converted);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                await _importReindexService.ReindexAfterImportAsync(result.Converted, "Goodreads rating conversion");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running Goodreads rating conversion");
                return StatusCode(500, new BookRatingConversionResult
                {
                    Success = false,
                    ErrorMessage = "Rating conversion failed unexpectedly",
                    StartedAt = startedAt
                });
            }
        }
    }
}
