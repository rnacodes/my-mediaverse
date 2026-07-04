using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Infrastructure.Services.Search;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Unit tests for the scheduled search index sync worker. They verify the config gate and that
    /// each collection is reindexed independently (one failing collection must not stop the others).
    /// The actual Typesense client is substituted — no real Typesense/OpenAI is touched.
    /// </summary>
    [Trait("Category", "Unit")]
    public class SearchIndexSyncHostedServiceTests
    {
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Builds the worker wired to <paramref name="typesense"/> via a substituted DI scope, so the
        /// hosted service's <c>CreateScope().GetRequiredService&lt;ITypesenseService&gt;()</c> resolves it.
        /// </summary>
        private static SearchIndexSyncHostedService BuildService(
            ITypesenseService typesense, SearchIndexSyncOptions options)
        {
            var scopedProvider = Substitute.For<IServiceProvider>();
            scopedProvider.GetService(typeof(ITypesenseService)).Returns(typesense);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(scopedProvider);

            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);

            var rootProvider = Substitute.For<IServiceProvider>();
            rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

            return new SearchIndexSyncHostedService(
                rootProvider,
                Substitute.For<ILogger<SearchIndexSyncHostedService>>(),
                Options.Create(options));
        }

        [Fact]
        public async Task Disabled_DoesNotReindexAnything()
        {
            var typesense = Substitute.For<ITypesenseService>();
            var service = BuildService(typesense, new SearchIndexSyncOptions { Enabled = false });

            await service.StartAsync(CancellationToken.None);
            await Task.Delay(50);
            await service.StopAsync(CancellationToken.None);

            await typesense.DidNotReceive().BulkReindexAllMediaItemsAsync();
            await typesense.DidNotReceive().BulkReindexAllMixlistsAsync();
            await typesense.DidNotReceive().BulkReindexAllNotesAsync();
            await typesense.DidNotReceive().BulkReindexAllHighlightsAsync();
        }

        [Fact]
        public async Task Enabled_ReindexesAllFourCollections()
        {
            var typesense = Substitute.For<ITypesenseService>();
            var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            typesense.BulkReindexAllMediaItemsAsync().Returns(Task.FromResult(3));
            typesense.BulkReindexAllMixlistsAsync().Returns(Task.FromResult(2));
            typesense.BulkReindexAllNotesAsync().Returns(Task.FromResult(5));
            // Highlights run last — use it to signal that a full pass completed.
            typesense.BulkReindexAllHighlightsAsync().Returns(_ => { done.TrySetResult(true); return Task.FromResult(7); });

            // No initial delay; the long interval is never reached because we stop after the first pass.
            var service = BuildService(typesense,
                new SearchIndexSyncOptions { Enabled = true, InitialDelayMinutes = 0, IntervalHours = 24 });

            await service.StartAsync(CancellationToken.None);
            (await Task.WhenAny(done.Task, Task.Delay(WaitTimeout))).Should().Be(done.Task, "the worker should complete a full pass");
            await service.StopAsync(CancellationToken.None);

            await typesense.Received(1).BulkReindexAllMediaItemsAsync();
            await typesense.Received(1).BulkReindexAllMixlistsAsync();
            await typesense.Received(1).BulkReindexAllNotesAsync();
            await typesense.Received(1).BulkReindexAllHighlightsAsync();
        }

        [Fact]
        public async Task Enabled_OneCollectionFailing_StillReindexesTheRest()
        {
            var typesense = Substitute.For<ITypesenseService>();
            var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            typesense.BulkReindexAllMediaItemsAsync().Returns(Task.FromResult(1));
            // Mixlists blows up mid-pass; the worker must catch it and carry on.
            typesense.BulkReindexAllMixlistsAsync().Returns<Task<int>>(_ => throw new InvalidOperationException("Typesense down"));
            typesense.BulkReindexAllNotesAsync().Returns(Task.FromResult(1));
            typesense.BulkReindexAllHighlightsAsync().Returns(_ => { done.TrySetResult(true); return Task.FromResult(1); });

            var service = BuildService(typesense,
                new SearchIndexSyncOptions { Enabled = true, InitialDelayMinutes = 0, IntervalHours = 24 });

            await service.StartAsync(CancellationToken.None);
            (await Task.WhenAny(done.Task, Task.Delay(WaitTimeout))).Should().Be(done.Task, "a failure in one collection must not abort the pass");
            await service.StopAsync(CancellationToken.None);

            await typesense.Received(1).BulkReindexAllMediaItemsAsync();
            await typesense.Received(1).BulkReindexAllMixlistsAsync();
            await typesense.Received(1).BulkReindexAllNotesAsync();
            await typesense.Received(1).BulkReindexAllHighlightsAsync();
        }
    }
}
