using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Data;
using Pgvector.EntityFrameworkCore;

namespace MyMediaVerse.Web.API.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Replaces passwords and URL-form credentials with <c>****</c> so a connection string
    /// can be safely logged.
    /// </summary>
    public static string MaskConnectionString(string connectionString)
    {
        var masked = Regex.Replace(connectionString, @"(Password|password)=([^;]+)", "$1=****");
        return Regex.Replace(masked, @"://([^:]+):([^@]+)@", "://****:****@");
    }

    /// <summary>
    /// Resolves the PostgreSQL connection string from (in priority order):
    /// 1. Configuration "DefaultConnection" (respects appsettings.{Env}.json)
    /// 2. DATABASE_URL env var (Render.com standard)
    /// 3. ConnectionStrings__DefaultConnection env var
    ///
    /// Normalizes Render-style postgres:// URLs to key-value form so
    /// NpgsqlConnectionStringBuilder can parse them cleanly. If normalization fails, throws —
    /// no silent fallbacks.
    /// </summary>
    public static string ResolveConnectionString(IConfiguration configuration, IWebHostEnvironment environment, ILogger logger)
    {
        var (connectionString, source) = ReadConnectionString(configuration);
        logger.LogInformation("Connection string source: {Source}", source);

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = HandleMissingConnectionString(environment, logger);
        }

        // Testing uses an in-memory DB registered by WebApplicationFactory; skip parsing.
        if (environment.IsTesting())
        {
            return connectionString;
        }

        var normalized = NormalizePostgresUrl(connectionString);

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(normalized);
        logger.LogInformation("Connection string parsed. Host: {Host}, Database: {Database}",
            builder.Host, builder.Database);
        return normalized;
    }

    private static (string? connectionString, string source) ReadConnectionString(IConfiguration configuration)
    {
        var configConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(configConnection))
            return (configConnection, "appsettings / DefaultConnection");

        var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(dbUrl))
            return (dbUrl, "DATABASE_URL env var");

        var csEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrEmpty(csEnv))
            return (csEnv, "ConnectionStrings__DefaultConnection env var");

        return (null, "none");
    }

    private static string HandleMissingConnectionString(IWebHostEnvironment environment, ILogger logger)
    {
        if (environment.IsDevelopment())
        {
            logger.LogWarning("No database connection string configured for development. Using placeholder; database operations will fail.");
            return "Host=localhost;Database=projectloopbreaker;Username=postgres;Password=password";
        }

        if (environment.IsTesting())
        {
            return "Host=localhost;Database=test;Username=test;Password=test";
        }

        throw new InvalidOperationException(
            "Database connection string is required but not configured. " +
            "Please set DATABASE_URL environment variable or configure DefaultConnection in appsettings.json");
    }

    /// <summary>
    /// Normalizes Render/DigitalOcean style <c>postgres://user:pass@host[:port]/db[?sslmode=...]</c>
    /// URLs into Npgsql key-value form. Accepts an already-formatted key-value string unchanged.
    /// Throws <see cref="InvalidOperationException"/> if the input is not parseable.
    /// </summary>
    internal static string NormalizePostgresUrl(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Connection string is empty.");

        var s = raw.Trim();

        if (s.Contains('%'))
            s = Uri.UnescapeDataString(s);

        if (s.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            s = "postgresql://" + s["postgres://".Length..];

        // Already key-value form? Hand off to Npgsql as-is.
        if (!s.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return s;

        Uri uri;
        try
        {
            uri = new Uri(s);
        }
        catch (UriFormatException ex)
        {
            throw new InvalidOperationException(
                "Connection string looked like a PostgreSQL URL but was not parseable. " +
                "Expected: postgresql://user:password@host[:port]/database[?sslmode=require]",
                ex);
        }

        var database = uri.AbsolutePath.TrimStart('/');
        if (string.IsNullOrEmpty(database))
            throw new InvalidOperationException(
                "PostgreSQL URL is missing the database name. Expected: postgresql://user:password@host/database");

        var userInfo = (uri.UserInfo ?? string.Empty).Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = database,
            Username = user,
            Password = password,
            SslMode = Npgsql.SslMode.Require
        };

        return builder.ToString();
    }

    /// <summary>
    /// Registers the EF Core DbContext with PostgreSQL + pgvector. Skipped for the Testing environment
    /// (WebApplicationFactory registers an InMemory DbContext instead).
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        string connectionString,
        IWebHostEnvironment environment)
    {
        if (environment.IsTesting())
        {
            return services;
        }

        // EnableDynamicJson() is required for Npgsql 8.x to serialize List<string> properties as JSONB.
        // Pgvector 0.3.x lacks the NpgsqlDataSourceBuilder extension, so we rely on EF Core's UseVector().
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<MediaLibraryDbContext>(options =>
            options.UseNpgsql(dataSource, o => o.UseVector()));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<MediaLibraryDbContext>());

        return services;
    }
}
