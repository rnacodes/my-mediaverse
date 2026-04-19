using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.IntegrationTests
{
    public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public WebApplicationFactory()
        {
            // Set environment variable BEFORE anything else runs
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            Console.WriteLine("🚀 WebApplicationFactory constructor called - Environment set to Testing");
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("🔧 InitializeAsync called - About to resolve services");
            using var scope = Services.CreateScope();
            Console.WriteLine($"🔍 Service provider created. Service count: {Services.GetType().GetProperty("Count")?.GetValue(Services) ?? "Unknown"}");
            
            var context = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
            
            // CRITICAL SAFETY CHECK #1: Verify we're using in-memory database
            // This prevents accidental pollution of production database during tests
            var isInMemory = context.Database.IsInMemory();
            var providerName = context.Database.ProviderName;

            Console.WriteLine("=== INTEGRATION TEST SAFETY CHECK ===");
            Console.WriteLine($"Database Provider: {providerName}");
            Console.WriteLine($"Is InMemory: {isInMemory}");

            // CRITICAL SAFETY CHECK #2: Ensure it's NOT PostgreSQL
            if (providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true ||
                providerName?.Contains("Postgres", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new InvalidOperationException(
                    "CRITICAL SAFETY VIOLATION: Integration tests are using PostgreSQL database!\n" +
                    $"Provider: {providerName}\n" +
                    "This would POLLUTE THE PRODUCTION DATABASE!\n" +
                    "Tests have been ABORTED.\n" +
                    "DO NOT RUN TESTS UNTIL THIS IS FIXED!");
            }

            // CRITICAL SAFETY CHECK #3: Ensure it IS InMemory
            if (!isInMemory)
            {
                throw new InvalidOperationException(
                    "CRITICAL SAFETY VIOLATION: Integration tests are NOT using in-memory database!\n" +
                    $"Provider: {providerName}\n" +
                    $"Is InMemory: {isInMemory}\n" +
                    "This would pollute a real database. Tests have been ABORTED.\n" +
                    "Check that WebApplicationFactory is properly configured.");
            }

            Console.WriteLine("ALL SAFETY CHECKS PASSED");
            Console.WriteLine("Integration tests using IN-MEMORY database (isolated from production)");
            Console.WriteLine("==========================================");
            
            await context.Database.EnsureCreatedAsync();
        }

        public new async Task DisposeAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
            await context.Database.EnsureDeletedAsync();
            await base.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // SET ENVIRONMENT FIRST - This is critical for Program.cs conditional registration
            builder.UseEnvironment("Testing");
            
            // Use ConfigureTestServices to run AFTER Program.cs
            builder.ConfigureTestServices(services =>
            {
                Console.WriteLine("=== WebApplicationFactory ConfigureServices START ===");
                
                // Remove ALL DbContext registrations (must remove all to prevent conflicts)
                var descriptorsToRemove = services.Where(
                    d => d.ServiceType == typeof(DbContextOptions<MediaLibraryDbContext>) ||
                         d.ServiceType == typeof(MediaLibraryDbContext) ||
                         d.ServiceType == typeof(DbContextOptions) ||
                         d.ImplementationType == typeof(MediaLibraryDbContext))
                    .ToList();
                
                Console.WriteLine($"Found {descriptorsToRemove.Count} DbContext registrations to remove");
                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                    Console.WriteLine($"REMOVED: {descriptor.ServiceType.Name} (Lifetime: {descriptor.Lifetime})");
                }

                // Also remove IApplicationDbContext if it exists
                var appDbContextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MyMediaVerse.Application.Interfaces.IApplicationDbContext));
                if (appDbContextDescriptor != null)
                {
                    services.Remove(appDbContextDescriptor);
                    Console.WriteLine("REMOVED: IApplicationDbContext");
                }

                // Add in-memory database for testing (FORCED - no PostgreSQL allowed)
                var dbName = "TestDatabase_" + Guid.NewGuid().ToString();
                Console.WriteLine($"Registering InMemory database: {dbName}");
                
                services.AddDbContext<MediaLibraryDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.EnableSensitiveDataLogging();
                });

                // Register IApplicationDbContext (Testing environment needs this)
                services.AddScoped<MyMediaVerse.Application.Interfaces.IApplicationDbContext>(provider =>
                {
                    var context = provider.GetRequiredService<MediaLibraryDbContext>();
                    return context;
                });
                
                Console.WriteLine("✅ DbContext and IApplicationDbContext registered");
                Console.WriteLine("=== WebApplicationFactory ConfigureServices END ===");

                // Replace real Typesense service with a no-op mock to prevent
                // test data from leaking into the production Typesense instance.
                // Without this, every CRUD operation in integration tests indexes
                // documents into the real Typesense server via environment variables,
                // creating orphaned search records that don't exist in any database.
                var typesenseDescriptors = services.Where(d =>
                    d.ServiceType == typeof(ITypeSenseService))
                    .ToList();

                foreach (var desc in typesenseDescriptors)
                {
                    services.Remove(desc);
                }

                var mockTypeSenseService = new Mock<ITypeSenseService>();
                services.AddScoped(_ => mockTypeSenseService.Object);
                Console.WriteLine("✅ ITypeSenseService replaced with no-op mock (prevents Typesense leakage)");

                // Configure ListenNotes API to use MOCK server for testing
                // See: https://www.listennotes.com/api/docs/?test=1
                var listenNotesDescriptor = services.Where(d => 
                    d.ServiceType == typeof(MyMediaVerse.Shared.Interfaces.IListenNotesApiClient) ||
                    d.ImplementationType?.Name == "ListenNotesApiClient")
                    .ToList();
                    
                foreach (var desc in listenNotesDescriptor)
                {
                    services.Remove(desc);
                }

                // Re-add ListenNotes API client with mock server URL
                services.AddHttpClient<MyMediaVerse.Shared.Interfaces.IListenNotesApiClient, 
                    MyMediaVerse.Infrastructure.Clients.ListenNotes.ListenNotesApiClient>(client =>
                {
                    // Mock server returns fake data for testing - no API key required
                    client.BaseAddress = new Uri("https://listen-api-test.listennotes.com/api/v2/");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
            });
        }
    }
}
