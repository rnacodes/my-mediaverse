using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Shared.DTOs.Trakt;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TraktController : ControllerBase
    {
        private readonly ITraktSyncService _syncService;
        private readonly ITraktApiClient _apiClient;
        private readonly ILogger<TraktController> _logger;

        public TraktController(
            ITraktSyncService syncService,
            ITraktApiClient apiClient,
            ILogger<TraktController> logger)
        {
            _syncService = syncService;
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <summary>
        /// Get Trakt connection status
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<TraktConnectionStatusDto>> GetStatus()
        {
            try
            {
                var status = await _syncService.GetStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Trakt connection status");
                return Ok(new TraktConnectionStatusDto { Connected = false });
            }
        }

        /// <summary>
        /// Start device auth flow - returns a user code and verification URL
        /// </summary>
        [HttpPost("auth/device-code")]
        public async Task<ActionResult<TraktDeviceCodeDto>> StartDeviceAuth()
        {
            try
            {
                _logger.LogInformation("Starting Trakt device auth flow");
                var deviceCode = await _apiClient.GetDeviceCodeAsync();
                return Ok(deviceCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Trakt device auth");
                return StatusCode(500, new { error = "Failed to start device authentication", details = ex.Message });
            }
        }

        /// <summary>
        /// Poll for device token after user has entered the code at trakt.tv/activate
        /// </summary>
        [HttpPost("auth/poll")]
        public async Task<ActionResult> PollDeviceToken([FromBody] TraktDevicePollRequestDto request)
        {
            try
            {
                var tokenResponse = await _apiClient.PollDeviceTokenAsync(request.DeviceCode);

                if (tokenResponse == null)
                {
                    // Still pending - user hasn't authorized yet
                    return Ok(new { status = "pending", message = "Waiting for user authorization" });
                }

                // User authorized - save the token
                await _syncService.SaveTokenAsync(tokenResponse);

                return Ok(new { status = "authorized", message = "Successfully connected to Trakt" });
            }
            catch (InvalidOperationException ex)
            {
                // Code expired or denied
                _logger.LogWarning(ex, "Trakt device auth failed");
                return BadRequest(new { status = "failed", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling Trakt device token");
                return StatusCode(500, new { error = "Failed to poll device token", details = ex.Message });
            }
        }

        /// <summary>
        /// Disconnect from Trakt - revokes token and removes from database
        /// </summary>
        [HttpPost("disconnect")]
        public async Task<ActionResult> Disconnect()
        {
            try
            {
                _logger.LogInformation("Disconnecting from Trakt");
                await _syncService.DisconnectAsync();
                return Ok(new { message = "Successfully disconnected from Trakt" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Trakt");
                return StatusCode(500, new { error = "Failed to disconnect", details = ex.Message });
            }
        }

        /// <summary>
        /// Sync watched movies and TV shows (including episodes)
        /// </summary>
        [HttpPost("sync/watched")]
        public async Task<ActionResult<TraktSyncResultDto>> SyncWatched()
        {
            try
            {
                _logger.LogInformation("Starting Trakt watched sync");
                var result = await _syncService.SyncWatchedAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Trakt watched items");
                return StatusCode(500, new TraktSyncResultDto
                {
                    Success = false,
                    Errors = new List<string> { ex.Message },
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Sync watchlist items
        /// </summary>
        [HttpPost("sync/watchlist")]
        public async Task<ActionResult<TraktSyncResultDto>> SyncWatchlist()
        {
            try
            {
                _logger.LogInformation("Starting Trakt watchlist sync");
                var result = await _syncService.SyncWatchlistAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Trakt watchlist");
                return StatusCode(500, new TraktSyncResultDto
                {
                    Success = false,
                    Errors = new List<string> { ex.Message },
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Sync ratings
        /// </summary>
        [HttpPost("sync/ratings")]
        public async Task<ActionResult<TraktSyncResultDto>> SyncRatings()
        {
            try
            {
                _logger.LogInformation("Starting Trakt ratings sync");
                var result = await _syncService.SyncRatingsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Trakt ratings");
                return StatusCode(500, new TraktSyncResultDto
                {
                    Success = false,
                    Errors = new List<string> { ex.Message },
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Sync all Trakt data (watched + watchlist + ratings)
        /// </summary>
        [HttpPost("sync/all")]
        public async Task<ActionResult<TraktSyncResultDto>> SyncAll()
        {
            try
            {
                _logger.LogInformation("Starting full Trakt sync");
                var result = await _syncService.SyncAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in full Trakt sync");
                return StatusCode(500, new TraktSyncResultDto
                {
                    Success = false,
                    Errors = new List<string> { ex.Message },
                    CompletedAt = DateTime.UtcNow
                });
            }
        }
    }
}
