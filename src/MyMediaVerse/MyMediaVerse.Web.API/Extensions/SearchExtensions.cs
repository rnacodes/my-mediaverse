using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Infrastructure.Services;
using Typesense;
using Typesense.Setup;

namespace MyMediaVerse.Web.API.Extensions;

public static class SearchExtensions
{
    public static IServiceCollection AddTypesense(this IServiceCollection services)
    {
        services.AddSingleton<ITypesenseClient>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Typesense");

            var apiKey = Environment.GetEnvironmentVariable("TYPESENSE_ADMIN_API_KEY") ??
                         configuration["Typesense:AdminApiKey"];
            var host = Environment.GetEnvironmentVariable("TYPESENSE_HOST") ??
                       configuration["Typesense:Host"];
            var portString = Environment.GetEnvironmentVariable("TYPESENSE_PORT") ??
                             configuration["Typesense:Port"] ?? "443";
            var protocol = Environment.GetEnvironmentVariable("TYPESENSE_PROTOCOL") ??
                           configuration["Typesense:Protocol"] ?? "https";

            var collectionPrefixEnv = Environment.GetEnvironmentVariable("TYPESENSE_COLLECTION_PREFIX");
            var collectionPrefixConfig = configuration["Typesense:CollectionPrefix"];
            var effectivePrefix = !string.IsNullOrEmpty(collectionPrefixEnv)
                ? collectionPrefixEnv
                : collectionPrefixConfig ?? "";

            logger.LogDebug(
                "Typesense config — ApiKey:{ApiKeyStatus}, Host:{Host}, Port:{Port}, Protocol:{Protocol}, CollectionPrefix:{Prefix}",
                string.IsNullOrEmpty(apiKey) ? "MISSING" : "SET",
                string.IsNullOrEmpty(host) ? "MISSING" : host,
                portString,
                protocol,
                effectivePrefix);

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(host))
            {
                logger.LogWarning("Typesense configuration is incomplete. Search functionality unavailable. Expected env vars: TYPESENSE_ADMIN_API_KEY, TYPESENSE_HOST (optional: TYPESENSE_PORT, TYPESENSE_PROTOCOL).");

                // Dummy client prevents crashes when Typesense is not configured.
                var dummyNodes = new List<Node> { new Node("localhost", "8108", "http") };
                var dummyConfig = new Config(dummyNodes, "dummy-key");
                var dummyHttpClient = new HttpClient();
                return new TypesenseClient(Options.Create(dummyConfig), dummyHttpClient);
            }

            if (!int.TryParse(portString, out int port))
            {
                logger.LogWarning("Invalid Typesense port '{PortString}', defaulting to 443.", portString);
                port = 443;
            }

            var nodes = new List<Node>
            {
                new Node(host, port.ToString(), protocol)
            };

            var config = new Config(nodes, apiKey);

            logger.LogInformation("Typesense client configured successfully.");

            var httpClient = new HttpClient();
            return new TypesenseClient(Options.Create(config), httpClient);
        });

        services.AddScoped<ITypeSenseService, TypeSenseService>();

        return services;
    }

    /// <summary>
    /// Initializes Typesense collections on startup. Safe to call when Typesense is unconfigured —
    /// exceptions are caught so the rest of the app can still boot.
    /// </summary>
    public static async Task InitializeTypesenseCollectionsAsync(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Typesense");

        try
        {
            using var scope = app.Services.CreateScope();
            var typeSenseService = scope.ServiceProvider.GetService<ITypeSenseService>();
            if (typeSenseService != null)
            {
                logger.LogInformation("Initializing Typesense collections...");
                await typeSenseService.EnsureCollectionExistsAsync();
                await typeSenseService.EnsureMixlistCollectionExistsAsync();
                await typeSenseService.EnsureNotesCollectionExistsAsync();
                await typeSenseService.EnsureHighlightsCollectionExistsAsync();
                logger.LogInformation("Typesense collection initialization complete (media_items, mixlists, obsidian_notes, highlights).");
            }
            else
            {
                logger.LogInformation("Typesense service not available. Skipping collection initialization.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to initialize Typesense collections. Application will continue, but search functionality may not work.");
        }
    }
}
