using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
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

            var optionsBuilder = new DbContextOptionsBuilder<MediaLibraryDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            // Create and return a new instance of the DbContext
            return new MediaLibraryDbContext(optionsBuilder.Options);
        }
    }
}
