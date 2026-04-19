namespace MyMediaVerse.Web.API.Extensions;

/// <summary>
/// Helpers for the env-var-OR-config pattern used throughout registration. We can't rely on
/// ASP.NET's default <c>__</c> env var mapping because our env var names (e.g. <c>LISTENNOTES_API_KEY</c>)
/// don't align with configuration keys (<c>ApiKeys:ListenNotes</c>).
/// </summary>
public static class ConfigResolverExtensions
{
    /// <summary>
    /// First non-empty value from the given env var names, then from the configuration key; else null.
    /// </summary>
    public static string? GetEnvOrConfig(this IConfiguration configuration, string configKey, params string[] envVarNames)
    {
        foreach (var envVarName in envVarNames)
        {
            var value = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        var fromConfig = configuration[configKey];
        return string.IsNullOrEmpty(fromConfig) ? null : fromConfig;
    }

    /// <summary>
    /// Same as <see cref="GetEnvOrConfig"/> but returns <paramref name="defaultValue"/> when all sources are empty.
    /// </summary>
    public static string GetEnvOrConfigOrDefault(
        this IConfiguration configuration,
        string configKey,
        string defaultValue,
        params string[] envVarNames)
        => configuration.GetEnvOrConfig(configKey, envVarNames) ?? defaultValue;
}
