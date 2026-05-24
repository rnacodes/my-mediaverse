using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public partial class ListenNotesServiceTests
    {
        private readonly IListenNotesApiClient _mockListenNotesApiClient;
        private readonly IPodcastService _mockPodcastService;
        private readonly IPodcastMappingService _mockPodcastMappingService;
        private readonly ILogger<ListenNotesService> _mockLogger;
        private readonly ListenNotesService _listenNotesService;

        public ListenNotesServiceTests()
        {
            _mockListenNotesApiClient = Substitute.For<IListenNotesApiClient>();
            _mockPodcastService = Substitute.For<IPodcastService>();
            _mockPodcastMappingService = Substitute.For<IPodcastMappingService>();
            _mockLogger = Substitute.For<ILogger<ListenNotesService>>();
            
            _listenNotesService = new ListenNotesService(
                _mockListenNotesApiClient,
                _mockPodcastService,
                _mockPodcastMappingService,
                _mockLogger);
        }

        #region Test Data Factory Methods

        private static SearchResultDto CreateSearchResultDto()
        {
            return new SearchResultDto
            {
                Count = 10,
                Total = 100,
                NextOffset = 10,
                Results = new List<PodcastSearchDto>
                {
                    new PodcastSearchDto
                    {
                        Id = "test-id",
                        TitleOriginal = "Test Podcast",
                        PublisherOriginal = "Test Publisher",
                        DescriptionOriginal = "Test Description"
                    }
                }
            };
        }

        private static PodcastSeriesDto CreateListenNotesPodcastSeriesDto()
        {
            return new PodcastSeriesDto
            {
                Id = "test-podcast-id",
                Title = "Test Podcast",
                Publisher = "Test Publisher",
                Description = "Test Description",
                Image = "https://example.com/image.jpg",
                Website = "https://example.com",
                Episodes = new List<PodcastEpisodeDto>()
            };
        }

        private static PodcastEpisodeDto CreateListenNotesPodcastEpisodeDto()
        {
            return new PodcastEpisodeDto
            {
                Id = "test-episode-id",
                Title = "Test Episode",
                Description = "Test Episode Description",
                AudioUrl = "https://example.com/audio.mp3",
                PublishDateMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                DurationInSeconds = 3600
            };
        }

        private static ListenNotesBestPodcastsDto CreateBestPodcastsDto()
        {
            return new ListenNotesBestPodcastsDto
            {
                Id = 1,
                Name = "Best Podcasts",
                Total = 50,
                Podcasts = new List<PodcastSearchDto>()
            };
        }

        private static ListenNotesRecommendationsDto CreateRecommendationsDto()
        {
            return new ListenNotesRecommendationsDto
            {
                Recommendations = new List<PodcastSearchDto>()
            };
        }

        private static ListenNotesPlaylistsDto CreatePlaylistsDto()
        {
            return new ListenNotesPlaylistsDto
            {
                Playlists = new List<ListenNotesPlaylistDto>(),
                Total = 10
            };
        }

        private static ListenNotesPlaylistDto CreatePlaylistDto()
        {
            return new ListenNotesPlaylistDto
            {
                Id = "test-playlist-id",
                Name = "Test Playlist",
                Description = "Test Playlist Description"
            };
        }

        private static ListenNotesGenresDto CreateGenresDto()
        {
            return new ListenNotesGenresDto
            {
                Genres = new List<GenreDto>
                {
                    new GenreDto { Id = 1, Name = "Comedy" },
                    new GenreDto { Id = 2, Name = "News" }
                }
            };
        }

        private static ListenNotesCuratedPodcastsDto CreateCuratedPodcastsDto()
        {
            return new ListenNotesCuratedPodcastsDto
            {
                CuratedLists = new List<ListenNotesCuratedPodcastDto>(),
                Total = 5
            };
        }

        private static ListenNotesCuratedPodcastDto CreateCuratedPodcastDto()
        {
            return new ListenNotesCuratedPodcastDto
            {
                Id = "test-curated-id",
                Title = "Test Curated Podcast",
                Description = "Test Curated Description"
            };
        }

        private static CreatePodcastSeriesDto CreatePodcastSeriesDto()
        {
            return new CreatePodcastSeriesDto
            {
                Title = "Test Podcast",
                Publisher = "Test Publisher",
                Description = "Test Description",
                Status = Status.Uncharted,
                IsSubscribed = false
            };
        }

        private static PodcastSeries CreatePodcastSeries()
        {
            return new PodcastSeries
            {
                Id = Guid.NewGuid(),
                Title = "Test Podcast",
                MediaType = MediaType.Podcast,
                Publisher = "Test Publisher",
                Description = "Test Description",
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                IsSubscribed = false
            };
        }

        private static CreatePodcastEpisodeDto CreatePodcastEpisodeDto()
        {
            return new CreatePodcastEpisodeDto
            {
                Title = "Test Episode",
                SeriesId = Guid.NewGuid(),
                Description = "Test Episode Description",
                Status = Status.Uncharted,
                AudioLink = "https://example.com/audio.mp3"
            };
        }

        private static PodcastEpisode CreatePodcastEpisode()
        {
            return new PodcastEpisode
            {
                Id = Guid.NewGuid(),
                Title = "Test Episode",
                MediaType = MediaType.Podcast,
                SeriesId = Guid.NewGuid(),
                Description = "Test Episode Description",
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                AudioLink = "https://example.com/audio.mp3"
            };
        }

        #endregion
    }
}
