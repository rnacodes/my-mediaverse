using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface ITopicsService
    {
        Task<IReadOnlyList<TopicResponseDto>> GetAllTopicsAsync();
        Task<IReadOnlyList<TopicResponseDto>> SearchTopicsAsync(string query);
        Task<TopicResponseDto?> GetTopicAsync(Guid id);

        // Returns the existing topic when one with the same normalized name already exists,
        // and a flag indicating whether a new row was created.
        Task<(TopicResponseDto Topic, bool Created)> CreateTopicAsync(CreateTopicDto dto);

        // Returns null when no topic exists for the id; throws InvalidOperationException
        // when another topic already has the new name.
        Task<TopicResponseDto?> UpdateTopicAsync(Guid id, CreateTopicDto dto);

        Task<bool> DeleteTopicAsync(Guid id);

        Task<BulkImportResultDto> ImportTopicsFromJsonAsync(IReadOnlyList<CreateTopicDto> topics);
        Task<BulkImportResultDto> ImportTopicsFromCsvAsync(Stream csvStream);
    }
}
