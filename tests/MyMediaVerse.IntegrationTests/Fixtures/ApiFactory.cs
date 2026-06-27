using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace MyMediaVerse.IntegrationTests.Fixtures
{
    /// <summary>
    /// HTTP integration test factory backed by a real PostgreSQL container with pgvector.
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
        private string? _originalConnectionStringEnv;
        private string? _originalDatabaseUrlEnv;

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            // Override the prod env vars BEFORE Program.cs's top-level code runs (which happens
            // lazily when Services is first touched below). Program.cs:36 reads the connection
            // string inline via builder.Configuration.GetConnectionString("DefaultConnection") —
            // that read happens before WebApplicationFactory's ConfigureAppConfiguration callbacks
            // can fire, so an IConfiguration override is too late. Mutating the env var here is the
            // only thing that intercepts the read. Originals are restored in DisposeAsync.
            _originalConnectionStringEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            _originalDatabaseUrlEnv = Environment.GetEnvironmentVariable("DATABASE_URL");
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
            Environment.SetEnvironmentVariable("DATABASE_URL", null);

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

            // Restore the dev-box env vars so subsequent shell commands see the original values.
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _originalConnectionStringEnv);
            Environment.SetEnvironmentVariable("DATABASE_URL", _originalDatabaseUrlEnv);
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

            // Override IConfiguration BEFORE Program.cs reads it, so DatabaseExtensions.ResolveConnectionString
            // returns the Testcontainers DB instead of whatever ConnectionStrings__DefaultConnection /
            // DATABASE_URL is set to on the dev box. This keeps the Render production connection string
            // out of Startup logs and out of any code path that reads the raw connection string from
            // IConfiguration (defense in depth — the DbContext is also overridden below).
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = ConnectionString
                });
            });

            builder.ConfigureTestServices(services =>
            {
                // Drop any DbContext registrations Program.cs may have added (it short-circuits
                // under "Testing", so this is defensive).
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
                    options.UseNpgsql(dataSource));

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

        /// <summary>
        /// Builds a child factory with <typeparamref name="T"/> swapped for an NSubstitute mock, then
        /// returns a client paired with the mock so tests can both drive HTTP calls and verify
        /// invocations (e.g. <c>mock.Received(1).DoX(...)</c>).
        ///
        /// Each call produces a fresh substitute and a fresh child host — the underlying Postgres
        /// container is still shared via the collection fixture, so DB state is preserved across
        /// the swap (reset between tests via <see cref="ResetDatabaseAsync"/>).
        /// </summary>
        public (HttpClient Client, T Mock) CreateClientWithSubstitute<T>(Action<T>? configure = null)
            where T : class
        {
            var mock = Substitute.For<T>();
            configure?.Invoke(mock);

            var factory = WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<T>();
                    services.AddSingleton(mock);
                });
            });

            return (factory.CreateClient(), mock);
        }

        /// <summary>
        /// Two-substitute variant for controllers whose handlers resolve two collaborators
        /// (e.g. <c>IGoogleBooksService</c> + <c>IBookMappingService</c>). Same swap mechanics as the
        /// single-substitute overload; child host is built once with both substitutions applied.
        ///
        /// Stacking two single-substitute calls won't work — the second call would build off the base
        /// factory, not the first call's child factory, so the first swap would be lost.
        /// </summary>
        public (HttpClient Client, T1 Mock1, T2 Mock2) CreateClientWithSubstitutes<T1, T2>(
            Action<T1>? configure1 = null,
            Action<T2>? configure2 = null)
            where T1 : class
            where T2 : class
        {
            var mock1 = Substitute.For<T1>();
            var mock2 = Substitute.For<T2>();
            configure1?.Invoke(mock1);
            configure2?.Invoke(mock2);

            var factory = WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<T1>();
                    services.RemoveAll<T2>();
                    services.AddSingleton(mock1);
                    services.AddSingleton(mock2);
                });
            });

            return (factory.CreateClient(), mock1, mock2);
        }
    }
}
