using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyMediaVerse.Infrastructure.Clients.TMDB;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using Polly;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Exercises the resilience handler wired onto the TMDB HttpClient (retry/backoff for
    /// transient 429 + 5xx, Retry-After honored, no retry on other 4xx). Uses the SAME options
    /// factory as production (<see cref="TmdbResilience.CreateRetryOptions"/>) with the backoff
    /// compressed to ~1ms so the retry path runs without real waits and no real API calls.
    /// </summary>
    // Both TMDB client test classes set the TMDB_API_KEY process variable, so they share a
    // collection to keep xUnit from running them side by side.
    [Collection("TmdbApiKeyEnvironment")]
    [Trait("Category", "Unit")]
    public class TmdbResilienceTests
    {
        private const string MovieJson = "{\"id\":27205,\"title\":\"Inception\"}";

        private static TmdbApiClient BuildClientWithRetry(TestHttpMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var builder = services.AddHttpClient("tmdb-test", client =>
                client.BaseAddress = new Uri("https://api.themoviedb.org/3/"));
            builder.ConfigurePrimaryHttpMessageHandler(() => handler);
            builder.AddResilienceHandler("tmdb-retry", pipeline =>
                pipeline.AddRetry(TmdbResilience.CreateRetryOptions(
                    baseDelay: TimeSpan.FromMilliseconds(1),
                    maxRetryAttempts: 3)));

            var httpClient = services.BuildServiceProvider()
                .GetRequiredService<IHttpClientFactory>().CreateClient("tmdb-test");

            Environment.SetEnvironmentVariable("TMDB_API_KEY", "test-api-key");
            return new TmdbApiClient(httpClient, NullLogger<TmdbApiClient>.Instance, Substitute.For<IConfiguration>());
        }

        [Fact]
        public async Task GetMovieDetailsAsync_ShouldRetryAndSucceed_After429()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests, retryAfterSeconds: 0),
                TestHttpMessageHandler.Json(HttpStatusCode.OK, MovieJson));
            var client = BuildClientWithRetry(handler);

            var result = await client.GetMovieDetailsAsync(27205);

            result.Title.Should().Be("Inception");
            handler.Requests.Should().HaveCount(2); // 429 retried, then success
        }

        [Fact]
        public async Task GetMovieDetailsAsync_ShouldRetryAndSucceed_After503()
        {
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable),
                TestHttpMessageHandler.Json(HttpStatusCode.OK, MovieJson));
            var client = BuildClientWithRetry(handler);

            var result = await client.GetMovieDetailsAsync(27205);

            result.Id.Should().Be(27205);
            handler.Requests.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMovieDetailsAsync_ShouldSurfaceFailure_WhenRetriesAreExhausted()
        {
            var handler = new TestHttpMessageHandler();
            handler.OnSend = (_, _) => Task.FromResult(TestHttpMessageHandler.Json(HttpStatusCode.TooManyRequests)());
            var client = BuildClientWithRetry(handler);

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMovieDetailsAsync(27205));

            exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            handler.Requests.Should().HaveCount(4); // first attempt + 3 retries
        }

        [Fact]
        public async Task GetMovieDetailsAsync_ShouldNotRetry_On404()
        {
            // A removed TMDB id stays removed; retrying only spends requests.
            var handler = new TestHttpMessageHandler();
            handler.RespondInSequence(
                TestHttpMessageHandler.Json(HttpStatusCode.NotFound),
                TestHttpMessageHandler.Json(HttpStatusCode.OK, MovieJson));
            var client = BuildClientWithRetry(handler);

            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMovieDetailsAsync(27205));

            handler.Requests.Should().HaveCount(1);
        }
    }
}
