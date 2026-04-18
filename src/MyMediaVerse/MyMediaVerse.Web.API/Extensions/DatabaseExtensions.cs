using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Interfaces;
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
    public static string ResolveConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
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
        Console.WriteLine($"Connection string source: {connectionSource}");

        if (string.IsNullOrEmpty(connectionString))
        {
            if (environment.IsDevelopment())
            {
                Console.WriteLine("WARNING: No database connection string configured for development.");
                Console.WriteLine("Using placeholder connection string. Database operations will fail.");
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

        Console.WriteLine($"Connection string length: {connectionString.Length}");
        Console.WriteLine($"Connection string starts with: {connectionString.Substring(0, Math.Min(20, connectionString.Length))}...");

        var pattern = Regex.Replace(connectionString, @"://([^:]+):([^@]+)@", "://[USER]:[PASSWORD]@");
        Console.WriteLine($"Connection string pattern: {pattern}");

        try
        {
            var testBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            Console.WriteLine($"Connection string parsed successfully. Host: {testBuilder.Host}, Database: {testBuilder.Database}");
            return connectionString;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to parse connection string: {ex.Message}");
            return TryFixConnectionString(connectionString, pattern, ex);
        }
    }

    private static string TryFixConnectionString(string connectionString, string pattern, Exception originalException)
    {
        var fixedConnectionString = connectionString.Trim();

        if (fixedConnectionString.Contains('%'))
        {
            fixedConnectionString = Uri.UnescapeDataString(fixedConnectionString);
            Console.WriteLine("Attempted to URL-decode the connection string");
        }

        if (fixedConnectionString.StartsWith("postgres://"))
        {
            fixedConnectionString = fixedConnectionString.Replace("postgres://", "postgresql://");
            Console.WriteLine("Converted postgres:// to postgresql://");
        }

        var uriMatch = Regex.Match(fixedConnectionString, @"^postgresql://([^:]+):([^@]+)@([^/]+)/([^?]+)");
        if (!uriMatch.Success)
        {
            Console.WriteLine("ERROR: Connection string doesn't match expected PostgreSQL URL format: postgresql://user:password@host/database");
            Console.WriteLine($"Full connection string length: {fixedConnectionString.Length}");
            Console.WriteLine($"Connection string ends with: ...{fixedConnectionString.Substring(Math.Max(0, fixedConnectionString.Length - 20))}");

            if (fixedConnectionString.EndsWith("/"))
            {
                Console.WriteLine("ERROR: Connection string ends with '/' but has no database name");
            }
            else if (!fixedConnectionString.Contains("/") || fixedConnectionString.LastIndexOf("/") == fixedConnectionString.IndexOf("//") + 1)
            {
                Console.WriteLine("ERROR: Connection string is missing database name part");
            }

            throw new InvalidOperationException($"Connection string format is invalid. Expected format: postgresql://user:password@host/database. Got: {pattern}", originalException);
        }

        Console.WriteLine($"Connection string appears to have correct format: user={uriMatch.Groups[1].Value}, host={uriMatch.Groups[3].Value}, database={uriMatch.Groups[4].Value}");

        try
        {
            var testBuilder2 = new Npgsql.NpgsqlConnectionStringBuilder(fixedConnectionString);
            Console.WriteLine($"Fixed connection string parsed successfully! Host: {testBuilder2.Host}, Database: {testBuilder2.Database}");
            return fixedConnectionString;
        }
        catch (Exception ex2)
        {
            Console.WriteLine($"ERROR: Even after fixes, connection string still invalid: {ex2.Message}");

            try
            {
                Console.WriteLine("Attempting to manually parse and rebuild connection string...");
                var user = uriMatch.Groups[1].Value;
                var password = uriMatch.Groups[2].Value;
                var host = uriMatch.Groups[3].Value;
                var database = uriMatch.Groups[4].Value;

                var rebuiltConnectionString = $"Host={host};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                Console.WriteLine($"Rebuilt connection string format: Host={host};Database={database};Username={user};Password=[HIDDEN];SSL Mode=Require;Trust Server Certificate=true");

                var testBuilder3 = new Npgsql.NpgsqlConnectionStringBuilder(rebuiltConnectionString);
                Console.WriteLine($"Rebuilt connection string parsed successfully! Host: {testBuilder3.Host}, Database: {testBuilder3.Database}");
                return rebuiltConnectionString;
            }
            catch (Exception ex3)
            {
                Console.WriteLine($"ERROR: Manual rebuild also failed: {ex3.Message}");
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
