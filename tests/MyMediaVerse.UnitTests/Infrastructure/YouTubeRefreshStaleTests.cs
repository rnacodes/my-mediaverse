using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// The YouTube refresh run: which items count as stale, how the limit is shared between
    /// channels, playlists and videos, and what happens when YouTube no longer has an item,
    /// a request fails, or the daily quota runs out.
    /// </summary>
    [Trait("Category", "Unit")]
    public class YouTubeRefreshStaleTests : InMemoryDbTestBase
    {
        private static readonly DateTime Stale = DateTime.UtcNow.AddDays(-45);
        private static readonly DateTime Fresh = DateTime.UtcNow.AddDays(-5);

        private readonly IYouTubeApiClient _mockYouTubeApiClient = Substitute.For<IYouTubeApiClient>();
        private readonly YouTubeRefreshService _service;

        public YouTubeRefreshStaleTests()
        {
            _service = new YouTubeRefreshService(
                Context, _mockYouTubeApiClient, NullLogger<YouTubeRefreshService>.Instance);

            // Unless a test says otherwise, YouTube answers every videos request with every id asked for.
            _mockYouTubeApiClient.GetVideosAsync(Arg.Any<List<string>>())
                .Returns(call => call.Arg<List<string>>().Select(id => VideoDto(id, $"Title of {id}")).ToList());
        }

        private async Task<Video> AddVideoAsync(string title, string? externalId, DateTime? refreshedAt, Action<Video>? configure = null)
        {
            var video = TestDataFactory.CreateVideo(title);
            video.ExternalId = externalId;
            video.YouTubeRefreshedAt = refreshedAt;
            configure?.Invoke(video);
            Context.Videos.Add(video);
            await Context.SaveChangesAsync();
            return video;
        }

        private async Task<YouTubeChannel> AddChannelAsync(string title, string externalId, DateTime? syncedAt)
        {
            var channel = TestDataFactory.CreateYouTubeChannel(title, externalId);
            channel.LastSyncedAt = syncedAt;
            Context.YouTubeChannels.Add(channel);
            await Context.SaveChangesAsync();
            return channel;
        }

        private async Task<YouTubePlaylist> AddPlaylistAsync(string title, string externalId, DateTime? syncedAt)
        {
            var playlist = TestDataFactory.CreateYouTubePlaylist(title, externalId);
            playlist.LastSyncedAt = syncedAt;
            Context.YouTubePlaylists.Add(playlist);
            await Context.SaveChangesAsync();
            return playlist;
        }

        private static YouTubeVideoDto VideoDto(string id, string title, string? channelId = null) => new()
        {
            Id = id,
            Snippet = new YouTubeVideoSnippetDto { Title = title, ChannelId = channelId }
        };

        private static YouTubeChannelDto ChannelDto(string id, string title) => new()
        {
            Id = id,
            Snippet = new YouTubeChannelSnippetDto { Title = title }
        };

        private static YouTubePlaylistDto PlaylistDto(string id, string title) => new()
        {
            Id = id,
            Snippet = new YouTubePlaylistSnippetDto { Title = title }
        };

        private Task<YouTubeRefreshResultDto> RunAsync(int limit = 200, int olderThanDays = 30)
            => _service.RefreshStaleAsync(limit, olderThanDays);

        private Video Stored(Video video)
        {
            Context.ChangeTracker.Clear();
            return Context.Videos.First(v => v.Id == video.Id);
        }

        #region Selection

        [Fact]
        public async Task RefreshStaleAsync_ShouldRefreshStaleAndNeverStampedVideos_AndLeaveTheRestAlone()
        {
            var stale = await AddVideoAsync("Stale", "stale000001", Stale);
            var neverStamped = await AddVideoAsync("Never Stamped", "never000001", null);
            var fresh = await AddVideoAsync("Fresh", "fresh000001", Fresh);
            var noId = await AddVideoAsync("Added by hand", null, null);
            var blankId = await AddVideoAsync("Blank id", "", null);
            var vimeo = await AddVideoAsync("On Vimeo", "123456", null, v => v.Platform = "Vimeo");

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.Operation.Should().Be("youtube-refresh-stale");
            result.TotalProcessed.Should().Be(2);
            result.VideosProcessed.Should().Be(2);
            result.UpdatedCount.Should().Be(2);
            result.RemainingCount.Should().Be(0);
            result.QuotaExceeded.Should().BeFalse();
            result.CompletedAt.Should().NotBeNull();

            await _mockYouTubeApiClient.Received(1).GetVideosAsync(
                Arg.Is<List<string>>(ids => ids.Count == 2 && ids.Contains("stale000001") && ids.Contains("never000001")));

            Stored(stale).YouTubeRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            Stored(stale).Title.Should().Be("Title of stale000001");
            Stored(neverStamped).YouTubeRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            Stored(fresh).YouTubeRefreshedAt.Should().BeCloseTo(Fresh, TimeSpan.FromMinutes(1));
            Stored(fresh).Title.Should().Be("Fresh");
            Stored(noId).YouTubeRefreshedAt.Should().BeNull();
            Stored(blankId).YouTubeRefreshedAt.Should().BeNull();
            Stored(vimeo).YouTubeRefreshedAt.Should().BeNull();
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldTreatThePlatformNameCaseInsensitively()
        {
            var video = await AddVideoAsync("Lowercase platform", "lower000001", null, v => v.Platform = "youtube");

            var result = await RunAsync();

            result.VideosProcessed.Should().Be(1);
            Stored(video).YouTubeRefreshedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldRefreshChannelsAndPlaylists_ByTheirSyncStamp()
        {
            var staleChannel = await AddChannelAsync("Stale channel", "UCstale", Stale);
            var freshChannel = await AddChannelAsync("Fresh channel", "UCfresh", Fresh);
            var stalePlaylist = await AddPlaylistAsync("Stale playlist", "PLstale", null);
            var freshPlaylist = await AddPlaylistAsync("Fresh playlist", "PLfresh", Fresh);
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCstale").Returns(ChannelDto("UCstale", "Renamed channel"));
            _mockYouTubeApiClient.GetPlaylistDetailsAsync("PLstale").Returns(PlaylistDto("PLstale", "Renamed playlist"));

            var result = await RunAsync();

            result.ChannelsProcessed.Should().Be(1);
            result.PlaylistsProcessed.Should().Be(1);
            result.VideosProcessed.Should().Be(0);
            result.UpdatedCount.Should().Be(2);
            await _mockYouTubeApiClient.DidNotReceive().GetChannelDetailsAsync("UCfresh");
            await _mockYouTubeApiClient.DidNotReceive().GetPlaylistDetailsAsync("PLfresh");

            Context.ChangeTracker.Clear();
            var channel = Context.YouTubeChannels.First(c => c.Id == staleChannel.Id);
            channel.Title.Should().Be("Renamed channel");
            channel.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            Context.YouTubeChannels.First(c => c.Id == freshChannel.Id).Title.Should().Be("Fresh channel");
            var playlist = Context.YouTubePlaylists.First(p => p.Id == stalePlaylist.Id);
            playlist.Title.Should().Be("Renamed playlist");
            playlist.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            Context.YouTubePlaylists.First(p => p.Id == freshPlaylist.Id).Title.Should().Be("Fresh playlist");
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldShareTheLimit_ChannelsFirst_ThenPlaylists_ThenVideos()
        {
            await AddChannelAsync("Channel A", "UCa", null);
            await AddChannelAsync("Channel B", "UCb", Stale);
            await AddPlaylistAsync("Playlist A", "PLa", null);
            await AddPlaylistAsync("Playlist B", "PLb", Stale);
            await AddVideoAsync("Video A", "video00000a", null);
            await AddVideoAsync("Video B", "video00000b", Stale);
            _mockYouTubeApiClient.GetChannelDetailsAsync(Arg.Any<string>())
                .Returns(call => ChannelDto(call.Arg<string>(), "Channel"));
            _mockYouTubeApiClient.GetPlaylistDetailsAsync(Arg.Any<string>())
                .Returns(call => PlaylistDto(call.Arg<string>(), "Playlist"));

            var result = await RunAsync(limit: 3);

            result.ChannelsProcessed.Should().Be(2);
            result.PlaylistsProcessed.Should().Be(1);
            result.VideosProcessed.Should().Be(0);
            result.TotalProcessed.Should().Be(3);
            result.RemainingCount.Should().Be(3);

            // Within a kind, a never-stamped row goes before a stale one.
            await _mockYouTubeApiClient.Received(1).GetPlaylistDetailsAsync("PLa");
            await _mockYouTubeApiClient.DidNotReceive().GetPlaylistDetailsAsync("PLb");
            await _mockYouTubeApiClient.DidNotReceive().GetVideosAsync(Arg.Any<List<string>>());
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldTakeTheOldestVideosFirst_NeverStampedBeforeStale()
        {
            await AddVideoAsync("Stale", "stale000001", DateTime.UtcNow.AddDays(-40));
            await AddVideoAsync("Older", "older000001", DateTime.UtcNow.AddDays(-90));
            await AddVideoAsync("Never Stamped", "never000001", null);

            var result = await RunAsync(limit: 2);

            result.VideosProcessed.Should().Be(2);
            result.RemainingCount.Should().Be(1);
            await _mockYouTubeApiClient.Received(1).GetVideosAsync(
                Arg.Is<List<string>>(ids => ids.SequenceEqual(new[] { "never000001", "older000001" })));
        }

        [Fact]
        public async Task RefreshStaleAsync_NothingStale_ShouldReportZeroProcessed_AndCallNothing()
        {
            await AddVideoAsync("Fresh", "fresh000001", Fresh);
            await AddChannelAsync("Fresh channel", "UCfresh", Fresh);

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.TotalProcessed.Should().Be(0);
            result.RemainingCount.Should().Be(0);
            _mockYouTubeApiClient.ReceivedCalls().Should().BeEmpty();
        }

        #endregion

        #region Batching

        [Fact]
        public async Task RefreshStaleAsync_ShouldNeverAskForMoreThanFiftyVideosAtOnce()
        {
            for (var i = 0; i < 120; i++)
            {
                Context.Videos.Add(CreateStaleVideo($"Video {i}", $"batch{i:D6}"));
            }
            await Context.SaveChangesAsync();

            var batchSizes = new List<int>();
            _mockYouTubeApiClient.GetVideosAsync(Arg.Do<List<string>>(ids => batchSizes.Add(ids.Count)))
                .Returns(call => call.Arg<List<string>>().Select(id => VideoDto(id, "Title")).ToList());

            var result = await RunAsync();

            batchSizes.Should().Equal(50, 50, 20);
            result.VideosProcessed.Should().Be(120);
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldMatchAnswersById_NotByPosition()
        {
            var first = await AddVideoAsync("First", "first000001", null);
            var second = await AddVideoAsync("Second", "second00001", null);
            _mockYouTubeApiClient.GetVideosAsync(Arg.Any<List<string>>()).Returns(new List<YouTubeVideoDto>
            {
                VideoDto("second00001", "Second, renamed"),
                VideoDto("first000001", "First, renamed")
            });

            await RunAsync();

            Stored(first).Title.Should().Be("First, renamed");
            Stored(second).Title.Should().Be("Second, renamed");
        }

        private static Video CreateStaleVideo(string title, string externalId)
        {
            var video = TestDataFactory.CreateVideo(title);
            video.ExternalId = externalId;
            return video;
        }

        #endregion

        #region Field policy

        [Fact]
        public async Task RefreshStaleAsync_ShouldOverwriteYouTubeOwnedValues_AndLeavePersonalOnesAlone()
        {
            var video = await AddVideoAsync("Old title", "abc00000001", Stale, v =>
            {
                v.Description = "Old description";
                v.LengthInSeconds = 100;
                v.Thumbnail = "https://cdn.example.com/my-image.jpg";
                v.Status = Status.Completed;
                v.Rating = Rating.Like;
                v.Notes = "My notes";
                v.Link = "https://www.youtube.com/watch?v=abc00000001";
                v.Topics.Add(new Topic { Name = "music" });
            });
            _mockYouTubeApiClient.GetVideosAsync(Arg.Any<List<string>>()).Returns(new List<YouTubeVideoDto>
            {
                new()
                {
                    Id = "abc00000001",
                    Snippet = new YouTubeVideoSnippetDto
                    {
                        Title = "New title",
                        Description = "New description",
                        PublishedAt = new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Utc),
                        Thumbnails = new YouTubeThumbnailsDto { High = new YouTubeThumbnailDto { Url = "https://i.ytimg.com/vi/abc/new.jpg" } }
                    },
                    ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT4M13S" }
                }
            });

            var result = await RunAsync();

            result.UpdatedCount.Should().Be(1);
            Context.ChangeTracker.Clear();
            var updated = Context.Videos.Include(v => v.Topics).First(v => v.Id == video.Id);
            updated.Title.Should().Be("New title");
            updated.Description.Should().Be("New description");
            updated.LengthInSeconds.Should().Be(253);
            updated.PublishedAt.Should().Be(new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Utc));
            updated.Thumbnail.Should().Be("https://cdn.example.com/my-image.jpg");
            updated.Status.Should().Be(Status.Completed);
            updated.Rating.Should().Be(Rating.Like);
            updated.Notes.Should().Be("My notes");
            updated.Link.Should().Be("https://www.youtube.com/watch?v=abc00000001");
            updated.Topics.Select(t => t.Name).Should().Equal("music");
        }

        [Fact]
        public async Task RefreshStaleAsync_WhenYouTubeHasNothingNew_ShouldCountUnchanged_ButStillStamp()
        {
            var video = await AddVideoAsync("Same title", "same0000001", Stale);
            _mockYouTubeApiClient.GetVideosAsync(Arg.Any<List<string>>())
                .Returns(new List<YouTubeVideoDto> { VideoDto("same0000001", "Same title") });

            var result = await RunAsync();

            result.UnchangedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(0);
            Stored(video).YouTubeRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldLinkAVideoToItsChannel_WhenThatChannelIsStored()
        {
            var channel = await AddChannelAsync("Stored channel", "UCstored", Fresh);
            var otherChannel = await AddChannelAsync("Other channel", "UCother", Fresh);
            var unlinked = await AddVideoAsync("Unlinked", "unlinked001", null);
            var linked = await AddVideoAsync("Linked", "linked00001", null, v => v.ChannelId = otherChannel.Id);
            var channelNotStored = await AddVideoAsync("Channel not stored", "nochannel01", null);
            _mockYouTubeApiClient.GetVideosAsync(Arg.Any<List<string>>()).Returns(new List<YouTubeVideoDto>
            {
                VideoDto("unlinked001", "Unlinked", "UCstored"),
                VideoDto("linked00001", "Linked", "UCstored"),
                VideoDto("nochannel01", "Channel not stored", "UCelsewhere")
            });

            await RunAsync();

            Stored(unlinked).ChannelId.Should().Be(channel.Id);
            Stored(linked).ChannelId.Should().Be(otherChannel.Id);
            Stored(channelNotStored).ChannelId.Should().BeNull();
            await _mockYouTubeApiClient.DidNotReceive().GetChannelDetailsAsync(Arg.Any<string>());
        }

        #endregion

        #region Items YouTube no longer has

        [Fact]
        public async Task RefreshStaleAsync_VideoYouTubeNoLongerReturns_ShouldBeSkipped_Kept_AndStamped()
        {
            var gone = await AddVideoAsync("Removed Upstream", "gone0000001", null);
            var fine = await AddVideoAsync("Still There", "fine0000001", Stale);
            _mockYouTubeApiClient.GetVideosAsync(Arg.Any<List<string>>())
                .Returns(new List<YouTubeVideoDto> { VideoDto("fine0000001", "Still There, renamed") });

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.SkippedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            result.Errors.Should().BeEmpty();
            result.Warnings.Should().ContainSingle()
                .Which.Should().Contain("Removed Upstream").And.Contain("gone0000001");
            result.RemainingCount.Should().Be(0);

            var kept = Stored(gone);
            kept.Title.Should().Be("Removed Upstream");
            kept.YouTubeRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task RefreshStaleAsync_ChannelAndPlaylistYouTubeNoLongerReturns_ShouldBeSkipped_Kept_AndStamped()
        {
            var channel = await AddChannelAsync("Closed channel", "UCgone", null);
            var playlist = await AddPlaylistAsync("Deleted playlist", "PLgone", null);
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCgone").Returns((YouTubeChannelDto?)null);
            _mockYouTubeApiClient.GetPlaylistDetailsAsync("PLgone").Returns((YouTubePlaylistDto?)null);

            var result = await RunAsync();

            result.SkippedCount.Should().Be(2);
            result.Warnings.Should().HaveCount(2);
            result.Warnings.Should().Contain(w => w.Contains("Closed channel"));
            result.Warnings.Should().Contain(w => w.Contains("Deleted playlist"));
            result.RemainingCount.Should().Be(0);

            Context.ChangeTracker.Clear();
            var keptChannel = Context.YouTubeChannels.First(c => c.Id == channel.Id);
            keptChannel.Title.Should().Be("Closed channel");
            keptChannel.LastSyncedAt.Should().NotBeNull();
            var keptPlaylist = Context.YouTubePlaylists.First(p => p.Id == playlist.Id);
            keptPlaylist.Title.Should().Be("Deleted playlist");
            keptPlaylist.LastSyncedAt.Should().NotBeNull();
        }

        #endregion

        #region Failures

        [Fact]
        public async Task RefreshStaleAsync_WhenOneChannelFails_ShouldCountItFailed_LeaveItUnstamped_AndKeepGoing()
        {
            var broken = await AddChannelAsync("Broken", "UCbroken", null);
            var fine = await AddChannelAsync("Fine", "UCfine", Stale);
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCbroken").ThrowsAsync(new HttpRequestException("Service Unavailable"));
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCfine").Returns(ChannelDto("UCfine", "Fine, renamed"));

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.FailedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(1);
            result.ChannelsProcessed.Should().Be(2);
            result.Errors.Should().ContainSingle().Which.Should().Contain("Broken");
            result.RemainingCount.Should().Be(1);

            Context.ChangeTracker.Clear();
            Context.YouTubeChannels.First(c => c.Id == broken.Id).LastSyncedAt.Should().BeNull();
            Context.YouTubeChannels.First(c => c.Id == fine.Id).LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task RefreshStaleAsync_WhenAVideoBatchFails_ShouldCountTheWholeBatchFailed_AndTryTheNextOne()
        {
            for (var i = 0; i < 60; i++)
            {
                Context.Videos.Add(CreateStaleVideo($"Video {i}", $"batch{i:D6}"));
            }
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetVideosAsync(Arg.Is<List<string>>(ids => ids.Count == 50))
                .ThrowsAsync(new HttpRequestException("Service Unavailable"));

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.FailedCount.Should().Be(50);
            result.UpdatedCount.Should().Be(10);
            result.VideosProcessed.Should().Be(60);
            result.Errors.Should().ContainSingle().Which.Should().Contain("50 videos");
            result.RemainingCount.Should().Be(50);
        }

        [Fact]
        public async Task RefreshStaleAsync_ShouldCapErrorsAtTwenty()
        {
            for (var i = 1; i <= 25; i++)
            {
                await AddChannelAsync($"Channel {i}", $"UC{i}", null);
            }
            _mockYouTubeApiClient.GetChannelDetailsAsync(Arg.Any<string>())
                .ThrowsAsync(new HttpRequestException("Service Unavailable"));

            var result = await RunAsync();

            result.FailedCount.Should().Be(25);
            result.Errors.Should().HaveCount(20);
        }

        [Fact]
        public async Task RefreshStaleAsync_WhenTheQuotaRunsOut_ShouldStop_SaveWhatWasDone_AndStillSucceed()
        {
            var first = await AddChannelAsync("First", "UCfirst", null);
            var second = await AddChannelAsync("Second", "UCsecond", Stale);
            var playlist = await AddPlaylistAsync("Playlist", "PLone", null);
            var video = await AddVideoAsync("Video", "video000001", null);
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCfirst").Returns(ChannelDto("UCfirst", "First, renamed"));
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCsecond")
                .ThrowsAsync(new YouTubeQuotaExceededException("quota used up", "quotaExceeded"));

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.QuotaExceeded.Should().BeTrue();
            result.UpdatedCount.Should().Be(1);
            result.FailedCount.Should().Be(0);
            result.TotalProcessed.Should().Be(1);
            result.RemainingCount.Should().Be(3);
            result.CompletedAt.Should().NotBeNull();
            result.Warnings.Should().ContainSingle().Which.Should().Contain("quota");
            await _mockYouTubeApiClient.DidNotReceive().GetPlaylistDetailsAsync(Arg.Any<string>());
            await _mockYouTubeApiClient.DidNotReceive().GetVideosAsync(Arg.Any<List<string>>());

            Context.ChangeTracker.Clear();
            var saved = Context.YouTubeChannels.First(c => c.Id == first.Id);
            saved.Title.Should().Be("First, renamed");
            saved.LastSyncedAt.Should().NotBeNull();
            Context.YouTubeChannels.First(c => c.Id == second.Id).LastSyncedAt.Should().BeCloseTo(Stale, TimeSpan.FromMinutes(1));
            Context.YouTubePlaylists.First(p => p.Id == playlist.Id).LastSyncedAt.Should().BeNull();
            Context.Videos.First(v => v.Id == video.Id).YouTubeRefreshedAt.Should().BeNull();
        }

        [Fact]
        public async Task RefreshStaleAsync_WhenTheQuotaRunsOutBetweenVideoBatches_ShouldKeepTheBatchesAlreadyDone()
        {
            for (var i = 0; i < 60; i++)
            {
                Context.Videos.Add(CreateStaleVideo($"Video {i}", $"batch{i:D6}"));
            }
            await Context.SaveChangesAsync();
            _mockYouTubeApiClient.GetVideosAsync(Arg.Is<List<string>>(ids => ids.Count == 10))
                .ThrowsAsync(new YouTubeQuotaExceededException("quota used up", "quotaExceeded"));

            var result = await RunAsync();

            result.Success.Should().BeTrue();
            result.QuotaExceeded.Should().BeTrue();
            result.UpdatedCount.Should().Be(50);
            result.RemainingCount.Should().Be(10);
            Context.ChangeTracker.Clear();
            Context.Videos.Count(v => v.YouTubeRefreshedAt != null).Should().Be(50);
        }

        [Fact]
        public async Task RefreshStaleAsync_WithoutAnApiKey_ShouldAbortTheRun()
        {
            var channel = await AddChannelAsync("Channel", "UCone", null);
            _mockYouTubeApiClient.GetChannelDetailsAsync(Arg.Any<string>()).ThrowsAsync(new YouTubeNotConfiguredException());

            var result = await RunAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not configured");
            result.CompletedAt.Should().BeNull();
            Context.ChangeTracker.Clear();
            Context.YouTubeChannels.First(c => c.Id == channel.Id).LastSyncedAt.Should().BeNull();
        }

        [Fact]
        public async Task RefreshStaleAsync_WhenCancelled_ShouldStopWithAWarning_AndSaveWhatWasDone()
        {
            using var cancellation = new CancellationTokenSource();
            var first = await AddChannelAsync("First", "UCfirst", null);
            var second = await AddChannelAsync("Second", "UCsecond", Stale);
            _mockYouTubeApiClient.GetChannelDetailsAsync("UCfirst").Returns(call =>
            {
                cancellation.Cancel();
                return ChannelDto("UCfirst", "First, renamed");
            });

            var result = await _service.RefreshStaleAsync(200, 30, cancellation.Token);

            result.Success.Should().BeTrue();
            result.UpdatedCount.Should().Be(1);
            result.Warnings.Should().ContainSingle().Which.Should().Contain("canceled");
            await _mockYouTubeApiClient.DidNotReceive().GetChannelDetailsAsync("UCsecond");

            Context.ChangeTracker.Clear();
            Context.YouTubeChannels.First(c => c.Id == first.Id).Title.Should().Be("First, renamed");
            Context.YouTubeChannels.First(c => c.Id == second.Id).Title.Should().Be("Second");
        }

        #endregion
    }
}
