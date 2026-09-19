using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Models;
using MyMediaVerse.Infrastructure.Services.Search;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using Typesense;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Pins the values the search index receives for podcasts. A series and its episodes share the
    /// Podcast media type, so the podcast-only fields are what let search tell them apart and find
    /// an episode by its show's name.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesensePodcastDocumentTests : InMemoryDbTestBase
    {
        private readonly ITypesenseClient _client = Substitute.For<ITypesenseClient>();
        private readonly TypesenseService _service;
        private MediaItemDocument? _indexed;

        public TypesensePodcastDocumentTests()
        {
            var config = new ConfigurationBuilder().Build();
            _service = new TypesenseService(_client, Context, NullLogger<TypesenseService>.Instance, config);

            _client.UpsertDocument(Arg.Any<string>(), Arg.Do<MediaItemDocument>(d => _indexed = d))
                .Returns(call => call.Arg<MediaItemDocument>());
        }

        private async Task<PodcastSeries> SeedSeries(bool isSubscribed = true, string? metadataSource = PodcastMetadataSources.Rss)
        {
            var series = new PodcastSeries
            {
                Id = Guid.NewGuid(),
                Title = "Darknet Diaries",
                Publisher = "Jack Rhysider",
                MediaType = MediaType.Podcast,
                IsSubscribed = isSubscribed,
                MetadataSource = metadataSource!,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.PodcastSeries.Add(series);
            await Context.SaveChangesAsync();
            return series;
        }

        [Fact]
        public async Task Series_IsIndexedAsSeries_WithSubscriptionAndProvenance_AndNoSeriesFields()
        {
            var series = await SeedSeries(isSubscribed: true);

            var found = await _service.ReindexMediaItemByIdAsync(series.Id);

            found.Should().BeTrue();
            _indexed.Should().NotBeNull();
            _indexed!.MediaType.Should().Be("Podcast");
            _indexed.PodcastType.Should().Be(PodcastDocumentTypes.Series);
            _indexed.IsSubscribed.Should().BeTrue();
            _indexed.MetadataSource.Should().Be(PodcastMetadataSources.Rss);
            _indexed.Publisher.Should().Be("Jack Rhysider");
            _indexed.SeriesTitle.Should().BeNull();
            _indexed.SeriesId.Should().BeNull();
        }

        [Fact]
        public async Task UnsubscribedSeries_IsIndexedWithIsSubscribedFalse_NotLeftUnset()
        {
            var series = await SeedSeries(isSubscribed: false);

            await _service.ReindexMediaItemByIdAsync(series.Id);

            _indexed!.IsSubscribed.Should().BeFalse();
        }

        [Fact]
        public async Task Episode_IsIndexedAsEpisode_WithItsParentShowsTitleAndId()
        {
            var series = await SeedSeries();
            var episode = new PodcastEpisode
            {
                Id = Guid.NewGuid(),
                Title = "Ep 1: The Phreaky World of PBX Hacking",
                Publisher = "Jack Rhysider",
                MediaType = MediaType.Podcast,
                SeriesId = series.Id,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.PodcastEpisodes.Add(episode);
            await Context.SaveChangesAsync();

            var found = await _service.ReindexMediaItemByIdAsync(episode.Id);

            found.Should().BeTrue();
            _indexed.Should().NotBeNull();
            _indexed!.Id.Should().Be(episode.Id.ToString());
            _indexed.MediaType.Should().Be("Podcast");
            _indexed.PodcastType.Should().Be(PodcastDocumentTypes.Episode);
            _indexed.SeriesTitle.Should().Be("Darknet Diaries");
            _indexed.SeriesId.Should().Be(series.Id.ToString());
            _indexed.Publisher.Should().Be("Jack Rhysider");
            _indexed.IsSubscribed.Should().BeNull();
            _indexed.MetadataSource.Should().BeNull();
        }
    }
}
