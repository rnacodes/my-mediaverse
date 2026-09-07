using System.Net;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Web
{
    /// <summary>
    /// Checks whether a link still answers. Sends HEAD first (cheap), falls back to GET when the
    /// server refuses HEAD, and follows redirects itself so the reported status is the page the
    /// browser would land on rather than the hop in between. The typed client is registered with
    /// automatic redirects turned off for that reason.
    /// </summary>
    public class HttpLinkChecker : ILinkChecker
    {
        private const int MaxRedirects = 5;

        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpLinkChecker> _logger;

        public HttpLinkChecker(HttpClient httpClient, ILogger<HttpLinkChecker> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<int> CheckAsync(string url, CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
                return 0;

            try
            {
                var method = HttpMethod.Head;
                for (var hop = 0; hop <= MaxRedirects; hop++)
                {
                    using var response = await SendAsync(method, uri, cancellationToken);

                    if (RefusesHead(response.StatusCode) && method == HttpMethod.Head)
                    {
                        method = HttpMethod.Get;
                        hop--; // The GET retry of the same URL is not a redirect hop.
                        continue;
                    }

                    if (IsRedirect(response.StatusCode) && response.Headers.Location != null && hop < MaxRedirects)
                    {
                        uri = response.Headers.Location.IsAbsoluteUri
                            ? response.Headers.Location
                            : new Uri(uri, response.Headers.Location);

                        // A 303 always means "GET the new location"; other redirects keep the method.
                        if (response.StatusCode == HttpStatusCode.SeeOther)
                            method = HttpMethod.Get;

                        continue;
                    }

                    return (int)response.StatusCode;
                }

                return 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogDebug(ex, "Link check could not reach {Url}", url);
                return 0;
            }
        }

        private Task<HttpResponseMessage> SendAsync(HttpMethod method, Uri uri, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(method, uri);
            // Headers are enough to judge the status; never download the body.
            return _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        private static bool RefusesHead(HttpStatusCode status) =>
            status is HttpStatusCode.MethodNotAllowed or HttpStatusCode.Forbidden or HttpStatusCode.NotImplemented;

        private static bool IsRedirect(HttpStatusCode status) =>
            status is HttpStatusCode.MovedPermanently
                or HttpStatusCode.Found
                or HttpStatusCode.SeeOther
                or HttpStatusCode.TemporaryRedirect
                or HttpStatusCode.PermanentRedirect;
    }
}
