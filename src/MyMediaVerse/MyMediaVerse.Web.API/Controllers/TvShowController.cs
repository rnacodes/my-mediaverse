using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Clients;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.DTOs;
using MyMediaVerse.Domain.Entities;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TvShowController : ControllerBase
    {
        private readonly ITvShowService _tvShowService;
        private readonly ITvShowMappingService _tvShowMappingService;
        private readonly ILogger<TvShowController> _logger;
        private readonly TmdbApiClient _tmdbClient;
        private readonly IAmazonS3? _s3Client;
        private readonly IConfiguration _configuration;

        public TvShowController(
            ITvShowService tvShowService,
            ITvShowMappingService tvShowMappingService,
            ILogger<TvShowController> logger,
            TmdbApiClient tmdbClient,
            IAmazonS3? s3Client,
            IConfiguration configuration)
        {
            _tvShowService = tvShowService;
            _tvShowMappingService = tvShowMappingService;
            _logger = logger;
            _tmdbClient = tmdbClient;
            _s3Client = s3Client;
            _configuration = configuration;
        }

        private async Task<string?> UploadImageFromUrlAsync(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || _s3Client == null)
            {
                return imageUrl; // Return original URL if S3 not configured or URL is empty
            }

            try
            {
                // Get DigitalOcean Spaces configuration
                var spacesConfig = _configuration.GetSection("DigitalOceanSpaces");
                var bucketName = spacesConfig["BucketName"];
                var endpoint = spacesConfig["Endpoint"];

                if (string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(endpoint))
                {
                    _logger.LogWarning("DigitalOcean Spaces configuration incomplete, keeping original image URL");
                    return imageUrl;
                }

                // Download the image from the URL
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");
                
                var response = await httpClient.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download image from URL {ImageUrl}: {StatusCode}", imageUrl, response.StatusCode);
                    return imageUrl;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                
                // Get file extension from content type
                var extension = contentType.ToLower() switch
                {
                    "image/jpeg" => ".jpg",
                    "image/jpg" => ".jpg", 
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };

                // Generate a unique file name
                var uniqueFileName = $"thumbnails/tvshows_{Guid.NewGuid()}{extension}";

                // Upload to DigitalOcean Spaces
                using var imageStream = await response.Content.ReadAsStreamAsync();
                
                var uploadRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = uniqueFileName,
                    InputStream = imageStream,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead // Make the file publicly accessible
                };

                await _s3Client.PutObjectAsync(uploadRequest);

                // Construct the public URL
                var publicUrl = $"https://{bucketName}.{endpoint}/{uniqueFileName}";

                _logger.LogInformation("Successfully uploaded TV show poster to DigitalOcean Spaces: {OriginalUrl} -> {PublicUrl}", imageUrl, publicUrl);

                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading TV show poster from URL {ImageUrl}, keeping original URL", imageUrl);
                return imageUrl; // Return original URL if upload fails
            }
        }

        // GET: api/tvshow
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TvShowResponseDto>>> GetAllTvShows()
        {
            try
            {
                var tvShows = await _tvShowService.GetAllTvShowsAsync();
                var response = await Task.WhenAll(tvShows.Select(t => _tvShowMappingService.MapToResponseDtoAsync(t)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all TV shows");
                return StatusCode(500, new { error = "Failed to retrieve TV shows", details = ex.Message });
            }
        }

        // GET: api/tvshow/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TvShowResponseDto>> GetTvShow(Guid id)
        {
            try
            {
                var tvShow = await _tvShowService.GetTvShowByIdAsync(id);
                if (tvShow == null)
                {
                    return NotFound($"TV show with ID {id} not found.");
                }

                var response = await _tvShowMappingService.MapToResponseDtoAsync(tvShow);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV show with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve TV show", details = ex.Message });
            }
        }

        // GET: api/tvshow/by-creator/{creator}
        [HttpGet("by-creator/{creator}")]
        public async Task<ActionResult<IEnumerable<TvShowResponseDto>>> GetTvShowsByCreator(string creator)
        {
            try
            {
                var tvShows = await _tvShowService.GetTvShowsByCreatorAsync(creator);
                var response = await Task.WhenAll(tvShows.Select(t => _tvShowMappingService.MapToResponseDtoAsync(t)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV shows by creator: {Creator}", creator);
                return StatusCode(500, new { error = "Failed to retrieve TV shows by creator", details = ex.Message });
            }
        }

        // GET: api/tvshow/by-year/{year}
        [HttpGet("by-year/{year}")]
        public async Task<ActionResult<IEnumerable<TvShowResponseDto>>> GetTvShowsByYear(int year)
        {
            try
            {
                var tvShows = await _tvShowService.GetTvShowsByYearAsync(year);
                var response = await Task.WhenAll(tvShows.Select(t => _tvShowMappingService.MapToResponseDtoAsync(t)));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV shows by year: {Year}", year);
                return StatusCode(500, new { error = "Failed to retrieve TV shows by year", details = ex.Message });
            }
        }

        // POST: api/tvshow
        [HttpPost]
        public async Task<IActionResult> CreateTvShow([FromBody] CreateTvShowDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("TV show data is required");
                }

                var tvShow = await _tvShowService.CreateTvShowAsync(dto);
                var response = await _tvShowMappingService.MapToResponseDtoAsync(tvShow);

                return CreatedAtAction(nameof(GetTvShow), new { id = tvShow.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating TV show");
                return StatusCode(500, new { error = "Failed to create TV show", details = ex.Message });
            }
        }

        // PUT: api/tvshow/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTvShow(Guid id, [FromBody] CreateTvShowDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("TV show data is required");
                }

                var tvShow = await _tvShowService.UpdateTvShowAsync(id, dto);
                var response = await _tvShowMappingService.MapToResponseDtoAsync(tvShow);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "TV show with ID {Id} not found for update", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating TV show with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update TV show", details = ex.Message });
            }
        }

        // DELETE: api/tvshow/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTvShow(Guid id)
        {
            try
            {
                var deleted = await _tvShowService.DeleteTvShowAsync(id);
                if (!deleted)
                {
                    return NotFound($"TV show with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting TV show with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete TV show", details = ex.Message });
            }
        }

        // POST: api/tvshow/from-tmdb/{tvShowId}
        [HttpPost("from-tmdb/{tvShowId}")]
        public async Task<IActionResult> ImportTvShowFromTmdb(int tvShowId)
        {
            try
            {
                _logger.LogInformation("Starting TV show import from TMDB for ID: {TvShowId}", tvShowId);

                // Get TV show data from TMDB API
                var tmdbTvShow = await _tmdbClient.GetTvShowDetailsAsync(tvShowId);
                _logger.LogInformation("Retrieved TV show data from TMDB for ID: {TvShowId}", tvShowId);

                // Map TMDB data to domain entity
                var tvShow = await _tvShowMappingService.MapFromTmdbAsync(tmdbTvShow);
                _logger.LogInformation("Mapped TV show data for: {Title}", tvShow.Title);

                // Save to database (keeping TMDB thumbnail URL directly instead of re-uploading)
                var createdTvShow = await _tvShowService.CreateTvShowAsync(new CreateTvShowDto
                {
                    Title = tvShow.Title,
                    Description = tvShow.Description,
                    Thumbnail = tvShow.Thumbnail,
                    Link = $"https://www.themoviedb.org/tv/{tvShow.TmdbId}",
                    TmdbId = tvShow.TmdbId,
                    TmdbRating = tvShow.TmdbRating,
                    TmdbPosterPath = tvShow.TmdbPosterPath,
                    Tagline = tvShow.Tagline,
                    Homepage = tvShow.Homepage,
                    OriginalLanguage = tvShow.OriginalLanguage,
                    OriginalName = tvShow.OriginalName,
                    FirstAirYear = tvShow.FirstAirYear,
                    LastAirYear = tvShow.LastAirYear,
                    NumberOfSeasons = tvShow.NumberOfSeasons,
                    NumberOfEpisodes = tvShow.NumberOfEpisodes,
                    Status = Status.Uncharted,
                    MediaType = MediaType.TVShow
                });

                var response = await _tvShowMappingService.MapToResponseDtoAsync(createdTvShow);
                _logger.LogInformation("Successfully imported TV show: {Title} with ID: {Id}", createdTvShow.Title, createdTvShow.Id);

                return CreatedAtAction(nameof(GetTvShow), new { id = createdTvShow.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing TV show from TMDB with ID: {TvShowId}", tvShowId);
                return StatusCode(500, new { error = "Failed to import TV show from TMDB", details = ex.Message });
            }
        }

        // GET: api/tvshow/{showId}/episodes
        [HttpGet("{showId}/episodes")]
        public async Task<ActionResult<IEnumerable<TvShowEpisodeResponseDto>>> GetEpisodesByShowId(Guid showId)
        {
            try
            {
                var episodes = await _tvShowService.GetEpisodesByShowIdAsync(showId);

                var response = episodes.Select(e => MapEpisodeToResponseDto(e));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving episodes for TV show {ShowId}", showId);
                return StatusCode(500, new { error = "Failed to retrieve TV show episodes", details = ex.Message });
            }
        }

        // GET: api/tvshow/episodes/{id}
        [HttpGet("episodes/{id}")]
        public async Task<ActionResult<TvShowEpisodeResponseDto>> GetTvShowEpisode(Guid id)
        {
            try
            {
                var episode = await _tvShowService.GetTvShowEpisodeByIdAsync(id);

                if (episode == null)
                {
                    return NotFound($"TV show episode with ID {id} not found.");
                }

                return Ok(MapEpisodeToResponseDto(episode));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving TV show episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve TV show episode", details = ex.Message });
            }
        }

        // POST: api/tvshow/episodes
        [HttpPost("episodes")]
        public async Task<IActionResult> CreateTvShowEpisode([FromBody] CreateTvShowEpisodeDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("TV show episode data is required");
                }

                var episode = await _tvShowService.CreateTvShowEpisodeAsync(dto);

                return CreatedAtAction(nameof(GetTvShowEpisode), new { id = episode.Id }, MapEpisodeToResponseDto(episode));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating TV show episode");
                return StatusCode(500, new { error = "Failed to create TV show episode", details = ex.Message });
            }
        }

        // DELETE: api/tvshow/episodes/{id}
        [HttpDelete("episodes/{id}")]
        public async Task<IActionResult> DeleteTvShowEpisode(Guid id)
        {
            try
            {
                var deleted = await _tvShowService.DeleteTvShowEpisodeAsync(id);

                if (!deleted)
                {
                    return NotFound($"TV show episode with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting TV show episode with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete TV show episode", details = ex.Message });
            }
        }

        private static TvShowEpisodeResponseDto MapEpisodeToResponseDto(TvShowEpisode episode)
        {
            return new TvShowEpisodeResponseDto
            {
                Id = episode.Id,
                Title = episode.Title,
                Description = episode.Description,
                MediaType = episode.MediaType,
                Status = episode.Status,
                DateAdded = episode.DateAdded,
                DateCompleted = episode.DateCompleted,
                Rating = episode.Rating,
                Link = episode.Link,
                Thumbnail = episode.GetEffectiveThumbnail(),
                ShowId = episode.ShowId,
                ShowTitle = episode.Show?.Title,
                SeasonNumber = episode.SeasonNumber,
                EpisodeNumber = episode.EpisodeNumber,
                AirDate = episode.AirDate,
                DurationInMinutes = episode.DurationInMinutes,
                TmdbEpisodeId = episode.TmdbEpisodeId,
                TraktEpisodeId = episode.TraktEpisodeId,
                StillPath = episode.StillPath,
                TraktPlays = episode.TraktPlays,
                TraktLastWatchedAt = episode.TraktLastWatchedAt,
                EpisodeIdentifier = episode.GetEpisodeIdentifier()
            };
        }

        // GET: api/tvshow/search-tmdb
        [HttpGet("search-tmdb")]
        public async Task<ActionResult<IEnumerable<TvShowSearchResultDto>>> SearchTmdbTvShows([FromQuery] string query, [FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query is required");
                }

                _logger.LogInformation("Searching TMDB for TV shows with query: {Query}", query);

                var searchResults = await _tmdbClient.SearchTvShowsAsync(query, page);
                var response = await Task.WhenAll(searchResults.Results.Select(t => _tvShowMappingService.MapToSearchResultDtoAsync(t)));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching TMDB TV shows with query: {Query}", query);
                return StatusCode(500, new { error = "Failed to search TMDB TV shows", details = ex.Message });
            }
        }
    }
}
