using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyMediaVerse.Web.API.Filters
{
    /// <summary>
    /// Filter that blocks write operations (POST, PUT, DELETE, PATCH) in Demo environment.
    /// Allows browsing and GET requests only for demo users.
    /// Can be bypassed with:
    /// - TOTP cookie (Demo_Write_Access) set by /api/demo/unlock endpoint (20 min expiry)
    /// - X-Demo-Admin-Key header matching DEMO_ADMIN_KEY environment variable
    /// </summary>
    public class DemoReadOnlyFilter : IAsyncActionFilter
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DemoReadOnlyFilter> _logger;
        private readonly IConfiguration _configuration;
        private const string AdminKeyHeader = "X-Demo-Admin-Key";
        private const string TotpCookieName = "Demo_Write_Access";

        public DemoReadOnlyFilter(
            IWebHostEnvironment environment,
            ILogger<DemoReadOnlyFilter> logger,
            IConfiguration configuration)
        {
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Only restrict in Demo environment
            if (!_environment.EnvironmentName.Equals("Demo", StringComparison.OrdinalIgnoreCase))
            {
                await next(); // Allow all operations in non-demo environments
                return;
            }

            var httpMethod = context.HttpContext.Request.Method;
            var isWriteOperation = httpMethod == HttpMethod.Post.Method ||
                                   httpMethod == HttpMethod.Put.Method ||
                                   httpMethod == HttpMethod.Delete.Method ||
                                   httpMethod == HttpMethod.Patch.Method;

            if (isWriteOperation)
            {
                var path = context.HttpContext.Request.Path.Value ?? "";

                // Check for TOTP cookie bypass (primary method for user-initiated access)
                if (context.HttpContext.Request.Cookies.TryGetValue(TotpCookieName, out var cookieValue) &&
                    cookieValue == "true")
                {
                    _logger.LogInformation(
                        "TOTP cookie bypass used. Method: {Method}, Path: {Path}",
                        httpMethod, path);
                    await next();
                    return;
                }

                // Check for admin key header bypass (fallback/programmatic access)
                var adminKey = Environment.GetEnvironmentVariable("DEMO_ADMIN_KEY")
                               ?? _configuration["DEMO_ADMIN_KEY"];
                if (!string.IsNullOrEmpty(adminKey))
                {
                    var providedKey = context.HttpContext.Request.Headers[AdminKeyHeader].FirstOrDefault();

                    _logger.LogDebug(
                        "Demo admin key check - Key configured: {Configured}, Header present: {HeaderPresent}",
                        !string.IsNullOrEmpty(adminKey),
                        !string.IsNullOrEmpty(providedKey));

                    if (!string.IsNullOrEmpty(providedKey) && providedKey == adminKey)
                    {
                        _logger.LogInformation(
                            "Demo admin key bypass used. Method: {Method}, Path: {Path}",
                            httpMethod,
                            path);
                        await next();
                        return;
                    }
                }

                // Allow /dev/seed-demo-data endpoint specifically for initial seeding
                if (path.Contains("/dev/seed-demo-data", StringComparison.OrdinalIgnoreCase))
                {
                    await next();
                    return;
                }

                _logger.LogWarning(
                    "Write operation blocked in Demo environment. Method: {Method}, Path: {Path}",
                    httpMethod,
                    path);

                context.Result = new ObjectResult(new
                {
                    error = "Write operations are disabled in demo mode",
                    message = "This demo environment is read-only. You can browse all content, but cannot create, update, or delete data. Use /api/demo/unlock with a valid TOTP code to gain temporary write access.",
                    allowedOperations = new[] { "GET" },
                    blockedOperation = httpMethod
                })
                {
                    StatusCode = 403 // Forbidden
                };
                return;
            }

            await next();
        }
    }
}
