using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.IntegrationTests.Fixtures;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Health endpoint behavior, focused on what the detailed check discloses.
    ///
    /// The detailed response intentionally reports configuration presence and database
    /// status to authenticated operators, but raw database exception text (message and
    /// exception type) is returned only in Development — deployed hosts log the full
    /// exception and answer with a generic message. Asserted here under "Testing",
    /// which exercises the same non-Development branch the deployed hosts take.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class HealthEndpointTests
    {
        private readonly ApiFactory _apiFactory;

        public HealthEndpointTests(ApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        [Fact]
        public async Task Health_IsAnonymouslyReachable()
        {
            var client = _apiFactory.CreateAnonymousClient();

            var response = await client.GetAsync("/api/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DetailedHealth_WhenDatabaseHealthy_ReportsHealthy()
        {
            var client = _apiFactory.CreateClient();

            var response = await client.GetAsync("/api/health/detailed");
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("healthy", body.RootElement.GetProperty("database").GetProperty("status").GetString());
        }

        [Fact]
        public async Task DetailedHealth_WhenDatabaseUnreachable_DoesNotLeakExceptionDetails()
        {
            // A child host whose DbContext points at a closed local port, so the health
            // check's database probe fails immediately without reaching any real service.
            var factory = _apiFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<MediaLibraryDbContext>>();
                    services.RemoveAll<MediaLibraryDbContext>();
                    services.AddDbContext<MediaLibraryDbContext>(options =>
                        options.UseNpgsql("Host=127.0.0.1;Port=1;Database=unreachable;Username=x;Password=x;Timeout=1"));
                });
            });
            var client = factory.CreateClient();

            var response = await client.GetAsync("/api/health/detailed");
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var database = body.RootElement.GetProperty("database");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("unhealthy", database.GetProperty("status").GetString());
            Assert.Equal(
                "Database connection failed. See server logs for details.",
                database.GetProperty("error").GetString());
            // The exception type is Development-only; null-valued properties are omitted
            // from responses entirely, so the key itself must be absent.
            Assert.False(database.TryGetProperty("errorType", out _));
        }
    }
}
