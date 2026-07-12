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

            // The Typesense client wants a BARE host (no scheme, no port). It also builds its URI in
            // the constructor, so a host like "http://localhost:8108" throws UriFormatException and
            // would take search down. Normalize common mistakes (full URL or host:port) into parts.
            (host, port, protocol) = NormalizeTypesenseEndpoint(host, port, protocol, logger);

            var nodes = new List<Node>
            {
                new Node(host, port.ToString(), protocol)
            };

            var config = new Config(nodes, apiKey);

            try
            {
                var httpClient = new HttpClient();
                var client = new TypesenseClient(Options.Create(config), httpClient);
                logger.LogInformation("Typesense client configured successfully (Host:{Host}, Port:{Port}, Protocol:{Protocol}).", host, port, protocol);
                return client;
            }
            catch (UriFormatException ex)
            {
                logger.LogError(ex, "Typesense host '{Host}' could not be parsed into a valid URI. Search will be disabled. Set TYPESENSE_HOST to a bare hostname (e.g. 'localhost' or 'search.example.com') with TYPESENSE_PORT/TYPESENSE_PROTOCOL separate.", host);
                return CreateDisabledTypesenseClient();
            }
        });

        services.AddScoped<ITypesenseService, TypesenseService>();

        // Best-effort "reindex after import" helper for interactive imports (see IImportReindexService).
        services.AddScoped<IImportReindexService, ImportReindexService>();

        return services;
    }

    /// <summary>
    /// Normalizes a configured Typesense host into the bare host + port/protocol the client expects.
    /// Tolerates a pasted full URL ("https://host:443") or a "host:port" string, which the v8 client
    /// would otherwise reject with a UriFormatException at construction.
    /// </summary>
    private static (string host, int port, string protocol) NormalizeTypesenseEndpoint(
        string host, int port, string protocol, ILogger logger)
    {
        host = host.Trim();

        // Full URL pasted in (e.g. "https://search.example.com" or "http://localhost:8108").
        if (host.Contains("://") && Uri.TryCreate(host, UriKind.Absolute, out var uri))
        {
            var normalizedPort = uri.IsDefaultPort ? port : uri.Port;
            logger.LogWarning(
                "TYPESENSE_HOST '{Raw}' looks like a URL; using Host='{Host}', Port={Port}, Protocol='{Protocol}'. Prefer a bare hostname with TYPESENSE_PORT/TYPESENSE_PROTOCOL separate.",
                host, uri.Host, normalizedPort, uri.Scheme);
            return (uri.Host, normalizedPort, uri.Scheme);
        }

        // "host:port" with no scheme (e.g. "localhost:8108").
        var lastColon = host.LastIndexOf(':');
        if (lastColon > 0 && int.TryParse(host[(lastColon + 1)..], out var embeddedPort))
        {
            var bareHost = host[..lastColon];
            logger.LogWarning(
                "TYPESENSE_HOST '{Raw}' includes a port; using Host='{Host}', Port={Port}. Prefer a bare hostname with TYPESENSE_PORT separate.",
                host, bareHost, embeddedPort);
            return (bareHost, embeddedPort, protocol);
        }

        return (host, port, protocol);
    }

    // Returned when Typesense is unconfigured so DI resolution doesn't fail.
    // Points at an unreachable localhost endpoint — any real search call raises a
    // connection error caught by TypesenseService's try/catch blocks, and the app
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
            var typesenseService = scope.ServiceProvider.GetService<ITypesenseService>();
            if (typesenseService != null)
            {
                logger.LogInformation("Initializing Typesense collections...");
                await typesenseService.EnsureCollectionExistsAsync();
                await typesenseService.EnsureMixlistCollectionExistsAsync();
                await typesenseService.EnsureNotesCollectionExistsAsync();
                await typesenseService.EnsureHighlightsCollectionExistsAsync();
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
