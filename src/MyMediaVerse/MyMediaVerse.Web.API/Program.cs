using MyMediaVerse.Web.API.Extensions;
using MyMediaVerse.Web.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllers(options =>
    {
        // Add DemoReadOnlyFilter globally - blocks write operations in Demo environment
        options.Filters.Add<MyMediaVerse.Web.API.Filters.DemoReadOnlyFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// --- Authentication (JWT + API Key) ---
builder.Services.AddJwtAndApiKeyAuthentication(builder.Configuration, startupLogger);

// --- Database (EF Core + PostgreSQL + pgvector) ---
var connectionString = DatabaseExtensions.ResolveConnectionString(builder.Configuration, builder.Environment, startupLogger);
builder.Services.AddDatabase(connectionString, builder.Environment);

// --- Application services, external API clients, background workers ---
builder.Services.AddMemoryCache();
builder.Services.AddApplicationServices();
builder.Services.AddExternalApiClients(builder.Configuration, startupLogger);
builder.Services.AddBackgroundServices(builder.Configuration, builder.Environment, startupLogger);

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
    options.RoutePrefix = string.Empty;
});

//app.UseHttpsRedirection();
app.UseRouting();

app.UseCors(CorsExtensions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
app.Logger.LogInformation("Connection string: {ConnectionString}", DatabaseExtensions.MaskConnectionString(connectionString));

app.Run();

// Make Program class accessible for testing
public partial class Program { }
