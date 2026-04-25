using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public class AddMediaToMixlistResult
    {
        public bool MixlistFound { get; set; }
        public bool MediaItemFound { get; set; }
        public bool AlreadyInMixlist { get; set; }
        public string? MixlistName { get; set; }
        public string? MediaItemTitle { get; set; }
    }

    public class RemoveMediaFromMixlistResult
    {
        public bool MixlistFound { get; set; }
        public bool MediaInMixlist { get; set; }
        public string? MixlistName { get; set; }
    }

    public class LinkNoteToMixlistResult
    {
        public bool MixlistFound { get; set; }
        public bool NoteFound { get; set; }
        public bool AlreadyLinked { get; set; }
    }

    public class GetNotesForMixlistResult
    {
        public bool MixlistFound { get; set; }
        public IReadOnlyList<LinkedNoteDto> Notes { get; set; } = Array.Empty<LinkedNoteDto>();
    }

    public class ImportMixlistsResult
    {
        public List<object> ImportedMixlists { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class ExportMixlistResult
    {
        public bool MixlistFound { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }

    public interface IMixlistService
    {
        Task<IReadOnlyList<MixlistResponseDto>> GetAllMixlistsAsync();
        Task<MixlistResponseDto?> GetMixlistAsync(Guid id);
        Task<IReadOnlyList<MixlistResponseDto>> SearchMixlistsAsync(string query);

        Task<MixlistResponseDto> CreateMixlistAsync(CreateMixlistDto dto);
        Task<AddMediaToMixlistResult> AddMediaItemToMixlistAsync(Guid mixlistId, Guid mediaItemId);
        Task<RemoveMediaFromMixlistResult> RemoveMediaItemFromMixlistAsync(Guid mixlistId, Guid mediaItemId);
        Task<MixlistResponseDto?> UpdateMixlistAsync(Guid id, UpdateMixlistDto dto);
        Task<bool> DeleteMixlistAsync(Guid id);

        Task<LinkNoteToMixlistResult> LinkNoteToMixlistAsync(Guid mixlistId, LinkNoteToMixlistDto dto);
        Task<bool> UnlinkNoteFromMixlistAsync(Guid mixlistId, Guid noteId);
        Task<GetNotesForMixlistResult> GetNotesForMixlistAsync(Guid mixlistId);

        Task<ImportMixlistsResult> ImportMixlistsAsync(IReadOnlyList<ImportMixlistDto> importDtos);
        Task<ExportMixlistResult> ExportMixlistAsync(Guid id);
        Task<ExportMixlistResult> ExportAllMixlistsAsync();
    }
}
