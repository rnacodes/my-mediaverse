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

            Console.WriteLine("=== Typesense Configuration Debug ===");
            Console.WriteLine($"API Key: {(string.IsNullOrEmpty(apiKey) ? "MISSING" : "SET")}");
            Console.WriteLine($"Host: {(string.IsNullOrEmpty(host) ? "MISSING" : host)}");
            Console.WriteLine($"Port: {portString}");
            Console.WriteLine($"Protocol: {protocol}");
            Console.WriteLine($"Collection Prefix (env var): {(collectionPrefixEnv == null ? "NOT SET" : $"'{collectionPrefixEnv}'")}");
            Console.WriteLine($"Collection Prefix (appsettings): {(collectionPrefixConfig == null ? "NOT SET" : $"'{collectionPrefixConfig}'")}");
            Console.WriteLine($"Effective Prefix: '{(!string.IsNullOrEmpty(collectionPrefixEnv) ? collectionPrefixEnv : collectionPrefixConfig ?? "")}'");

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(host))
            {
                Console.WriteLine("WARNING: Typesense configuration is incomplete.");
                Console.WriteLine("Search functionality will not be available until properly configured.");
                Console.WriteLine("Expected environment variables:");
                Console.WriteLine("  TYPESENSE_ADMIN_API_KEY");
                Console.WriteLine("  TYPESENSE_HOST (e.g., search.mymediaverseuniverse.com)");
                Console.WriteLine("  TYPESENSE_PORT (default: 443)");
                Console.WriteLine("  TYPESENSE_PROTOCOL (default: https)");

                // Dummy client prevents crashes when Typesense is not configured.
                var dummyNodes = new List<Node> { new Node("localhost", "8108", "http") };
                var dummyConfig = new Config(dummyNodes, "dummy-key");
                var dummyHttpClient = new HttpClient();
                return new TypesenseClient(Options.Create(dummyConfig), dummyHttpClient);
            }

            if (!int.TryParse(portString, out int port))
            {
                Console.WriteLine($"WARNING: Invalid Typesense port '{portString}', defaulting to 443");
                port = 443;
            }

            var nodes = new List<Node>
            {
                new Node(host, port.ToString(), protocol)
            };

            var config = new Config(nodes, apiKey);

            Console.WriteLine("Typesense client configured successfully.");

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
        try
        {
            using var scope = app.Services.CreateScope();
            var typeSenseService = scope.ServiceProvider.GetService<ITypeSenseService>();
            if (typeSenseService != null)
            {
                Console.WriteLine("Initializing Typesense collections...");
                await typeSenseService.EnsureCollectionExistsAsync();
                Console.WriteLine("Typesense media_items collection initialized.");
                await typeSenseService.EnsureMixlistCollectionExistsAsync();
                Console.WriteLine("Typesense mixlists collection initialized.");
                await typeSenseService.EnsureNotesCollectionExistsAsync();
                Console.WriteLine("Typesense obsidian_notes collection initialized.");
                await typeSenseService.EnsureHighlightsCollectionExistsAsync();
                Console.WriteLine("Typesense highlights collection initialized.");
                Console.WriteLine("Typesense collection initialization complete.");
            }
            else
            {
                Console.WriteLine("Typesense service not available. Skipping collection initialization.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to initialize Typesense collections: {ex.Message}");
            Console.WriteLine("Application will continue, but search functionality may not work.");
        }
    }
}
