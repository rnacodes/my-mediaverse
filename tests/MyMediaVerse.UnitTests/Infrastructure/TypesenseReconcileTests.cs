using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Services.Search;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Typesense;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Unit tests for the orphan-reconciliation that the bulk reindex runs to propagate Postgres
    /// deletes into Typesense. The bulk reindex upserts in place (it no longer drops the collection),
    /// so rows deleted in Postgres would otherwise linger as ghost search hits until a manual reset.
    /// Covers the ID-diff logic and the fail-safe that skips deletion when the index can't be listed.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseReconcileTests
    {
        private const string Collection = "media_items";

        private static TypesenseService CreateService(ITypesenseClient client)
        {
            // Reconcile only touches the Typesense client; the DbContext is never used here.
            var context = Substitute.For<IApplicationDbContext>();
            var config = new ConfigurationBuilder().Build();
            return new TypesenseService(client, context, NullLogger<TypesenseService>.Instance, config);
        }

        private static List<TypesenseService.IdProjection> Indexed(params string[] ids) =>
            ids.Select(id => new TypesenseService.IdProjection { Id = id }).ToList();

        // ---------- Pure diff ----------

        [Fact]
        public void ComputeOrphanDocumentIds_ReturnsIndexedIdsMissingFromLiveSet()
        {
            var orphans = TypesenseService.ComputeOrphanDocumentIds(
                indexedIds: new[] { "A", "B", "C" },
                liveIds: new[] { "A", "B" });

            orphans.Should().BeEquivalentTo(new[] { "C" });
        }

        [Fact]
        public void ComputeOrphanDocumentIds_ReturnsEmpty_WhenEveryIndexedIdIsStillLive()
        {
            var orphans = TypesenseService.ComputeOrphanDocumentIds(
                indexedIds: new[] { "A", "B" },
                liveIds: new[] { "A", "B", "C" });

            orphans.Should().BeEmpty();
        }

        [Fact]
        public void ComputeOrphanDocumentIds_ReturnsAllIndexed_WhenLiveSetIsEmpty()
        {
            var orphans = TypesenseService.ComputeOrphanDocumentIds(
                indexedIds: new[] { "A", "B" },
                liveIds: Array.Empty<string>());

            orphans.Should().BeEquivalentTo(new[] { "A", "B" });
        }

        [Fact]
        public void ComputeOrphanDocumentIds_DeduplicatesAndIgnoresEmptyIds()
        {
            var orphans = TypesenseService.ComputeOrphanDocumentIds(
                indexedIds: new[] { "C", "C", "", "D" },
                liveIds: new[] { "A" });

            orphans.Should().BeEquivalentTo(new[] { "C", "D" });
        }

        // ---------- Reconcile orchestration ----------

        [Fact]
        public async Task ReconcileDeletedDocumentsAsync_DeletesOnlyOrphans()
        {
            var client = Substitute.For<ITypesenseClient>();
            client.ExportDocuments<TypesenseService.IdProjection>(
                    Collection, Arg.Any<ExportParameters>(), Arg.Any<CancellationToken>())
                .Returns(Indexed("A", "B", "C"));

            var service = CreateService(client);

            var removed = await service.ReconcileDeletedDocumentsAsync(Collection, new[] { "A", "B" });

            removed.Should().Be(1);
            await client.Received(1).DeleteDocument<TypesenseService.IdProjection>(Collection, "C");
            await client.DidNotReceive().DeleteDocument<TypesenseService.IdProjection>(Collection, "A");
            await client.DidNotReceive().DeleteDocument<TypesenseService.IdProjection>(Collection, "B");
        }

        [Fact]
        public async Task ReconcileDeletedDocumentsAsync_DeletesNothing_WhenIndexMatchesLiveSet()
        {
            var client = Substitute.For<ITypesenseClient>();
            client.ExportDocuments<TypesenseService.IdProjection>(
                    Collection, Arg.Any<ExportParameters>(), Arg.Any<CancellationToken>())
                .Returns(Indexed("A", "B"));

            var service = CreateService(client);

            var removed = await service.ReconcileDeletedDocumentsAsync(Collection, new[] { "A", "B" });

            removed.Should().Be(0);
            await client.DidNotReceive().DeleteDocument<TypesenseService.IdProjection>(
                Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task ReconcileDeletedDocumentsAsync_SkipsDeletion_WhenListingTheIndexFails()
        {
            var client = Substitute.For<ITypesenseClient>();
            client.ExportDocuments<TypesenseService.IdProjection>(
                    Collection, Arg.Any<ExportParameters>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception("typesense unreachable"));

            var service = CreateService(client);

            // Fail-safe: an unreadable index must never trigger a (possibly mass) delete.
            var removed = await service.ReconcileDeletedDocumentsAsync(Collection, new[] { "A" });

            removed.Should().Be(0);
            await client.DidNotReceive().DeleteDocument<TypesenseService.IdProjection>(
                Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
