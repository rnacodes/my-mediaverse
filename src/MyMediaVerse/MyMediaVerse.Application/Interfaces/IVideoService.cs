using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// The outcome of saving a video: the stored row, and whether this call created it or
    /// found it already in the library.
    /// </summary>
    public record VideoCreationResult(Video Video, bool Created);

    public interface IVideoService
    {
        // Standard CRUD operations
        Task<IEnumerable<Video>> GetAllVideosAsync();
        Task<Video?> GetVideoByIdAsync(Guid id);
        Task<IEnumerable<Video>> GetVideosByChannelAsync(Guid channelId);
        Task<VideoCreationResult> CreateVideoAsync(CreateVideoDto dto);
        Task<Video> UpdateVideoAsync(Guid id, CreateVideoDto dto);
        Task<bool> DeleteVideoAsync(Guid id);

        /// <summary>
        /// Get the video that holds this id on this platform, or null. An exact id match only:
        /// rows that could be the same video by link or title are not considered.
        /// </summary>
        Task<Video?> GetVideoByExternalIdAsync(string platform, string externalId);

        // Get playlists containing a video
        Task<IEnumerable<YouTubePlaylist>> GetPlaylistsForVideoAsync(Guid videoId);

        /// <summary>
        /// Save a video unless it is already in the library. An existing row is returned as it
        /// is, gaining only the id and the blank fields the incoming video can fill.
        /// </summary>
        Task<VideoCreationResult> SaveVideoAsync(Video video);
    }
}
