namespace MyMediaVerse.Web.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "AllowFrontend";

    public static IServiceCollection AddCorsPolicies(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (environment.IsDevelopment() || environment.EnvironmentName == "Demo")
                {
                    var devOrigins = new List<string>
                    {
                        "http://localhost:3000",    // React default
                        "https://localhost:3000",   // React HTTPS
                        "http://localhost:5173",    // Vite default
                        "https://localhost:5173",   // Vite HTTPS
                        "http://localhost:5174",    // Vite alternate port
                        "https://localhost:5174",   // Vite alternate port HTTPS
                        "http://localhost:4200",    // Angular default
                        "https://localhost:4200"    // Angular HTTPS
                    };

                    var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
                    if (!string.IsNullOrEmpty(frontendUrl))
                    {
                        devOrigins.Add(frontendUrl);
                    }

                    var additionalOrigins = Environment.GetEnvironmentVariable("ADDITIONAL_CORS_ORIGINS");
                    if (!string.IsNullOrEmpty(additionalOrigins))
                    {
                        devOrigins.AddRange(additionalOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    }

                    policy.WithOrigins(devOrigins.ToArray())
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else
                {
                    var allowedOrigins = new List<string>
                    {
                        "https://www.mymediaverseuniverse.com",
                        "https://mymediaverseuniverse.com"
                    };

                    var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
                    if (!string.IsNullOrEmpty(frontendUrl))
                    {
                        allowedOrigins.Add(frontendUrl);
                    }

                    var configuredOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();
                    if (configuredOrigins != null)
                    {
                        allowedOrigins.AddRange(configuredOrigins);
                    }

                    if (allowedOrigins.Any())
                    {
                        policy.WithOrigins(allowedOrigins.ToArray())
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                    else
                    {
                        logger.LogWarning("No specific frontend origins configured. Allowing all origins for CORS.");
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                }
            });
        });

        return services;
    }
}
