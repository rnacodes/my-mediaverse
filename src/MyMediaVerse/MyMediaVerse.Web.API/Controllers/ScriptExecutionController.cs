using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Controller for executing and monitoring normalization scripts.
    /// Acts as a proxy to the Python FastAPI script runner service.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScriptExecutionController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ScriptExecutionController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public ScriptExecutionController(
            IHttpClientFactory httpClientFactory,
            ILogger<ScriptExecutionController> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
        }

        private string GetScriptRunnerUrl()
        {
            return Environment.GetEnvironmentVariable("SCRIPT_RUNNER_URL")
                ?? _configuration["ScriptRunner:BaseUrl"]
                ?? "http://localhost:8001";
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("ScriptRunner");

            // Add API key if configured
            var apiKey = Environment.GetEnvironmentVariable("SCRIPT_RUNNER_API_KEY")
                ?? _configuration["ScriptRunner:ApiKey"];

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Remove("X-API-Key");
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }

            return client;
        }

        /// <summary>
        /// Check if the script runner service is available.
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckHealth()
        {
            try
            {
                var client = CreateClient();
                var response = await client.GetAsync($"{GetScriptRunnerUrl()}/health");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return StatusCode(503, new { status = "unhealthy", service = "script_runner" });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Script runner service unavailable");
                return StatusCode(503, new { status = "unavailable", service = "script_runner", error = ex.Message });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(503, new { status = "timeout", service = "script_runner" });
            }
        }

        /// <summary>
        /// Get all script execution jobs.
        /// </summary>
        [HttpGet("jobs")]
        public async Task<IActionResult> GetAllJobs([FromQuery] int limit = 50)
        {
            try
            {
                var client = CreateClient();
                var response = await client.GetAsync($"{GetScriptRunnerUrl()}/jobs?limit={limit}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get jobs: {StatusCode} - {Content}", response.StatusCode, content);
                    return StatusCode((int)response.StatusCode, content);
                }

                return Content(content, "application/json");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to get jobs from script runner");
                return StatusCode(503, new { error = "Script runner service unavailable", details = ex.Message });
            }
        }

        /// <summary>
        /// Get status of a specific job.
        /// </summary>
        [HttpGet("jobs/{jobId}")]
        public async Task<IActionResult> GetJob(string jobId)
        {
            try
            {
                var client = CreateClient();
                var response = await client.GetAsync($"{GetScriptRunnerUrl()}/jobs/{jobId}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, content);
                }

                return Content(content, "application/json");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to get job {JobId}", jobId);
                return StatusCode(503, new { error = "Script runner service unavailable", details = ex.Message });
            }
        }

        /// <summary>
        /// Run the normalize_notes script (database normalization).
        /// </summary>
        [HttpPost("normalize-notes")]
        public async Task<IActionResult> RunNormalizeNotes([FromBody] NormalizeNotesRequest? request)
        {
            _logger.LogInformation("Running normalize_notes script with dryRun={DryRun}", request?.DryRun ?? false);

            return await RunScript(new
            {
                script_type = "normalize_notes",
                dry_run = request?.DryRun ?? false,
                verbose = request?.Verbose ?? false
            });
        }

        /// <summary>
        /// Run the normalize_vault script (file system normalization).
        /// </summary>
        [HttpPost("normalize-vault")]
        public async Task<IActionResult> RunNormalizeVault([FromBody] NormalizeVaultRequest? request)
        {
            if (string.IsNullOrWhiteSpace(request?.VaultPath))
            {
                return BadRequest(new { error = "vaultPath is required" });
            }

            _logger.LogInformation(
                "Running normalize_vault script on {VaultPath} with dryRun={DryRun}",
                request.VaultPath,
                request.DryRun);

            return await RunScript(new
            {
                script_type = "normalize_vault",
                dry_run = request.DryRun,
                verbose = request.Verbose,
                vault_path = request.VaultPath,
                use_ai = request.UseAI,
                backup = request.Backup
            });
        }

        /// <summary>
        /// Cancel a running job.
        /// </summary>
        [HttpPost("jobs/{jobId}/cancel")]
        public async Task<IActionResult> CancelJob(string jobId)
        {
            try
            {
                var client = CreateClient();
                var response = await client.PostAsync(
                    $"{GetScriptRunnerUrl()}/jobs/{jobId}/cancel",
                    null);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, content);
                }

                return Content(content, "application/json");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
                return StatusCode(503, new { error = "Script runner service unavailable", details = ex.Message });
            }
        }

        private async Task<IActionResult> RunScript(object payload)
        {
            try
            {
                var client = CreateClient();
                var json = JsonSerializer.Serialize(payload, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{GetScriptRunnerUrl()}/jobs", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Script execution failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return StatusCode((int)response.StatusCode, responseContent);
                }

                return Content(responseContent, "application/json");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to run script");
                return StatusCode(503, new { error = "Script runner service unavailable", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request to run the normalize_notes script.
    /// </summary>
    public class NormalizeNotesRequest
    {
        public bool DryRun { get; set; } = false;
        public bool Verbose { get; set; } = false;
    }

    /// <summary>
    /// Request to run the normalize_vault script.
    /// </summary>
    public class NormalizeVaultRequest
    {
        public bool DryRun { get; set; } = false;
        public bool Verbose { get; set; } = false;
        public string? VaultPath { get; set; }
        public bool UseAI { get; set; } = false;
        public bool Backup { get; set; } = false;
    }
}
