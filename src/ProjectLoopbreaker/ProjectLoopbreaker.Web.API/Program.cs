using Microsoft.EntityFrameworkCore;
using ProjectLoopbreaker.Infrastructure.Data;
using ProjectLoopbreaker.Infrastructure.Clients;
using ProjectLoopbreaker.Infrastructure.Services;
using ProjectLoopbreaker.Infrastructure.Repositories;
using ProjectLoopbreaker.Domain.Interfaces;
using ProjectLoopbreaker.Application.Interfaces;
using ProjectLoopbreaker.Application.Services;
using ProjectLoopbreaker.Shared.Interfaces;
using ProjectLoopbreaker.Web.API.Middleware;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Typesense;
using Typesense.Setup;
using Pgvector.EntityFrameworkCore;
using ProjectLoopbreaker.Web.API.Authentication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Add CORS Policy ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Demo")
        {
            // Development/Demo: Allow localhost on common frontend ports
            var devOrigins = new List<string>
            {
                "http://localhost:3000",    // React default
                "https://localhost:3000",   // React HTTPS
                "http://localhost:5173",    // Vite default
                "https://localhost:5173",   // Vite HTTPS
                "http://localhost:5174",    // Vite alternate port
                "https://localhost:5174",   // Vite alternate port HTTPS
                "http://localhost:4200",    // Angular default
                "https://localhost:4200"    // Angular HTTPS
            };

            // In Demo environment, also allow the deployed demo frontend URL
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
            if (!string.IsNullOrEmpty(frontendUrl))
            {
                devOrigins.Add(frontendUrl);
            }

            // Allow production frontend to make cross-origin requests to demo API
            // (needed for the DemoUnlockPage admin panel on the production site)
            var additionalOrigins = Environment.GetEnvironmentVariable("ADDITIONAL_CORS_ORIGINS");
            if (!string.IsNullOrEmpty(additionalOrigins))
            {
                devOrigins.AddRange(additionalOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            policy.WithOrigins(devOrigins.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();  // Required for HttpOnly cookies (JWT refresh tokens)
        }
        else
        {
            // Production: Allow your frontend domain and configure based on environment variables
            var allowedOrigins = new List<string>
            {
                "https://www.mymediaverseuniverse.com",  // Production frontend URL
                "https://mymediaverseuniverse.com"       // Production frontend URL (without www)
            };
            
            // Add frontend URL from environment variable if provided
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
            if (!string.IsNullOrEmpty(frontendUrl))
            {
                allowedOrigins.Add(frontendUrl);
            }
            
            // Add any additional allowed origins from configuration
            var configuredOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
            if (configuredOrigins != null)
            {
                allowedOrigins.AddRange(configuredOrigins);
            }
            
            if (allowedOrigins.Any())
            {
                policy.WithOrigins(allowedOrigins.ToArray())
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                // Fallback: Allow any origin (less secure, but prevents CORS errors during deployment testing)
                Console.WriteLine("WARNING: No specific frontend origins configured. Allowing all origins for CORS.");
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        }
    });
});

