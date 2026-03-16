using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IArticleService
    {
        // Basic CRUD operations
        Task<IEnumerable<Article>> GetAllArticlesAsync();
        Task<Article?> GetArticleByIdAsync(Guid id);
        Task<Article> CreateArticleAsync(CreateArticleDto dto);
        Task<Article> UpdateArticleAsync(Guid id, CreateArticleDto dto);
        Task<bool> DeleteArticleAsync(Guid id);

        // Query operations
        Task<IEnumerable<Article>> GetArticlesByAuthorAsync(string author);
        Task<IEnumerable<Article>> GetArchivedArticlesAsync();
        Task<IEnumerable<Article>> GetStarredArticlesAsync();

        // Sync operations
        Task<Article> UpdateArticleSyncStatusAsync(Guid id, bool isArchived, bool isStarred);

        // Content management
        Task<string?> GetArticleContentAsync(Guid id);
        Task<bool> UpdateArticleContentAsync(Guid id, string htmlContent);
    }
}
