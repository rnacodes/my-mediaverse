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
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");

        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrEmpty(jwtSecret) && (environment.IsDevelopment() || environment.IsTesting()))
        {
            jwtSecret = jwtSettings["Secret"];
        }

        if (string.IsNullOrEmpty(jwtSecret))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "No JWT secret configured. Deployed hosts must set the JWT_SECRET environment variable. " +
                    "Refusing to start without authentication outside Development.");
            }

            logger.LogWarning("No JWT secret configured. Authentication will not work. Set JWT_SECRET env var or JwtSettings:Secret in appsettings.json.");
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

        logger.LogInformation("JWT authentication configured. Issuer: {Issuer}, Audience: {Audience}",
            jwtSettings["Issuer"], jwtSettings["Audience"]);

        var n8nApiKey = Environment.GetEnvironmentVariable("N8N_API_KEY");
        if (!string.IsNullOrEmpty(n8nApiKey))
        {
            logger.LogInformation("API Key authentication configured for N8N.");
        }
        else
        {
            logger.LogInformation("N8N_API_KEY not configured. API key authentication is disabled.");
        }

        return services;
    }
}