// Configure JSON serialization to return enums as strings and handle circular references
builder.Services.AddControllers(options =>
    {
        // Add DemoReadOnlyFilter globally - blocks write operations in Demo environment
        options.Filters.Add<ProjectLoopbreaker.Web.API.Filters.DemoReadOnlyFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// --- Configure JWT and API Key Authentication ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["Secret"];

if (string.IsNullOrEmpty(jwtSecret))
{
    Console.WriteLine("WARNING: No JWT secret configured. Authentication will not work.");
    Console.WriteLine("Please set JWT_SECRET environment variable or configure JwtSettings:Secret in appsettings.json");
}
else
{
    var key = Encoding.ASCII.GetBytes(jwtSecret);

    // Configure authentication with multiple schemes: JWT Bearer and API Key
    // Uses a policy scheme to automatically select the appropriate scheme based on the request
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "MultiAuth";
        options.DefaultChallengeScheme = "MultiAuth";
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = true; // Set to true in production if using HTTPS
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
        };
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, options => { })
    .AddPolicyScheme("MultiAuth", "JWT or API Key", options =>
    {
        // If X-API-Key header is present, use API key authentication
        // Otherwise, use JWT Bearer authentication
        options.ForwardDefaultSelector = context =>
        {
            if (context.Request.Headers.ContainsKey(ApiKeyAuthenticationOptions.HeaderName))
            {
                return ApiKeyAuthenticationOptions.DefaultScheme;
            }
            return JwtBearerDefaults.AuthenticationScheme;
        };
    });

    builder.Services.AddAuthorization();

    Console.WriteLine("JWT Authentication configured successfully.");
    Console.WriteLine($"JWT Issuer: {jwtSettings["Issuer"]}");
    Console.WriteLine($"JWT Audience: {jwtSettings["Audience"]}");

    // Check if API key authentication is configured
    var n8nApiKey = Environment.GetEnvironmentVariable("N8N_API_KEY");
    if (!string.IsNullOrEmpty(n8nApiKey))
    {
        Console.WriteLine("API Key authentication configured for N8N.");
    }
    else
    {
        Console.WriteLine("INFO: N8N_API_KEY not configured. API key authentication is disabled.");
    }
}

// --- Configure EF Core & PostgreSQL ---
// Try to get connection string from various sources with priority:
// 1. Configuration DefaultConnection (respects environment-specific appsettings like appsettings.Demo.json)
// 2. Environment variable DATABASE_URL (Render.com standard)
// 3. Environment variable ConnectionStrings__DefaultConnection
var configConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = (!string.IsNullOrEmpty(configConnectionString) ? configConnectionString : null) ??
                      Environment.GetEnvironmentVariable("DATABASE_URL") ??
                      Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

string connectionSource;
if (!string.IsNullOrEmpty(configConnectionString))
    connectionSource = $"appsettings.{builder.Environment.EnvironmentName}.json";
else if (Environment.GetEnvironmentVariable("DATABASE_URL") != null)
    connectionSource = "DATABASE_URL env var";
else if (Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") != null)
    connectionSource = "ConnectionStrings__DefaultConnection env var";
else
    connectionSource = "none";
Console.WriteLine($"Connection string source: {connectionSource}");

// Validate connection string before proceeding
if (string.IsNullOrEmpty(connectionString))
{
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("WARNING: No database connection string configured for development.");
        Console.WriteLine("Using placeholder connection string. Database operations will fail.");
        connectionString = "Host=localhost;Database=projectloopbreaker;Username=postgres;Password=password";
    }
    else if (builder.Environment.EnvironmentName == "Testing")
    {
        // For testing environment, use a dummy connection string as in-memory database will be configured
        connectionString = "Host=localhost;Database=test;Username=test;Password=test";
    }
    else
    {
        throw new InvalidOperationException("Database connection string is required but not configured. Please set DATABASE_URL environment variable or configure DefaultConnection in appsettings.json");
    }
}

// Debug connection string (safely)
Console.WriteLine($"Connection string length: {connectionString.Length}");
Console.WriteLine($"Connection string starts with: {connectionString.Substring(0, Math.Min(20, connectionString.Length))}...");

// Show the connection string pattern without exposing credentials
var pattern = System.Text.RegularExpressions.Regex.Replace(connectionString, @"://([^:]+):([^@]+)@", "://[USER]:[PASSWORD]@");
Console.WriteLine($"Connection string pattern: {pattern}");

