using Microsoft.AspNetCore.Authorization;

namespace MyMediaVerse.Web.API.Middleware
{
    /// <summary>
    /// Blocks write operations (POST, PUT, DELETE, PATCH) in the Demo environment unless the
    /// caller holds the TOTP write-access cookie set by <c>/api/demo/unlock</c>.
    ///
    /// Runs after routing (so <c>[AllowAnonymous]</c> endpoint metadata is readable) and before
    /// authentication, so "this host is read-only right now" is answered before "who are you":
    /// anonymous demo writes get a friendly 403 with a machine-readable code instead of a bare 401.
    /// Holding the cookie only opens the write window — authentication and the authorization
    /// policy still require a valid token for the request to succeed.
    /// </summary>
    public class DemoWriteGateMiddleware
    {
        /// <summary>
        /// Stable machine-readable identifier for the demo read-only rejection. The frontend
        /// keys its read-only dialog off this value; do not change it without updating the client.
        /// </summary>
        public const string DemoReadOnlyCode = "demo_read_only";

        public const string TotpCookieName = "Demo_Write_Access";

        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DemoWriteGateMiddleware> _logger;

        public DemoWriteGateMiddleware(
            RequestDelegate next,
            IWebHostEnvironment environment,
            ILogger<DemoWriteGateMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_environment.EnvironmentName.Equals("Demo", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var method = context.Request.Method;
            var isWriteOperation = HttpMethods.IsPost(method) ||
                                   HttpMethods.IsPut(method) ||
                                   HttpMethods.IsDelete(method) ||
                                   HttpMethods.IsPatch(method);

            if (!isWriteOperation)
            {
                await _next(context);
                return;
            }

            // Endpoints that opted out of authentication (login, demo unlock/lock) are the
            // host's entry points and must stay reachable while the site is read-only.
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? "";

            if (context.Request.Cookies.TryGetValue(TotpCookieName, out var cookieValue) &&
                cookieValue == "true")
            {
                _logger.LogInformation(
                    "Demo write window open (TOTP cookie present). Method: {Method}, Path: {Path}",
                    method, path);
                await _next(context);
                return;
            }

            _logger.LogWarning(
                "Write operation blocked in Demo environment. Method: {Method}, Path: {Path}",
                method, path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = DemoReadOnlyCode,
                error = "Write operations are disabled in demo mode",
                message = "This demo environment is read-only. You can browse all content, but cannot " +
                          "create, update, or delete data. Use /api/demo/unlock with a valid TOTP code " +
                          "to gain temporary write access.",
                allowedOperations = new[] { "GET" },
                blockedOperation = method
            });
        }
    }

    public static class DemoWriteGateMiddlewareExtensions
    {
        public static IApplicationBuilder UseDemoWriteGate(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DemoWriteGateMiddleware>();
        }
    }
}
