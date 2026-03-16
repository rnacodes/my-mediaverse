using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Service interface for managing Website media items.
    /// </summary>
    public interface IWebsiteService
    {
        // Basic CRUD operations
        Task<IEnumerable<Website>> GetAllWebsitesAsync();
        Task<Website?> GetWebsiteByIdAsync(Guid id);
        Task<Website> CreateWebsiteAsync(CreateWebsiteDto dto);
        Task<Website> UpdateWebsiteAsync(Guid id, CreateWebsiteDto dto);
        Task<bool> DeleteWebsiteAsync(Guid id);
        
        // Import operations
        Task<Website> ImportWebsiteFromUrlAsync(ImportWebsiteDto dto);
        Task<ScrapedWebsiteDataDto> ScrapeWebsitePreviewAsync(string url);
        
        // Query operations
        Task<IEnumerable<Website>> GetWebsitesByDomainAsync(string domain);
        Task<IEnumerable<Website>> GetWebsitesWithRssFeedsAsync();
        Task<Website?> GetWebsiteByUrlAsync(string url);
    }
}