// Handle potential connection string format issues
try
{
    // Test if the connection string can be parsed
    var testBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
    Console.WriteLine($"Connection string parsed successfully. Host: {testBuilder.Host}, Database: {testBuilder.Database}");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Failed to parse connection string: {ex.Message}");
    
    // Check for common issues and try to fix them
    var fixedConnectionString = connectionString;
    
    // Issue 1: Remove any leading/trailing whitespace
    fixedConnectionString = fixedConnectionString.Trim();
    
    // Issue 2: Handle URL-encoded characters that might cause issues
    if (fixedConnectionString.Contains("%"))
    {
        fixedConnectionString = Uri.UnescapeDataString(fixedConnectionString);
        Console.WriteLine("Attempted to URL-decode the connection string");
    }
    
    // Issue 3: Check if it's a postgres:// URL that needs to be converted to postgresql://
    if (fixedConnectionString.StartsWith("postgres://"))
    {
        fixedConnectionString = fixedConnectionString.Replace("postgres://", "postgresql://");
        Console.WriteLine("Converted postgres:// to postgresql://");
    }
    
    // Issue 4: Check if the connection string is complete (has all required parts)
    // Use [^?]+ to stop at query parameters (e.g., ?sslmode=require)
    var uriMatch = System.Text.RegularExpressions.Regex.Match(fixedConnectionString, @"^postgresql://([^:]+):([^@]+)@([^/]+)/([^?]+)");
    if (!uriMatch.Success)
    {
        Console.WriteLine("ERROR: Connection string doesn't match expected PostgreSQL URL format: postgresql://user:password@host/database");
        Console.WriteLine($"Full connection string length: {fixedConnectionString.Length}");
        Console.WriteLine($"Connection string ends with: ...{fixedConnectionString.Substring(Math.Max(0, fixedConnectionString.Length - 20))}");
        
        // Check if it's just missing the database name or has other issues
        if (fixedConnectionString.EndsWith("/"))
        {
            Console.WriteLine("ERROR: Connection string ends with '/' but has no database name");
        }
        else if (!fixedConnectionString.Contains("/") || fixedConnectionString.LastIndexOf("/") == fixedConnectionString.IndexOf("//") + 1)
        {
            Console.WriteLine("ERROR: Connection string is missing database name part");
        }
        
        throw new InvalidOperationException($"Connection string format is invalid. Expected format: postgresql://user:password@host/database. Got: {pattern}");
    }
    
    Console.WriteLine($"Connection string appears to have correct format: user={uriMatch.Groups[1].Value}, host={uriMatch.Groups[3].Value}, database={uriMatch.Groups[4].Value}");
    
    // Try parsing the fixed connection string
    try
    {
        var testBuilder2 = new Npgsql.NpgsqlConnectionStringBuilder(fixedConnectionString);
        Console.WriteLine($"Fixed connection string parsed successfully! Host: {testBuilder2.Host}, Database: {testBuilder2.Database}");
        connectionString = fixedConnectionString;
    }
    catch (Exception ex2)
    {
        Console.WriteLine($"ERROR: Even after fixes, connection string still invalid: {ex2.Message}");
        
        // Try alternative: parse manually and rebuild
        try
        {
            Console.WriteLine("Attempting to manually parse and rebuild connection string...");
            var user = uriMatch.Groups[1].Value;
            var password = uriMatch.Groups[2].Value;
            var host = uriMatch.Groups[3].Value;
            var database = uriMatch.Groups[4].Value;
            
            // Rebuild as key-value format instead of URL format
            var rebuiltConnectionString = $"Host={host};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
            Console.WriteLine($"Rebuilt connection string format: Host={host};Database={database};Username={user};Password=[HIDDEN];SSL Mode=Require;Trust Server Certificate=true");
            
            var testBuilder3 = new Npgsql.NpgsqlConnectionStringBuilder(rebuiltConnectionString);
            Console.WriteLine($"Rebuilt connection string parsed successfully! Host: {testBuilder3.Host}, Database: {testBuilder3.Database}");
            connectionString = rebuiltConnectionString;
        }
        catch (Exception ex3)
        {
            Console.WriteLine($"ERROR: Manual rebuild also failed: {ex3.Message}");
            throw new InvalidOperationException($"Database connection string format is invalid and cannot be fixed. Original error: {ex.Message}. After fixes: {ex2.Message}. After rebuild: {ex3.Message}");
        }
    }
}

