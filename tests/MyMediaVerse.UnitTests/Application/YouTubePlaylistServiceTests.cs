using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
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
        private readonly ILogger<YouTubePlaylistService> _mockLogger;
        private readonly YouTubePlaylistService _service;
        private readonly IThumbnailStorageService _mockThumbnailStorage = Substitute.For<IThumbnailStorageService>();
        private readonly ITypesenseService _mockTypesense = Substitute.For<ITypesenseService>();

        public YouTubePlaylistServiceTests()
        {
            _mockYouTubeApiClient = Substitute.For<IYouTubeApiClient>();
            _mockMappingService = Substitute.For<IYouTubeMappingService>();
            _mockLogger = Substitute.For<ILogger<YouTubePlaylistService>>();

            // A sync builds its new videos with the real mapper.
            var realMapper = new YouTubeMappingService();
            _mockMappingService
                .MapPlaylistItemsToVideoEntities(Arg.Any<List<YouTubePlaylistItemDto>>(), Arg.Any<List<YouTubeVideoDto>?>())
                .Returns(call => realMapper.MapPlaylistItemsToVideoEntities(
                    call.ArgAt<List<YouTubePlaylistItemDto>>(0), call.ArgAt<List<YouTubeVideoDto>?>(1)));

            // Deletes go through the real shared delete path, so its effects are asserted here too.
            var mediaService = new MediaService(
                Context, Substitute.For<ILogger<MediaService>>(), _mockThumbnailStorage, _mockTypesense);

            _service = new YouTubePlaylistService(
                Context,
                _mockYouTubeApiClient,
                _mockMappingService,
                mediaService,
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
        public async Task DeletePlaylistAsync_DetachesLinks_KeepsItsVideos_AndRemovesThePlaylistFromTheSearchIndex()
        {
            var playlist = TestDataFactory.CreateYouTubePlaylist("Test Playlist", "PLtest123");
            var video = new Video { Title = "In the playlist", Platform = "YouTube", ExternalId = "inlist00001" };
            var mixlist = TestDataFactory.CreateMixlist("Favorites");
            mixlist.MediaItems.Add(playlist);
            Context.Mixlists.Add(mixlist);
            Context.YouTubePlaylists.Add(playlist);
            Context.Videos.Add(video);
            Context.Add(new YouTubePlaylistVideo { YouTubePlaylistId = playlist.Id, VideoId = video.Id, Position = 0 });
            await Context.SaveChangesAsync();

            var result = await _service.DeletePlaylistAsync(playlist.Id);

            Assert.True(result);
            Assert.False(Context.MediaItems.Any(m => m.Id == playlist.Id));
            Assert.Empty(Context.Mixlists.Single().MediaItems);
            Assert.True(Context.Videos.Any(v => v.Id == video.Id));
            await _mockTypesense.Received(1).DeleteMediaItemAsync(playlist.Id);
            await _mockTypesense.DidNotReceive().DeleteMediaItemAsync(video.Id);
        }

        [Fact]
        public async Task DeletePlaylistAsync_WhenTheIdBelongsToAnotherMediaType_ReturnsFalse_AndDeletesNothing()
        {
            var video = new Video { Title = "Not a playlist", Platform = "YouTube" };
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();

            var result = await _service.DeletePlaylistAsync(video.Id);

            Assert.False(result);
            Assert.True(Context.Videos.Any(v => v.Id == video.Id));
            await _mockTypesense.DidNotReceive().DeleteMediaItemAsync(Arg.Any<Guid>());
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

        #region SyncPlaylistVideosAsync mirror Tests

        private const string MirrorPlaylistId = "PLmirror";

        private async Task<YouTubePlaylist> SeedMirrorPlaylistAsync()
        {
            var playlist = new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = "Mirror",
                PlaylistExternalId = MirrorPlaylistId,
                MediaType = MediaType.Playlist
            };
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetPlaylistDetailsAsync(MirrorPlaylistId).Returns(new YouTubePlaylistDto
            {
                Id = MirrorPlaylistId,
                Snippet = new YouTubePlaylistSnippetDto { Title = "Mirror" }
            });
            return playlist;
        }

        private async Task<Video> SeedVideoAsync(string? externalId, string title = "Stored video", string? link = null)
        {
            var video = new Video { Title = title, Platform = "YouTube", ExternalId = externalId, Link = link };
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();
            return video;
        }

        private async Task LinkAsync(YouTubePlaylist playlist, Video video, int position)
        {
            Context.Add(new YouTubePlaylistVideo { YouTubePlaylistId = playlist.Id, VideoId = video.Id, Position = position });
            await Context.SaveChangesAsync();
        }

        private static YouTubePlaylistItemDto Item(string videoId, int position, string? title = null) => new()
        {
            Snippet = new YouTubePlaylistItemSnippetDto
            {
                Title = title ?? $"Video {videoId}",
                ChannelTitle = "Channel",
                Position = position,
                PublishedAt = new DateTime(2025, 8, 26, 2, 39, 50, DateTimeKind.Utc),
                ResourceId = new YouTubeResourceIdDto { VideoId = videoId }
            }
        };

        private void YouTubePlaylistHas(params YouTubePlaylistItemDto[] items) =>
            _mockYouTubeApiClient.GetAllPlaylistItemsAsync(MirrorPlaylistId).Returns(items.ToList());

        /// <summary>YouTube returns details for every requested id, except the ones listed as unavailable.</summary>
        private void YouTubeReturnsVideoDetails(string? channelId = null, params string[] unavailable) =>
            _mockYouTubeApiClient.GetVideosAsync(Arg.Any<List<string>>()).Returns(call => call.Arg<List<string>>()
                .Where(id => !unavailable.Contains(id))
                .Select(id => new YouTubeVideoDto
                {
                    Id = id,
                    Snippet = new YouTubeVideoSnippetDto { Title = $"Video {id}", ChannelId = channelId },
                    ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT5M" }
                })
                .ToList());

        private List<YouTubePlaylistVideo> StoredLinks(YouTubePlaylist playlist)
        {
            Context.ChangeTracker.Clear();
            return Context.Set<YouTubePlaylistVideo>()
                .Include(link => link.Video)
                .Where(link => link.YouTubePlaylistId == playlist.Id)
                .OrderBy(link => link.Position)
                .ToList();
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_AddsAVideoThatIsNewToTheLibrary_AndLinksItAtItsPosition()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            YouTubePlaylistHas(Item("newvideo001", 0));
            YouTubeReturnsVideoDetails();

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(1, result.VideosCreated);
            Assert.Equal(0, result.VideosLinked);
            var link = Assert.Single(StoredLinks(playlist));
            Assert.Equal(0, link.Position);
            Assert.Equal("newvideo001", link.Video.ExternalId);
            Assert.Equal("YouTube", link.Video.Platform);
            Assert.Equal(Status.Uncharted, link.Video.Status);
            Assert.Equal(300, link.Video.LengthInSeconds);
            Assert.NotNull(link.Video.YouTubeRefreshedAt);
            Assert.Equal(DateTimeKind.Utc, link.VideoPublishedAt!.Value.Kind);
            Assert.Equal(1, Context.YouTubePlaylists.Single(p => p.Id == playlist.Id).VideoCount);
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_ReusesAStoredVideo_WithoutChangingIt_OrAskingYouTubeForIt()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            var stored = await SeedVideoAsync("stored00001", title: "My own title");
            stored.Notes = "My notes";
            stored.Status = Status.Completed;
            await Context.SaveChangesAsync();
            YouTubePlaylistHas(Item("stored00001", 0, title: "Title on YouTube"));

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(0, result.VideosCreated);
            Assert.Equal(1, result.VideosLinked);
            var link = Assert.Single(StoredLinks(playlist));
            Assert.Equal(stored.Id, link.VideoId);
            Assert.Equal("My own title", link.Video.Title);
            Assert.Equal("My notes", link.Video.Notes);
            Assert.Equal(Status.Completed, link.Video.Status);
            Assert.Equal(1, Context.Videos.Count());
            await _mockYouTubeApiClient.DidNotReceive().GetVideosAsync(Arg.Any<List<string>>());
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_ReusesARowSavedBeforeIdsWereStored_AndGivesItItsId()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            var legacy = await SeedVideoAsync(null, link: "https://www.youtube.com/watch?v=legacy00001");
            YouTubePlaylistHas(Item("legacy00001", 0));

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(0, result.VideosCreated);
            Assert.Equal(1, result.VideosLinked);
            var link = Assert.Single(StoredLinks(playlist));
            Assert.Equal(legacy.Id, link.VideoId);
            Assert.Equal("legacy00001", link.Video.ExternalId);
            Assert.Equal(1, Context.Videos.Count());
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_UnlinksAVideoThatLeftThePlaylist_AndKeepsItsRow()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            var staying = await SeedVideoAsync("staying0001");
            var leaving = await SeedVideoAsync("leaving0001");
            await LinkAsync(playlist, staying, 0);
            await LinkAsync(playlist, leaving, 1);
            YouTubePlaylistHas(Item("staying0001", 0));

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(1, result.VideosUnlinked);
            var link = Assert.Single(StoredLinks(playlist));
            Assert.Equal(staying.Id, link.VideoId);
            Assert.True(Context.Videos.Any(v => v.Id == leaving.Id));
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_FollowsYouTubesOrder_ForVideosAlreadyLinked()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            var first = await SeedVideoAsync("first000001");
            var second = await SeedVideoAsync("second00001");
            await LinkAsync(playlist, first, 0);
            await LinkAsync(playlist, second, 1);
            YouTubePlaylistHas(Item("second00001", 0), Item("first000001", 1));

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(2, result.PositionsUpdated);
            var links = StoredLinks(playlist);
            Assert.Equal(new[] { second.Id, first.Id }, links.Select(link => link.VideoId));
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_WhenThePlaylistListsAVideoTwice_LinksItOnce_AtItsFirstPosition()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            YouTubePlaylistHas(Item("twice000001", 0), Item("other000001", 1), Item("twice000001", 2));
            YouTubeReturnsVideoDetails();

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(2, result.VideosCreated);
            var links = StoredLinks(playlist);
            Assert.Equal(2, links.Count);
            Assert.Equal(0, links.Single(link => link.Video.ExternalId == "twice000001").Position);
            Assert.Equal(2, Context.YouTubePlaylists.Single(p => p.Id == playlist.Id).VideoCount);
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_AsksYouTubeForNewVideos_InBatchesOfFifty()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            var items = Enumerable.Range(0, 120).Select(i => Item($"batch{i:D6}", i)).ToArray();
            YouTubePlaylistHas(items);
            YouTubeReturnsVideoDetails();

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(120, result.VideosCreated);
            await _mockYouTubeApiClient.Received(3).GetVideosAsync(Arg.Any<List<string>>());
            await _mockYouTubeApiClient.DidNotReceive().GetVideosAsync(Arg.Is<List<string>>(ids => ids.Count > 50));
            Assert.Equal(120, StoredLinks(playlist).Count);
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_SkipsAVideoYouTubeReturnsNoDetailsFor()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            YouTubePlaylistHas(Item("available01", 0), Item("unavailabl1", 1));
            YouTubeReturnsVideoDetails(unavailable: "unavailabl1");

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(1, result.VideosCreated);
            var link = Assert.Single(StoredLinks(playlist));
            Assert.Equal("available01", link.Video.ExternalId);
            Assert.False(Context.Videos.Any(v => v.ExternalId == "unavailabl1"));
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_LinksANewVideoToItsChannel_OnlyWhenThatChannelIsStored()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            var channel = TestDataFactory.CreateYouTubeChannel("Stored Channel", "UCstored");
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();
            YouTubePlaylistHas(Item("fromstored1", 0), Item("fromother01", 1));
            _mockYouTubeApiClient.GetVideosAsync(Arg.Any<List<string>>()).Returns(new List<YouTubeVideoDto>
            {
                new() { Id = "fromstored1", Snippet = new YouTubeVideoSnippetDto { Title = "A", ChannelId = "UCstored" } },
                new() { Id = "fromother01", Snippet = new YouTubeVideoSnippetDto { Title = "B", ChannelId = "UCunknown" } }
            });

            await _service.SyncPlaylistVideosAsync(playlist.Id);

            var links = StoredLinks(playlist);
            Assert.Equal(channel.Id, links.Single(link => link.Video.ExternalId == "fromstored1").Video.ChannelId);
            Assert.Null(links.Single(link => link.Video.ExternalId == "fromother01").Video.ChannelId);
            Assert.Equal(1, Context.YouTubeChannels.Count());
        }

        [Fact]
        public async Task SyncPlaylistVideosAsync_ASecondSyncOfAnUnchangedPlaylist_ChangesNothing()
        {
            var playlist = await SeedMirrorPlaylistAsync();
            YouTubePlaylistHas(Item("steady00001", 0), Item("steady00002", 1));
            YouTubeReturnsVideoDetails();
            await _service.SyncPlaylistVideosAsync(playlist.Id);
            Context.ChangeTracker.Clear();
            _mockYouTubeApiClient.ClearReceivedCalls();

            var result = await _service.SyncPlaylistVideosAsync(playlist.Id);

            Assert.Equal(0, result.VideosCreated);
            Assert.Equal(0, result.VideosLinked);
            Assert.Equal(0, result.VideosUnlinked);
            Assert.Equal(0, result.PositionsUpdated);
            Assert.Equal(2, StoredLinks(playlist).Count);
            Assert.Equal(2, Context.Videos.Count());
            await _mockYouTubeApiClient.DidNotReceive().GetVideosAsync(Arg.Any<List<string>>());
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
