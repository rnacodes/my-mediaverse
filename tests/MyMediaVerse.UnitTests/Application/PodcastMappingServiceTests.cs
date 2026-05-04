using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.UnitTests.Application
{
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

        #region MapToPodcastAsync

        [Fact]
        public async Task MapToPodcastAsync_ValidJson_MapsPodcastSeries()
        {
            var json = JsonSerializer.Serialize(new
            {
                id = "podcast123",
                title = "Tech Talks",
                publisher = "Tech Corp",
                description = "A podcast about technology",
                website = "https://techtalks.com",
                image = "https://img.com/podcast.jpg",
                thumbnail = "https://img.com/podcast_thumb.jpg"
            });

            var result = await _service.MapToPodcastAsync(json);

            result.Should().NotBeNull();
            result.Title.Should().Be("Tech Talks");
            result.Publisher.Should().Be("Tech Corp");
            result.Description.Should().Be("A podcast about technology");
            result.Link.Should().Be("https://techtalks.com");
            result.MediaType.Should().Be(MediaType.Podcast);
            result.PodcastType.Should().Be(PodcastType.Series);
            result.Status.Should().Be(Status.Uncharted);
            result.ExternalId.Should().Be("podcast123");
            // Thumbnail should be original URL since S3 is null
            result.Thumbnail.Should().Be("https://img.com/podcast.jpg");
        }

        [Fact]
        public async Task MapToPodcastAsync_WithGenres_AddsGenresLowercase()
        {
            var json = JsonSerializer.Serialize(new
            {
                id = "podcast123",
                title = "Tech Talks",
                genres = new[]
                {
                    new { id = 1, name = "Technology" },
                    new { id = 2, name = "Science" }
                }
            });

            var result = await _service.MapToPodcastAsync(json);

            result.Genres.Should().HaveCount(2);
            result.Genres.Select(g => g.Name).Should().Contain("technology");
            result.Genres.Select(g => g.Name).Should().Contain("science");
        }

        [Fact]
        public async Task MapToPodcastAsync_NoGenres_EmptyGenresCollection()
        {
            var json = JsonSerializer.Serialize(new
            {
                id = "podcast123",
                title = "Simple Podcast"
            });

            var result = await _service.MapToPodcastAsync(json);

            result.Genres.Should().BeEmpty();
        }

        [Fact]
        public async Task MapToPodcastAsync_InvalidJson_ThrowsApplicationException()
        {
            var invalidJson = "not valid json {{{";

            Func<Task> act = () => _service.MapToPodcastAsync(invalidJson);

            await act.Should().ThrowAsync<ApplicationException>()
                .WithMessage("*Failed to map podcast series*");
        }

        #endregion

        #region MapToPodcastEpisodeAsync

        [Fact]
        public async Task MapToPodcastEpisodeAsync_ValidJson_MapsEpisode()
        {
            var parentId = Guid.NewGuid();
            var json = JsonSerializer.Serialize(new
            {
                id = "ep123",
                title = "Episode 1: Getting Started",
                description = "First episode description",
                link = "https://techtalks.com/ep1",
                audio = "https://audio.com/ep1.mp3",
                pub_date_ms = 1704067200000L, // 2024-01-01 UTC
                audio_length_sec = 1800,
                image = "https://img.com/ep1.jpg"
            });

            var result = await _service.MapToPodcastEpisodeAsync(json, parentId);

            result.Should().NotBeNull();
            result.Title.Should().Be("Episode 1: Getting Started");
            result.Description.Should().Be("First episode description");
            result.Link.Should().Be("https://techtalks.com/ep1");
            result.AudioLink.Should().Be("https://audio.com/ep1.mp3");
            result.PodcastType.Should().Be(PodcastType.Episode);
            result.ParentPodcastId.Should().Be(parentId);
            result.DurationInSeconds.Should().Be(1800);
            result.ExternalId.Should().Be("ep123");
        }

        [Fact]
        public async Task MapToPodcastEpisodeAsync_WithTopics_AddsTopicsLowercase()
        {
            var json = JsonSerializer.Serialize(new
            {
                id = "ep123",
                title = "Episode",
                pub_date_ms = 1704067200000L,
                topics = new[] { "AI", "Machine Learning" }
            });

            var result = await _service.MapToPodcastEpisodeAsync(json);

            result.Topics.Should().HaveCount(2);
            result.Topics.Select(t => t.Name).Should().Contain("ai");
            result.Topics.Select(t => t.Name).Should().Contain("machine learning");
        }

        [Fact]
        public async Task MapToPodcastEpisodeAsync_NullParentId_ParentIsNull()
        {
            var json = JsonSerializer.Serialize(new
            {
                id = "ep123",
                title = "Standalone Episode",
                pub_date_ms = 1704067200000L
            });

            var result = await _service.MapToPodcastEpisodeAsync(json);

            result.ParentPodcastId.Should().BeNull();
        }

        [Fact]
        public async Task MapToPodcastEpisodeAsync_InvalidJson_ThrowsApplicationException()
        {
            Func<Task> act = () => _service.MapToPodcastEpisodeAsync("invalid json");

            await act.Should().ThrowAsync<ApplicationException>()
                .WithMessage("*Failed to map podcast episode*");
        }

        #endregion

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

        #region MapSearchResultToPodcastAsync

        [Fact]
        public async Task MapSearchResultToPodcastAsync_ValidSearchResult_MapsPodcast()
        {
            var searchResult = new SearchResultDto
            {
                Results = new List<PodcastSearchDto>
                {
                    new PodcastSearchDto
                    {
                        Id = "sr_podcast1",
                        TitleOriginal = "Original Title",
                        TitleHighlighted = "<b>Highlighted</b>",
                        PublisherOriginal = "Original Publisher",
                        DescriptionOriginal = "Original Description",
                        Image = "https://img.com/search.jpg",
                        Website = "https://podcast.com",
                        Genres = new List<GenreDto>
                        {
                            new GenreDto { Id = 1, Name = "Comedy" },
                            new GenreDto { Id = 2, Name = "Entertainment" }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(searchResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = await _service.MapSearchResultToPodcastAsync(json);

            result.Should().NotBeNull();
            result!.Title.Should().Be("Original Title");
            result.Publisher.Should().Be("Original Publisher");
            result.Description.Should().Be("Original Description");
            result.ExternalId.Should().Be("sr_podcast1");
            result.PodcastType.Should().Be(PodcastType.Series);
            result.Genres.Should().HaveCount(2);
            result.Genres.Select(g => g.Name).Should().Contain("comedy");
            result.Genres.Select(g => g.Name).Should().Contain("entertainment");
        }

        [Fact]
        public async Task MapSearchResultToPodcastAsync_NoResults_ReturnsNull()
        {
            var searchResult = new SearchResultDto { Results = new List<PodcastSearchDto>() };
            var json = JsonSerializer.Serialize(searchResult);

            var result = await _service.MapSearchResultToPodcastAsync(json);

            result.Should().BeNull();
        }

        [Fact]
        public async Task MapSearchResultToPodcastAsync_NullResults_ReturnsNull()
        {
            var searchResult = new SearchResultDto { Results = null };
            var json = JsonSerializer.Serialize(searchResult);

            var result = await _service.MapSearchResultToPodcastAsync(json);

            result.Should().BeNull();
        }

        #endregion
    }
}