// Register DbContext (skip for Testing environment - WebApplicationFactory will register InMemory)
if (builder.Environment.EnvironmentName != "Testing")
{
    // Configure EF Core with PostgreSQL, pgvector support, and dynamic JSON serialization
    // EnableDynamicJson() is required for Npgsql 8.x to serialize List<string> properties as JSONB
    // Note: The Pgvector NuGet package 0.3.x doesn't include the NpgsqlDataSourceBuilder extension,
    // so we rely on the EF Core UseVector() configuration which handles the type mapping internally.
    var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.EnableDynamicJson();
    var dataSource = dataSourceBuilder.Build();

    builder.Services.AddDbContext<MediaLibraryDbContext>(options =>
        options.UseNpgsql(dataSource, o => o.UseVector()));

    // Register IApplicationDbContext
    builder.Services.AddScoped<IApplicationDbContext>(provider =>
        provider.GetRequiredService<MediaLibraryDbContext>());
}
// Note: In Testing environment, WebApplicationFactory will register InMemory DbContext

// Register Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPodcastMappingService, PodcastMappingService>();
builder.Services.AddScoped<IPodcastService, PodcastService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IBookMappingService, BookMappingService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IMovieMappingService, MovieMappingService>();
builder.Services.AddScoped<ITvShowService, TvShowService>();
builder.Services.AddScoped<ITvShowMappingService, TvShowMappingService>();
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IYouTubeService, YouTubeService>();
builder.Services.AddScoped<IYouTubeMappingService, YouTubeMappingService>();
builder.Services.AddScoped<IYouTubeChannelService, YouTubeChannelService>();
builder.Services.AddScoped<IYouTubePlaylistService, YouTubePlaylistService>();
builder.Services.AddScoped<ITmdbService, TmdbService>();
builder.Services.AddScoped<IListenNotesService, ListenNotesService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IArticleMappingService, ArticleMappingService>();
builder.Services.AddScoped<IArticleDeduplicationService, ArticleDeduplicationService>();
builder.Services.AddScoped<IWebsiteService, WebsiteService>();
builder.Services.AddScoped<IWebsiteMappingService, WebsiteMappingService>();
builder.Services.AddScoped<IGoodreadsImportService, GoodreadsImportService>();

// Register Document services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentMappingService, DocumentMappingService>();

// Register a generic HttpClient for use in controllers
builder.Services.AddHttpClient();

// Configure Script Runner HTTP client (for Python FastAPI script execution service)
builder.Services.AddHttpClient("ScriptRunner", client =>
{
    var baseUrl = Environment.GetEnvironmentVariable("SCRIPT_RUNNER_URL")
        ?? builder.Configuration["ScriptRunner:BaseUrl"]
        ?? "http://localhost:8001";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(10); // Long timeout for script execution
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");

    var apiKey = Environment.GetEnvironmentVariable("SCRIPT_RUNNER_API_KEY")
        ?? builder.Configuration["ScriptRunner:ApiKey"];

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

// Register Readwise services
builder.Services.AddScoped<IHighlightService, HighlightService>();
builder.Services.AddScoped<IReadwiseService, ReadwiseService>();
builder.Services.AddScoped<IReaderService, ReaderService>();

// Configure YouTube API client  
builder.Services.AddHttpClient<IYouTubeApiClient, YouTubeApiClient>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");
});

// In Program.cs

