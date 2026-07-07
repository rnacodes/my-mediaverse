using System.Net;
using System.Text;

namespace MyMediaVerse.UnitTests.TestHelpers
{
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? OnSend { get; set; }
        public List<HttpRequestMessage> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (OnSend != null) return await OnSend(request, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public void RespondWith(HttpResponseMessage response)
            => OnSend = (_, _) => Task.FromResult(response);

        public void RespondWith(HttpStatusCode statusCode, string content, string mediaType = "application/json")
            => OnSend = (_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, mediaType)
            });

        public void RespondWith(HttpStatusCode statusCode, byte[] content, string mediaType = "application/octet-stream")
            => OnSend = (_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType) } }
            });

        /// <summary>
        /// Respond with a different message per successive call, in order. Each factory builds
        /// a fresh response so it can be safely read/disposed on retries. Once the sequence is
        /// exhausted, further calls return 200 OK. Useful for exercising retry/backoff paths.
        /// </summary>
        public void RespondInSequence(params Func<HttpResponseMessage>[] factories)
        {
            var queue = new Queue<Func<HttpResponseMessage>>(factories);
            OnSend = (_, _) =>
            {
                var factory = queue.Count > 0 ? queue.Dequeue() : () => new HttpResponseMessage(HttpStatusCode.OK);
                return Task.FromResult(factory());
            };
        }

        /// <summary>
        /// Convenience factory for a JSON response with an optional Retry-After header (seconds).
        /// </summary>
        public static Func<HttpResponseMessage> Json(HttpStatusCode statusCode, string content = "{}", int? retryAfterSeconds = null)
            => () =>
            {
                var response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                };
                if (retryAfterSeconds.HasValue)
                    response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds.Value));
                return response;
            };
    }
}
