using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Infrastructure.Data;

namespace MyMediaVerse.UnitTests.TestHelpers
{
    /// <summary>
    /// Base class for tests that use an in-memory database.
    /// Provides a fresh database instance for each test and handles cleanup.
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
