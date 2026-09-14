using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestHelpers;
using Entry = MyMediaVerse.Application.Utilities.PodcastEpisodeIdentityIndex.Entry;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastEpisodeIdentityIndexTests : InMemoryDbTestBase
    {
        private static readonly DateTime Day = new(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Find_MatchesByGuid_ThenAudioUrl_ThenTitleAndDay()
        {
            var byGuid = new Entry(Guid.NewGuid(), "guid-1", "https://cdn.example.com/a.mp3", "A", Day);
            var byAudio = new Entry(Guid.NewGuid(), null, "https://cdn.example.com/b.mp3", "B", Day);
            var byTitle = new Entry(Guid.NewGuid(), null, null, "Trailer", Day);
            var index = PodcastEpisodeIdentityIndex.From(new[] { byGuid, byAudio, byTitle });

            index.Find(" guid-1 ", null, null, null).Should().Be(byGuid);
            index.Find("other-guid", "http://www.cdn.example.com/b.mp3/", null, null).Should().Be(byAudio);
            index.Find(null, null, "TRAILER", Day.AddHours(10)).Should().Be(byTitle);
        }

        [Fact]
        public void Find_TitleAlone_IsNotEnough()
        {
            var index = PodcastEpisodeIdentityIndex.From(new[] { new Entry(Guid.NewGuid(), null, null, "Trailer", Day) });

            index.Find(null, null, "Trailer", Day.AddDays(1)).Should().BeNull();
            index.Find(null, null, "Trailer", null).Should().BeNull();
        }

        [Fact]
        public void NewestReleaseDate_IgnoresUndatedEpisodes_AndTracksAdds()
        {
            var index = PodcastEpisodeIdentityIndex.From(new[]
            {
                new Entry(Guid.NewGuid(), null, null, "Old", Day.AddDays(-10)),
                new Entry(Guid.NewGuid(), null, null, "Undated", null)
            });
            index.NewestReleaseDate.Should().Be(Day.AddDays(-10));

            index.Add(new Entry(Guid.NewGuid(), "g", null, "New", Day));
            index.NewestReleaseDate.Should().Be(Day);

            PodcastEpisodeIdentityIndex.From(Array.Empty<Entry>()).NewestReleaseDate.Should().BeNull();
        }

        [Fact]
        public void AssignGuid_MakesTheEpisodeFindableByGuid_AndOwnershipIsReported()
        {
            var stored = new Entry(Guid.NewGuid(), null, "https://cdn.example.com/a.mp3", "A", Day);
            var index = PodcastEpisodeIdentityIndex.From(new[] { stored });

            index.AssignGuid(stored, "new-guid");

            index.Find("new-guid", null, null, null)!.Id.Should().Be(stored.Id);
            index.GuidOwnedByOther("new-guid", stored.Id).Should().BeFalse();
            index.GuidOwnedByOther("new-guid", Guid.NewGuid()).Should().BeTrue();
        }

        [Fact]
        public async Task BuildAsync_LoadsOnlyTheSeriesEpisodes()
        {
            var series = new PodcastSeries { Title = "Show", MediaType = MediaType.Podcast };
            var other = new PodcastSeries { Title = "Other", MediaType = MediaType.Podcast };
            Context.PodcastSeries.AddRange(series, other);
            Context.PodcastEpisodes.AddRange(
                new PodcastEpisode { Title = "Mine", MediaType = MediaType.Podcast, SeriesId = series.Id, RssGuid = "mine", ReleaseDate = Day },
                new PodcastEpisode { Title = "Theirs", MediaType = MediaType.Podcast, SeriesId = other.Id, RssGuid = "theirs", ReleaseDate = Day.AddDays(5) });
            await Context.SaveChangesAsync();

            var index = await PodcastEpisodeIdentityIndex.BuildAsync(Context.PodcastEpisodes, series.Id);

            index.Find("mine", null, null, null).Should().NotBeNull();
            index.Find("theirs", null, null, null).Should().BeNull();
            index.NewestReleaseDate.Should().Be(Day);
        }
    }
}
