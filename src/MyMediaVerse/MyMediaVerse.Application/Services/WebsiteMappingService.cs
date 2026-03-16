using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for mapping Website entities to DTOs.
    /// </summary>
    public class WebsiteMappingService : IWebsiteMappingService
    {
        private readonly ILogger<WebsiteMappingService> _logger;

        public WebsiteMappingService(ILogger<WebsiteMappingService> logger)
        {
            _logger = logger;
        }

        public Task<WebsiteResponseDto> MapToResponseDtoAsync(Website website)
        {
            var dto = new WebsiteResponseDto
            {
                Id = website.Id,
                Title = website.Title,
                Description = website.Description,
                Link = website.Link,
                Thumbnail = website.Thumbnail,
                RssFeedUrl = website.RssFeedUrl,
                Domain = website.Domain,
                Author = website.Author,
                Publication = website.Publication,
                LastCheckedDate = website.LastCheckedDate,
                DateAdded = website.DateAdded,
                Status = website.Status.ToString(),
                Rating = website.Rating?.ToString(),
                Notes = website.Notes,
                Topics = website.Topics?.Select(t => t.Name).ToList() ?? new List<string>(),
                Genres = website.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                MediaType = website.MediaType.ToString(),
                // Archive fields for future ArchiveBox integration
                ArchiveUrl = website.ArchiveUrl,
                ArchivedAt = website.ArchivedAt,
                ArchiveStatus = website.ArchiveStatus,
                WaybackUrl = website.WaybackUrl
            };

            return Task.FromResult(dto);
        }

        public async Task<IEnumerable<WebsiteResponseDto>> MapToResponseDtoAsync(IEnumerable<Website> websites)
        {
            var tasks = websites.Select(MapToResponseDtoAsync);
            return await Task.WhenAll(tasks);
        }
    }
}


