using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Models;
using MyMediaVerse.Infrastructure.Services.Search;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using Typesense;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Covers the targeted reindex used after imports and syncs that touch a few known rows. It must
    /// send exactly the documents the other reindex paths build, and only for the requested items.
    /// The Typesense client is substituted; the database is EF Core InMemory.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseTargetedReindexTests : InMemoryDbTestBase
    {
        private readonly ITypesenseClient _client = Substitute.For<ITypesenseClient>();
        private readonly TypesenseService _service;
        private readonly List<MediaItemDocument> _imported = new();
        private MediaItemDocument? _upserted;

        public TypesenseTargetedReindexTests()
        {
            var config = new ConfigurationBuilder().Build();
            _service = new TypesenseService(_client, Context, NullLogger<TypesenseService>.Instance, config);

            _client.ImportDocuments(
                    Arg.Any<string>(), Arg.Any<IEnumerable<MediaItemDocument>>(), Arg.Any<int>(),
                    Arg.Any<ImportType>(), Arg.Any<int?>(), Arg.Any<bool?>())
                .Returns(call =>
                {
                    var documents = call.Arg<IEnumerable<MediaItemDocument>>().ToList();
                    _imported.AddRange(documents);
                    return documents.Select(d => new ImportResponse(true, null!, null!, d.Id)).ToList();
                });
            _client.UpsertDocument(Arg.Any<string>(), Arg.Do<MediaItemDocument>(d => _upserted = d))
                .Returns(call => call.Arg<MediaItemDocument>());
        }

        private async Task<Video> SeedVideoAsync(string title)
        {
            var channel = new YouTubeChannel
            {
                Title = "Some Channel",
                ChannelExternalId = $"UC{Guid.NewGuid():N}",
                MediaType = MediaType.Channel
            };
            var video = new Video
            {
                Title = title,
                Description = "A video",
                Platform = "YouTube",
                ExternalId = Guid.NewGuid().ToString("N")[..11],
                LengthInSeconds = 300,
                MediaType = MediaType.Video,
                Channel = channel,
                Topics = new List<Topic> { new() { Name = "coding" } },
                Genres = new List<Genre> { new() { Name = "tech" } }
            };
            Context.AddRange(channel, video);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return video;
        }

        [Fact]
        public async Task ReindexMediaItemsByIdsAsync_WithNoIds_ReturnsZero_WithoutCallingTypesense()
        {
            var count = await _service.ReindexMediaItemsByIdsAsync(Array.Empty<Guid>());

            count.Should().Be(0);
            _client.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task ReindexMediaItemsByIdsAsync_BuildsTheSameDocumentAsTheSingleItemReindex()
        {
            var video = await SeedVideoAsync("Same document");

            await _service.ReindexMediaItemsByIdsAsync(new[] { video.Id });
            await _service.ReindexMediaItemByIdAsync(video.Id);

            var targeted = _imported.Should().ContainSingle().Subject;
            _upserted.Should().NotBeNull();
            JsonSerializer.Serialize(targeted).Should().Be(JsonSerializer.Serialize(_upserted));
            targeted.ChannelTitle.Should().Be("Some Channel");
            targeted.Topics.Should().Equal("coding");
        }

        [Fact]
        public async Task ReindexMediaItemsByIdsAsync_SendsOnlyTheRequestedItems_AndSkipsUnknownIds()
        {
            var wanted = await SeedVideoAsync("Wanted");
            await SeedVideoAsync("Not requested");

            var count = await _service.ReindexMediaItemsByIdsAsync(new[] { wanted.Id, wanted.Id, Guid.NewGuid() });

            count.Should().Be(1);
            _imported.Select(d => d.Id).Should().Equal(wanted.Id.ToString());
            await _client.Received(1).ImportDocuments(
                Arg.Any<string>(), Arg.Any<IEnumerable<MediaItemDocument>>(), 40,
                ImportType.Upsert, Arg.Any<int?>(), Arg.Any<bool?>());
            await _client.DidNotReceive().ExportDocuments<TypesenseService.IdProjection>(
                Arg.Any<string>(), Arg.Any<ExportParameters>(), Arg.Any<CancellationToken>());
        }
    }
}
