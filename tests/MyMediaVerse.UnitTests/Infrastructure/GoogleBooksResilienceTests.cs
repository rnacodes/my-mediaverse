using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.Infrastructure.Clients.Google;
using MyMediaVerse.UnitTests.TestHelpers;
using Polly;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Exercises the resilience handler wired onto the Google Books HttpClient (retry/backoff for
    /// transient 429 + 5xx, no retry once the daily quota is spent, no retry on 403). Uses the
    /// SAME options factory as production (<see cref="GoogleBooksResilience.CreateRetryOptions"/>)
    /// with the backoff compressed to ~1ms so the retry path runs without real waits and no real
    /// API calls.
    /// </summary>
    [Trait("Category", "Unit")]
    public class GoogleBooksResilienceTests
    {
        private const string TestUrl = "https://www.googleapis.com/books/v1/volumes?q=dune";

        private const string DailyQuotaBody = """
            {
              "error": {
                "code": 429,
                "message": "Quota exceeded for quota metric 'Queries' and limit 'Queries per day' of service 'books.googleapis.com' for consumer 'project_number:000000000000'.",
                "status": "RESOURCE_EXHAUSTED",
                "details": [
                  {
                    "@type": "type.googleapis.com/google.rpc.ErrorInfo",
                    "reason": "RATE_LIMIT_EXCEEDED",
                    "domain": "googleapis.com",
                    "metadata": {
                      "quota_unit": "1/d/{project}",
                      "quota_limit": "defaultPerDayPerProject",
                      "quota_metric": "books.googleapis.com/default",
                      "service": "books.googleapis.com"
                    }
                  }
                ]
              }
            }
            """;

        private const string PerMinuteQuotaBody = """
            {
              "error": {
                "code": 429,
                "message": "Quota exceeded for quota metric 'Queries' and limit 'Queries per minute per user' of service 'books.googleapis.com' for consumer 'project_number:000000000000'.",
                "status": "RESOURCE_EXHAUSTED",
                "details": [
                  {
                    "@type": "type.googleapis.com/google.rpc.ErrorInfo",
                    "reason": "RATE_LIMIT_EXCEEDED",
                    "domain": "googleapis.com",
                    "metadata": {
                      "quota_unit": "1/min/{project}/{user}",
                      "quota_limit": "defaultPerMinutePerUser",
                      "quota_metric": "books.googleapis.com/default",
                      "service": "books.googleapis.com"
                    }
                  }
                ]
              }
            }
            """;

        private static HttpClient BuildClientWithRetry(TestHttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var builder = services.AddHttpClient("googlebooks-test");
            builder.ConfigurePrimaryHttpMessageHandler(() => handler);
            builder.AddResilienceHandler("googlebooks-retry", pipeline =>
                pipeline.AddRetry(GoogleBooksResilience.CreateRetryOptions(
                    baseDelay: TimeSpan.FromMilliseconds(1),
                    maxRetryAttempts: 3)));

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IHttpClientFactory>().CreateClient("googlebooks-test");
        }

        [Fact]
        public async Task RetryPolicy_ShouldRetryAndSucceed_AfterPerMinute429()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, PerMinuteQuotaBody),
                TestHttpMessageHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            handler.Requests.Should().HaveCount(2); // 429 retried, then success
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("")]
        [InlineData("Too Many Requests")]
        public async Task RetryPolicy_ShouldRetry_When429BodyDoesNotNameADailyLimit(string body)
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, body),
                TestHttpMessageHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            handler.Requests.Should().HaveCount(2);
        }

        [Fact]
        public async Task RetryPolicy_ShouldNotRetry_WhenDailyQuotaIsExhausted()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, DailyQuotaBody),
                TestHttpMessageHandler.Json(HttpStatusCode.OK));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            handler.Requests.Should().HaveCount(1);
        }

        [Fact]
        public async Task RetryPolicy_ShouldNotRetry_WhenOnlyTheMessageNamesADailyLimit()
        {
            const string body = "{\"error\":{\"code\":429,\"message\":\"Quota exceeded for limit 'Queries per day'.\"}}";
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, body),
                TestHttpMessageHandler.Json(HttpStatusCode.OK));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            handler.Requests.Should().HaveCount(1);
        }

        [Fact]
        public async Task RetryPolicy_ShouldLeaveTheBodyReadable_AfterADailyQuota429()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, DailyQuotaBody));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);
            var body = await response.Content.ReadAsStringAsync();

            body.Should().Contain("Queries per day");
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
        public async Task RetryPolicy_ShouldGiveUp_AfterMaxAttemptsOfPerMinute429()
        {
            var handler = new TestHttpMessageHandler();
            handler.OnSend = (_, _) => Task.FromResult(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, PerMinuteQuotaBody)());
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            handler.Requests.Should().HaveCount(4); // first attempt + 3 retries
        }

        [Fact]
        public async Task RetryPolicy_ShouldNotRetry_On403()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.Forbidden),
                TestHttpMessageHandler.Json(HttpStatusCode.OK));
            var client = BuildClientWithRetry(handler);

            var response = await client.GetAsync(TestUrl);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            handler.Requests.Should().HaveCount(1);
        }
    }
}
