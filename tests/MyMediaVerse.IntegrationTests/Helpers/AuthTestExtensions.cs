using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyMediaVerse.IntegrationTests.Helpers
{
    /// <summary>
    /// JWT helpers lifted out of the legacy <c>WebApplicationFactory</c>. Resolves credentials
    /// the same way <c>AuthController</c> does (env vars first, then defaults), POSTs to
    /// <c>/api/auth/login</c>, and returns a bearer token.
    /// </summary>
    public static class AuthTestExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static (string Username, string Password) ResolveCredentials()
        {
            var username = Environment.GetEnvironmentVariable("AUTH_USERNAME") ?? "admin";
            var password = Environment.GetEnvironmentVariable("AUTH_PASSWORD") ?? "password123";
            return (username, password);
        }

        public static async Task<string> GetAccessTokenAsync(this HttpClient client)
        {
            var (username, password) = ResolveCredentials();
            var payload = JsonSerializer.Serialize(new { username, password }, JsonOptions);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/auth/login", content);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);
            return json.GetProperty("token").GetString()!;
        }

        public static async Task AuthenticateAsync(this HttpClient client)
        {
            var token = await client.GetAccessTokenAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
