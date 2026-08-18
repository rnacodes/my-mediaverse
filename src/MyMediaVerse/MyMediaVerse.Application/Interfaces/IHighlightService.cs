using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IHighlightService
    {
        Task<IEnumerable<Highlight>> GetAllHighlightsAsync();
        Task<Highlight?> GetHighlightByIdAsync(Guid id);
        Task<IEnumerable<Highlight>> GetHighlightsByArticleIdAsync(Guid articleId);
        Task<IEnumerable<Highlight>> GetHighlightsByBookIdAsync(Guid bookId);
        Task<IEnumerable<Highlight>> GetHighlightsByTagAsync(string tag);
        Task<IEnumerable<Highlight>> GetUnlinkedHighlightsAsync();
        Task<Highlight> CreateHighlightAsync(CreateHighlightDto dto);

        /// <summary>
        /// Partially updates a highlight. Null DTO fields are left unchanged;
        /// empty strings clear optional fields, an empty tag list clears tags.
        /// </summary>
        Task<Highlight> UpdateHighlightAsync(Guid id, UpdateHighlightDto dto);

        /// <summary>
        /// Sets the highlight's media link to an article, a book, or nothing.
        /// Providing both targets is invalid; both null unlinks.
        /// </summary>
        Task<Highlight> SetHighlightLinkAsync(Guid id, Guid? articleId, Guid? bookId);

        Task<bool> DeleteHighlightAsync(Guid id);
        
        /// <summary>
        /// Syncs all highlights from Readwise API
        /// </summary>
        Task<HighlightSyncResultDto> SyncHighlightsFromReadwiseAsync();
        
        /// <summary>
        /// Syncs only highlights updated after a specific date
        /// </summary>
        Task<HighlightSyncResultDto> SyncHighlightsIncrementalAsync(DateTime lastSyncDate);

        /// <summary>
        /// Cleans all existing highlights by removing HTML/CSS from their text.
        /// Returns the number of highlights that were cleaned.
        /// </summary>
        Task<int> CleanAllHighlightTextAsync();

        /// <summary>
        /// Creates multiple highlights in a single transaction.
        /// Auto-links highlights to existing books/articles by title/author or URL.
        /// </summary>
        Task<BulkHighlightResultDto> BulkCreateHighlightsAsync(List<CreateHighlightDto> dtos);
    }
}

