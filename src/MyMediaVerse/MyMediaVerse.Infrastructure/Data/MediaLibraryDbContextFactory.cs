using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pgvector.EntityFrameworkCore;
using System.IO;

namespace MyMediaVerse.Infrastructure.Data
{
    public class MediaLibraryDbContextFactory : IDesignTimeDbContextFactory<MediaLibraryDbContext>
    {
        public MediaLibraryDbContext CreateDbContext(string[] args)
        {
            // Build configuration from the appsettings.json file in the Web API project
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Create DbContext options with pgvector support
            // The Pgvector.EntityFrameworkCore package handles Vector type mapping via UseVector()
            var optionsBuilder = new DbContextOptionsBuilder<MediaLibraryDbContext>();
            optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());

            // Create and return a new instance of the DbContext
            return new MediaLibraryDbContext(optionsBuilder.Options);
        }
    }
}
