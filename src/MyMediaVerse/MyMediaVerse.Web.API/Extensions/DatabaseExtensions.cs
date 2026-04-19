using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Data;
using Pgvector.EntityFrameworkCore;

namespace MyMediaVerse.Web.API.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Resolves the PostgreSQL connection string from (in priority order):
    /// 1. Configuration "DefaultConnection" (respects appsettings.{Env}.json)
    /// 2. DATABASE_URL env var (Render.com standard)
    /// 3. ConnectionStrings__DefaultConnection env var
    ///
    /// Applies best-effort fixes (URL decoding, postgres:// -> postgresql://, rebuild as key/value) so
    /// Render-style URLs parse cleanly through NpgsqlConnectionStringBuilder.
    /// </summary>
    public static string ResolveConnectionString(IConfiguration configuration, IWebHostEnvironment environment, ILogger logger)
    {
        var configConnectionString = configuration.GetConnectionString("DefaultConnection");
        var connectionString = (!string.IsNullOrEmpty(configConnectionString) ? configConnectionString : null) ??
                              Environment.GetEnvironmentVariable("DATABASE_URL") ??
                              Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        string connectionSource;
        if (!string.IsNullOrEmpty(configConnectionString))
            connectionSource = $"appsettings.{environment.EnvironmentName}.json";
        else if (Environment.GetEnvironmentVariable("DATABASE_URL") != null)
            connectionSource = "DATABASE_URL env var";
        else if (Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") != null)
            connectionSource = "ConnectionStrings__DefaultConnection env var";
        else
            connectionSource = "none";
        logger.LogInformation("Connection string source: {Source}", connectionSource);

        if (string.IsNullOrEmpty(connectionString))
        {
            if (environment.IsDevelopment())
            {
                logger.LogWarning("No database connection string configured for development. Using placeholder; database operations will fail.");
                connectionString = "Host=localhost;Database=projectloopbreaker;Username=postgres;Password=password";
            }
            else if (environment.EnvironmentName == "Testing")
            {
                connectionString = "Host=localhost;Database=test;Username=test;Password=test";
            }
            else
            {
                throw new InvalidOperationException("Database connection string is required but not configured. Please set DATABASE_URL environment variable or configure DefaultConnection in appsettings.json");
            }
        }

        if (environment.EnvironmentName == "Testing")
        {
            return connectionString;
        }

        logger.LogDebug("Connection string length: {Length}", connectionString.Length);
        logger.LogDebug("Connection string starts with: {Prefix}...",
            connectionString.Substring(0, Math.Min(20, connectionString.Length)));

        var pattern = Regex.Replace(connectionString, @"://([^:]+):([^@]+)@", "://[USER]:[PASSWORD]@");
        logger.LogDebug("Connection string pattern: {Pattern}", pattern);

        try
        {
            var testBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            logger.LogInformation("Connection string parsed successfully. Host: {Host}, Database: {Database}",
                testBuilder.Host, testBuilder.Database);
            return connectionString;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse connection string. Attempting fixes.");
            return TryFixConnectionString(connectionString, pattern, ex, logger);
        }
    }

    private static string TryFixConnectionString(string connectionString, string pattern, Exception originalException, ILogger logger)
    {
        var fixedConnectionString = connectionString.Trim();

        if (fixedConnectionString.Contains('%'))
        {
            fixedConnectionString = Uri.UnescapeDataString(fixedConnectionString);
            logger.LogInformation("URL-decoded the connection string.");
        }

        if (fixedConnectionString.StartsWith("postgres://"))
        {
            fixedConnectionString = fixedConnectionString.Replace("postgres://", "postgresql://");
            logger.LogInformation("Converted postgres:// to postgresql://.");
        }

        var uriMatch = Regex.Match(fixedConnectionString, @"^postgresql://([^:]+):([^@]+)@([^/]+)/([^?]+)");
        if (!uriMatch.Success)
        {
            logger.LogError("Connection string doesn't match expected PostgreSQL URL format: postgresql://user:password@host/database. Length: {Length}, ends with: ...{Suffix}",
                fixedConnectionString.Length,
                fixedConnectionString.Substring(Math.Max(0, fixedConnectionString.Length - 20)));

            if (fixedConnectionString.EndsWith("/"))
            {
                logger.LogError("Connection string ends with '/' but has no database name.");
            }
            else if (!fixedConnectionString.Contains("/") || fixedConnectionString.LastIndexOf("/") == fixedConnectionString.IndexOf("//") + 1)
            {
                logger.LogError("Connection string is missing database name part.");
            }

            throw new InvalidOperationException($"Connection string format is invalid. Expected format: postgresql://user:password@host/database. Got: {pattern}", originalException);
        }

        logger.LogInformation("Connection string parsed as URL. user={User}, host={Host}, database={Database}",
            uriMatch.Groups[1].Value, uriMatch.Groups[3].Value, uriMatch.Groups[4].Value);

        try
        {
            var testBuilder2 = new Npgsql.NpgsqlConnectionStringBuilder(fixedConnectionString);
            logger.LogInformation("Fixed connection string parsed successfully. Host: {Host}, Database: {Database}",
                testBuilder2.Host, testBuilder2.Database);
            return fixedConnectionString;
        }
        catch (Exception ex2)
        {
            logger.LogError(ex2, "Even after fixes, connection string still invalid. Attempting manual rebuild.");

            try
            {
                var user = uriMatch.Groups[1].Value;
                var password = uriMatch.Groups[2].Value;
                var host = uriMatch.Groups[3].Value;
                var database = uriMatch.Groups[4].Value;

                var rebuiltConnectionString = $"Host={host};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                logger.LogInformation("Rebuilt connection string as key/value format. Host={Host}, Database={Database}, Username={User}",
                    host, database, user);

                var testBuilder3 = new Npgsql.NpgsqlConnectionStringBuilder(rebuiltConnectionString);
                logger.LogInformation("Rebuilt connection string parsed successfully. Host: {Host}, Database: {Database}",
                    testBuilder3.Host, testBuilder3.Database);
                return rebuiltConnectionString;
            }
            catch (Exception ex3)
            {
                logger.LogError(ex3, "Manual rebuild also failed.");
                throw new InvalidOperationException($"Database connection string format is invalid and cannot be fixed. Original error: {originalException.Message}. After fixes: {ex2.Message}. After rebuild: {ex3.Message}", originalException);
            }
        }
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
        if (environment.EnvironmentName == "Testing")
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
