using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class PodcastMappingServiceTests
    {
        private readonly IThumbnailStorageService _mockThumbnailStorage;
        private readonly ILogger<PodcastMappingService> _mockLogger;
        private readonly PodcastMappingService _service;

        public PodcastMappingServiceTests()
        {
            _mockThumbnailStorage = Substitute.For<IThumbnailStorageService>();
            _mockLogger = Substitute.For<ILogger<PodcastMappingService>>();
            // Default: pass-through -- UploadFromUrlAsync returns the input URL unchanged,
            // matching the original "no S3 configured" fallback behavior the tests assume.
            _mockThumbnailStorage
                .UploadFromUrlAsync(Arg.Any<string?>(), Arg.Any<string>())
                .Returns(callInfo => callInfo.ArgAt<string?>(0));
            _service = new PodcastMappingService(_mockThumbnailStorage, _mockLogger);
        }

        #region MapFromListenNotesSeriesDto

        [Fact]
        public void MapFromListenNotesSeriesDto_ValidDto_MapsCorrectly()
        {
            var podcastDto = new PodcastSeriesDto
            {
                Id = "podcast456",
                Title = "Science Weekly",
                Publisher = "Science Media",
                Description = "Weekly science news",
                Website = "https://sciweekly.com",
                Image = "https://img.com/science.jpg",
                Thumbnail = "https://img.com/science_thumb.jpg",
                Episodes = new List<PodcastEpisodeDto>
                {
                    new PodcastEpisodeDto { Id = "ep1", Title = "Ep 1" },
                    new PodcastEpisodeDto { Id = "ep2", Title = "Ep 2" }
                }
            };

            var result = _service.MapFromListenNotesSeriesDto(podcastDto);

            result.Title.Should().Be("Science Weekly");
            result.Publisher.Should().Be("Science Media");
            result.Description.Should().Be("Weekly science news");
            result.Link.Should().Be("https://sciweekly.com");
            result.ExternalId.Should().Be("podcast456");
            result.MediaType.Should().Be(MediaType.Podcast);
            result.Status.Should().Be(Status.Uncharted);
            result.TotalEpisodes.Should().Be(2);
            result.Thumbnail.Should().Be("https://img.com/science.jpg");
        }

        [Fact]
        public void MapFromListenNotesSeriesDto_NullTitle_DefaultsToEmptyString()
        {
            var podcastDto = new PodcastSeriesDto { Title = null };

            var result = _service.MapFromListenNotesSeriesDto(podcastDto);

            result.Title.Should().BeEmpty();
        }

        [Fact]
        public void MapFromListenNotesSeriesDto_NoEpisodes_TotalEpisodesIsZero()
        {
            var podcastDto = new PodcastSeriesDto
            {
                Title = "Test",
                Episodes = null
            };

            var result = _service.MapFromListenNotesSeriesDto(podcastDto);

            result.TotalEpisodes.Should().Be(0);
        }

        [Fact]
        public void MapFromListenNotesSeriesDto_PrefersImageOverThumbnail()
        {
            var podcastDto = new PodcastSeriesDto
            {
                Title = "Test",
                Image = "https://img.com/full.jpg",
                Thumbnail = "https://img.com/thumb.jpg"
            };

            var result = _service.MapFromListenNotesSeriesDto(podcastDto);

            result.Thumbnail.Should().Be("https://img.com/full.jpg");
        }

        #endregion

        #region MapFromListenNotesEpisodeDto

        [Fact]
        public void MapFromListenNotesEpisodeDto_ValidDto_MapsCorrectly()
        {
            var episodeDto = new PodcastEpisodeDto
            {
                Id = "ep789",
                Title = "Episode 42",
                Description = "The meaning of everything",
                Link = "https://podcast.com/ep42",
                AudioUrl = "https://audio.com/ep42.mp3",
                PublishDateMs = 1704067200000L,
                DurationInSeconds = 3600,
                Image = "https://img.com/ep42.jpg"
            };

            var result = _service.MapFromListenNotesEpisodeDto(episodeDto);

            result.Title.Should().Be("Episode 42");
            result.Description.Should().Be("The meaning of everything");
            result.Link.Should().Be("https://podcast.com/ep42");
            result.AudioLink.Should().Be("https://audio.com/ep42.mp3");
            result.ExternalId.Should().Be("ep789");
            result.DurationInSeconds.Should().Be(3600);
            result.MediaType.Should().Be(MediaType.Podcast);
            result.Status.Should().Be(Status.Uncharted);
            result.SeriesId.Should().Be(Guid.Empty); // Will be set by caller
        }

        [Fact]
        public void MapFromListenNotesEpisodeDto_NullTitle_DefaultsToEmptyString()
        {
            var episodeDto = new PodcastEpisodeDto { Title = null, PublishDateMs = 1704067200000L };

            var result = _service.MapFromListenNotesEpisodeDto(episodeDto);

            result.Title.Should().BeEmpty();
        }

        #endregion
    }
}
