using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
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
    [Trait("Category", "Unit")]
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

        #region Import result Tests

        [Fact]
        public async Task ImportPlaylistFromYouTubeAsync_ForANewPlaylist_SavesItAndReportsItAsCreated()
        {
            var playlistDto = new YouTubePlaylistDto { Id = "PLnew", Snippet = new YouTubePlaylistSnippetDto { Title = "New Playlist" } };
            _mockYouTubeApiClient.GetPlaylistDetailsAsync("PLnew").Returns(playlistDto);
            _mockMappingService.MapPlaylistToYouTubePlaylistEntity(playlistDto).Returns(new YouTubePlaylist
            {
                Title = "New Playlist",
                PlaylistExternalId = "PLnew",
                MediaType = MediaType.Playlist
            });

            var result = await _service.ImportPlaylistFromYouTubeAsync("PLnew");

            Assert.True(result.Created);
            Assert.Equal("PLnew", result.Playlist.PlaylistExternalId);
            Assert.Single(Context.YouTubePlaylists);
        }

        [Fact]
        public async Task ImportPlaylistFromYouTubeAsync_WhenThePlaylistIsAlreadyStored_ReturnsItWithoutCallingYouTube()
        {
            var stored = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Stored Playlist",
                PlaylistExternalId = "PLstored",
                MediaType = MediaType.Playlist
            };
            Context.YouTubePlaylists.Add(stored);
            await Context.SaveChangesAsync();

            var result = await _service.ImportPlaylistFromYouTubeAsync("PLstored");

            Assert.False(result.Created);
            Assert.Equal(stored.Id, result.Playlist.Id);
            Assert.Single(Context.YouTubePlaylists);
            await _mockYouTubeApiClient.DidNotReceive().GetPlaylistDetailsAsync(Arg.Any<string>());
        }

        #endregion

        #region SyncPlaylistVideosAsync Tests

        [Fact]
        public async Task SyncPlaylistVideosAsync_RefreshesThePlaylistsOwnDetails_AndStampsTheSync()
        {
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Old name",
                Description = "Old description",
                PlaylistExternalId = "PLsync",
                MediaType = MediaType.Playlist,
                Notes = "My notes"
            };
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetPlaylistDetailsAsync("PLsync").Returns(new YouTubePlaylistDto
            {
                Id = "PLsync",
                Snippet = new YouTubePlaylistSnippetDto
                {
                    Title = "New name",
                    Description = "New description",
                    Thumbnails = new YouTubeThumbnailsDto { High = new YouTubeThumbnailDto { Url = "https://i.ytimg.com/vi/abc/new.jpg" } }
                },
                Status = new YouTubePlaylistStatusDto { PrivacyStatus = "public" }
            });
            _mockYouTubeApiClient.GetAllPlaylistItemsAsync("PLsync").Returns(new List<YouTubePlaylistItemDto>());

            await _service.SyncPlaylistVideosAsync(playlist.Id);

            Context.ChangeTracker.Clear();
            var stored = await Context.YouTubePlaylists.FindAsync(playlist.Id);
            Assert.NotNull(stored);
            Assert.Equal("New name", stored.Title);
            Assert.Equal("New description", stored.Description);
            Assert.Equal("https://i.ytimg.com/vi/abc/new.jpg", stored.Thumbnail);
            Assert.Equal("public", stored.PrivacyStatus);
            Assert.Equal("My notes", stored.Notes);
            Assert.NotNull(stored.LastSyncedAt);
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_CountsOnlyTheVideosStillAvailable()
        {
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Playlist",
                PlaylistExternalId = "PLsync",
                MediaType = MediaType.Playlist
            };
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetPlaylistDetailsAsync("PLsync").Returns(new YouTubePlaylistDto
            {
                Id = "PLsync",
                Snippet = new YouTubePlaylistSnippetDto { Title = "Playlist" },
                ContentDetails = new YouTubePlaylistContentDetailsDto { ItemCount = 2 }
            });
            _mockYouTubeApiClient.GetAllPlaylistItemsAsync("PLsync").Returns(new List<YouTubePlaylistItemDto>
            {
                new()
                {
                    Snippet = new YouTubePlaylistItemSnippetDto
                    {
                        Title = "Still here",
                        ChannelTitle = "Channel",
                        ResourceId = new YouTubeResourceIdDto { VideoId = "here0000001" }
                    }
                },
                new()
                {
                    Snippet = new YouTubePlaylistItemSnippetDto
                    {
                        Title = "Deleted video",
                        ResourceId = new YouTubeResourceIdDto { VideoId = "gone0000001" }
                    }
                }
            });

            await _service.SyncPlaylistVideosAsync(playlist.Id);

            Context.ChangeTracker.Clear();
            var stored = await Context.YouTubePlaylists.FindAsync(playlist.Id);
            Assert.Equal(1, stored!.VideoCount);
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_WhenThePlaylistLeftYouTube_ThrowsNotFound_AndKeepsTheStoredRow()
        {
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Stored Playlist",
                PlaylistExternalId = "PLgone",
                MediaType = MediaType.Playlist
            };
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetPlaylistDetailsAsync("PLgone").Returns((YouTubePlaylistDto?)null);

            var exception = await Assert.ThrowsAsync<YouTubeResourceNotFoundException>(
                () => _service.SyncPlaylistVideosAsync(playlist.Id));

            Assert.Equal("playlist", exception.ResourceType);
            await _mockYouTubeApiClient.DidNotReceive().GetAllPlaylistItemsAsync(Arg.Any<string>());
            Context.ChangeTracker.Clear();
            var stored = await Context.YouTubePlaylists.FindAsync(playlist.Id);
            Assert.Equal("Stored Playlist", stored!.Title);
            Assert.Null(stored.LastSyncedAt);
        }

        #endregion

        #region YouTube not-found Tests

        [Fact]
        public async Task ImportPlaylistFromYouTubeAsync_WhenYouTubeHasNoSuchPlaylist_ThrowsNotFound_AndStoresNothing()
        {
            _mockYouTubeApiClient.GetPlaylistDetailsAsync("PLgone").Returns((YouTubePlaylistDto?)null);

            var exception = await Assert.ThrowsAsync<YouTubeResourceNotFoundException>(
                () => _service.ImportPlaylistFromYouTubeAsync("PLgone"));

            Assert.Equal("playlist", exception.ResourceType);
            Assert.Equal("PLgone", exception.Identifier);
            Assert.Empty(Context.YouTubePlaylists);
        }

        #endregion
    }
}
