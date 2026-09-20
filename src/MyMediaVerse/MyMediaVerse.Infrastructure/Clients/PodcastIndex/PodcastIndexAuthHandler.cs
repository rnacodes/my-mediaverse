using System.Security.Cryptography;
using System.Text;

namespace MyMediaVerse.Infrastructure.Clients.PodcastIndex
{
    /// <summary>The Podcast Index API key and secret, read once at startup.</summary>
    public sealed record PodcastIndexCredentials(string? Key, string? Secret)
    {
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Key) && !string.IsNullOrWhiteSpace(Secret);
    }

    /// <summary>
    /// Signs each Podcast Index request: <c>X-Auth-Key</c>, <c>X-Auth-Date</c> (unix seconds; the API accepts
    /// a three-minute window) and <c>Authorization</c>, the lowercase hex SHA-1 of key + secret + date.
    /// </summary>
    public sealed class PodcastIndexAuthHandler : DelegatingHandler
    {
        private readonly PodcastIndexCredentials _credentials;

        public PodcastIndexAuthHandler(PodcastIndexCredentials credentials)
        {
            _credentials = credentials;
        }

        public static string BuildAuthorization(string key, string secret, long unixSeconds) =>
            Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(key + secret + unixSeconds)));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_credentials.IsConfigured)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                request.Headers.Remove("X-Auth-Key");
                request.Headers.Remove("X-Auth-Date");
                request.Headers.Remove("Authorization");
                request.Headers.TryAddWithoutValidation("X-Auth-Key", _credentials.Key);
                request.Headers.TryAddWithoutValidation("X-Auth-Date", now.ToString(System.Globalization.CultureInfo.InvariantCulture));
                request.Headers.TryAddWithoutValidation("Authorization", BuildAuthorization(_credentials.Key!, _credentials.Secret!, now));
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
