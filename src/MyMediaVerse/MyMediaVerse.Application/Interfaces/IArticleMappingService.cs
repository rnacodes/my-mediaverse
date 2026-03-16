using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IArticleMappingService
    {
        Task<ArticleResponseDto> MapToResponseDtoAsync(Article article);
        Task<IEnumerable<ArticleResponseDto>> MapToResponseDtoAsync(IEnumerable<Article> articles);
    }
}
