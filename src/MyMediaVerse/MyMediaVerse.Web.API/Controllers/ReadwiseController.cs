using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Conventions;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Cross-cutting Readwise operations: connection validation and the unified sync
    /// that runs the Reader (documents) and Readwise (highlights) steps together.
    /// Article-specific Reader operations live on ArticleController.
    /// </summary>
    // Proxies the owner's personal Readwise/Reader library via an app-wide API token,
    // so these endpoints exist only on hosts the owner uses.
    [Environments("Production", "Development", "Testing")]
    [ApiController]
    [Route("api/[controller]")]
    public class ReadwiseController : ControllerBase
    {
        private readonly IReadwiseService _readwiseService;
        private readonly IReadwiseSyncService _readwiseSyncService;
        private readonly IImportReindexService _importReindexService;
        private readonly ILogger<ReadwiseController> _logger;

        public ReadwiseController(
            IReadwiseService readwiseService,
            IReadwiseSyncService readwiseSyncService,
            IImportReindexService importReindexService,
            ILogger<ReadwiseController> logger)
        {
            _readwiseService = readwiseService;
            _readwiseSyncService = readwiseSyncService;
            _importReindexService = importReindexService;
            _logger = logger;
        }

        /// <summary>
        /// Validates the Readwise API token (highlights API).
        /// </summary>
        // Exercises the owner's API token; diagnostic, operator-only.
        [Authorize]
        [HttpGet("validate")]
        public async Task<ActionResult<object>> ValidateConnection()
        {
            try
            {
                var isValid = await _readwiseService.ValidateConnectionAsync();
                return Ok(new
                {
                    connected = isValid,
                    message = isValid
                        ? "Readwise API connection is valid"
                        : "Readwise API connection failed"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Ok(new
                {
                    connected = false,
                    message = "Readwise API not configured",
                    details = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ok(new
                {
                    connected = false,
                    message = "Invalid API token",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Readwise connection");
                return Ok(new
                {
                    connected = false,
                    message = "Connection validation failed",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Unified sync: Reader documents, then Readwise highlights. The two steps run
        /// independently with their own cursors; a failure in either is reported as a
        /// failed run (500 with the result body) but never stops the other step.
        /// </summary>
        /// <param name="incremental">
        /// If true (default), each step only syncs items updated since its last fully-successful run
        /// (falling back to the last 7 days until one has been recorded).
        /// </param>
        // Writes to the library from the owner's Readwise/Reader accounts via the app-wide token; never a visitor action.
        [Authorize]
        [HttpPost("sync")]
        public async Task<ActionResult<ReadwiseSyncAllResultDto>> SyncAll([FromQuery] bool incremental = true)
        {
            try
            {
                var result = await _readwiseSyncService.SyncAllAsync(incremental);

                // Articles and stub books index into the media collection; highlights maintain their own index.
                await _importReindexService.ReindexAfterImportAsync(result.TotalMediaItemsProcessed, "Readwise sync");
                result.ReindexTriggered = result.TotalMediaItemsProcessed > 0;

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in unified Readwise sync");
                return StatusCode(500, new ReadwiseSyncAllResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }
    }
}
