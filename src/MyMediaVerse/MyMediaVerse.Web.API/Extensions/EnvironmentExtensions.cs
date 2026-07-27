namespace MyMediaVerse.Web.API.Extensions;

public static class EnvironmentExtensions
{
    /// <summary>
    /// True when the host is running under the "Testing" environment used by
    /// <c>WebApplicationFactory</c>-based integration tests. Used to skip real
    /// DB / background-service registrations in favor of in-memory equivalents.
    /// </summary>
    public static bool IsTesting(this IWebHostEnvironment environment)
        => environment.IsEnvironment("Testing");

    /// <summary>
    /// True when the host is running as the public demo deployment.
    /// </summary>
    public static bool IsDemo(this IWebHostEnvironment environment)
        => environment.IsEnvironment("Demo");
}
