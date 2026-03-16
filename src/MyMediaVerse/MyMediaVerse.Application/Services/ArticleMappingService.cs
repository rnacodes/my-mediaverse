using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Application.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class ArticleMappingService : IArticleMappingService
    {
        private readonly ILogger<ArticleMappingService> _logger;
        private readonly IConfiguration _configuration;

        public ArticleMappingService(ILogger<ArticleMappingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ArticleResponseDto> MapToResponseDtoAsync(Article article)
        {
            // Content is now stored directly in database (FullTextContent column)
            // ContentStoragePath is kept for backwards compatibility but no longer used

            return await Task.FromResult(new ArticleResponseDto
            {
                Id = article.Id,
                Title = article.Title,
                Description = article.Description,
                MediaType = article.MediaType,
                Status = article.Status,
                DateAdded = article.DateAdded,
                Link = article.Link,
                Thumbnail = article.Thumbnail,
                Rating = article.Rating,
                OwnershipStatus = article.OwnershipStatus,
                DateCompleted = article.DateCompleted,
                Notes = article.Notes,
                RelatedNotes = article.RelatedNotes,
                Topics = article.Topics.Select(t => t.Name).ToArray(),
                Genres = article.Genres.Select(g => g.Name).ToArray(),
                ContentStoragePath = article.ContentStoragePath, // Legacy field, kept for backwards compatibility
                ContentUrl = null, // No longer using S3
                IsArchived = article.IsArchived,
                IsStarred = article.IsStarred,
                LastSyncDate = article.LastSyncDate,
                Author = article.Author,
                Publication = article.Publication,
                PublicationDate = article.PublicationDate,
                ReadingProgress = article.ReadingProgress,
                WordCount = article.WordCount,
                EstimatedReadingTime = article.GetEstimatedReadingTime(),
                ReadwiseDocumentId = article.ReadwiseDocumentId,
                HasFullTextContent = !string.IsNullOrEmpty(article.FullTextContent),
                ReaderLocation = article.ReaderLocation
            });
        }

        public async Task<IEnumerable<ArticleResponseDto>> MapToResponseDtoAsync(IEnumerable<Article> articles)
        {
            var tasks = articles.Select(a => MapToResponseDtoAsync(a));
            return await Task.WhenAll(tasks);
        }
    }
}
