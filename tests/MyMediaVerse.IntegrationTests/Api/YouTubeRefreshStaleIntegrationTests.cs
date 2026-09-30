using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The YouTube refresh endpoint against a real PostgreSQL database, with only the YouTube
    /// client substituted: candidate selection and ordering, stored timestamps, and the
    /// contract body all run through the real query provider.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class YouTubeRefreshStaleIntegrationTests : IAsyncLifetime
    {
        private const string Endpoint = "/api/youtube/refresh-stale";

        private static readonly DateTime Stale = DateTime.UtcNow.AddDays(-45);
        private static readonly DateTime Fresh = DateTime.UtcNow.AddDays(-5);

        private readonly ApiFactory _factory;

        public YouTubeRefreshStaleIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region Helpers

        private static Video Video(string title, string? externalId, DateTime? refreshedAt, string platform = "YouTube") => new()
        {
            Title = title,
            ExternalId = externalId,
            Platform = platform,
            MediaType = MediaType.Video,
            YouTubeRefreshedAt = refreshedAt
        };

        private static YouTubeChannel Channel(string title, string externalId, DateTime? syncedAt) => new()
        {
            Title = title,
            ChannelExternalId = externalId,
            MediaType = MediaType.Channel,
            LastSyncedAt = syncedAt
        };

        private static YouTubePlaylist Playlist(string title, string externalId, DateTime? syncedAt) => new()
        {
            Title = title,
            PlaylistExternalId = externalId,
            MediaType = MediaType.Playlist,
            LastSyncedAt = syncedAt
        };

        private static YouTubeVideoDto VideoDto(string id, string title, string? channelId = null) => new()
        {
            Id = id,
            Snippet = new YouTubeVideoSnippetDto
            {
                Title = title,
                ChannelId = channelId,
                PublishedAt = new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Unspecified)
            },
            ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT4M13S" }
        };

        private (HttpClient Client, IYouTubeApiClient YouTube) ClientWithYouTube(Action<IYouTubeApiClient> configure) =>
            _factory.CreateClientWithSubstitute(configure);

        private async Task<T> FromDatabase<T>(Func<MediaLibraryDbContext, Task<T>> query)
        {
            using var scope = _factory.Services.CreateScope();
            return await query(scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>());
        }

        private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content).RootElement.Clone();
        }

        #endregion

        #region Access and validation

        [Fact]
        public async Task RefreshStale_ShouldReturnUnauthorized_WithoutToken()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsync(Endpoint, null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("limit=0")]
        [InlineData("limit=1001")]
        [InlineData("olderThanDays=-1")]
        [InlineData("olderThanDays=3651")]
        public async Task RefreshStale_ShouldRejectOutOfRangeParameters(string query)
        {
            var (client, youTube) = ClientWithYouTube(_ => { });

            var response = await client.PostAsync($"{Endpoint}?{query}", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await ReadBodyAsync(response);
            body.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
            youTube.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task RefreshStale_OnEmptyLibrary_ShouldReturnOkWithContractBody()
        {
            var (client, youTube) = ClientWithYouTube(_ => { });

            var response = await client.PostAsync(Endpoint, null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("operation").GetString().Should().Be("youtube-refresh-stale");
            body.GetProperty("updatedCount").GetInt32().Should().Be(0);
            body.GetProperty("unchangedCount").GetInt32().Should().Be(0);
            body.GetProperty("skippedCount").GetInt32().Should().Be(0);
            body.GetProperty("failedCount").GetInt32().Should().Be(0);
            body.GetProperty("totalProcessed").GetInt32().Should().Be(0);
            body.GetProperty("remainingCount").GetInt32().Should().Be(0);
            body.GetProperty("videosProcessed").GetInt32().Should().Be(0);
            body.GetProperty("channelsProcessed").GetInt32().Should().Be(0);
            body.GetProperty("playlistsProcessed").GetInt32().Should().Be(0);
            body.GetProperty("quotaExceeded").GetBoolean().Should().BeFalse();
            body.GetProperty("errors").GetArrayLength().Should().Be(0);
            body.GetProperty("warnings").GetArrayLength().Should().Be(0);
            body.GetProperty("reindexTriggered").GetBoolean().Should().BeFalse();
            body.TryGetProperty("startedAt", out _).Should().BeTrue();
            body.TryGetProperty("completedAt", out _).Should().BeTrue();
            youTube.ReceivedCalls().Should().BeEmpty();
        }

        #endregion

        #region The run

        [Fact]
        public async Task RefreshStale_ShouldRefreshStaleRowsOfAllThreeKinds_AndLeaveFreshOnesAlone()
        {
            await TestDataSeeder.AddAsync(_factory,
                Channel("Stale channel", "UCstale", Stale),
                Channel("Fresh channel", "UCfresh", Fresh));
            await TestDataSeeder.AddAsync(_factory,
                Playlist("Never synced playlist", "PLnever", null),
                Playlist("Fresh playlist", "PLfresh", Fresh));
            await TestDataSeeder.AddAsync(_factory,
                Video("Stale video", "stale000001", Stale),
                Video("Never refreshed video", "never000001", null),
                Video("Fresh video", "fresh000001", Fresh),
                Video("Added by hand", null, null),
                Video("On Vimeo", "123456", null, platform: "Vimeo"));

            var (client, youTube) = ClientWithYouTube(mock =>
            {
                mock.GetChannelDetailsAsync("UCstale").Returns(new YouTubeChannelDto
                {
                    Id = "UCstale",
                    Snippet = new YouTubeChannelSnippetDto { Title = "Renamed channel" },
                    Statistics = new YouTubeChannelStatisticsDto { SubscriberCount = "2000" }
                });
                mock.GetPlaylistDetailsAsync("PLnever").Returns(new YouTubePlaylistDto
                {
                    Id = "PLnever",
                    Snippet = new YouTubePlaylistSnippetDto { Title = "Renamed playlist" }
                });
                mock.GetVideosAsync(Arg.Any<List<string>>()).Returns(call =>
                    call.Arg<List<string>>().Select(id => VideoDto(id, $"Title of {id}", "UCstale")).ToList());
            });

            var response = await client.PostAsync(Endpoint, null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("updatedCount").GetInt32().Should().Be(4);
            body.GetProperty("channelsProcessed").GetInt32().Should().Be(1);
            body.GetProperty("playlistsProcessed").GetInt32().Should().Be(1);
            body.GetProperty("videosProcessed").GetInt32().Should().Be(2);
            body.GetProperty("remainingCount").GetInt32().Should().Be(0);
            body.GetProperty("reindexTriggered").GetBoolean().Should().BeTrue();

            // Never-refreshed rows go first.
            await youTube.Received(1).GetVideosAsync(
                Arg.Is<List<string>>(ids => ids.SequenceEqual(new[] { "never000001", "stale000001" })));
            await youTube.DidNotReceive().GetChannelDetailsAsync("UCfresh");
            await youTube.DidNotReceive().GetPlaylistDetailsAsync("PLfresh");

            var channels = await FromDatabase(db => db.YouTubeChannels.AsNoTracking().ToListAsync());
            var staleChannel = channels.Single(c => c.ChannelExternalId == "UCstale");
            staleChannel.Title.Should().Be("Renamed channel");
            staleChannel.SubscriberCount.Should().Be(2000);
            staleChannel.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            channels.Single(c => c.ChannelExternalId == "UCfresh").Title.Should().Be("Fresh channel");

            var playlists = await FromDatabase(db => db.YouTubePlaylists.AsNoTracking().ToListAsync());
            playlists.Single(p => p.PlaylistExternalId == "PLnever").Title.Should().Be("Renamed playlist");
            playlists.Single(p => p.PlaylistExternalId == "PLnever").LastSyncedAt.Should().NotBeNull();
            playlists.Single(p => p.PlaylistExternalId == "PLfresh").Title.Should().Be("Fresh playlist");

            var videos = await FromDatabase(db => db.Videos.AsNoTracking().ToListAsync());
            var staleVideo = videos.Single(v => v.ExternalId == "stale000001");
            staleVideo.Title.Should().Be("Title of stale000001");
            staleVideo.LengthInSeconds.Should().Be(253);
            staleVideo.PublishedAt.Should().Be(new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Utc));
            staleVideo.ChannelId.Should().Be(staleChannel.Id);
            staleVideo.YouTubeRefreshedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            videos.Single(v => v.ExternalId == "never000001").YouTubeRefreshedAt.Should().NotBeNull();
            videos.Single(v => v.ExternalId == "fresh000001").Title.Should().Be("Fresh video");
            videos.Single(v => v.Title == "Added by hand").YouTubeRefreshedAt.Should().BeNull();
            videos.Single(v => v.Title == "On Vimeo").YouTubeRefreshedAt.Should().BeNull();
        }

        [Fact]
        public async Task RefreshStale_RunTwice_ShouldFindNothingLeftTheSecondTime()
        {
            await TestDataSeeder.AddAsync(_factory, Video("Stale video", "stale000001", Stale));
            var (client, youTube) = ClientWithYouTube(mock =>
                mock.GetVideosAsync(Arg.Any<List<string>>()).Returns(call =>
                    call.Arg<List<string>>().Select(id => VideoDto(id, "Renamed")).ToList()));

            var first = await ReadBodyAsync(await client.PostAsync(Endpoint, null));
            var second = await ReadBodyAsync(await client.PostAsync(Endpoint, null));

            first.GetProperty("totalProcessed").GetInt32().Should().Be(1);
            second.GetProperty("totalProcessed").GetInt32().Should().Be(0);
            second.GetProperty("reindexTriggered").GetBoolean().Should().BeFalse();
            await youTube.Received(1).GetVideosAsync(Arg.Any<List<string>>());
        }

        [Fact]
        public async Task RefreshStale_ItemYouTubeNoLongerReturns_ShouldBeKept_Stamped_AndReportedAsSkipped()
        {
            await TestDataSeeder.AddAsync(_factory, Video("Removed Upstream", "gone0000001", null));
            var (client, _) = ClientWithYouTube(mock =>
                mock.GetVideosAsync(Arg.Any<List<string>>()).Returns(new List<YouTubeVideoDto>()));

            var response = await client.PostAsync(Endpoint, null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("skippedCount").GetInt32().Should().Be(1);
            body.GetProperty("remainingCount").GetInt32().Should().Be(0);
            body.GetProperty("warnings")[0].GetString().Should().Contain("Removed Upstream");
            body.GetProperty("reindexTriggered").GetBoolean().Should().BeFalse();

            var kept = await FromDatabase(db => db.Videos.AsNoTracking().SingleAsync());
            kept.Title.Should().Be("Removed Upstream");
            kept.YouTubeRefreshedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task RefreshStale_ShouldHonorTheLimit_AndReportWhatRemains()
        {
            await TestDataSeeder.AddAsync(_factory,
                Video("Oldest", "oldest00001", DateTime.UtcNow.AddDays(-90)),
                Video("Older", "older000001", DateTime.UtcNow.AddDays(-60)),
                Video("Stale", "stale000001", DateTime.UtcNow.AddDays(-40)));
            var (client, youTube) = ClientWithYouTube(mock =>
                mock.GetVideosAsync(Arg.Any<List<string>>()).Returns(call =>
                    call.Arg<List<string>>().Select(id => VideoDto(id, "Renamed")).ToList()));

            var response = await client.PostAsync($"{Endpoint}?limit=2", null);

            var body = await ReadBodyAsync(response);
            body.GetProperty("totalProcessed").GetInt32().Should().Be(2);
            body.GetProperty("remainingCount").GetInt32().Should().Be(1);
            await youTube.Received(1).GetVideosAsync(
                Arg.Is<List<string>>(ids => ids.SequenceEqual(new[] { "oldest00001", "older000001" })));
        }

        #endregion
    }
}
