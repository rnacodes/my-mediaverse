using MyMediaVerse.Infrastructure.Clients.AI;
using MyMediaVerse.Infrastructure.Clients.Google;
using MyMediaVerse.Infrastructure.Clients.ListenNotes;
using MyMediaVerse.Infrastructure.Clients.Obsidian;
using MyMediaVerse.Infrastructure.Clients.OpenLibrary;
using MyMediaVerse.Infrastructure.Clients.Paperless;
using MyMediaVerse.Infrastructure.Clients.Readwise;
using MyMediaVerse.Infrastructure.Clients.TMDB;
using MyMediaVerse.Infrastructure.Clients.Trakt;
using MyMediaVerse.Infrastructure.Clients.YouTube;
using MyMediaVerse.Infrastructure.Services;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Extensions;

public static class ExternalApiClientsExtensions
{
    public static IServiceCollection AddExternalApiClients(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        services.AddHttpClient();

        services.AddScriptRunnerClient(configuration, logger);
        services.AddYouTubeApiClient();
        services.AddListenNotesApiClient(configuration, logger);
        services.AddReadwiseClients(configuration, logger);
        services.AddOpenLibraryApiClient();
        services.AddPaperlessApiClient(configuration, logger);
        services.AddGoogleBooksApiClient();
        services.AddQuartzApiClient();
        services.AddOpenAIEmbeddingsClient(configuration, logger);
        services.AddGradientAIClient(configuration, logger);
        services.AddWebsiteScrapingClients();
        services.AddRssFeedClient();
        services.AddTmdbClient(configuration, logger);
        services.AddTraktClient();

        return services;
    }

