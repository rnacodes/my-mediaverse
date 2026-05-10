using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Infrastructure.Data;

namespace MyMediaVerse.UnitTests.TestHelpers
{
    /// <summary>
    /// Base class for tests that use an in-memory database.
    /// Provides a fresh database instance for each test and handles cleanup.
    ///
    /// <para>
    /// <b>Provider limits — DO NOT use this base for tests that depend on real Postgres semantics:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>EF.Functions.ILike</c> — InMemory has no case-insensitive LIKE translator and throws at runtime.</description></item>
    ///   <item><description>Full-text search (<c>tsvector</c>, <c>to_tsquery</c>, <c>plainto_tsquery</c>) — Postgres-only.</description></item>
    ///   <item><description>JSONB operators (<c>-&gt;&gt;</c>, <c>@&gt;</c>, etc.) and dynamic JSON serialization — InMemory stores POCOs, no JSONB query path.</description></item>
    ///   <item><description>pgvector (<c>vector</c> column type, <c>&lt;-&gt;</c> / <c>&lt;=&gt;</c> operators, <c>VectorSearchRepository</c>) — extension only exists on Postgres.</description></item>
    ///   <item><description>Database-generated SQL (sequences, raw SQL via <c>FromSqlRaw</c> using Postgres syntax, computed columns).</description></item>
    /// </list>
    /// <para>
    /// For any of the above, use <c>ApiFactory</c> (HTTP) or <c>DatabaseFixture</c> (non-HTTP)
    /// in <c>MyMediaVerse.IntegrationTests</c> — they boot a pgvector/pgvector:pg16 container.
    /// </para>
    /// </summary>
    public class InMemoryDbTestBase : IDisposable
    {
        protected readonly MediaLibraryDbContext Context;
        private readonly string _databaseName;

        protected InMemoryDbTestBase()
        {
            // Use a unique database name for each test instance to ensure test isolation
            _databaseName = Guid.NewGuid().ToString();
            
            var options = new DbContextOptionsBuilder<MediaLibraryDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .EnableSensitiveDataLogging()
                .Options;

            Context = new MediaLibraryDbContext(options);
            
            // Ensure the database is created
            Context.Database.EnsureCreated();
        }

        /// <summary>
        /// Cleanup: Delete the database and dispose the context
        /// </summary>
        public void Dispose()
        {
            try
            {
                Context.Database.EnsureDeleted();
                Context.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Context already disposed, ignore
            }
        }
    }
}
