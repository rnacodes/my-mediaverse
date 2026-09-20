using System.Net;

namespace MyMediaVerse.Infrastructure.Clients.Itunes
{
    /// <summary>
    /// Takes a slot from <see cref="ItunesRateLimiter"/> before each Apple call. When no slot frees up in
    /// time the call is refused with an <see cref="HttpRequestException"/> carrying 429, which callers
    /// report as "busy" rather than as Apple being down.
    /// </summary>
    public sealed class ItunesRateLimitHandler : DelegatingHandler
    {
        public const string BusyMessage = "Apple Podcasts is busy; try again shortly.";

        private readonly ItunesRateLimiter _rateLimiter;

        public ItunesRateLimitHandler(ItunesRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var lease = await _rateLimiter.Limiter.AcquireAsync(1, cancellationToken);
            if (!lease.IsAcquired)
                throw new HttpRequestException(BusyMessage, null, HttpStatusCode.TooManyRequests);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
