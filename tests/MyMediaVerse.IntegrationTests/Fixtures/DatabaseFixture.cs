using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Infrastructure.Data;
using Npgsql;
using Pgvector.EntityFrameworkCore;
using Respawn;
using Testcontainers.PostgreSql;

namespace MyMediaVerse.IntegrationTests.Fixtures
{
    /// <summary>
    /// Standalone Postgres + pgvector fixture for non-HTTP infrastructure tests
    /// (repositories, EF behaviour, vector search, jsonb queries).
    ///
    /// Use via <c>[Collection("Database")]</c> + constructor injection so the same container
    /// is shared across the run. Reset between tests with <see cref="ResetAsync"/>.
    /// </summary>
    public class DatabaseFixture : IAsyncLifetime
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

            await using (var context = CreateContext())
            {
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

        public async Task DisposeAsync()
        {
            if (_respawnerConnection is not null)
            {
                await _respawnerConnection.DisposeAsync();
            }
            await _container.DisposeAsync();
        }

        /// <summary>
        /// Returns a fresh DbContext bound to the container. Caller owns disposal.
        /// </summary>
        public MediaLibraryDbContext CreateContext()
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            var options = new DbContextOptionsBuilder<MediaLibraryDbContext>()
                .UseNpgsql(dataSource, o => o.UseVector())
                .Options;

            return new MediaLibraryDbContext(options);
        }

        public async Task ResetAsync()
        {
            if (_respawner is null || _respawnerConnection is null)
            {
                throw new InvalidOperationException("DatabaseFixture has not been initialized.");
            }
            await _respawner.ResetAsync(_respawnerConnection);
        }
    }
}
