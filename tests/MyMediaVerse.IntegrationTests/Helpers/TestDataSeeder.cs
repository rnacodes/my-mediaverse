using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Infrastructure.Data;

namespace MyMediaVerse.IntegrationTests.Helpers
{
    /// <summary>
    /// Seeds entities directly via DbContext within a scope. Avoids HTTP-based seeding,
    /// which would create circular dependencies between tests and the endpoints under test.
    /// </summary>
    public static class TestDataSeeder
    {
        public static async Task SeedAsync(ApiFactory factory, Func<MediaLibraryDbContext, Task> seed)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
            await seed(context);
            await context.SaveChangesAsync();
        }

        public static async Task SeedAsync(DatabaseFixture fixture, Func<MediaLibraryDbContext, Task> seed)
        {
            await using var context = fixture.CreateContext();
            await seed(context);
            await context.SaveChangesAsync();
        }

        public static Task AddAsync<TEntity>(ApiFactory factory, params TEntity[] entities)
            where TEntity : class
        {
            return SeedAsync(factory, async context =>
            {
                await context.Set<TEntity>().AddRangeAsync(entities);
            });
        }
    }
}
