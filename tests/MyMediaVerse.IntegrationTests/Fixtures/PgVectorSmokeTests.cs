using Npgsql;
using Testcontainers.PostgreSql;

namespace MyMediaVerse.IntegrationTests.Fixtures
{
    /// <summary>
    /// Phase 1.2.10 smoke test. Boots a pgvector/pgvector:pg16 container, runs SELECT 1, disposes.
    /// Proves Docker is reachable, the image is available, and Testcontainers + Npgsql wiring works
    /// before any real integration test depends on the same path.
    ///
    /// Per the plan, this [Fact] is deletable at the end of Phase 2.9 once real integration tests
    /// are exercising the same path.
    /// </summary>
    public class PgVectorSmokeTests
    {
        [Fact]
        public async Task PgVectorContainer_BootsAndAcceptsConnection()
        {
            await using var container = new PostgreSqlBuilder()
                .WithImage("pgvector/pgvector:pg16")
                .Build();

            await container.StartAsync();

            await using var connection = new NpgsqlConnection(container.GetConnectionString());
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync();

            Assert.Equal(1, Convert.ToInt32(result));
        }
    }
}
