using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MyMediaVerse.Web.API.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Named policy applied to the demo TOTP unlock endpoint to slow brute-force guessing.
    /// </summary>
    public const string DemoUnlockPolicy = "demo-unlock";

    // Header names used by the proxies in front of the demo/prod API. The real client IP
    // must be read from these, not the TCP peer, or every request shares one partition
    // (the proxy) and the whole rate limit + audit log becomes useless.
    private const string CloudflareClientIpHeader = "CF-Connecting-IP";
    private const string ForwardedForHeader = "X-Forwarded-For";

    public static IServiceCollection AddDemoRateLimiting(this IServiceCollection services)
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
        });

        return services;
    }

    /// <summary>
    /// Resolves the originating client IP through the proxy chain in front of the API
    /// (Cloudflare, then Render/other reverse proxies), falling back to the direct
    /// connection. Used for both rate-limit partitioning and audit logging so probing
    /// is attributed to the actual visitor, not the proxy.
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