// Configure ListenNotes API client
builder.Services.AddHttpClient<IListenNotesApiClient, ListenNotesApiClient>(client =>
{
    client.BaseAddress = new Uri("https://listen-api.listennotes.com/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(30); // Add 30 second timeout
    
    var apiKey = Environment.GetEnvironmentVariable("LISTENNOTES_API_KEY") ?? 
                 builder.Configuration["ApiKeys:ListenNotes"];
    
    Console.WriteLine($"API Key value: {apiKey}");

    if (string.IsNullOrEmpty(apiKey) || apiKey == "LISTENNOTES_API_KEY")
    {
        Console.WriteLine("WARNING: No valid ListenNotes API key found. ListenNotes functionality will be limited.");
        Console.WriteLine("Please set a valid API key in environment variable LISTENNOTES_API_KEY or configuration.");
        // Don't throw exception, just log warning
    }
    else
    {
        client.DefaultRequestHeaders.Add("X-ListenAPI-Key", apiKey);
    }
});

// Configure Readwise API client
builder.Services.AddHttpClient<IReadwiseApiClient, ReadwiseApiClient>(client =>
{
    client.BaseAddress = new Uri("https://readwise.io/api/v2/");
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");
    
    var apiKey = Environment.GetEnvironmentVariable("READWISE_API_KEY") ?? 
                 Environment.GetEnvironmentVariable("READWISE_API_TOKEN") ??
                 builder.Configuration["ApiKeys:Readwise"];
    
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

// Configure Readwise Reader API client
builder.Services.AddHttpClient<IReaderApiClient, ReaderApiClient>(client =>
{
    client.BaseAddress = new Uri("https://readwise.io/api/v3/");
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");
    
    // Reader uses the same API key as Readwise
    var apiKey = Environment.GetEnvironmentVariable("READWISE_API_KEY") ?? 
                 Environment.GetEnvironmentVariable("READWISE_API_TOKEN") ??
                 builder.Configuration["ApiKeys:Readwise"];
    
    if (string.IsNullOrEmpty(apiKey) || apiKey == "READWISE_API_TOKEN")
    {
        Console.WriteLine("WARNING: No valid Readwise API key found. Reader functionality will be limited.");
    }
    else
    {
        Console.WriteLine("Readwise Reader API key configured successfully");
    }
});

// Configure Open Library API client
builder.Services.AddHttpClient<IOpenLibraryApiClient, OpenLibraryApiClient>(client =>
{
    client.BaseAddress = new Uri("https://openlibrary.org/");
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0 (https://github.com/yourrepo/projectloopbreaker)");
});

// Configure Paperless-ngx API client
// NOTE: Requires PAPERLESS_API_URL and PAPERLESS_API_TOKEN environment variables
// Example: PAPERLESS_API_URL=http://localhost:8000/api, PAPERLESS_API_TOKEN=your-token-here
builder.Services.AddHttpClient<IPaperlessApiClient, PaperlessApiClient>(client =>
{
    var apiUrl = Environment.GetEnvironmentVariable("PAPERLESS_API_URL") ??
                 builder.Configuration["Paperless:ApiUrl"];
    var apiToken = Environment.GetEnvironmentVariable("PAPERLESS_API_TOKEN") ??
                   builder.Configuration["Paperless:ApiToken"];

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

        // Set placeholder base address to prevent null reference
        client.BaseAddress = new Uri("http://localhost:8000/api/");
    }
    else
    {
        // Ensure URL ends with /
        if (!apiUrl.EndsWith("/"))
            apiUrl += "/";

        client.BaseAddress = new Uri(apiUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Token {apiToken}");
        Console.WriteLine("Paperless-ngx API client configured successfully.");
    }

    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");
    client.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for document operations
});

// Register OpenLibrary service
builder.Services.AddScoped<IOpenLibraryService, OpenLibraryService>();

// Configure Google Books API client
builder.Services.AddHttpClient<IGoogleBooksApiClient, GoogleBooksApiClient>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/books/v1/");
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");
});

// Register Google Books service
builder.Services.AddScoped<IGoogleBooksService, GoogleBooksService>();

// Configure Quartz API client for Obsidian notes sync
builder.Services.AddHttpClient<IQuartzApiClient, QuartzApiClient>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");
    client.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for fetching content index
});

// Register Note service
builder.Services.AddScoped<INoteService, NoteService>();

// Register AI service for description and embedding generation
builder.Services.AddScoped<IAIService, AIService>();

// Register Vector Search repository (uses pgvector for similarity queries)
builder.Services.AddScoped<IVectorSearchRepository, VectorSearchRepository>();

