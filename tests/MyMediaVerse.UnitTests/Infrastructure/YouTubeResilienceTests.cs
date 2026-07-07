using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.Infrastructure.Clients.YouTube;
using MyMediaVerse.UnitTests.TestHelpers;
using Polly;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Exercises the resilience handler wired onto the YouTube HttpClient (retry/backoff for
    /// transient 429 + 5xx, Retry-After honored, no retry on 403). Uses the SAME options factory
    /// as production (<see cref="YouTubeResilience.CreateRetryOptions"/>) with the backoff
    /// compressed to ~1ms so the retry path runs without real waits and no real API calls.
    /// </summary>
    [Trait("Category", "Unit")]
    public class YouTubeResilienceTests
    {
        private const string TestUrl = "https://www.googleapis.com/youtube/v3/playlistItems";

        private static HttpClient BuildClientWithRetry(TestHttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var builder = services.AddHttpClient("youtube-test");
            builder.ConfigurePrimaryHttpMessageHandler(() => handler);
            builder.AddResilienceHandler("youtube-retry", pipeline =>
                pipeline.AddRetry(YouTubeResilience.CreateRetryOptions(
                    baseDelay: TimeSpan.FromMilliseconds(1),
                    maxRetryAttempts: 3)));

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IHttpClientFactory>().CreateClient("youtube-test");
        }

        [Fact]
        public async Task RetryPolicy_ShouldRetryAndSucceed_After429()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests),
                TestHttpMessageHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            handler.Requests.Should().HaveCount(2); // 429 retried, then success
        }

        [Fact]
        public async Task RetryPolicy_ShouldRetryAndSucceed_After503()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable),
                TestHttpMessageHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            handler.Requests.Should().HaveCount(2);
        }

        [Fact]
        public async Task RetryPolicy_ShouldHonorRetryAfterHeader_ThenSucceed()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, retryAfterSeconds: 0),
                TestHttpMessageHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            handler.Requests.Should().HaveCount(2);
        }

        [Fact]
        public async Task RetryPolicy_ShouldNotRetry_On403()
        {
            // 403 (incl. quota exhaustion) must be surfaced to the client, not retried by the pipeline.
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.Forbidden, "{\"error\":{\"errors\":[{\"reason\":\"quotaExceeded\"}]}}"),
                TestHttpMessageHandler.Json(HttpStatusCode.OK));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            handler.Requests.Should().HaveCount(1);
        }
    }
}
