using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    /// <summary>
    /// Outcome of a website create/import: the persisted row plus whether it was inserted
    /// (true) or the URL matched an existing website (false), which then absorbed any new
    /// metadata the caller supplied.
    /// </summary>
    public record WebsiteCreationResult(Website Website, bool Created);

    /// <summary>
    /// Service interface for managing Website media items.
    /// </summary>
    public interface IWebsiteService
    {
        // Basic CRUD operations
        Task<IEnumerable<Website>> GetAllWebsitesAsync();
        Task<Website?> GetWebsiteByIdAsync(Guid id);
        Task<WebsiteCreationResult> CreateWebsiteAsync(CreateWebsiteDto dto);
        Task<Website> UpdateWebsiteAsync(Guid id, CreateWebsiteDto dto);
        Task<bool> DeleteWebsiteAsync(Guid id);

        // Import operations
        Task<WebsiteCreationResult> ImportWebsiteFromUrlAsync(ImportWebsiteDto dto);
        Task<WebsitePreviewDto> ScrapeWebsitePreviewAsync(string url);

        /// <summary>
        /// Renders a fresh screenshot and stores it as the website's thumbnail. Returns null when
        /// the website does not exist. Without <paramref name="force"/> a website that already
        /// has a thumbnail is left alone and reported as skipped.
        /// </summary>
        Task<WebsiteScreenshotResultDto?> RegenerateScreenshotAsync(Guid id, bool force, CancellationToken cancellationToken = default);

        /// <summary>
        /// The whole website library as a Netscape bookmark file (topics as TAGS), ready to be
        /// imported into a browser or bookmark manager.
        /// </summary>
        Task<(byte[] Content, string FileName)> ExportBookmarksAsync();

        // Query operations
        Task<IEnumerable<Website>> GetWebsitesByDomainAsync(string domain);
        Task<IEnumerable<Website>> GetWebsitesWithRssFeedsAsync();
    }
}
