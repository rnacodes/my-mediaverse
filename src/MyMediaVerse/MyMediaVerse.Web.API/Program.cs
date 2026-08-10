using MyMediaVerse.Web.API.Conventions;
using MyMediaVerse.Web.API.Extensions;
using MyMediaVerse.Web.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// --- Sentry error monitoring ---
// Reads the DSN from the SENTRY_DSN env var. When unset (local dev, Testing),
// the SDK is disabled and this is a no-op. Environment name mirrors ASPNETCORE_ENVIRONMENT
// (Production / Demo / Development) so issues stay separable per host in Sentry.
builder.WebHost.UseSentry(o =>
{
    o.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? string.Empty;
    o.Environment = builder.Environment.EnvironmentName;
    // Performance/tracing is handled in a later milestone; keep it off for now.
    o.TracesSampleRate = 0.0;
});

// Bootstrap logger for startup code (registration runs before app.Services is built).
// Uses the same Logging config as the final app, and is disposed at shutdown.
using var startupLoggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    logging.AddConsole();
});
var startupLogger = startupLoggerFactory.CreateLogger("Startup");

// --- CORS ---
builder.Services.AddCorsPolicies(builder.Configuration, builder.Environment, startupLogger);

// --- Controllers + JSON options ---
// The environment gating convention strips [Environments]-restricted endpoints from
// the route table on hosts where they don't apply (404 + absent from Swagger).
builder.Services.AddControllers(options =>
    {
        options.Conventions.Add(new EnvironmentGatingConvention(builder.Environment.EnvironmentName));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// --- Rate limiting (demo unlock brute-force, AI-inference reads, external API proxies) ---
builder.Services.AddApiRateLimiting();

// --- Authentication (JWT + API Key) ---
builder.Services.AddJwtAndApiKeyAuthentication(builder.Configuration, builder.Environment, startupLogger);

// --- Database (EF Core + PostgreSQL + pgvector) ---
var connectionString = DatabaseExtensions.ResolveConnectionString(builder.Configuration, builder.Environment, startupLogger);
builder.Services.AddDatabase(connectionString, builder.Environment);
builder.Services.AddStartupBanner(builder.Environment, builder.Configuration, connectionString);

// --- Application services, external API clients, background workers ---
builder.Services.AddMemoryCache();
builder.Services.AddApplicationServices();
builder.Services.AddExternalApiClients(builder.Configuration, startupLogger);
builder.Services.AddBackgroundServices(builder.Configuration, builder.Environment);

// --- Storage + Search ---
builder.Services.AddS3Storage();
builder.Services.AddTypesense();

// --- Swagger / OpenAPI ---
builder.Services.AddSwaggerWithAuth();

var app = builder.Build();

// Initialize Typesense collections on startup (safe if Typesense is unconfigured).
await app.InitializeTypesenseCollectionsAsync();

// Configure the HTTP request pipeline.

// Global exception handler first so it catches everything downstream.
app.UseGlobalExceptionHandler();

// Protect Swagger UI with Basic Authentication in non-development environments.
if (!app.Environment.IsDevelopment())
{
    app.UseSwaggerBasicAuth();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My MediaVerse API V1");
    options.RoutePrefix = "swagger";
});

// No in-app HTTPS redirection: TLS terminates at the hosting platform's edge, which
// forces HTTPS before requests reach this app, so the container only ever serves plain
// HTTP.
app.UseRouting();

app.UseCors(CorsExtensions.PolicyName);

app.UseRateLimiter();

// Demo read-only write gate. Must sit after UseRouting (it reads [AllowAnonymous] endpoint
// metadata) and before UseAuthentication, so a blocked demo write returns a friendly 403
// with a machine-readable code rather than a bare 401 from the authorization layer.
app.UseDemoWriteGate();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.LogStartupBanner();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
