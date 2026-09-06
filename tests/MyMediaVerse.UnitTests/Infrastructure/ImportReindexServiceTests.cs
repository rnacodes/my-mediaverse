using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Infrastructure.Services.Search;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Unit tests for the best-effort "reindex after import" helper. They verify the count gate
    /// (nothing imported → no reindex) and that a Typesense failure is swallowed so it never bubbles
    /// up and fails the import. The Typesense service is substituted — no real Typesense/OpenAI call.
    /// </summary>
    [Trait("Category", "Unit")]
    public class ImportReindexServiceTests
    {
        private static ImportReindexService BuildService(ITypesenseService typesense)
            => new(typesense, Substitute.For<ILogger<ImportReindexService>>());

        [Fact]
        public async Task ZeroImported_DoesNotReindex()
        {
            var typesense = Substitute.For<ITypesenseService>();
            var service = BuildService(typesense);

            await service.ReindexAfterImportAsync(0, "Goodreads CSV");

            await typesense.DidNotReceive().BulkReindexAllMediaItemsAsync();
        }

        [Fact]
        public async Task NegativeImported_DoesNotReindex()
        {
            var typesense = Substitute.For<ITypesenseService>();
            var service = BuildService(typesense);

            await service.ReindexAfterImportAsync(-1, "Goodreads CSV");

            await typesense.DidNotReceive().BulkReindexAllMediaItemsAsync();
        }

        [Fact]
        public async Task PositiveImported_ReindexesMediaOnce()
        {
            var typesense = Substitute.For<ITypesenseService>();
            typesense.BulkReindexAllMediaItemsAsync().Returns(Task.FromResult(5));
            var service = BuildService(typesense);

            await service.ReindexAfterImportAsync(3, "Goodreads CSV");

            await typesense.Received(1).BulkReindexAllMediaItemsAsync();
        }

        [Fact]
        public async Task ReindexThrows_IsSwallowed_DoesNotBubbleUp()
        {
            var typesense = Substitute.For<ITypesenseService>();
            typesense.BulkReindexAllMediaItemsAsync()
                .Returns<Task<int>>(_ => throw new InvalidOperationException("Typesense down"));
            var service = BuildService(typesense);

            // Must not throw — the import already committed its rows; a search hiccup can't fail it.
            var act = async () => await service.ReindexAfterImportAsync(2, "Goodreads CSV");

            await act.Should().NotThrowAsync();
            await typesense.Received(1).BulkReindexAllMediaItemsAsync();
        }

        [Fact]
        public async Task ReindexItem_ReindexesJustThatItem()
        {
            var typesense = Substitute.For<ITypesenseService>();
            var id = Guid.NewGuid();
            typesense.ReindexMediaItemByIdAsync(id).Returns(Task.FromResult(true));
            var service = BuildService(typesense);

            await service.ReindexItemAfterImportAsync(id, "Open Library import");

            await typesense.Received(1).ReindexMediaItemByIdAsync(id);
            await typesense.DidNotReceive().BulkReindexAllMediaItemsAsync();
        }

        [Fact]
        public async Task ReindexItem_NotIndexed_DoesNotThrow()
        {
            var typesense = Substitute.For<ITypesenseService>();
            var id = Guid.NewGuid();
            typesense.ReindexMediaItemByIdAsync(id).Returns(Task.FromResult(false));
            var service = BuildService(typesense);

            var act = async () => await service.ReindexItemAfterImportAsync(id, "Open Library import");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ReindexItem_Throws_IsSwallowed_DoesNotBubbleUp()
        {
            var typesense = Substitute.For<ITypesenseService>();
            var id = Guid.NewGuid();
            typesense.ReindexMediaItemByIdAsync(id)
                .Returns<Task<bool>>(_ => throw new InvalidOperationException("Typesense down"));
            var service = BuildService(typesense);

            var act = async () => await service.ReindexItemAfterImportAsync(id, "book enrichment");

            await act.Should().NotThrowAsync();
            await typesense.Received(1).ReindexMediaItemByIdAsync(id);
        }
    }
}
