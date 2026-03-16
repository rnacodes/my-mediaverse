using System.Net;
using System.Text.Json;

namespace MyMediaVerse.Web.API.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Ensure CORS headers are present even for error responses
            if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
            {
                var origin = context.Request.Headers["Origin"].ToString();
                
                // List of allowed origins (should match Program.cs CORS configuration)
                var allowedOrigins = new[]
                {
                    "https://www.mymediaverseuniverse.com",
                    "https://mymediaverseuniverse.com"
                };

                // Check if origin is allowed
                if (!string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin))
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                    context.Response.Headers["Access-Control-Allow-Headers"] = "*";
                    context.Response.Headers["Access-Control-Allow-Methods"] = "*";
                }
                // In production, if FRONTEND_URL environment variable is set, also allow it
                else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FRONTEND_URL")))
                {
                    var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
                    if (origin == frontendUrl)
                    {
                        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                        context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                        context.Response.Headers["Access-Control-Allow-Headers"] = "*";
                        context.Response.Headers["Access-Control-Allow-Methods"] = "*";
                    }
                }
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var errorResponse = new ErrorResponse
            {
                StatusCode = context.Response.StatusCode,
                Message = "An error occurred while processing your request.",
                Details = _environment.IsDevelopment() || _environment.IsEnvironment("Staging") 
                    ? exception.ToString() 
                    : exception.Message,
                ExceptionType = exception.GetType().Name,
                Path = context.Request.Path,
                Timestamp = DateTime.UtcNow
            };

            // Log detailed error information
            _logger.LogError("Error Response: {@ErrorResponse}", errorResponse);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? ExceptionType { get; set; }
        public string? Path { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Extension method to register the middleware
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}

