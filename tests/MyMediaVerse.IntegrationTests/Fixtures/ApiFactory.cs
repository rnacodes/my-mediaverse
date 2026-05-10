using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;
using Npgsql;
using Pgvector.EntityFrameworkCore;
using Respawn;
using Testcontainers.PostgreSql;

namespace MyMediaVerse.IntegrationTests.Fixtures
{
    /// <summary>
    /// HTTP integration test factory backed by a real PostgreSQL container with pgvector.
    /// Replaces the in-memory <c>WebApplicationFactory</c> from Phase 1.0.
    ///
    /// Lifecycle:
    /// - <c>InitializeAsync</c> starts the container, builds the host, runs EF migrations,
    ///   and arms a Respawn checkpoint that excludes <c>__EFMigrationsHistory</c>.
    /// - <c>ResetDatabaseAsync</c> resets to that checkpoint between tests.
    ///
    /// Background workers stay disabled (Program.cs short-circuits on the "Testing" environment).
    /// External clients (Typesense, ListenNotes) are still substituted out.
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .Build();

        private Respawner? _respawner;
        private NpgsqlConnection? _respawnerConnection;

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            using (var scope = Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
                await context.Database.MigrateAsync();
            }

            _respawnerConnection = new NpgsqlConnection(ConnectionString);
            await _respawnerConnection.OpenAsync();
            _respawner = await Respawner.CreateAsync(_respawnerConnection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new[] { new Respawn.Graph.Table("__EFMigrationsHistory") }
            });
        }

        public new async Task DisposeAsync()
        {
            if (_respawnerConnection is not null)
            {
                await _respawnerConnection.DisposeAsync();
            }
            await _container.DisposeAsync();
            await base.DisposeAsync();
        }

        public async Task ResetDatabaseAsync()
        {
            if (_respawner is null || _respawnerConnection is null)
            {
                throw new InvalidOperationException("ApiFactory has not been initialized.");
            }
            await _respawner.ResetAsync(_respawnerConnection);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // "Testing" keeps Program.cs's background-worker short-circuit in place.
            // Real Postgres is wired in below via ConfigureTestServices.
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Drop any DbContext registrations Program.cs may have added (it short-circuits
                // under "Testing", so this is defensive — the legacy WebApplicationFactory also
                // ran this loop and we want ApiFactory to be safe regardless of how that path evolves).
                var dbContextDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<MediaLibraryDbContext>) ||
                    d.ServiceType == typeof(MediaLibraryDbContext) ||
                    d.ServiceType == typeof(IApplicationDbContext))
                    .ToList();
                foreach (var descriptor in dbContextDescriptors)
                {
                    services.Remove(descriptor);
                }

                // Register the real DbContext against the Testcontainers Postgres instance.
                // EnableDynamicJson() mirrors production (required for List<string> JSONB columns).
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
                dataSourceBuilder.EnableDynamicJson();
                var dataSource = dataSourceBuilder.Build();

                services.AddDbContext<MediaLibraryDbContext>(options =>
                    options.UseNpgsql(dataSource, o => o.UseVector()));

                services.AddScoped<IApplicationDbContext>(provider =>
                    provider.GetRequiredService<MediaLibraryDbContext>());

                // Substitute external services so tests stay hermetic.
                ReplaceWithSubstitute<ITypesenseService>(services);

                // ListenNotes points at the public mock server when not substituted out elsewhere.
                var listenNotesDescriptors = services.Where(d =>
                    d.ServiceType == typeof(IListenNotesApiClient) ||
                    d.ImplementationType?.Name == "ListenNotesApiClient")
                    .ToList();
                foreach (var descriptor in listenNotesDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddHttpClient<IListenNotesApiClient,
                    MyMediaVerse.Infrastructure.Clients.ListenNotes.ListenNotesApiClient>(client =>
                {
                    client.BaseAddress = new Uri("https://listen-api-test.listennotes.com/api/v2/");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
            });
        }

        private static void ReplaceWithSubstitute<TService>(IServiceCollection services) where TService : class
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(TService)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
            var substitute = Substitute.For<TService>();
            services.AddScoped(_ => substitute);
        }
    }
}
