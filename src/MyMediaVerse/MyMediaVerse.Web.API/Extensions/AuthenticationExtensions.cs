using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MyMediaVerse.Web.API.Authentication;

namespace MyMediaVerse.Web.API.Extensions;

public static class AuthenticationExtensions
{
    public const string MultiAuthScheme = "MultiAuth";

    public static IServiceCollection AddJwtAndApiKeyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["Secret"];

        if (string.IsNullOrEmpty(jwtSecret))
        {
            Console.WriteLine("WARNING: No JWT secret configured. Authentication will not work.");
            Console.WriteLine("Please set JWT_SECRET environment variable or configure JwtSettings:Secret in appsettings.json");
            return services;
        }

        var key = Encoding.ASCII.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = MultiAuthScheme;
            options.DefaultChallengeScheme = MultiAuthScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationOptions.DefaultScheme, options => { })
        .AddPolicyScheme(MultiAuthScheme, "JWT or API Key", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                if (context.Request.Headers.ContainsKey(ApiKeyAuthenticationOptions.HeaderName))
                {
                    return ApiKeyAuthenticationOptions.DefaultScheme;
                }
                return JwtBearerDefaults.AuthenticationScheme;
            };
        });

        services.AddAuthorization();

        Console.WriteLine("JWT Authentication configured successfully.");
        Console.WriteLine($"JWT Issuer: {jwtSettings["Issuer"]}");
        Console.WriteLine($"JWT Audience: {jwtSettings["Audience"]}");

        var n8nApiKey = Environment.GetEnvironmentVariable("N8N_API_KEY");
        if (!string.IsNullOrEmpty(n8nApiKey))
        {
            Console.WriteLine("API Key authentication configured for N8N.");
        }
        else
        {
            Console.WriteLine("INFO: N8N_API_KEY not configured. API key authentication is disabled.");
        }

        return services;
    }
}
