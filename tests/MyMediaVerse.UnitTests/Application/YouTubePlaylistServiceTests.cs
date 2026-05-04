using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;
using Xunit;

namespace MyMediaVerse.UnitTests.Application
{
    /// <summary>
    /// Unit tests for YouTubePlaylistService
    /// Note: Only basic tests included. Complex many-to-many relationship tests
    /// and import functionality are better suited for integration tests.
    /// </summary>
    public class YouTubePlaylistServiceTests : InMemoryDbTestBase
    {
        private readonly IYouTubeApiClient _mockYouTubeApiClient;
        private readonly IYouTubeMappingService _mockMappingService;
        private readonly IVideoService _mockVideoService;
        private readonly ILogger<YouTubePlaylistService> _mockLogger;
        private readonly YouTubePlaylistService _service;

        public YouTubePlaylistServiceTests()
        {
            _mockYouTubeApiClient = Substitute.For<IYouTubeApiClient>();
            _mockMappingService = Substitute.For<IYouTubeMappingService>();
            _mockVideoService = Substitute.For<IVideoService>();
            _mockLogger = Substitute.For<ILogger<YouTubePlaylistService>>();

            _service = new YouTubePlaylistService(
                Context,
                _mockYouTubeApiClient,
                _mockMappingService,
                _mockVideoService,
                _mockLogger
            );
        }

        #region GetPlaylistByIdAsync Tests

        [Fact]
        public async Task GetPlaylistByIdAsync_WithValidId_ReturnsPlaylist()
        {
            // Arrange
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Test Playlist",
                PlaylistExternalId = "PLtest123",
                MediaType = MediaType.Video,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetPlaylistByIdAsync(playlist.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(playlist.Id, result.Id);
            Assert.Equal("Test Playlist", result.Title);
            Assert.Equal("PLtest123", result.PlaylistExternalId);
        }

        [Fact]
        public async Task GetPlaylistByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.GetPlaylistByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetPlaylistByExternalIdAsync Tests

        [Fact]
        public async Task GetPlaylistByExternalIdAsync_WithValidExternalId_ReturnsPlaylist()
        {
            // Arrange
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Test Playlist",
                PlaylistExternalId = "PLtest123",
                MediaType = MediaType.Video,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetPlaylistByExternalIdAsync("PLtest123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PLtest123", result.PlaylistExternalId);
            Assert.Equal("Test Playlist", result.Title);
        }

        [Fact]
        public async Task GetPlaylistByExternalIdAsync_WithInvalidExternalId_ReturnsNull()
        {
            // Arrange & Act
            var result = await _service.GetPlaylistByExternalIdAsync("PLnonexistent");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllPlaylistsAsync Tests

        [Fact]
        public async Task GetAllPlaylistsAsync_WithNoPlaylists_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetAllPlaylistsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllPlaylistsAsync_WithMultiplePlaylists_ReturnsAllPlaylists()
        {
            // Arrange
            var playlists = new[]
            {
                new YouTubePlaylist
                {
                    Id = Guid.NewGuid(),
                    Title = "Playlist 1",
                    PlaylistExternalId = "PL001",
                    MediaType = MediaType.Video,
                    Status = Status.Uncharted,
                    DateAdded = DateTime.UtcNow
                },
                new YouTubePlaylist
                {
                    Id = Guid.NewGuid(),
                    Title = "Playlist 2",
                    PlaylistExternalId = "PL002",
                    MediaType = MediaType.Video,
                    Status = Status.Uncharted,
                    DateAdded = DateTime.UtcNow
                }
            };
            Context.YouTubePlaylists.AddRange(playlists);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllPlaylistsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region DeletePlaylistAsync Tests

        [Fact]
        public async Task DeletePlaylistAsync_WithValidId_DeletesPlaylist()
        {
            // Arrange
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Test Playlist",
                PlaylistExternalId = "PLtest123",
                MediaType = MediaType.Video,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.DeletePlaylistAsync(playlist.Id);

            // Assert
            Assert.True(result);
            
            var deletedPlaylist = await Context.YouTubePlaylists.FindAsync(playlist.Id);
            Assert.Null(deletedPlaylist);
        }

        [Fact]
        public async Task DeletePlaylistAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.DeletePlaylistAsync(nonExistentId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region SavePlaylistAsync Tests

        [Fact]
        public async Task SavePlaylistAsync_WithNewPlaylist_CreatesPlaylist()
        {
            // Arrange
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "New Playlist",
                PlaylistExternalId = "PLnew123",
                MediaType = MediaType.Video,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };

            // Act
            var result = await _service.SavePlaylistAsync(playlist);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Playlist", result.Title);
            
            var savedPlaylist = await Context.YouTubePlaylists.FindAsync(playlist.Id);
            Assert.NotNull(savedPlaylist);
        }

        [Fact]
        public async Task SavePlaylistAsync_WithExistingPlaylistAndUpdateTrue_UpdatesPlaylist()
        {
            // Arrange
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Original Title",
                PlaylistExternalId = "PLtest123",
                MediaType = MediaType.Video,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow
            };
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();

            // Detach to simulate update scenario
            Context.ChangeTracker.Clear();
            
            playlist.Title = "Updated Title";

            // Act
            var result = await _service.SavePlaylistAsync(playlist, updateIfExists: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            
            var updatedPlaylist = await Context.YouTubePlaylists.FindAsync(playlist.Id);
            Assert.NotNull(updatedPlaylist);
            Assert.Equal("Updated Title", updatedPlaylist.Title);
        }

        #endregion
    }
}