// Register Recommendation service for vector similarity searches (uses pgvector)
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// Configure Note Description Generation background service
builder.Services.Configure<NoteDescriptionGenerationOptions>(
    builder.Configuration.GetSection(NoteDescriptionGenerationOptions.SectionName));

// Only register the Note Description Generation hosted service if not in Testing environment
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddHostedService<NoteDescriptionGenerationHostedService>();
    Console.WriteLine("Note description generation background service registered.");
}

// Configure Embedding Generation background service
builder.Services.Configure<EmbeddingGenerationOptions>(
    builder.Configuration.GetSection(EmbeddingGenerationOptions.SectionName));

// Only register the Embedding Generation hosted service if not in Testing environment
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddHostedService<EmbeddingGenerationHostedService>();
    Console.WriteLine("Embedding generation background service registered.");
}

// Configure Obsidian Note Sync background service
builder.Services.Configure<ObsidianNoteSyncOptions>(
    builder.Configuration.GetSection(ObsidianNoteSyncOptions.SectionName));

// Only register the Obsidian sync hosted service if not in Testing environment
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddHostedService<ObsidianNoteSyncHostedService>();
    Console.WriteLine("Obsidian note sync background service registered.");
}

// Configure Book Description Enrichment background service
builder.Services.Configure<BookDescriptionEnrichmentOptions>(
    builder.Configuration.GetSection(BookDescriptionEnrichmentOptions.SectionName));
builder.Services.AddScoped<IBookDescriptionEnrichmentService, BookDescriptionEnrichmentService>();

// Only register the hosted service if not in Testing environment
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddHostedService<BookDescriptionEnrichmentHostedService>();
    Console.WriteLine("Book description enrichment background service registered.");
}

// Configure Movie/TV TMDB Enrichment background service
builder.Services.Configure<MovieTvEnrichmentOptions>(
    builder.Configuration.GetSection(MovieTvEnrichmentOptions.SectionName));
builder.Services.AddScoped<IMovieTvEnrichmentService, MovieTvEnrichmentService>();

// Only register the hosted service if not in Testing environment
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddHostedService<MovieTvEnrichmentHostedService>();
    Console.WriteLine("Movie/TV TMDB enrichment background service registered.");
}

// Configure Podcast ListenNotes Enrichment background service
builder.Services.Configure<PodcastEnrichmentOptions>(
    builder.Configuration.GetSection(PodcastEnrichmentOptions.SectionName));
builder.Services.AddScoped<IPodcastEnrichmentService, PodcastEnrichmentService>();

// Only register the hosted service if not in Testing environment
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddHostedService<PodcastEnrichmentHostedService>();
    Console.WriteLine("Podcast ListenNotes enrichment background service registered.");
}

// Configure OpenAI client for embeddings
// Requires OPENAI_API_KEY environment variable
// Optional: OPENAI_EMBEDDING_MODEL (default: text-embedding-3-large)
// Optional: OPENAI_DIMENSIONS (default: 1024)
var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                   builder.Configuration["OpenAI:ApiKey"];

