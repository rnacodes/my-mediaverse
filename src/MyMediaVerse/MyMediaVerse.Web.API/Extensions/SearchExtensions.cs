using Microsoft.Extensions.Options;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Infrastructure.Services.Search;
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

            var apiKey = configuration.GetEnvOrConfig("Typesense:AdminApiKey", "TYPESENSE_ADMIN_API_KEY");
            var host = configuration.GetEnvOrConfig("Typesense:Host", "TYPESENSE_HOST");
            var portString = configuration.GetEnvOrConfigOrDefault("Typesense:Port", "443", "TYPESENSE_PORT");
            var protocol = configuration.GetEnvOrConfigOrDefault("Typesense:Protocol", "https", "TYPESENSE_PROTOCOL");
            var effectivePrefix = configuration.GetEnvOrConfigOrDefault("Typesense:CollectionPrefix", string.Empty, "TYPESENSE_COLLECTION_PREFIX");

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
                return CreateDisabledTypesenseClient();
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

    // Returned when Typesense is unconfigured so DI resolution doesn't fail.
    // Points at an unreachable localhost endpoint — any real search call raises a
    // connection error caught by TypeSenseService's try/catch blocks, and the app
    // continues to run with search disabled.
    private static ITypesenseClient CreateDisabledTypesenseClient()
    {
        var unreachableNode = new Node(host: "localhost", port: "8108", protocol: "http");
        var disabledConfig = new Config(new List<Node> { unreachableNode }, apiKey: "disabled");
        return new TypesenseClient(Options.Create(disabledConfig), new HttpClient());
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
