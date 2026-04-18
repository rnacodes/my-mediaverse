using MyMediaVerse.Infrastructure.Clients;
using MyMediaVerse.Infrastructure.Services;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Extensions;

public static class ExternalApiClientsExtensions
{
    public static IServiceCollection AddExternalApiClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient();

        services.AddScriptRunnerClient(configuration);
        services.AddYouTubeApiClient();
        services.AddListenNotesApiClient(configuration);
        services.AddReadwiseClients(configuration);
        services.AddOpenLibraryApiClient();
        services.AddPaperlessApiClient(configuration);
        services.AddGoogleBooksApiClient();
        services.AddQuartzApiClient();
        services.AddOpenAIEmbeddingsClient(configuration);
        services.AddGradientAIClient(configuration);
        services.AddWebsiteScrapingClients();
        services.AddRssFeedClient();
        services.AddTmdbClient(configuration);
        services.AddTraktClient();

        return services;
    }

    private static void AddScriptRunnerClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("ScriptRunner", client =>
        {
            var baseUrl = Environment.GetEnvironmentVariable("SCRIPT_RUNNER_URL")
                ?? configuration["ScriptRunner:BaseUrl"]
                ?? "http://localhost:8001";

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");

            var apiKey = Environment.GetEnvironmentVariable("SCRIPT_RUNNER_API_KEY")
                ?? configuration["ScriptRunner:ApiKey"];

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                Console.WriteLine("Script Runner HTTP client configured with API key.");
            }
            else
            {
                Console.WriteLine("Script Runner HTTP client configured (no API key).");
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

    private static void AddListenNotesApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IListenNotesApiClient, ListenNotesApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://listen-api.listennotes.com/api/v2/");
            client.Timeout = TimeSpan.FromSeconds(30);

            var apiKey = Environment.GetEnvironmentVariable("LISTENNOTES_API_KEY") ??
                         configuration["ApiKeys:ListenNotes"];

            Console.WriteLine($"API Key value: {apiKey}");

            if (string.IsNullOrEmpty(apiKey) || apiKey == "LISTENNOTES_API_KEY")
            {
                Console.WriteLine("WARNING: No valid ListenNotes API key found. ListenNotes functionality will be limited.");
                Console.WriteLine("Please set a valid API key in environment variable LISTENNOTES_API_KEY or configuration.");
            }
            else
            {
                client.DefaultRequestHeaders.Add("X-ListenAPI-Key", apiKey);
            }
        });
    }

    private static void AddReadwiseClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IReadwiseApiClient, ReadwiseApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://readwise.io/api/v2/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");

            var apiKey = Environment.GetEnvironmentVariable("READWISE_API_KEY") ??
                         Environment.GetEnvironmentVariable("READWISE_API_TOKEN") ??
                         configuration["ApiKeys:Readwise"];

            if (string.IsNullOrEmpty(apiKey) || apiKey == "READWISE_API_TOKEN")
            {
                Console.WriteLine("WARNING: No valid Readwise API key found. Readwise functionality will be limited.");
                Console.WriteLine("Please set a valid API key in environment variable READWISE_API_KEY, READWISE_API_TOKEN, or configuration.");
            }
            else
            {
                Console.WriteLine("Readwise API key configured successfully");
            }
        });

        services.AddHttpClient<IReaderApiClient, ReaderApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://readwise.io/api/v3/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");

            var apiKey = Environment.GetEnvironmentVariable("READWISE_API_KEY") ??
                         Environment.GetEnvironmentVariable("READWISE_API_TOKEN") ??
                         configuration["ApiKeys:Readwise"];

            if (string.IsNullOrEmpty(apiKey) || apiKey == "READWISE_API_TOKEN")
            {
                Console.WriteLine("WARNING: No valid Readwise API key found. Reader functionality will be limited.");
            }
            else
            {
                Console.WriteLine("Readwise Reader API key configured successfully");
            }
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

    private static void AddPaperlessApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IPaperlessApiClient, PaperlessApiClient>(client =>
        {
            var apiUrl = Environment.GetEnvironmentVariable("PAPERLESS_API_URL") ??
                         configuration["Paperless:ApiUrl"];
            var apiToken = Environment.GetEnvironmentVariable("PAPERLESS_API_TOKEN") ??
                           configuration["Paperless:ApiToken"];

            Console.WriteLine("=== Paperless-ngx Configuration Debug ===");
            Console.WriteLine($"API URL: {(string.IsNullOrEmpty(apiUrl) ? "NOT CONFIGURED" : apiUrl)}");
            Console.WriteLine($"API Token: {(string.IsNullOrEmpty(apiToken) ? "NOT CONFIGURED" : "SET")}");

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiToken))
            {
                Console.WriteLine("WARNING: Paperless-ngx API is not configured.");
                Console.WriteLine("Document sync functionality will not be available until properly configured.");
                Console.WriteLine("Expected environment variables:");
                Console.WriteLine("  PAPERLESS_API_URL (e.g., http://localhost:8000/api)");
                Console.WriteLine("  PAPERLESS_API_TOKEN (API token from Paperless-ngx settings)");

                client.BaseAddress = new Uri("http://localhost:8000/api/");
            }
            else
            {
                if (!apiUrl.EndsWith('/'))
                    apiUrl += "/";

                client.BaseAddress = new Uri(apiUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Token {apiToken}");
                Console.WriteLine("Paperless-ngx API client configured successfully.");
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

    private static void AddOpenAIEmbeddingsClient(this IServiceCollection services, IConfiguration configuration)
    {
        var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                           configuration["OpenAI:ApiKey"];

        services.AddHttpClient("OpenAIEmbeddings", client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
            client.Timeout = TimeSpan.FromSeconds(60);

            if (!string.IsNullOrEmpty(openAIApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAIApiKey}");
                var embeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-large";
                var dimensions = Environment.GetEnvironmentVariable("OPENAI_DIMENSIONS") ?? "1024";
                Console.WriteLine($"OpenAI embeddings configured: {embeddingModel} ({dimensions}D)");
            }
            else
            {
                Console.WriteLine("WARNING: OpenAI API key not configured. Embedding generation will be disabled.");
            }
        });
    }

    private static void AddGradientAIClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IGradientAIClient, GradientAIClient>(client =>
        {
            var baseUrl = Environment.GetEnvironmentVariable("GRADIENT_BASE_URL") ??
                          configuration["GradientAI:BaseUrl"] ??
                          "https://api.gradient.ai/api/v1/";

            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
            client.Timeout = TimeSpan.FromSeconds(60);

            var gradientApiKey = Environment.GetEnvironmentVariable("GRADIENT_API_KEY") ??
                                 configuration["GradientAI:ApiKey"];

            if (!string.IsNullOrEmpty(gradientApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {gradientApiKey}");
                Console.WriteLine("Gradient AI client configured for text generation.");
            }
            else
            {
                Console.WriteLine("WARNING: Gradient AI API key not configured. Text generation will be disabled.");
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

    private static void AddTmdbClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<TmdbApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");

            var apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY") ??
                         configuration["ApiKeys:TMDB"];

            if (string.IsNullOrEmpty(apiKey) || apiKey == "TMDB_API_KEY")
            {
                Console.WriteLine("WARNING: No valid TMDB API key found. TMDB functionality will be limited.");
                Console.WriteLine("Please set a valid API key in environment variable TMDB_API_KEY or configuration.");
            }
            else
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
