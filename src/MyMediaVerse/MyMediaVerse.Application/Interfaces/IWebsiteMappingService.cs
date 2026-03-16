using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service interface for mapping Website entities to/from DTOs.
    /// </summary>
    public interface IWebsiteMappingService
    {
        Task<WebsiteResponseDto> MapToResponseDtoAsync(Website website);
        Task<IEnumerable<WebsiteResponseDto>> MapToResponseDtoAsync(IEnumerable<Website> websites);
    }
}


