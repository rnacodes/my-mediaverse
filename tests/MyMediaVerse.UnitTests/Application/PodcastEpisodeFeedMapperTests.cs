using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.Podcasts;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastEpisodeFeedMapperTests
    {
        private static readonly PodcastSeries Series = new()
        {
            Id = Guid.NewGuid(), Title = "Darknet Diaries", MediaType = MediaType.Podcast, Publisher = "Jack Rhysider"
        };

        private static FeedEpisode Item() => new()
        {
            Title = "179: The Courthouse - Revisited",
            Description = "Gabby and Justin return.",
            EnclosureUrl = "https://cdn.example.com/ep179.mp3",
            EnclosureType = "audio/mpeg",
            PublishedAt = new DateTime(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc),
            Guid = "prx_7057_82aaba05",
            DurationSeconds = 5702,
            ImageUrl = "https://cdn.example.com/ep179.jpg",
            EpisodeNumber = 179,
            SeasonNumber = 1,
            Link = "https://darknetdiaries.com/episode/179/"
        };

        [Fact]
        public void ToCreateDto_MapsEveryFeedField()
        {
            var dto = PodcastEpisodeFeedMapper.ToCreateDto(Item(), Series)!;

            dto.Title.Should().Be("179: The Courthouse - Revisited");
            dto.SeriesId.Should().Be(Series.Id);
            dto.Status.Should().Be(Status.Uncharted);
            dto.Description.Should().Be("Gabby and Justin return.");
            dto.AudioLink.Should().Be("https://cdn.example.com/ep179.mp3");
            dto.Link.Should().Be("https://darknetdiaries.com/episode/179/");
            dto.Thumbnail.Should().Be("https://cdn.example.com/ep179.jpg");
            dto.ReleaseDate.Should().Be(new DateTime(2026, 9, 1, 7, 0, 0, DateTimeKind.Utc));
            dto.DurationInSeconds.Should().Be(5702);
            dto.EpisodeNumber.Should().Be(179);
            dto.SeasonNumber.Should().Be(1);
            dto.RssGuid.Should().Be("prx_7057_82aaba05");
            dto.Publisher.Should().Be("Jack Rhysider");
            dto.Topics.Should().BeEmpty();
            dto.Genres.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not a url")]
        public void ToCreateDto_ReturnsNull_WithoutAPlayableEnclosure(string? enclosureUrl)
        {
            PodcastEpisodeFeedMapper.ToCreateDto(Item() with { EnclosureUrl = enclosureUrl }, Series).Should().BeNull();
        }

        [Fact]
        public void ToCreateDto_FillsMissingTitle_AndDropsValuesTooLongForTheirColumns()
        {
            var item = Item() with
            {
                Title = "  ",
                Guid = new string('g', 501),
                Link = "https://example.com/" + new string('a', 2000),
                ImageUrl = "https://example.com/" + new string('b', 2000),
                DurationSeconds = null
            };

            var dto = PodcastEpisodeFeedMapper.ToCreateDto(item, Series)!;

            dto.Title.Should().Be(PodcastEpisodeFeedMapper.UntitledEpisode);
            dto.RssGuid.Should().BeNull();
            dto.Link.Should().BeNull();
            dto.Thumbnail.Should().BeNull();
            dto.DurationInSeconds.Should().Be(0);
        }

        [Fact]
        public void ToCreateDto_TruncatesLongTitles()
        {
            var dto = PodcastEpisodeFeedMapper.ToCreateDto(Item() with { Title = new string('t', 600) }, Series)!;

            dto.Title.Should().HaveLength(500);
        }
    }
}
