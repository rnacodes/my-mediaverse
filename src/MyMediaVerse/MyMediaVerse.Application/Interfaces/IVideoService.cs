using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IVideoService
    {
        // Standard CRUD operations
        Task<IEnumerable<Video>> GetAllVideosAsync();
        Task<Video?> GetVideoByIdAsync(Guid id);
        Task<IEnumerable<Video>> GetVideosByChannelAsync(Guid channelId);
        Task<Video> CreateVideoAsync(CreateVideoDto dto);
        Task<Video> UpdateVideoAsync(Guid id, CreateVideoDto dto);
        Task<bool> DeleteVideoAsync(Guid id);
        
        // Get playlists containing a video
        Task<IEnumerable<YouTubePlaylist>> GetPlaylistsForVideoAsync(Guid videoId);

        // Existing methods
        Task<Video> SaveVideoAsync(Video video, bool updateIfExists = true);
        Task<bool> VideoExistsAsync(string title, Guid? channelId = null);
        Task<Video?> GetVideoByTitleAsync(string title, Guid? channelId = null);
    }
}
