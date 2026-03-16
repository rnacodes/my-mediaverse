using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace MyMediaVerse.Web.API.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if X-API-Key header exists
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var apiKeyHeader))
        {
            // No API key header - let other schemes handle it
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeader.ToString();
        var validApiKey = _configuration["N8N_API_KEY"]
            ?? Environment.GetEnvironmentVariable("N8N_API_KEY");

        if (string.IsNullOrEmpty(validApiKey))
        {
            Logger.LogWarning("N8N_API_KEY is not configured. API key authentication is disabled.");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Use constant-time comparison to prevent timing attacks
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedApiKey),
            Encoding.UTF8.GetBytes(validApiKey)))
        {
            Logger.LogWarning("Invalid API key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create claims principal for authenticated service
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "N8N-Service"),
            new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
            new Claim("service", "n8n")
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationOptions.DefaultScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.DefaultScheme);

        Logger.LogInformation("API key authentication successful for N8N-Service");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