    private static void AddScriptRunnerClient(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var baseUrl = Environment.GetEnvironmentVariable("SCRIPT_RUNNER_URL")
            ?? configuration["ScriptRunner:BaseUrl"]
            ?? "http://localhost:8001";
        var apiKey = Environment.GetEnvironmentVariable("SCRIPT_RUNNER_API_KEY")
            ?? configuration["ScriptRunner:ApiKey"];

        if (!string.IsNullOrEmpty(apiKey))
        {
            logger.LogInformation("Script Runner HTTP client configured with API key.");
        }
        else
        {
            logger.LogInformation("Script Runner HTTP client configured (no API key).");
        }

        services.AddHttpClient("ScriptRunner", client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
        });
    }

    private static void AddYouTubeApiClient(this IServiceCollection services)
    {
        services.AddHttpClient<IYouTubeApiClient, YouTubeApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
        });
    }

    private static void AddListenNotesApiClient(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var apiKey = Environment.GetEnvironmentVariable("LISTENNOTES_API_KEY") ??
                     configuration["ApiKeys:ListenNotes"];
        var hasApiKey = !string.IsNullOrEmpty(apiKey) && apiKey != "LISTENNOTES_API_KEY";

        if (!hasApiKey)
        {
            logger.LogWarning("No valid ListenNotes API key found. ListenNotes functionality will be limited. Set LISTENNOTES_API_KEY env var or ApiKeys:ListenNotes in configuration.");
        }

        services.AddHttpClient<IListenNotesApiClient, ListenNotesApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://listen-api.listennotes.com/api/v2/");
            client.Timeout = TimeSpan.FromSeconds(30);

            if (hasApiKey)
            {
                client.DefaultRequestHeaders.Add("X-ListenAPI-Key", apiKey);
            }
        });
    }

    private static void AddReadwiseClients(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var apiKey = Environment.GetEnvironmentVariable("READWISE_API_KEY") ??
                     Environment.GetEnvironmentVariable("READWISE_API_TOKEN") ??
                     configuration["ApiKeys:Readwise"];
        var hasApiKey = !string.IsNullOrEmpty(apiKey) && apiKey != "READWISE_API_TOKEN";

        if (!hasApiKey)
        {
            logger.LogWarning("No valid Readwise API key found. Readwise + Reader functionality will be limited. Set READWISE_API_KEY env var, READWISE_API_TOKEN env var, or ApiKeys:Readwise in configuration.");
        }
        else
        {
            logger.LogInformation("Readwise API key configured successfully.");
        }

        services.AddHttpClient<IReadwiseApiClient, ReadwiseApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://readwise.io/api/v2/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
        });

        services.AddHttpClient<IReaderApiClient, ReaderApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://readwise.io/api/v3/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
        });
    }

    private static void AddOpenLibraryApiClient(this IServiceCollection services)
    {
        services.AddHttpClient<IOpenLibraryApiClient, OpenLibraryApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://openlibrary.org/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0 (https://github.com/yourrepo/projectloopbreaker)");
        });
    }

    private static void AddPaperlessApiClient(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var apiUrl = Environment.GetEnvironmentVariable("PAPERLESS_API_URL") ??
                     configuration["Paperless:ApiUrl"];
        var apiToken = Environment.GetEnvironmentVariable("PAPERLESS_API_TOKEN") ??
                       configuration["Paperless:ApiToken"];
        var isConfigured = !string.IsNullOrEmpty(apiUrl) && !string.IsNullOrEmpty(apiToken);

        if (!isConfigured)
        {
            logger.LogWarning("Paperless-ngx API is not configured. Document sync unavailable. ApiUrl={ApiUrl}, ApiToken={TokenStatus}. Expected env vars: PAPERLESS_API_URL, PAPERLESS_API_TOKEN.",
                string.IsNullOrEmpty(apiUrl) ? "NOT CONFIGURED" : apiUrl,
                string.IsNullOrEmpty(apiToken) ? "NOT CONFIGURED" : "SET");
        }
        else
        {
            if (!apiUrl!.EndsWith('/'))
                apiUrl += "/";
            logger.LogInformation("Paperless-ngx API client configured. ApiUrl={ApiUrl}", apiUrl);
        }

        services.AddHttpClient<IPaperlessApiClient, PaperlessApiClient>(client =>
        {
            if (isConfigured)
            {
                client.BaseAddress = new Uri(apiUrl!);
                client.DefaultRequestHeaders.Add("Authorization", $"Token {apiToken}");
            }
            else
            {
                // Placeholder so DI consumers don't NRE; real calls will fail loudly.
                client.BaseAddress = new Uri("http://localhost:8000/api/");
            }

            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    }

    private static void AddGoogleBooksApiClient(this IServiceCollection services)
    {
        services.AddHttpClient<IGoogleBooksApiClient, GoogleBooksApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/books/v1/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
        });
    }

    private static void AddQuartzApiClient(this IServiceCollection services)
    {
        services.AddHttpClient<IQuartzApiClient, QuartzApiClient>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    }

    private static void AddOpenAIEmbeddingsClient(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                           configuration["OpenAI:ApiKey"];

        if (!string.IsNullOrEmpty(openAIApiKey))
        {
            var embeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-large";
            var dimensions = Environment.GetEnvironmentVariable("OPENAI_DIMENSIONS") ?? "1024";
            logger.LogInformation("OpenAI embeddings configured. Model={Model}, Dimensions={Dimensions}", embeddingModel, dimensions);
        }
        else
        {
            logger.LogWarning("OpenAI API key not configured. Embedding generation will be disabled.");
        }

        services.AddHttpClient("OpenAIEmbeddings", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
            client.Timeout = TimeSpan.FromSeconds(60);

            if (!string.IsNullOrEmpty(openAIApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAIApiKey}");
            }
        });
    }

    private static void AddGradientAIClient(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var baseUrl = Environment.GetEnvironmentVariable("GRADIENT_BASE_URL") ??
                      configuration["GradientAI:BaseUrl"] ??
                      "https://api.gradient.ai/api/v1/";
        var gradientApiKey = Environment.GetEnvironmentVariable("GRADIENT_API_KEY") ??
                             configuration["GradientAI:ApiKey"];

        if (!string.IsNullOrEmpty(gradientApiKey))
        {
            logger.LogInformation("Gradient AI client configured for text generation.");
        }
        else
        {
            logger.LogWarning("Gradient AI API key not configured. Text generation will be disabled.");
        }

        services.AddHttpClient<IGradientAIClient, GradientAIClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
            client.Timeout = TimeSpan.FromSeconds(60);

            if (!string.IsNullOrEmpty(gradientApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {gradientApiKey}");
            }
        });
    }

    private static void AddWebsiteScrapingClients(this IServiceCollection services)
    {
        services.AddHttpClient<IWebsiteScraperService, WebsiteScraperService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IWebsiteScreenshotService, WebsiteScreenshotService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
    }

    private static void AddRssFeedClient(this IServiceCollection services)
    {
        services.AddHttpClient<IRssFeedService, RssFeedService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
    }

    private static void AddTmdbClient(this IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY") ??
                     configuration["ApiKeys:TMDB"];
        var hasApiKey = !string.IsNullOrEmpty(apiKey) && apiKey != "TMDB_API_KEY";

        if (!hasApiKey)
        {
            logger.LogWarning("No valid TMDB API key found. TMDB functionality will be limited. Set TMDB_API_KEY env var or ApiKeys:TMDB in configuration.");
        }

        services.AddHttpClient<TmdbApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            if (hasApiKey)
            {
                client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0 (https://github.com/yourrepo/projectloopbreaker)");
            }
        });

        services.AddScoped<ITmdbApiClient>(provider => provider.GetRequiredService<TmdbApiClient>());
    }

    private static void AddTraktClient(this IServiceCollection services)
    {
        services.AddHttpClient<ITraktApiClient, TraktApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.trakt.tv/");
            client.DefaultRequestHeaders.Add("trakt-api-version", "2");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
        });
    }
}
