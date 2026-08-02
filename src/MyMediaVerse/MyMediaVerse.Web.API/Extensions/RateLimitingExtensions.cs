using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MyMediaVerse.Web.API.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Named policy applied to the demo TOTP unlock endpoint to slow brute-force guessing.
    /// </summary>
    public const string DemoUnlockPolicy = "demo-unlock";

    /// <summary>
    /// Named policy for endpoints that run an embedding or LLM model on every request
    /// (semantic search, vibe search). Bounds the per-visitor inference spend.
    /// </summary>
    public const string ExpensiveReadPolicy = "expensive-read";

    /// <summary>
    /// Named policy for endpoints that proxy metered third-party APIs (TMDB, YouTube,
    /// ListenNotes, book search) on the app's own keys. Bounds per-visitor quota burn.
    /// </summary>
    public const string ExternalProxyPolicy = "external-proxy";

    // Header names used by the proxies in front of the demo/prod API. The real client IP
    // must be read from these, not the TCP peer, or every request shares one partition
    // (the proxy) and the whole rate limit + audit log becomes useless.
    private const string CloudflareClientIpHeader = "CF-Connecting-IP";
    private const string ForwardedForHeader = "X-Forwarded-For";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(DemoUnlockPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ResolveClientIp(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            options.AddPolicy(ExpensiveReadPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ResolveClientIp(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            options.AddPolicy(ExternalProxyPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ResolveClientIp(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });

        return services;
    }

    /// <summary>
    /// Resolves the originating client IP through the proxy chain in front of the API
    /// Used for both rate-limit partitioning and audit logging
    /// </summary>
    public static string ResolveClientIp(HttpContext httpContext)
    {
        var cfIp = httpContext.Request.Headers[CloudflareClientIpHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cfIp))
        {
            return cfIp.Trim();
        }

        // X-Forwarded-For is a comma-separated list; the left-most entry is the original client.
        var forwardedFor = httpContext.Request.Headers[ForwardedForHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var firstHop = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                       .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstHop))
            {
                return firstHop;
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