builder.Services.AddHttpClient("OpenAIEmbeddings", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");
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

// Configure Gradient/DigitalOcean AI client for text generation (descriptions)
// Requires GRADIENT_API_KEY environment variable
// Optional: GRADIENT_GENERATION_MODEL (default: gpt-4-turbo)
builder.Services.AddHttpClient<IGradientAIClient, GradientAIClient>(client =>
{
    var baseUrl = Environment.GetEnvironmentVariable("GRADIENT_BASE_URL") ??
                  builder.Configuration["GradientAI:BaseUrl"] ??
                  "https://api.gradient.ai/api/v1/";

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0");
    client.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for AI operations

    var gradientApiKey = Environment.GetEnvironmentVariable("GRADIENT_API_KEY") ??
                         builder.Configuration["GradientAI:ApiKey"];

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

// Configure Website Scraper service
builder.Services.AddHttpClient<IWebsiteScraperService, ProjectLoopbreaker.Infrastructure.Services.WebsiteScraperService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure Website Screenshot service (uses thum.io for screenshots)
builder.Services.AddHttpClient<IWebsiteScreenshotService, ProjectLoopbreaker.Infrastructure.Services.WebsiteScreenshotService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure RSS Feed service
builder.Services.AddHttpClient<IRssFeedService, ProjectLoopbreaker.Infrastructure.Services.RssFeedService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Configure TMDB API client
builder.Services.AddHttpClient<TmdbApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    
    var apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY") ?? 
                 builder.Configuration["ApiKeys:TMDB"];

    if (string.IsNullOrEmpty(apiKey) || apiKey == "TMDB_API_KEY")
    {
        Console.WriteLine("WARNING: No valid TMDB API key found. TMDB functionality will be limited.");
        Console.WriteLine("Please set a valid API key in environment variable TMDB_API_KEY or configuration.");
        // Don't throw exception, just log warning
    }
    else
    {
        // TMDB uses query parameter for API key
        client.DefaultRequestHeaders.Add("User-Agent", "ProjectLoopbreaker/1.0 (https://github.com/yourrepo/projectloopbreaker)");
    }
});
builder.Services.AddScoped<ITmdbApiClient>(provider => provider.GetRequiredService<TmdbApiClient>());

// Configure DigitalOcean Spaces S3 Client (optional - won't break app if not configured)
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    
    // Get DigitalOcean Spaces configuration
    var spacesConfig = configuration.GetSection("DigitalOceanSpaces");
    var accessKey = spacesConfig["AccessKey"];
    var secretKey = spacesConfig["SecretKey"];
    var endpoint = spacesConfig["Endpoint"];
    var region = spacesConfig["Region"];
    var bucketName = spacesConfig["BucketName"];

    // Enhanced debugging
    Console.WriteLine("=== DigitalOcean Spaces Configuration Debug ===");
    Console.WriteLine($"AccessKey: {(string.IsNullOrEmpty(accessKey) ? "MISSING" : "SET")}");
    Console.WriteLine($"SecretKey: {(string.IsNullOrEmpty(secretKey) ? "MISSING" : "SET")}");
    Console.WriteLine($"Endpoint: {(string.IsNullOrEmpty(endpoint) ? "MISSING" : endpoint)}");
    Console.WriteLine($"Region: {(string.IsNullOrEmpty(region) ? "MISSING" : region)}");
    Console.WriteLine($"BucketName: {(string.IsNullOrEmpty(bucketName) ? "MISSING" : bucketName)}");

    // Check if any values are still placeholders or empty
    var hasPlaceholders = accessKey == "SPACES_ACCESS_KEY" || secretKey == "SPACES_SECRET_KEY" || 
                         endpoint == "SPACES_ENDPOINT" || region == "SPACES_REGION" ||
                         bucketName == "SPACES_BUCKET_NAME";

    if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || 
        string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(region) || 
        string.IsNullOrEmpty(bucketName) || hasPlaceholders)
    {
        Console.WriteLine("WARNING: DigitalOcean Spaces configuration is incomplete or contains placeholder values.");
        Console.WriteLine("Thumbnail upload functionality will not be available until properly configured.");
        Console.WriteLine("Expected environment variables:");
        Console.WriteLine("  DIGITALOCEANSPACES__ACCESSKEY");
        Console.WriteLine("  DIGITALOCEANSPACES__SECRETKEY");
        Console.WriteLine("  DIGITALOCEANSPACES__ENDPOINT");
        Console.WriteLine("  DIGITALOCEANSPACES__REGION");
        Console.WriteLine("  DIGITALOCEANSPACES__BUCKETNAME");
        
        // Return a null client - the UploadController will handle this gracefully
        return null!;
    }

    var config = new Amazon.S3.AmazonS3Config
    {
        ServiceURL = $"https://{endpoint}",
        ForcePathStyle = false // DigitalOcean Spaces uses virtual-hosted-style requests
    };

    Console.WriteLine($"Configuring DigitalOcean Spaces client with endpoint: {endpoint}");
    
    return new Amazon.S3.AmazonS3Client(accessKey, secretKey, config);
});

