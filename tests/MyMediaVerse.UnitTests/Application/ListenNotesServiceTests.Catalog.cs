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
    public partial class ListenNotesServiceTests
    {
        #region Search Operations Tests

        [Fact]
        public async Task SearchAsync_ShouldReturnSearchResults_WhenValidQueryProvided()
        {
            // Arrange
            var query = "joe rogan";
            var expectedResult = CreateSearchResultDto();
            
            _mockListenNotesApiClient
                .SearchAsync(query, null, null, null, null, null, null, null, null, null, null, null, null, null)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.SearchAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).SearchAsync(query, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task SearchAsync_ShouldPassAllParameters_WhenAllParametersProvided()
        {
            // Arrange
            var query = "test";
            var type = "podcast";
            var offset = 10;
            var lenMin = 30;
            var lenMax = 60;
            var genreIds = "1,2,3";
            var publishedBefore = "2023-01-01";
            var publishedAfter = "2022-01-01";
            var onlyIn = "title";
            var language = "en";
            var region = "us";
            var sortByDate = "1";
            var safeMode = "1";
            var uniquePodcasts = "1";
            var expectedResult = CreateSearchResultDto();
            
            _mockListenNotesApiClient
                .SearchAsync(query, type, offset, lenMin, lenMax, genreIds, publishedBefore, publishedAfter, onlyIn, language, region, sortByDate, safeMode, uniquePodcasts)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.SearchAsync(query, type, offset, lenMin, lenMax, genreIds, publishedBefore, publishedAfter, onlyIn, language, region, sortByDate, safeMode, uniquePodcasts);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).SearchAsync(query, type, offset, lenMin, lenMax, genreIds, publishedBefore, publishedAfter, onlyIn, language, region, sortByDate, safeMode, uniquePodcasts);
        }

        #endregion

        #region Podcast Operations Tests

        [Fact]
        public async Task GetPodcastByIdAsync_ShouldReturnPodcastDetails_WhenValidIdProvided()
        {
            // Arrange
            var podcastId = "test-podcast-id";
            var expectedResult = CreateListenNotesPodcastSeriesDto();
            
            _mockListenNotesApiClient
                .GetPodcastByIdAsync(podcastId, null)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetPodcastByIdAsync(podcastId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetPodcastByIdAsync(podcastId, null);
        }

        [Fact]
        public async Task GetBestPodcastsAsync_ShouldReturnBestPodcasts_WhenCalled()
        {
            // Arrange
            var expectedResult = CreateBestPodcastsDto();
            
            _mockListenNotesApiClient
                .GetBestPodcastsAsync(null, null, null, null, null)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetBestPodcastsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetBestPodcastsAsync(null, null, null, null, null);
        }

        [Fact]
        public async Task GetPodcastRecommendationsAsync_ShouldReturnRecommendations_WhenValidIdProvided()
        {
            // Arrange
            var podcastId = "test-podcast-id";
            var expectedResult = CreateRecommendationsDto();
            
            _mockListenNotesApiClient
                .GetPodcastRecommendationsAsync(podcastId, null)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetPodcastRecommendationsAsync(podcastId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetPodcastRecommendationsAsync(podcastId, null);
        }

        #endregion

        #region Episode Operations Tests

        [Fact]
        public async Task GetEpisodeByIdAsync_ShouldReturnEpisodeDetails_WhenValidIdProvided()
        {
            // Arrange
            var episodeId = "test-episode-id";
            var expectedResult = CreateListenNotesPodcastEpisodeDto();
            
            _mockListenNotesApiClient
                .GetEpisodeByIdAsync(episodeId)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetEpisodeByIdAsync(episodeId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetEpisodeByIdAsync(episodeId);
        }

        [Fact]
        public async Task GetEpisodeRecommendationsAsync_ShouldReturnRecommendations_WhenValidIdProvided()
        {
            // Arrange
            var episodeId = "test-episode-id";
            var expectedResult = CreateRecommendationsDto();
            
            _mockListenNotesApiClient
                .GetEpisodeRecommendationsAsync(episodeId, null)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetEpisodeRecommendationsAsync(episodeId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetEpisodeRecommendationsAsync(episodeId, null);
        }

        #endregion

        #region Playlist Operations Tests

        [Fact]
        public async Task GetPlaylistsAsync_ShouldReturnPlaylists_WhenCalled()
        {
            // Arrange
            var expectedResult = CreatePlaylistsDto();
            
            _mockListenNotesApiClient
                .GetPlaylistsAsync()
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetPlaylistsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetPlaylistsAsync();
        }

        [Fact]
        public async Task GetPlaylistByIdAsync_ShouldReturnPlaylistDetails_WhenValidIdProvided()
        {
            // Arrange
            var playlistId = "test-playlist-id";
            var expectedResult = CreatePlaylistDto();
            
            _mockListenNotesApiClient
                .GetPlaylistByIdAsync(playlistId)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetPlaylistByIdAsync(playlistId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetPlaylistByIdAsync(playlistId);
        }

        #endregion

        #region Genre Operations Tests

        [Fact]
        public async Task GetGenresAsync_ShouldReturnGenres_WhenCalled()
        {
            // Arrange
            var expectedResult = CreateGenresDto();
            
            _mockListenNotesApiClient
                .GetGenresAsync()
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetGenresAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetGenresAsync();
        }

        #endregion

        #region Curated Content Operations Tests

        [Fact]
        public async Task GetCuratedPodcastsAsync_ShouldReturnCuratedPodcasts_WhenCalled()
        {
            // Arrange
            var expectedResult = CreateCuratedPodcastsDto();
            
            _mockListenNotesApiClient
                .GetCuratedPodcastsAsync(null)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetCuratedPodcastsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetCuratedPodcastsAsync(null);
        }

        [Fact]
        public async Task GetCuratedPodcastByIdAsync_ShouldReturnCuratedPodcastDetails_WhenValidIdProvided()
        {
            // Arrange
            var curatedPodcastId = "test-curated-id";
            var expectedResult = CreateCuratedPodcastDto();
            
            _mockListenNotesApiClient
                .GetCuratedPodcastByIdAsync(curatedPodcastId)
                .Returns(expectedResult);

            // Act
            var result = await _listenNotesService.GetCuratedPodcastByIdAsync(curatedPodcastId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResult);
            _mockListenNotesApiClient.Received(1).GetCuratedPodcastByIdAsync(curatedPodcastId);
        }

        #endregion
    }
}
