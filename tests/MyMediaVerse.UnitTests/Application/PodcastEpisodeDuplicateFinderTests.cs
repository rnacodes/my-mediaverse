using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastEpisodeDuplicateFinderTests : InMemoryDbTestBase
    {
        private readonly Guid _seriesId = Guid.NewGuid();
        private readonly Guid _otherSeriesId = Guid.NewGuid();

        public PodcastEpisodeDuplicateFinderTests()
        {
            Context.PodcastSeries.Add(new PodcastSeries { Id = _seriesId, Title = "Show", MediaType = MediaType.Podcast });
            Context.PodcastSeries.Add(new PodcastSeries { Id = _otherSeriesId, Title = "Other", MediaType = MediaType.Podcast });
        }

        private PodcastEpisode AddEpisode(
            Guid seriesId,
            string title = "Episode",
            string? rssGuid = null,
            string? externalId = null,
            string? audioLink = null,
            DateTime? releaseDate = null)
        {
            var episode = new PodcastEpisode
            {
                SeriesId = seriesId,
                Title = title,
                MediaType = MediaType.Podcast,
                RssGuid = rssGuid,
                ExternalId = externalId,
                AudioLink = audioLink,
                ReleaseDate = releaseDate
            };
            Context.PodcastEpisodes.Add(episode);
            return episode;
        }

        [Fact]
        public async Task FindExistingAsync_MatchesOnRssGuid_WithinTheSeriesOnly()
        {
            AddEpisode(_otherSeriesId, rssGuid: "item-1");
            var target = AddEpisode(_seriesId, rssGuid: "item-1");
            await Context.SaveChangesAsync();

            var match = await PodcastEpisodeDuplicateFinder.FindExistingAsync(Context.PodcastEpisodes,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, RssGuid = "item-1" });

            match!.Id.Should().Be(target.Id);
        }

        [Fact]
        public async Task FindExistingAsync_DoesNotMatchAnEpisodeFromAnotherSeries()
        {
            AddEpisode(_otherSeriesId, rssGuid: "item-1", externalId: "ln-1", audioLink: "https://cdn.example.com/1.mp3");
            await Context.SaveChangesAsync();

            var match = await PodcastEpisodeDuplicateFinder.FindExistingAsync(Context.PodcastEpisodes,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, RssGuid = "item-1", ExternalId = "ln-1", AudioLink = "https://cdn.example.com/1.mp3" });

            match.Should().BeNull();
        }

        [Fact]
        public async Task FindExistingAsync_FallsBackToExternalId()
        {
            var target = AddEpisode(_seriesId, externalId: "ln-7");
            await Context.SaveChangesAsync();

            var match = await PodcastEpisodeDuplicateFinder.FindExistingAsync(Context.PodcastEpisodes,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, RssGuid = "unknown", ExternalId = "ln-7" });

            match!.Id.Should().Be(target.Id);
        }

        [Fact]
        public async Task FindExistingAsync_MatchesOnNormalizedEnclosureUrl()
        {
            var target = AddEpisode(_seriesId, audioLink: "https://cdn.example.com/ep/42.mp3");
            AddEpisode(_seriesId, audioLink: "https://cdn.example.com/ep/43.mp3");
            await Context.SaveChangesAsync();

            var match = await PodcastEpisodeDuplicateFinder.FindExistingAsync(Context.PodcastEpisodes,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, AudioLink = "http://www.cdn.example.com/ep/42.mp3?utm_source=feed" });

            match!.Id.Should().Be(target.Id);
        }

        [Fact]
        public async Task FindExistingAsync_TitleAndReleaseDay_RequireBoth()
        {
            var target = AddEpisode(_seriesId, title: "Trailer", releaseDate: new DateTime(2024, 5, 1, 9, 0, 0, DateTimeKind.Utc));
            await Context.SaveChangesAsync();

            (await PodcastEpisodeDuplicateFinder.FindExistingAsync(Context.PodcastEpisodes,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, Title = "trailer", ReleaseDate = new DateTime(2024, 5, 1, 18, 30, 0, DateTimeKind.Utc) }))!
                .Id.Should().Be(target.Id);
            (await PodcastEpisodeDuplicateFinder.FindExistingAsync(Context.PodcastEpisodes,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, Title = "Trailer" })).Should().BeNull();
            (await PodcastEpisodeDuplicateFinder.FindExistingAsync(Context.PodcastEpisodes,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, Title = "Trailer", ReleaseDate = new DateTime(2024, 5, 2, 0, 0, 0, DateTimeKind.Utc) })).Should().BeNull();
        }

        [Fact]
        public async Task AbsorbIdentityAsync_FillsMissingGuid_UnlessAnotherEpisodeInTheSeriesOwnsIt()
        {
            var existing = AddEpisode(_seriesId, audioLink: "https://cdn.example.com/1.mp3");
            AddEpisode(_seriesId, rssGuid: "taken");
            await Context.SaveChangesAsync();

            (await PodcastEpisodeDuplicateFinder.AbsorbIdentityAsync(Context.PodcastEpisodes, existing,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, RssGuid = "taken" })).Should().BeFalse();
            existing.RssGuid.Should().BeNull();

            (await PodcastEpisodeDuplicateFinder.AbsorbIdentityAsync(Context.PodcastEpisodes, existing,
                new PodcastEpisodeIdentity { SeriesId = _seriesId, RssGuid = "fresh", ExternalId = "ln-1" })).Should().BeTrue();
            existing.RssGuid.Should().Be("fresh");
            existing.ExternalId.Should().Be("ln-1");
        }
    }
}
