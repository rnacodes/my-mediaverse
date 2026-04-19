using System.Text;

namespace MyMediaVerse.Web.API.Middleware
{
    /// <summary>
    /// Gates Swagger UI + JSON endpoints behind HTTP Basic Authentication so the API docs
    /// are not publicly browsable in non-Development environments. Credentials come from
    /// <c>AUTH_USERNAME</c>/<c>AUTH_PASSWORD</c> env vars, falling back to <c>Auth:Username</c>
    /// /<c>Auth:Password</c> in configuration.
    /// </summary>
    public class SwaggerBasicAuthMiddleware
    {
        private const string AuthRealm = "My MediaVerse API";

        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsSwaggerRequest(context.Request.Path))
            {
                await _next(context);
                return;
            }

            if (TryAuthenticate(context.Request.Headers.Authorization))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = $"Basic realm=\"{AuthRealm}\"";
        }

        private static bool IsSwaggerRequest(PathString requestPath)
        {
            var path = requestPath.Value?.TrimEnd('/') ?? string.Empty;
            return path.Length == 0
                || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
        }

        private bool TryAuthenticate(string? authHeader)
        {
            if (string.IsNullOrEmpty(authHeader)
                || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string decoded;
            try
            {
                var encoded = authHeader["Basic ".Length..].Trim();
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            catch (FormatException)
            {
                return false;
            }

            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex <= 0) return false;

            var username = decoded[..separatorIndex];
            var password = decoded[(separatorIndex + 1)..];

            var expectedUsername = Environment.GetEnvironmentVariable("AUTH_USERNAME")
                ?? _configuration["Auth:Username"];
            var expectedPassword = Environment.GetEnvironmentVariable("AUTH_PASSWORD")
                ?? _configuration["Auth:Password"];

            return !string.IsNullOrEmpty(expectedUsername)
                && !string.IsNullOrEmpty(expectedPassword)
                && username == expectedUsername
                && password == expectedPassword;
        }
    }

    public static class SwaggerBasicAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseSwaggerBasicAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SwaggerBasicAuthMiddleware>();
        }
    }
}
