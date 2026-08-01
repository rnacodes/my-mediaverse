using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Infrastructure.Data;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly MediaLibraryDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            MediaLibraryDbContext context,
            IConfiguration configuration,
            ILogger<HealthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Basic health check endpoint - returns 200 if API is running
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Uptime probes (e.g. the hosting platform's health check) must reach this without credentials.
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }

        /// <summary>
        /// Detailed health check - tests database connectivity and service configuration
        /// </summary>
        [HttpGet("detailed")]
        [Authorize] // Exposes database error details and configuration presence; operators only.
        public async Task<IActionResult> GetDetailed()
        {
            var databaseHealth = await CheckDatabaseHealth();
            
            var health = new
            {
                api = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                },
                database = databaseHealth,
                configuration = CheckConfiguration()
            };

            // If database is unhealthy, return 503
            if (!databaseHealth.IsHealthy)
            {
                return StatusCode(503, health);
            }

            return Ok(health);
        }

        private async Task<DatabaseHealthResult> CheckDatabaseHealth()
        {
            try
            {
                // Try to connect to database and execute a simple query
                await _context.Database.CanConnectAsync();
                
                // Get a count of genres as a simple test query
                var genreCount = await _context.Genres.CountAsync();
                
                return new DatabaseHealthResult
                {
                    Status = "healthy",
                    IsHealthy = true,
                    CanConnect = true,
                    GenreCount = genreCount,
                    Provider = _context.Database.ProviderName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return new DatabaseHealthResult
                {
                    Status = "unhealthy",
                    IsHealthy = false,
                    CanConnect = false,
                    Error = ex.Message,
                    ErrorType = ex.GetType().Name,
                    Provider = _context.Database.ProviderName
                };
            }
        }

        private class DatabaseHealthResult
        {
            public string Status { get; set; } = string.Empty;
            public bool IsHealthy { get; set; }
            public bool CanConnect { get; set; }
            public int? GenreCount { get; set; }
            public string? Provider { get; set; }
            public string? Error { get; set; }
            public string? ErrorType { get; set; }
        }

        private object CheckConfiguration()
        {
            // Check for critical configuration
            var hasConnectionString = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("DATABASE_URL") ??
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
                _configuration.GetConnectionString("DefaultConnection"));

            var hasJwtSecret = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("JWT_SECRET") ??
                _configuration["JwtSettings:Secret"]);

            var hasTypesenseConfig = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("TYPESENSE_ADMIN_API_KEY") ??
                _configuration["Typesense:AdminApiKey"]);

            return new
            {
                hasConnectionString = hasConnectionString,
                hasJwtSecret = hasJwtSecret,
                hasTypesenseConfig = hasTypesenseConfig,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };
        }

        /// <summary>
        /// CORS test endpoint - helps verify CORS is working
        /// </summary>
        [HttpGet("cors-test")]
        [Authorize]
        public IActionResult CorsTest()
        {
            var origin = Request.Headers["Origin"].ToString();
            var allowedOrigin = Response.Headers["Access-Control-Allow-Origin"].ToString();
            
            return Ok(new
            {
                message = "CORS is working correctly if you can see this response",
                requestOrigin = origin,
                allowedOrigin = allowedOrigin,
                timestamp = DateTime.UtcNow
            });
        }
    }
}

