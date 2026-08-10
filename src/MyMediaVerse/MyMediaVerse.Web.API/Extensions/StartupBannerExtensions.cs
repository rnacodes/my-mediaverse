using System.Text;

namespace MyMediaVerse.Web.API.Extensions;

/// <summary>
/// The access-control posture this host booted with, rendered once and kept so it can be
/// reprinted later. Which rules apply is spread across the environment name, the fallback
/// authorization policy, the demo write gate, and several environment variables; this puts
/// the resolved answers in one place so a running host can be identified at a glance.
/// Only presence of secrets is reported, never their values.
/// </summary>
public sealed record StartupBanner(string Text);

public static class StartupBannerExtensions
{
    /// <summary>
    /// Renders the banner from startup state and registers it, so both the startup log and
    /// the operator endpoint report exactly the same thing.
    /// </summary>
    public static IServiceCollection AddStartupBanner(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        string? connectionString)
        => services.AddSingleton(new StartupBanner(Build(environment, configuration, connectionString)));

    public static void LogStartupBanner(this WebApplication app)
        => app.Logger.LogInformation("{Banner}", app.Services.GetRequiredService<StartupBanner>().Text);

    private static string Build(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        string? connectionString)
    {
        var isDemo = environment.IsDemo();
        var isDevelopment = environment.IsDevelopment();

        var reads = isDemo ? "anonymous GET allowed" : "authentication required";
        var writes = isDemo ? "blocked unless TOTP-unlocked" : "authentication required";

        // Login falls back to well-known development credentials only when nothing is
        // configured, so the banner distinguishes "configured" from "using the default".
        var credentials = Configured("AUTH_USERNAME", configuration["Auth:Username"])
            ? "configured"
            : isDevelopment ? "DEV DEFAULTS (admin/password123)" : "NOT CONFIGURED — login impossible";

        var database = string.IsNullOrWhiteSpace(connectionString)
            ? "(not configured)"
            : DatabaseExtensions.MaskConnectionString(connectionString);

        return new StringBuilder()
            .AppendLine()
            .AppendLine("┌─ My MediaVerse API ────────────────────────────────")
            .AppendLine($"│ Environment      : {environment.EnvironmentName}")
            .AppendLine($"│ Demo host        : {(isDemo ? "yes" : "no")}")
            .AppendLine($"│ Reads            : {reads}")
            .AppendLine($"│ Writes           : {writes}")
            .AppendLine($"│ Demo write gate  : {(isDemo ? "active" : "inactive")}")
            .AppendLine($"│ Login credentials: {credentials}")
            .AppendLine($"│ JWT_SECRET       : {(Configured("JWT_SECRET", null) ? "present" : "MISSING")}")
            .AppendLine($"│ Swagger          : /swagger{(isDevelopment ? " (open)" : " (basic auth)")}")
            .AppendLine($"│ Database         : {database}")
            .AppendLine("└────────────────────────────────────────────────────")
            .ToString();
    }

    private static bool Configured(string variable, string? configValue) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)) ||
        !string.IsNullOrWhiteSpace(configValue);
}
