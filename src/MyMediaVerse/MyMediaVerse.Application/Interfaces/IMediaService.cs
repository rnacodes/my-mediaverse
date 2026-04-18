using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IMediaService
    {
        // Queries
        Task<IEnumerable<MediaItemResponseDto>> GetAllMediaAsync();
        Task<MediaItemResponseDto?> GetMediaItemAsync(Guid id);
        Task<IEnumerable<BaseMediaItem>> SearchMediaAsync(string query);
        Task<IEnumerable<MediaItemResponseDto>> GetMediaByTopicAsync(Guid topicId);
        Task<IEnumerable<MediaItemResponseDto>> GetMediaByGenreAsync(Guid genreId);
        Task<IEnumerable<MediaItemResponseDto>> GetMediaByTypeAsync(string mediaType);

        // Mutations
        Task<MediaItemResponseDto> CreateMediaItemAsync(CreateMediaItemDto dto);
        Task<MediaItemResponseDto> UpdateMediaItemAsync(Guid id, CreateMediaItemDto dto);
        Task<bool> DeleteMediaItemAsync(Guid id, string? baseUrl = null);
        Task<(int deletedCount, List<string> thumbnailErrors)> BulkDeleteMediaItemsAsync(List<Guid> ids, string? baseUrl = null);

        // Export
        Task<(byte[] content, string fileName)?> ExportMediaItemAsync(Guid id);
        Task<(byte[] content, string fileName)> ExportAllMediaAsync();
    }
}
