namespace MyMediaVerse.Web.API.Conventions;

/// <summary>
/// Restricts a controller or action to the listed host environments.
/// <see cref="EnvironmentGatingConvention"/> removes non-matching endpoints from the
/// route table at startup, so on every other host they return 404 and never appear
/// in Swagger — there is no trace of them rather than a 401/403 to probe.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class EnvironmentsAttribute : Attribute
{
    public EnvironmentsAttribute(params string[] environments)
    {
        Environments = environments;
    }

    public IReadOnlyCollection<string> Environments { get; }

    public bool Matches(string environmentName) =>
        Environments.Contains(environmentName, StringComparer.OrdinalIgnoreCase);
}
