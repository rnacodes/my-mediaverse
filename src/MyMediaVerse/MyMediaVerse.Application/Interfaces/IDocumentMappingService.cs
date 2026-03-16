using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.Paperless;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service interface for mapping between Paperless-ngx DTOs and MyMediaVerse Document entities.
    /// </summary>
    public interface IDocumentMappingService
    {
        // ============================================
        // Entity to DTO mappings
        // ============================================

        /// <summary>
        /// Maps a Document entity to a DocumentResponseDto.
        /// </summary>
        DocumentResponseDto MapToResponseDto(Document document);

        /// <summary>
        /// Maps multiple Document entities to DocumentResponseDtos.
        /// </summary>
        IEnumerable<DocumentResponseDto> MapToResponseDtos(IEnumerable<Document> documents);

        // ============================================
        // Paperless to Entity mappings
        // ============================================

        /// <summary>
        /// Maps a Paperless document DTO to a CreateDocumentDto.
        /// Resolves tag IDs to names and document type/correspondent IDs to names.
        /// </summary>
        Task<CreateDocumentDto> MapFromPaperlessAsync(
            PaperlessDocumentDto paperlessDocument,
            IReadOnlyDictionary<int, string> tagLookup,
            IReadOnlyDictionary<int, string> documentTypeLookup,
            IReadOnlyDictionary<int, string> correspondentLookup,
            string paperlessBaseUrl);

        /// <summary>
        /// Maps Paperless tags to MyMediaVerse Topics and Genres.
        /// Uses prefix-based mapping (topic:*, genre:*) and known genre detection.
        /// </summary>
        (List<string> Topics, List<string> Genres) MapPaperlessTagsToTopicsAndGenres(IEnumerable<string> paperlessTags);

        // ============================================
        // Lookup building helpers
        // ============================================

        /// <summary>
        /// Builds a lookup dictionary from tag ID to tag name.
        /// </summary>
        IReadOnlyDictionary<int, string> BuildTagLookup(IEnumerable<PaperlessTagDto> tags);

        /// <summary>
        /// Builds a lookup dictionary from document type ID to name.
        /// </summary>
        IReadOnlyDictionary<int, string> BuildDocumentTypeLookup(IEnumerable<PaperlessDocumentTypeDto> documentTypes);

        /// <summary>
        /// Builds a lookup dictionary from correspondent ID to name.
        /// </summary>
        IReadOnlyDictionary<int, string> BuildCorrespondentLookup(IEnumerable<PaperlessCorrespondentDto> correspondents);
    }
}
