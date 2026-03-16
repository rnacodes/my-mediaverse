using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class ListenNotesService : IListenNotesService
    {
        private readonly IListenNotesApiClient _listenNotesApiClient;
        private readonly IPodcastService _podcastService;
        private readonly IPodcastMappingService _podcastMappingService;
        private readonly ILogger<ListenNotesService> _logger;

        public ListenNotesService(
            IListenNotesApiClient listenNotesApiClient,
            IPodcastService podcastService,
            IPodcastMappingService podcastMappingService,
            ILogger<ListenNotesService> logger)
        {
            _listenNotesApiClient = listenNotesApiClient;
            _podcastService = podcastService;
            _podcastMappingService = podcastMappingService;
            _logger = logger;
        }

        // Search operations (return DTOs for API consumption)
        public async Task<SearchResultDto> SearchAsync(string query, string? type = null, int? offset = null, 
            int? lenMin = null, int? lenMax = null, string? genreIds = null, 
            string? publishedBefore = null, string? publishedAfter = null, 
            string? onlyIn = null, string? language = null, string? region = null, 
            string? sortByDate = null, string? safeMode = null, string? uniquePodcasts = null)
        {
            _logger.LogInformation("Searching podcasts with query: {Query}", query);
            return await _listenNotesApiClient.SearchAsync(query, type, offset, lenMin, lenMax, genreIds,
                publishedBefore, publishedAfter, onlyIn, language, region, sortByDate, safeMode, uniquePodcasts);
        }

        // Podcast operations (return DTOs for API consumption)
        public async Task<PodcastSeriesDto> GetPodcastByIdAsync(string id, string? nextEpisodePubDate = null)
        {
            _logger.LogInformation("Getting podcast details for ID: {PodcastId}", id);
            return await _listenNotesApiClient.GetPodcastByIdAsync(id, nextEpisodePubDate);
        }

        public async Task<ListenNotesBestPodcastsDto> GetBestPodcastsAsync(int? genreId = null, int? page = null, 
            string? region = null, string? sortByDate = null, bool? safeMode = null)
        {
            _logger.LogInformation("Getting best podcasts");
            return await _listenNotesApiClient.GetBestPodcastsAsync(genreId, page, region, sortByDate, safeMode);
        }

        public async Task<ListenNotesRecommendationsDto> GetPodcastRecommendationsAsync(string id, bool? safeMode = null)
        {
            _logger.LogInformation("Getting podcast recommendations for ID: {PodcastId}", id);
            return await _listenNotesApiClient.GetPodcastRecommendationsAsync(id, safeMode);
        }

        // Episode operations (return DTOs for API consumption)
        public async Task<PodcastEpisodeDto> GetEpisodeByIdAsync(string id)
        {
            _logger.LogInformation("Getting episode details for ID: {EpisodeId}", id);
            return await _listenNotesApiClient.GetEpisodeByIdAsync(id);
        }

        public async Task<ListenNotesRecommendationsDto> GetEpisodeRecommendationsAsync(string id, bool? safeMode = null)
        {
            _logger.LogInformation("Getting episode recommendations for ID: {EpisodeId}", id);
            return await _listenNotesApiClient.GetEpisodeRecommendationsAsync(id, safeMode);
        }

        // Playlist operations (return DTOs for API consumption)
        public async Task<ListenNotesPlaylistsDto> GetPlaylistsAsync()
        {
            _logger.LogInformation("Getting playlists");
            return await _listenNotesApiClient.GetPlaylistsAsync();
        }

        public async Task<ListenNotesPlaylistDto> GetPlaylistByIdAsync(string id)
        {
            _logger.LogInformation("Getting playlist details for ID: {PlaylistId}", id);
            return await _listenNotesApiClient.GetPlaylistByIdAsync(id);
        }

        // Genre operations (return DTOs for API consumption)
        public async Task<ListenNotesGenresDto> GetGenresAsync()
        {
            _logger.LogInformation("Getting genres");
            return await _listenNotesApiClient.GetGenresAsync();
        }

        // Curated content operations (return DTOs for API consumption)
        public async Task<ListenNotesCuratedPodcastsDto> GetCuratedPodcastsAsync(int? page = null)
        {
            _logger.LogInformation("Getting curated podcasts");
            return await _listenNotesApiClient.GetCuratedPodcastsAsync(page);
        }

        public async Task<ListenNotesCuratedPodcastDto> GetCuratedPodcastByIdAsync(string id)
        {
            _logger.LogInformation("Getting curated podcast details for ID: {CuratedPodcastId}", id);
            return await _listenNotesApiClient.GetCuratedPodcastByIdAsync(id);
        }

        // Import operations (business logic - convert DTOs to Domain Entities)
        public async Task<PodcastSeries> ImportPodcastSeriesAsync(string podcastId)
        {
            try
            {
                _logger.LogInformation("Importing podcast series with ID: {PodcastId}", podcastId);

                // Get podcast details from ListenNotes API
                var podcastDto = await _listenNotesApiClient.GetPodcastByIdAsync(podcastId);

                // Check if podcast series already exists by title and publisher
                var existingSeries = await _podcastService.GetPodcastSeriesByTitleAsync(
                    podcastDto.Title, podcastDto.Publisher);
                if (existingSeries != null)
                {
                    _logger.LogInformation("Podcast series {Title} by {Publisher} already exists", 
                        podcastDto.Title, podcastDto.Publisher);
                    return existingSeries;
                }

                // Map ListenNotes DTO to CreatePodcastSeriesDto
                var createSeriesDto = _podcastMappingService.MapFromListenNotesSeriesDto(podcastDto);
                
                // Automatically subscribe when importing a series
                createSeriesDto.IsSubscribed = true;

                // Save to database through domain service
                var savedSeries = await _podcastService.CreatePodcastSeriesAsync(createSeriesDto);
                
                _logger.LogInformation("Successfully imported podcast series: {Title} (ListenNotes ID: {PodcastId})", 
                    podcastDto.Title, podcastId);
                
                return savedSeries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing podcast series with ID: {PodcastId}", podcastId);
                throw;
            }
        }

        public async Task<PodcastEpisode> ImportPodcastEpisodeAsync(string episodeId, Guid seriesId)
        {
            try
            {
                _logger.LogInformation("Importing podcast episode with ID: {EpisodeId} for series: {SeriesId}", episodeId, seriesId);

                // Get episode details from ListenNotes API
                var episodeDto = await _listenNotesApiClient.GetEpisodeByIdAsync(episodeId);

                if (episodeDto == null)
                {
                    _logger.LogWarning("ListenNotes API returned null for episode ID: {EpisodeId}", episodeId);
                    throw new InvalidOperationException($"Episode with external ID {episodeId} not found on ListenNotes.");
                }

                // Check if episode already exists by external ID
                var existingEpisodes = await _podcastService.GetEpisodesBySeriesIdAsync(seriesId);
                var existingEpisode = existingEpisodes.FirstOrDefault(e => e.ExternalId == episodeId);
                if (existingEpisode != null)
                {
                    _logger.LogInformation("Episode {Title} already exists (ID: {ExistingEpisodeId})", episodeDto.Title, existingEpisode.Id);
                    return existingEpisode;
                }

                // Map ListenNotes episode DTO to CreatePodcastEpisodeDto
                var createEpisodeDto = _podcastMappingService.MapFromListenNotesEpisodeDto(episodeDto);
                createEpisodeDto.SeriesId = seriesId;

                // Set publisher if available from episodeDto
                // ListenNotes episode DTOs often don't include publisher directly.
                // We could fetch the series to get it, but for now, we'll leave it to be filled by the series.
                // createEpisodeDto.Publisher = episodeDto.Podcast?.Publisher; 

                // Save to database through domain service
                var savedEpisode = await _podcastService.CreatePodcastEpisodeAsync(createEpisodeDto);
                
                _logger.LogInformation("Successfully imported episode: {Title} (ListenNotes ID: {EpisodeId}, Internal ID: {InternalId})", 
                    episodeDto.Title, episodeId, savedEpisode.Id);
                
                return savedEpisode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing episode with ID: {EpisodeId}", episodeId);
                throw;
            }
        }

        public async Task<PodcastSeries?> ImportPodcastSeriesByNameAsync(string podcastName)
        {
            try
            {
                _logger.LogInformation("Searching and importing podcast series by name: {PodcastName}", podcastName);

                // Search for podcast by name
                var searchResults = await _listenNotesApiClient.SearchAsync(podcastName, "podcast");
                
                if (searchResults?.Results == null || !searchResults.Results.Any())
                {
                    _logger.LogInformation("No podcast found with name: {PodcastName}", podcastName);
                    return null;
                }

                // Get the first result and import it
                var firstResult = searchResults.Results.First();
                if (string.IsNullOrEmpty(firstResult.Id))
                {
                    _logger.LogWarning("First search result has no ID for podcast name: {PodcastName}", podcastName);
                    return null;
                }

                // Import the podcast series using its ID
                var series = await ImportPodcastSeriesAsync(firstResult.Id);
                
                _logger.LogInformation("Successfully imported podcast series by name: {PodcastName} -> {Title}", 
                    podcastName, series.Title);
                
                return series;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing podcast series by name: {PodcastName}", podcastName);
                throw;
            }
        }
    }
}
