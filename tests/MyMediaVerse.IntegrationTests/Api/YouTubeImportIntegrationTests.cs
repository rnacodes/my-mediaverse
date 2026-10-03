using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The import endpoints' contract: 201 with the stored item when the import added it, 200
    /// with the stored item when it was already in the library, one library row either way,
    /// and a search reindex only for what an import added.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class YouTubeImportIntegrationTests : IAsyncLifetime
    {
        private const string VideoId = "dQw4w9WgXcQ";
        private const string ChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw";
        private const string PlaylistId = "PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf";

        private readonly ApiFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubeImportIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        #region Helpers

        // Real services and a real database; only YouTube itself is substituted.
        private (HttpClient Client, IYouTubeApiClient YouTube) ClientWithYouTube()
        {
            return _factory.CreateClientWithSubstitute<IYouTubeApiClient>(mock =>
            {
                mock.GetVideoDetailsAsync(VideoId).Returns(new YouTubeVideoDto
                {
                    Id = VideoId,
                    Snippet = new YouTubeVideoSnippetDto
                    {
                        Title = "Never Gonna Give You Up",
                        Description = "The official video",
                        ChannelId = ChannelId,
                        ChannelTitle = "Rick Astley",
                        PublishedAt = new DateTime(2009, 10, 25, 6, 57, 33, DateTimeKind.Utc)
                    },
                    ContentDetails = new YouTubeVideoContentDetailsDto { Duration = "PT3M33S" }
                });
                mock.GetChannelDetailsAsync(ChannelId).Returns(new YouTubeChannelDto
                {
                    Id = ChannelId,
                    Snippet = new YouTubeChannelSnippetDto { Title = "Rick Astley" },
                    Statistics = new YouTubeChannelStatisticsDto { SubscriberCount = "4000000" }
                });
                mock.GetPlaylistDetailsAsync(PlaylistId).Returns(new YouTubePlaylistDto
                {
                    Id = PlaylistId,
                    Snippet = new YouTubePlaylistSnippetDto { Title = "Favorites" },
                    ContentDetails = new YouTubePlaylistContentDetailsDto { ItemCount = 12 }
                });
            });
        }

        private static Video StoredVideo(string title = "Never Gonna Give You Up") => new()
        {
            Title = title,
            ExternalId = VideoId,
            Platform = "YouTube",
            MediaType = MediaType.Video
        };

        private Task<HttpResponseMessage> ImportUrl(HttpClient client, string url) =>
            client.PostAsJsonAsync("/api/youtube/import/url", new { url }, _jsonOptions);

        private async Task<T> Read<T>(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<T>(_jsonOptions))!;

        private async Task<List<T>> GetAll<T>(HttpClient client, string path) =>
            (await client.GetFromJsonAsync<List<T>>(path, _jsonOptions))!;

        #endregion

        #region Video import, end to end

        [Fact]
        public async Task ImportVideo_Twice_ShouldReturnCreatedThenOk_AndStoreOneRow()
        {
            var (client, youTube) = ClientWithYouTube();

            var first = await client.PostAsync($"/api/youtube/import/video/{VideoId}", null);
            var second = await client.PostAsync($"/api/youtube/import/video/{VideoId}", null);

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.OK);

            var created = await Read<VideoResponseDto>(first);
            var existing = await Read<VideoResponseDto>(second);
            created.Title.Should().Be("Never Gonna Give You Up");
            created.MediaType.Should().Be(MediaType.Video);
            created.Platform.Should().Be("YouTube");
            created.ExternalId.Should().Be(VideoId);
            created.LengthInSeconds.Should().Be(213);
            created.PublishedAt.Should().Be(new DateTime(2009, 10, 25, 6, 57, 33, DateTimeKind.Utc));
            created.ChannelId.Should().NotBeNull();
            first.Headers.Location!.ToString().ToLowerInvariant().Should().EndWith($"/api/video/{created.Id}");
            existing.Id.Should().Be(created.Id);
            existing.Channel.Should().NotBeNull();
            existing.Channel!.ChannelExternalId.Should().Be(ChannelId);

            (await GetAll<VideoResponseDto>(client, "/api/video")).Should().ContainSingle();
            (await GetAll<YouTubeChannelResponseDto>(client, "/api/youtubechannel")).Should().ContainSingle();

            // The second import found the stored id and never asked YouTube.
            await youTube.Received(1).GetVideoDetailsAsync(VideoId);
        }

        [Fact]
        public async Task ImportVideo_ShouldNotSendOwnershipInTheBody()
        {
            var (client, _) = ClientWithYouTube();

            var response = await client.PostAsync($"/api/youtube/import/video/{VideoId}", null);

            var body = await Read<JsonElement>(response);
            body.TryGetProperty("ownershipStatus", out _).Should().BeFalse();
            body.TryGetProperty("id", out _).Should().BeTrue();
            body.TryGetProperty("mediaType", out _).Should().BeTrue();
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc")]
        [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ")]
        public async Task ImportFromUrl_ForAVideoAlreadyStored_ShouldReturnOkWithTheStoredVideo(string url)
        {
            var (client, youTube) = ClientWithYouTube();
            var created = await Read<VideoResponseDto>(
                await client.PostAsync($"/api/youtube/import/video/{VideoId}", null));
            youTube.ClearReceivedCalls();

            var response = await ImportUrl(client, url);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await Read<VideoResponseDto>(response)).Id.Should().Be(created.Id);
            (await GetAll<VideoResponseDto>(client, "/api/video")).Should().ContainSingle();
            youTube.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task ImportFromUrl_ForAChannelAndAPlaylist_ShouldReturnTheirOwnBodies_CreatedThenOk()
        {
            var (client, _) = ClientWithYouTube();

            var channelFirst = await ImportUrl(client, $"https://www.youtube.com/channel/{ChannelId}");
            var channelSecond = await ImportUrl(client, $"https://www.youtube.com/channel/{ChannelId}");
            var playlistFirst = await ImportUrl(client, $"https://www.youtube.com/playlist?list={PlaylistId}");
            var playlistSecond = await ImportUrl(client, $"https://www.youtube.com/playlist?list={PlaylistId}");

            channelFirst.StatusCode.Should().Be(HttpStatusCode.Created);
            channelSecond.StatusCode.Should().Be(HttpStatusCode.OK);
            var channel = await Read<YouTubeChannelResponseDto>(channelFirst);
            channel.MediaType.Should().Be(MediaType.Channel);
            channel.ChannelExternalId.Should().Be(ChannelId);
            channel.SubscriberCount.Should().Be(4000000);
            (await Read<YouTubeChannelResponseDto>(channelSecond)).Id.Should().Be(channel.Id);

            playlistFirst.StatusCode.Should().Be(HttpStatusCode.Created);
            playlistSecond.StatusCode.Should().Be(HttpStatusCode.OK);
            var playlist = await Read<YouTubePlaylistResponseDto>(playlistFirst);
            playlist.MediaType.Should().Be(MediaType.Playlist);
            playlist.PlaylistExternalId.Should().Be(PlaylistId);
            playlist.VideoCount.Should().Be(12);
            (await Read<YouTubePlaylistResponseDto>(playlistSecond)).Id.Should().Be(playlist.Id);
        }

        [Fact]
        public async Task ImportChannel_And_ImportPlaylist_Twice_ShouldReturnCreatedThenOk()
        {
            var (client, youTube) = ClientWithYouTube();

            var channelFirst = await client.PostAsync($"/api/youtubechannel/import/{ChannelId}", null);
            var channelSecond = await client.PostAsync($"/api/youtubechannel/import/{ChannelId}", null);
            var playlistFirst = await client.PostAsync($"/api/youtubeplaylist/import/{PlaylistId}", null);
            var playlistSecond = await client.PostAsync($"/api/youtubeplaylist/import/{PlaylistId}", null);

            channelFirst.StatusCode.Should().Be(HttpStatusCode.Created);
            channelSecond.StatusCode.Should().Be(HttpStatusCode.OK);
            playlistFirst.StatusCode.Should().Be(HttpStatusCode.Created);
            playlistSecond.StatusCode.Should().Be(HttpStatusCode.OK);

            var channel = await Read<YouTubeChannelResponseDto>(channelFirst);
            channelFirst.Headers.Location!.ToString().ToLowerInvariant().Should().EndWith($"/api/youtubechannel/{channel.Id}");
            var playlist = await Read<YouTubePlaylistResponseDto>(playlistFirst);
            playlistFirst.Headers.Location!.ToString().ToLowerInvariant().Should().EndWith($"/api/youtubeplaylist/{playlist.Id}");

            (await GetAll<YouTubeChannelResponseDto>(client, "/api/youtubechannel")).Should().ContainSingle();
            (await GetAll<YouTubePlaylistResponseDto>(client, "/api/youtubeplaylist")).Should().ContainSingle();
            await youTube.Received(1).GetChannelDetailsAsync(ChannelId);
            await youTube.Received(1).GetPlaylistDetailsAsync(PlaylistId);
        }

        #endregion

        #region Bad ids

        [Theory]
        [InlineData("short")]
        [InlineData("twelve_chars")]
        [InlineData("has.a.dot.x")]
        public async Task ImportVideo_WithAnIdThatIsNotAVideoId_ShouldReturnBadRequest_WithoutCallingYouTube(string videoId)
        {
            var (client, service) = _factory.CreateClientWithSubstitute<IYouTubeService>();

            var response = await client.PostAsync($"/api/youtube/import/video/{videoId}", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await Read<JsonElement>(response)).GetProperty("error").GetString().Should().Contain(videoId);
            service.ReceivedCalls().Should().BeEmpty();
        }

        #endregion

        #region Search reindex

        [Fact]
        public async Task ImportVideo_ThatWasAdded_ShouldReindexTheVideo_AndTheChannelItBroughtAlong()
        {
            var video = StoredVideo();
            var channel = new YouTubeChannel { Title = "Rick Astley", ChannelExternalId = ChannelId, MediaType = MediaType.Channel };
            var (client, _, reindex) = _factory.CreateClientWithSubstitutes<IYouTubeService, IImportReindexService>(
                service => service.ImportVideoAsync(VideoId).Returns(new YouTubeImportResult(video, true, channel)));

            var response = await client.PostAsync($"/api/youtube/import/video/{VideoId}", null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            await reindex.Received(1).ReindexItemAfterImportAsync(video.Id, Arg.Any<string>());
            await reindex.Received(1).ReindexItemAfterImportAsync(channel.Id, Arg.Any<string>());
        }

        [Fact]
        public async Task ImportVideo_ThatWasAlreadyStored_ShouldNotReindex()
        {
            var video = StoredVideo();
            var (client, _, reindex) = _factory.CreateClientWithSubstitutes<IYouTubeService, IImportReindexService>(
                service => service.ImportVideoAsync(VideoId).Returns(new YouTubeImportResult(video, false)));

            var response = await client.PostAsync($"/api/youtube/import/video/{VideoId}", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            reindex.ReceivedCalls().Should().BeEmpty();
        }

        [Fact]
        public async Task ImportChannel_And_ImportPlaylist_ShouldReindexOnlyWhatWasAdded()
        {
            var added = new YouTubeChannel { Title = "Added", ChannelExternalId = "UCadded", MediaType = MediaType.Channel };
            var stored = new YouTubeChannel { Title = "Stored", ChannelExternalId = "UCstored", MediaType = MediaType.Channel };
            var (channelClient, _, channelReindex) = _factory.CreateClientWithSubstitutes<IYouTubeChannelService, IImportReindexService>(
                service =>
                {
                    service.ImportChannelFromYouTubeAsync("UCadded").Returns(new YouTubeChannelCreationResult(added, true));
                    service.ImportChannelFromYouTubeAsync("UCstored").Returns(new YouTubeChannelCreationResult(stored, false));
                });

            await channelClient.PostAsync("/api/youtubechannel/import/UCadded", null);
            await channelClient.PostAsync("/api/youtubechannel/import/UCstored", null);

            await channelReindex.Received(1).ReindexItemAfterImportAsync(added.Id, Arg.Any<string>());
            await channelReindex.DidNotReceive().ReindexItemAfterImportAsync(stored.Id, Arg.Any<string>());

            var addedPlaylist = new YouTubePlaylist { Title = "Added", PlaylistExternalId = "PLadded", MediaType = MediaType.Playlist };
            var storedPlaylist = new YouTubePlaylist { Title = "Stored", PlaylistExternalId = "PLstored", MediaType = MediaType.Playlist };
            var (playlistClient, _, playlistReindex) = _factory.CreateClientWithSubstitutes<IYouTubePlaylistService, IImportReindexService>(
                service =>
                {
                    service.ImportPlaylistFromYouTubeAsync("PLadded").Returns(new YouTubePlaylistCreationResult(addedPlaylist, true));
                    service.ImportPlaylistFromYouTubeAsync("PLstored").Returns(new YouTubePlaylistCreationResult(storedPlaylist, false));
                });

            await playlistClient.PostAsync("/api/youtubeplaylist/import/PLadded", null);
            await playlistClient.PostAsync("/api/youtubeplaylist/import/PLstored", null);

            await playlistReindex.Received(1).ReindexItemAfterImportAsync(addedPlaylist.Id, Arg.Any<string>());
            await playlistReindex.DidNotReceive().ReindexItemAfterImportAsync(storedPlaylist.Id, Arg.Any<string>());
        }

        #endregion

        #region Access and rate limits

        [Theory]
        [InlineData("/api/youtube/import/video/dQw4w9WgXcQ")]
        [InlineData("/api/youtube/import/url")]
        [InlineData("/api/youtubechannel/import/UCuAXFkgsw1L7xaCfnd5JJOw")]
        [InlineData("/api/youtubechannel/00000000-0000-0000-0000-000000000001/sync")]
        [InlineData("/api/youtubeplaylist/import/PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf")]
        [InlineData("/api/youtubeplaylist/00000000-0000-0000-0000-000000000001/sync")]
        public async Task ImportAndSyncEndpoints_ShouldReturnUnauthorized_WithoutToken(string path)
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsync(path, null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Search_ShouldAllowTwentyCallsAnHourPerVisitor_ThenReturn429()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IYouTubeService>(mock =>
                mock.SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>())
                    .Returns(new YouTubeSearchResultDto()));

            async Task<HttpResponseMessage> SearchFrom(string ip)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/youtube/search?query=test");
                request.Headers.Add("CF-Connecting-IP", ip);
                return await client.SendAsync(request);
            }

            for (var i = 0; i < 20; i++)
            {
                (await SearchFrom("203.0.113.10")).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            (await SearchFrom("203.0.113.10")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            (await SearchFrom("203.0.113.11")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion
    }
}
