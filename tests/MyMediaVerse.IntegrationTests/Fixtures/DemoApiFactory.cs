using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyMediaVerse.IntegrationTests.Fixtures
{
    /// <summary>
    /// <see cref="ApiFactory"/> variant that runs the host in the <c>Demo</c> environment,
    /// for tests asserting demo-specific behavior: the write gate middleware, the
    /// anonymous-GET authorization policy, and the TOTP unlock flow.
    /// </summary>
    public class DemoApiFactory : ApiFactory
    {
        /// <summary>Base32 TOTP secret the test host is configured with; tests use it to compute valid codes.</summary>
        public const string TotpSecret = "JBSWY3DPEHPK3PXP";

        // Demo is a deployed environment name: authentication refuses to start without a JWT
        // secret from the environment. The base factory's static constructor guarantees
        // JWT_SECRET is set before any host builds.

        public DemoApiFactory()
        {
            // The unlock endpoint prefers the DEMO_TOTP_SECRET env var over configuration, so a
            // dev box carrying a real secret would make the server reject codes computed from
            // <see cref="TotpSecret"/>. Pin it process-locally; the endpoint reads it per
            // request, so setting it at construction is order-safe.
            Environment.SetEnvironmentVariable("DEMO_TOTP_SECRET", TotpSecret);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            // Later call wins over the base factory's "Testing".
            builder.UseEnvironment("Demo");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DemoTotpSecret"] = TotpSecret
                });
            });

            builder.ConfigureTestServices(services =>
            {
                // The enrichment workers are only skipped under "Testing", so under "Demo"
                // they would register; strip them to keep the test host hermetic.
                var workers = services
                    .Where(d => d.ServiceType == typeof(IHostedService) &&
                                d.ImplementationType?.Namespace?.StartsWith(
                                    "MyMediaVerse.Infrastructure", StringComparison.Ordinal) == true)
                    .ToList();
                foreach (var worker in workers)
                {
                    services.Remove(worker);
                }
            });
        }
    }
}
