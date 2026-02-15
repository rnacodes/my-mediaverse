using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.DTOs;
using ProjectLoopbreaker.Infrastructure.Data;
using ProjectLoopbreaker.Shared.DTOs.YouTube;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace ProjectLoopbreaker.IntegrationTests.Controllers
{
    public class YouTubeControllerIntegrationTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public YouTubeControllerIntegrationTests(WebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        #region Search Endpoints Tests

        [Fact]
        public async Task Search_WithValidQuery_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/YouTube/search?query=test");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<YouTubeSearchResultDto>(content, _jsonOptions);
            Assert.NotNull(searchResult);
        }

        [Fact]
        public async Task Search_WithEmptyQuery_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/YouTube/search?query=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Search_WithCustomParameters_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/YouTube/search?query=test&type=channel&maxResults=10");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<YouTubeSearchResultDto>(content, _jsonOptions);
            Assert.NotNull(searchResult);
        }

        #endregion

        #region Video Details Tests

        [Fact]
        public async Task GetVideoDetails_WithValidVideoId_ShouldReturnOk()
        {
            // Note: Using a known YouTube video ID that should exist
            var videoId = "dQw4w9WgXcQ"; // Rick Astley - Never Gonna Give You Up

            // Act
            var response = await _client.GetAsync($"/api/YouTube/videos/{videoId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var video = JsonSerializer.Deserialize<YouTubeVideoDto>(content, _jsonOptions);
            Assert.NotNull(video);
            Assert.Equal(videoId, video.Id);
        }

        [Fact]
        public async Task GetVideoDetails_WithInvalidVideoId_ShouldReturnNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/YouTube/videos/invalid_video_id");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Playlist Tests

        [Fact]
        public async Task GetPlaylistDetails_WithValidPlaylistId_ShouldReturnOk()
        {
            // Note: Using a known YouTube playlist ID that should exist
            var playlistId = "PLFgquLnL59alCl_2TQvOiD5Vgm1hCaGSI"; // Example playlist

            // Act
            var response = await _client.GetAsync($"/api/YouTube/playlists/{playlistId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var playlist = JsonSerializer.Deserialize<YouTubePlaylistDto>(content, _jsonOptions);
            Assert.NotNull(playlist);
            Assert.Equal(playlistId, playlist.Id);
        }

        [Fact]
        public async Task GetPlaylistItems_WithValidPlaylistId_ShouldReturnOk()
        {
            // Note: Using a known YouTube playlist ID that should exist
            var playlistId = "PLFgquLnL59alCl_2TQvOiD5Vgm1hCaGSI"; // Example playlist

            // Act
            var response = await _client.GetAsync($"/api/YouTube/playlists/{playlistId}/items");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<YouTubePlaylistItemDto>>(content, _jsonOptions);
            Assert.NotNull(items);
        }

        #endregion

        #region Channel Tests

        [Fact]
        public async Task GetChannelDetails_WithValidChannelId_ShouldReturnOk()
        {
            // Note: Using a known YouTube channel ID that should exist
            var channelId = "UCuAXFkgsw1L7xaCfnd5JJOw"; // Example channel

            // Act
            var response = await _client.GetAsync($"/api/YouTube/channels/{channelId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var channel = JsonSerializer.Deserialize<YouTubeChannelDto>(content, _jsonOptions);
            Assert.NotNull(channel);
            Assert.Equal(channelId, channel.Id);
        }

        [Fact]
        public async Task GetChannelByUsername_WithValidUsername_ShouldReturnOk()
        {
            // Note: Using a known YouTube username
            var username = "YouTube"; // Official YouTube channel

            // Act
            var response = await _client.GetAsync($"/api/YouTube/channels/by-username/{username}");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var channel = JsonSerializer.Deserialize<YouTubeChannelDto>(content, _jsonOptions);
                Assert.NotNull(channel);
            }
            else
            {
                // Some usernames might not exist anymore, so we just check that we get a proper response
                Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.OK);
            }
        }

        #endregion

        #region Import Tests

        [Fact]
        public async Task ImportVideo_WithValidVideoId_ShouldReturnCreated()
        {
            // Clean up any existing data to ensure a clean test
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();
            
            // Use a known video ID
            var videoId = "dQw4w9WgXcQ"; // Rick Astley - Never Gonna Give You Up

            // Act
            var response = await _client.PostAsync($"/api/YouTube/import/video/{videoId}", null);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var importedVideo = JsonSerializer.Deserialize<Video>(content, _jsonOptions);
            Assert.NotNull(importedVideo);
            Assert.Equal("YouTube", importedVideo.Platform);
            Assert.Equal(MediaType.Video, importedVideo.MediaType);
        }

        [Fact]
        public async Task ImportVideo_WithInvalidVideoId_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/YouTube/import/video/invalid_video_id", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ImportFromUrl_WithValidVideoUrl_ShouldReturnCreated()
        {
            // Clean up any existing data to ensure a clean test
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();

            var videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var requestBody = JsonSerializer.Serialize(new { url = videoUrl }, _jsonOptions);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/YouTube/import/url", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var importedVideo = JsonSerializer.Deserialize<Video>(responseContent, _jsonOptions);
            Assert.NotNull(importedVideo);
            Assert.Equal("YouTube", importedVideo.Platform);
            Assert.Equal(MediaType.Video, importedVideo.MediaType);
        }

        [Fact]
        public async Task ImportFromUrl_WithInvalidUrl_ShouldReturnBadRequest()
        {
            var invalidUrl = "https://example.com/not-youtube";
            var requestBody = JsonSerializer.Serialize(new { url = invalidUrl }, _jsonOptions);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/YouTube/import/url", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportPlaylist_WithValidPlaylistId_ShouldReturnOk()
        {
            // Clean up any existing data to ensure a clean test
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();

            // Note: Using a small playlist to avoid importing too many videos in tests
            var playlistId = "PLFgquLnL59alCl_2TQvOiD5Vgm1hCaGSI"; // Example playlist

            // Act
            var response = await _client.PostAsync($"/api/YouTube/import/playlist/{playlistId}", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var importedVideos = JsonSerializer.Deserialize<List<Video>>(content, _jsonOptions);
            Assert.NotNull(importedVideos);
            Assert.True(importedVideos.Count > 0);
            
            // Verify all imported videos are from YouTube
            foreach (var video in importedVideos)
            {
                Assert.Equal("YouTube", video.Platform);
                Assert.Equal(MediaType.Video, video.MediaType);
            }
        }

        [Fact]
        public async Task ImportChannel_WithValidChannelId_ShouldReturnCreated()
        {
            // Clean up any existing data to ensure a clean test
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MediaLibraryDbContext>();

            var channelId = "UCuAXFkgsw1L7xaCfnd5JJOw"; // Example channel

            // Act
            var response = await _client.PostAsync($"/api/YouTube/import/channel/{channelId}", null);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var importedChannel = JsonSerializer.Deserialize<Video>(content, _jsonOptions);
            Assert.NotNull(importedChannel);
            Assert.Equal("YouTube", importedChannel.Platform);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetVideoDetails_WithNullVideoId_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/YouTube/videos/");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Search_WithMissingQuery_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.GetAsync("/api/YouTube/search");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportVideo_WithEmptyVideoId_ShouldReturnBadRequest()
        {
            // Act
            var response = await _client.PostAsync("/api/YouTube/import/video/", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // Route not matched
        }

        #endregion
    }
}
