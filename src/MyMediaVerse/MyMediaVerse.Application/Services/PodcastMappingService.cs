using System.Text.Json;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace MyMediaVerse.Application.Services
{
    public class PodcastMappingService : IPodcastMappingService
    {
        private const string ThumbnailKeyPrefix = "thumbnails/imported_";

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly IThumbnailStorageService _thumbnailStorage;
        private readonly ILogger<PodcastMappingService> _logger;

        public PodcastMappingService(IThumbnailStorageService thumbnailStorage, ILogger<PodcastMappingService> logger)
        {
            _thumbnailStorage = thumbnailStorage;
            _logger = logger;
        }

        public async Task<Podcast> MapToPodcastAsync(string jsonResponse)
        {
            try
            {
                var podcastDto = JsonSerializer.Deserialize<PodcastSeriesDto>(jsonResponse, _jsonOptions);

                // Try to extract genre information from the raw JSON if not in the DTO
                string? genreInfo = null;
                var jsonDocument = JsonDocument.Parse(jsonResponse);
                if (jsonDocument.RootElement.TryGetProperty("genres", out var genresElement))
                {
                    // If genres is available, extract it as a comma-separated list
                    if (genresElement.ValueKind == JsonValueKind.Array)
                    {
                        var genres = new List<string>();
                        foreach (var genre in genresElement.EnumerateArray())
                        {
                            if (genre.TryGetProperty("name", out var nameElement))
                            {
                                genres.Add(nameElement.GetString() ?? string.Empty);
                            }
                        }
                        genreInfo = string.Join(", ", genres);
                    }
                }

                // Upload thumbnail to DigitalOcean Spaces if available
                var originalThumbnailUrl = podcastDto?.Image ?? podcastDto?.Thumbnail;
                _logger.LogInformation("Processing thumbnail - Original URL: {OriginalUrl}", originalThumbnailUrl);

                var uploadedThumbnailUrl = await _thumbnailStorage.UploadFromUrlAsync(originalThumbnailUrl, ThumbnailKeyPrefix);
                _logger.LogInformation("Thumbnail processing result - Original: {OriginalUrl}, Uploaded: {UploadedUrl}", originalThumbnailUrl, uploadedThumbnailUrl);

                var podcast = new Podcast
                {
                    Title = podcastDto?.Title ?? string.Empty,
                    MediaType = MediaType.Podcast,
                    PodcastType = PodcastType.Series, // Default to Series for API imports
                    Link = podcastDto?.Website,
                    Description = podcastDto?.Description,
                    Thumbnail = uploadedThumbnailUrl,
                    DateAdded = DateTime.UtcNow,
                    Status = Status.Uncharted,
                    ExternalId = podcastDto?.Id,
                    Publisher = podcastDto?.Publisher
                };

                // Add genres to the new Genres collection
                if (!string.IsNullOrEmpty(genreInfo))
                {
                    var genreNames = genreInfo.Split(',').Select(g => g.Trim().ToLowerInvariant()).Where(g => !string.IsNullOrEmpty(g));
                    foreach (var genreName in genreNames)
                    {
                        podcast.Genres.Add(new Genre { Name = genreName });
                    }
                }

                return await Task.FromResult(podcast);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to map podcast series from ListenNotes API response", ex);
            }
        }

        public async Task<Podcast> MapToPodcastEpisodeAsync(string jsonResponse, Guid? parentPodcastId = null)
        {
            try
            {
                var episodeDto = JsonSerializer.Deserialize<PodcastEpisodeDto>(jsonResponse, _jsonOptions);

                // Try to extract topics from the raw JSON if available
                string? topicsInfo = null;
                var jsonDocument = JsonDocument.Parse(jsonResponse);
                if (jsonDocument.RootElement.TryGetProperty("topics", out var topicsElement))
                {
                    // If topics is available, extract it as a comma-separated list
                    if (topicsElement.ValueKind == JsonValueKind.Array)
                    {
                        var topics = new List<string>();
                        foreach (var topic in topicsElement.EnumerateArray())
                        {
                            topics.Add(topic.GetString() ?? string.Empty);
                        }
                        topicsInfo = string.Join(", ", topics);
                    }
                }

                // Upload thumbnail to DigitalOcean Spaces if available
                var originalThumbnailUrl = episodeDto?.Image ?? episodeDto?.Thumbnail;
                var uploadedThumbnailUrl = await _thumbnailStorage.UploadFromUrlAsync(originalThumbnailUrl, ThumbnailKeyPrefix);

                var podcastEpisode = new Podcast
                {
                    Title = episodeDto?.Title ?? string.Empty,
                    MediaType = MediaType.Podcast,
                    PodcastType = PodcastType.Episode,
                    Link = episodeDto?.Link,
                    Description = episodeDto?.Description,
                    Thumbnail = uploadedThumbnailUrl,
                    DateAdded = DateTime.UtcNow,
                    Status = Status.Uncharted,
                    ParentPodcastId = parentPodcastId,
                    AudioLink = episodeDto?.AudioUrl,
                    ReleaseDate = episodeDto != null ? DateTimeOffset.FromUnixTimeMilliseconds(episodeDto.PublishDateMs).DateTime : DateTime.UtcNow,
                    DurationInSeconds = episodeDto?.DurationInSeconds ?? 0,
                    ExternalId = episodeDto?.Id
                };

                // Add topics to the new Topics collection
                if (!string.IsNullOrEmpty(topicsInfo))
                {
                    var topicNames = topicsInfo.Split(',').Select(t => t.Trim().ToLowerInvariant()).Where(t => !string.IsNullOrEmpty(t));
                    foreach (var topicName in topicNames)
                    {
                        podcastEpisode.Topics.Add(new Topic { Name = topicName });
                    }
                }

                return await Task.FromResult(podcastEpisode);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to map podcast episode from ListenNotes API response", ex);
            }
        }

        public async Task<Podcast> MapToPodcastWithEpisodesAsync(string jsonResponse)
        {
            try
            {
                // First, try to parse as search results
                try
                {
                    var searchResults = JsonSerializer.Deserialize<SearchResultDto>(jsonResponse, _jsonOptions);
                    if (searchResults?.Results != null && searchResults.Results.Any())
                    {
                        // Get the first podcast from search results
                        var firstResult = searchResults.Results.First();
                        
                        // Convert search result to PodcastSeriesDto format for mapping
                        var podcastForMapping = new PodcastSeriesDto
                        {
                            Id = firstResult.Id,
                            Title = firstResult.TitleOriginal ?? firstResult.TitleHighlighted ?? "Unknown Title",
                            Publisher = firstResult.PublisherOriginal ?? firstResult.PublisherHighlighted ?? "Unknown Publisher",
                            Description = firstResult.DescriptionOriginal ?? firstResult.DescriptionHighlighted ?? "No description available",
                            Image = firstResult.Image ?? string.Empty,
                            Thumbnail = firstResult.Thumbnail ?? string.Empty
                        };

                        var mappingJson = JsonSerializer.Serialize(podcastForMapping, _jsonOptions);
                        return await MapToPodcastAsync(mappingJson);
                    }
                }
                catch (JsonException)
                {
                    // If it's not search results, continue with original logic
                }

                // Original logic for single podcast with episodes
                var podcastDto = JsonSerializer.Deserialize<PodcastSeriesDto>(jsonResponse, _jsonOptions);

                var series = await MapToPodcastAsync(jsonResponse);

                if (podcastDto?.Episodes != null && podcastDto.Episodes.Any())
                {
                    foreach (var episodeDto in podcastDto.Episodes)
                    {
                        // Convert episodeDto to JSON to reuse the MapToPodcastEpisode method
                        string episodeJson = JsonSerializer.Serialize(episodeDto, _jsonOptions);
                        var episode = await MapToPodcastEpisodeAsync(episodeJson, series.Id);
                        series.Episodes.Add(episode);
                    }
                }

                return series;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to map podcast series with episodes from ListenNotes API response", ex);
            }
        }

        public async Task<Podcast?> MapSearchResultToPodcastAsync(string searchJsonResponse)
        {
            try
            {
                var searchResults = JsonSerializer.Deserialize<SearchResultDto>(searchJsonResponse, _jsonOptions);
                if (searchResults?.Results == null || !searchResults.Results.Any())
                {
                    return null;
                }

                // Get the first podcast from search results
                var firstResult = searchResults.Results.First();
                
                // Convert search result to podcast
                var podcast = new Podcast
                {
                    Title = firstResult.TitleOriginal ?? firstResult.TitleHighlighted ?? "Unknown Title",
                    MediaType = MediaType.Podcast,
                    PodcastType = PodcastType.Series,
                    Link = firstResult.Website,
                    Description = firstResult.DescriptionOriginal ?? firstResult.DescriptionHighlighted ?? "No description available",
                    Thumbnail = firstResult.Image ?? firstResult.Thumbnail,
                    DateAdded = DateTime.UtcNow,
                    Status = Status.Uncharted,
                    ExternalId = firstResult.Id,
                    Publisher = firstResult.PublisherOriginal ?? firstResult.PublisherHighlighted ?? "Unknown Publisher"
                };

                // Add genres to the podcast
                if (firstResult.Genres?.Any() == true)
                {
                    foreach (var genre in firstResult.Genres)
                    {
                        podcast.Genres.Add(new Genre { Name = genre.Name?.ToLowerInvariant() ?? string.Empty });
                    }
                }

                return await Task.FromResult(podcast);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to map search result to podcast from ListenNotes API response", ex);
            }
        }

        // New methods (working with DTOs)
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
