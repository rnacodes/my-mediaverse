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
    }
}