// Configure Typesense Client for search functionality
builder.Services.AddSingleton<ITypesenseClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    
    // Get Typesense configuration from environment variables or appsettings
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

    // Check if configuration is complete
    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(host))
    {
        Console.WriteLine("WARNING: Typesense configuration is incomplete.");
        Console.WriteLine("Search functionality will not be available until properly configured.");
        Console.WriteLine("Expected environment variables:");
        Console.WriteLine("  TYPESENSE_ADMIN_API_KEY");
        Console.WriteLine("  TYPESENSE_HOST (e.g., search.mymediaverseuniverse.com)");
        Console.WriteLine("  TYPESENSE_PORT (default: 443)");
        Console.WriteLine("  TYPESENSE_PROTOCOL (default: https)");
        
        // Return a dummy client that won't be usable
        // This prevents the app from crashing if Typesense is not configured
        var dummyNodes = new List<Node> { new Node("localhost", "8108", "http") };
        var dummyConfig = new Config(dummyNodes, "dummy-key");
        var dummyHttpClient = new HttpClient();
        return new TypesenseClient(Microsoft.Extensions.Options.Options.Create(dummyConfig), dummyHttpClient);
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
    return new TypesenseClient(Microsoft.Extensions.Options.Options.Create(config), httpClient);
});

// Register Typesense service
builder.Services.AddScoped<ITypeSenseService, TypeSenseService>();

// Add Swagger/OpenAPI services with JWT Bearer authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My MediaVerse API",
        Version = "v1",
        Description = "Personal media library management API for podcasts, books, movies, TV shows, YouTube, articles, and websites."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token (the 'Bearer' prefix is added automatically)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Initialize Typesense collections on startup (only if Typesense is configured)
try
{
    using (var scope = app.Services.CreateScope())
    {
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
}
catch (Exception ex)
{
    Console.WriteLine($"WARNING: Failed to initialize Typesense collections: {ex.Message}");
    Console.WriteLine("Application will continue, but search functionality may not work.");
}

// Configure the HTTP request pipeline.

// Add global exception handler first to catch all unhandled exceptions
app.UseGlobalExceptionHandler();

// Protect Swagger UI with Basic Authentication in non-development environments
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value?.TrimEnd('/') ?? "";

        var isSwaggerPath = path == ""
            || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

        if (isSwaggerPath)
        {
            string? authHeader = context.Request.Headers.Authorization;

            if (authHeader != null && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var encoded = authHeader["Basic ".Length..].Trim();
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                    var separatorIndex = decoded.IndexOf(':');

                    if (separatorIndex > 0)
                    {
                        var username = decoded[..separatorIndex];
                        var password = decoded[(separatorIndex + 1)..];

                        var expectedUsername = Environment.GetEnvironmentVariable("AUTH_USERNAME")
                            ?? builder.Configuration["Auth:Username"];
                        var expectedPassword = Environment.GetEnvironmentVariable("AUTH_PASSWORD")
                            ?? builder.Configuration["Auth:Password"];

                        if (!string.IsNullOrEmpty(expectedUsername)
                            && !string.IsNullOrEmpty(expectedPassword)
                            && username == expectedUsername
                            && password == expectedPassword)
                        {
                            await next();
                            return;
                        }
                    }
                }
                catch (FormatException)
                {
                    // Invalid base64, fall through to 401
                }
            }

            context.Response.StatusCode = 401;
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Project Loopbreaker API\"";
            return;
        }

        await next();
    });
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Project Loopbreaker API V1");
    options.RoutePrefix = string.Empty;
});

//app.UseHttpsRedirection();
app.UseRouting(); // Ensure routing is enabled

// --- Use CORS Policy ---
app.UseCors("AllowFrontend");

// --- Use Authentication and Authorization (order is important!) ---
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Connection string: {connectionString}");

app.Run();

// Make Program class accessible for testing
public partial class Program { }