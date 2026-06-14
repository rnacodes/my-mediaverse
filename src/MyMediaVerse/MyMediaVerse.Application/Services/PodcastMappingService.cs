using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using Microsoft.Extensions.Logging;

namespace MyMediaVerse.Application.Services
{
    public class PodcastMappingService : IPodcastMappingService
    {
        private readonly ILogger<PodcastMappingService> _logger;

        public PodcastMappingService(ILogger<PodcastMappingService> logger)
        {
            _logger = logger;
        }

        public CreatePodcastSeriesDto MapFromListenNotesSeriesDto(PodcastSeriesDto podcastDto)
        {
            try
            {
                _logger.LogInformation("Mapping ListenNotes podcast DTO to CreatePodcastSeriesDto for: {Title}", podcastDto.Title);

                var createSeriesDto = new CreatePodcastSeriesDto
                {
                    Title = podcastDto.Title ?? string.Empty,
                    MediaType = MediaType.Podcast,
                    Link = podcastDto.Website,
                    Description = podcastDto.Description,
                    Status = Status.Uncharted,
                    Publisher = podcastDto.Publisher,
                    ExternalId = podcastDto.Id,
                    Thumbnail = podcastDto.Image ?? podcastDto.Thumbnail,
                    TotalEpisodes = podcastDto.Episodes?.Count ?? 0
                };

                return createSeriesDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping ListenNotes podcast DTO to CreatePodcastSeriesDto");
                throw new ApplicationException("Failed to map ListenNotes podcast DTO to CreatePodcastSeriesDto", ex);
            }
        }

        public CreatePodcastEpisodeDto MapFromListenNotesEpisodeDto(PodcastEpisodeDto episodeDto)
        {
            try
            {
                _logger.LogInformation("Mapping ListenNotes episode DTO to CreatePodcastEpisodeDto for: {Title}", episodeDto.Title);

                var createEpisodeDto = new CreatePodcastEpisodeDto
                {
                    Title = episodeDto.Title ?? string.Empty,
                    MediaType = MediaType.Podcast,
                    SeriesId = Guid.Empty, // Will be set by caller
                    Link = episodeDto.Link,
                    Description = episodeDto.Description,
                    Status = Status.Uncharted,
                    AudioLink = episodeDto.AudioUrl,
                    ExternalId = episodeDto.Id,
                    Thumbnail = episodeDto.Image ?? episodeDto.Thumbnail,
                    ReleaseDate = DateTimeOffset.FromUnixTimeMilliseconds(episodeDto.PublishDateMs).UtcDateTime,
                    DurationInSeconds = episodeDto.DurationInSeconds
                };

                return createEpisodeDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping ListenNotes episode DTO to CreatePodcastEpisodeDto");
                throw new ApplicationException("Failed to map ListenNotes episode DTO to CreatePodcastEpisodeDto", ex);
            }
        }
    }
}
