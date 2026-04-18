using System.Text;
using System.Text.RegularExpressions;
using MyMediaVerse.Web.API.Extensions;
using MyMediaVerse.Web.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// --- CORS ---
builder.Services.AddCorsPolicies(builder.Configuration, builder.Environment);

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
builder.Services.AddJwtAndApiKeyAuthentication(builder.Configuration);

// --- Database (EF Core + PostgreSQL + pgvector) ---
var connectionString = DatabaseExtensions.ResolveConnectionString(builder.Configuration, builder.Environment);
builder.Services.AddDatabase(connectionString, builder.Environment);

// --- Application services, external API clients, background workers ---
builder.Services.AddApplicationServices();
builder.Services.AddExternalApiClients(builder.Configuration);
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
                            ?? app.Configuration["Auth:Username"];
                        var expectedPassword = Environment.GetEnvironmentVariable("AUTH_PASSWORD")
                            ?? app.Configuration["Auth:Password"];

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
app.UseRouting();

app.UseCors(CorsExtensions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
var maskedConnectionString = Regex.Replace(connectionString, @"(Password|password)=([^;]+)", "$1=****");
maskedConnectionString = Regex.Replace(maskedConnectionString, @"://([^:]+):([^@]+)@", "://****:****@");
Console.WriteLine($"Connection string: {maskedConnectionString}");

app.Run();

// Make Program class accessible for testing
public partial class Program { }
