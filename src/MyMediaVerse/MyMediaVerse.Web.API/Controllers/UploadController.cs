using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.DTOs;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using System.Linq;

namespace MyMediaVerse.Web.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private const string ThumbnailKeyPrefix = "thumbnails/";
        private static readonly string[] AllowedImageContentTypes =
            { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };

        private readonly MediaLibraryDbContext _context;
        private readonly ILogger<UploadController> _logger;
        private readonly IThumbnailStorageService _thumbnailStorage;
        private readonly IAmazonS3? _s3Client;
        private readonly IConfiguration _configuration;
        private readonly IGoodreadsImportService _goodreadsImportService;
        private readonly IImportReindexService _importReindexService;
        private readonly IBookRatingEnrichmentService _ratingEnrichmentService;

        public UploadController(
            MediaLibraryDbContext context,
            ILogger<UploadController> logger,
            IThumbnailStorageService thumbnailStorage,
            IAmazonS3? s3Client,
            IConfiguration configuration,
            IGoodreadsImportService goodreadsImportService,
            IImportReindexService importReindexService,
            IBookRatingEnrichmentService ratingEnrichmentService)
        {
            _context = context;
            _logger = logger;
            _thumbnailStorage = thumbnailStorage;
            _s3Client = s3Client;
            _configuration = configuration;
            _goodreadsImportService = goodreadsImportService;
            _importReindexService = importReindexService;
            _ratingEnrichmentService = ratingEnrichmentService;
        }

        // POST: api/upload/thumbnail-from-url
        [HttpPost("thumbnail-from-url")]
        public async Task<IActionResult> UploadThumbnailFromUrl([FromBody] UploadFromUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.Url))
            {
                return BadRequest("URL is required.");
            }

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "MyMediaVerse/1.0");

                using var response = await httpClient.GetAsync(request.Url);
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest($"Failed to download image from URL: {response.StatusCode}");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                if (!AllowedImageContentTypes.Contains(contentType.ToLowerInvariant()))
                {
                    return BadRequest("URL must point to an image (JPEG, PNG, GIF, or WebP).");
                }

                using var imageStream = await response.Content.ReadAsStreamAsync();
                var uploadResult = await _thumbnailStorage.UploadStreamAsync(imageStream, contentType, ThumbnailKeyPrefix);

                if (uploadResult == null)
                {
                    return StatusCode(500, "DigitalOcean Spaces is not configured. Please configure DigitalOceanSpaces environment variables.");
                }

                _logger.LogInformation("Successfully uploaded thumbnail from URL: {Url} -> {PublicUrl}", request.Url, uploadResult.PublicUrl);
                return Ok(new { url = uploadResult.PublicUrl, fileName = uploadResult.Key, originalUrl = request.Url });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error downloading image from URL: {Url}", request.Url);
                return StatusCode(500, $"Error downloading image from URL: {ex.Message}");
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Error uploading thumbnail to DigitalOcean Spaces");
                return StatusCode(500, $"Error uploading to DigitalOcean Spaces: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during thumbnail upload from URL");
                return StatusCode(500, new { error = "Failed to upload thumbnail from URL", details = ex.Message });
            }
        }

        // DELETE: api/upload/thumbnail
        [HttpDelete("thumbnail")]
        public async Task<IActionResult> DeleteThumbnail([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("Thumbnail URL is required.");
            }

            await _thumbnailStorage.DeleteAsync(url);
            return Ok(new { message = "Thumbnail deletion processed", url });
        }

        // POST: api/upload/thumbnail
        [HttpPost("thumbnail")]
        public async Task<IActionResult> UploadThumbnail(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (!AllowedImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest("File must be an image (JPEG, PNG, GIF, or WebP).");
            }

            const int maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return BadRequest("File size must be less than 5MB.");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadResult = await _thumbnailStorage.UploadStreamAsync(memoryStream, file.ContentType, ThumbnailKeyPrefix);
                if (uploadResult == null)
                {
                    return StatusCode(500, "DigitalOcean Spaces is not configured. Please configure DigitalOceanSpaces environment variables.");
                }

                _logger.LogInformation("Successfully uploaded thumbnail: {Url}", uploadResult.PublicUrl);
                return Ok(new { url = uploadResult.PublicUrl, fileName = uploadResult.Key });
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Error uploading thumbnail to DigitalOcean Spaces. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}",
                    ex.StatusCode, ex.ErrorCode);
                return StatusCode(500, $"Error uploading to DigitalOcean Spaces: [{ex.StatusCode}] {ex.ErrorCode} - {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during thumbnail upload");
                return StatusCode(500, new { error = "Failed to upload thumbnail", details = ex.Message });
            }
        }

        // GET: api/upload/spaces-status
        [HttpGet("spaces-status")]
        public async Task<IActionResult> CheckSpacesStatus()
        {
            try
            {
                if (_s3Client == null)
                {
                    return Ok(new { status = "not_configured", message = "S3 client is not configured" });
                }

                var spacesConfig = _configuration.GetSection("DigitalOceanSpaces");
                var bucketName = spacesConfig["BucketName"];
                var endpoint = spacesConfig["Endpoint"];
                var region = spacesConfig["Region"];

                if (string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(endpoint))
                {
                    return Ok(new { status = "not_configured", message = "DigitalOcean Spaces configuration is incomplete" });
                }

                // Try listing objects with max 1 result to verify connectivity and auth
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = 1
                };

                var response = await _s3Client.ListObjectsV2Async(listRequest);

                return Ok(new
                {
                    status = "connected",
                    bucket = bucketName,
                    endpoint = endpoint,
                    region = region,
                    objectCount = response.KeyCount
                });
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Spaces health check failed. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}", ex.StatusCode, ex.ErrorCode);
                return Ok(new
                {
                    status = "error",
                    errorCode = ex.ErrorCode,
                    statusCode = (int)ex.StatusCode,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Spaces health check");
                return Ok(new { status = "error", message = ex.Message });
            }
        }

        // POST: api/upload/csv
        [HttpPost("csv")]
        public async Task<IActionResult> UploadCsv(IFormFile file, [FromForm] string? mediaType = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("File must be a CSV");
                }

                var results = new List<object>();
                var errors = new List<string>();
                var importedItems = new List<object>();
                int successCount = 0;
                int errorCount = 0;

                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                // Read the header
                csv.Read();
                csv.ReadHeader();
                var headers = csv.HeaderRecord;

                if (headers == null)
                {
                    return BadRequest("CSV file must have headers");
                }

                // Check if MediaType column exists in the CSV
                var hasMediaTypeColumn = headers.Any(h => h.Equals("MediaType", StringComparison.OrdinalIgnoreCase));
                MediaType? fixedMediaType = null;

                if (!hasMediaTypeColumn && string.IsNullOrEmpty(mediaType))
                {
                    return BadRequest("CSV file must include a 'MediaType' column, or you must specify a media type parameter");
                }

                // If mediaType parameter is provided, use it for all rows (legacy behavior)
                if (!string.IsNullOrEmpty(mediaType))
                {
                    if (!Enum.TryParse<MediaType>(mediaType, true, out var parsedType))
                    {
                        return BadRequest($"Invalid media type: {mediaType}. Supported types: Book, Movie, TVShow, Article, Video, Website");
                    }
                    fixedMediaType = parsedType;
                    _logger.LogInformation("Processing CSV upload with fixed media type: {MediaType}", fixedMediaType);
                }
                else
                {
                    _logger.LogInformation("Processing CSV upload with per-row media types from MediaType column");
                }

                // Process rows based on media type (either from column or parameter)
                while (csv.Read())
                {
                    try
                    {
                        BaseMediaItem? mediaItem = null;
                        MediaType rowMediaType;

                        // Determine media type for this row
                        if (fixedMediaType.HasValue)
                        {
                            rowMediaType = fixedMediaType.Value;
                        }
                        else
                        {
                            // Read MediaType from the current row
                            var mediaTypeStr = GetCsvValue(csv, "MediaType");
                            if (string.IsNullOrEmpty(mediaTypeStr))
                            {
                                errors.Add($"Row {csv.CurrentIndex}: MediaType column is empty");
                                errorCount++;
                                continue;
                            }

                            if (!Enum.TryParse<MediaType>(mediaTypeStr, true, out rowMediaType))
                            {
                                errors.Add($"Row {csv.CurrentIndex}: Invalid media type '{mediaTypeStr}'");
                                errorCount++;
                                continue;
                            }
                        }

                        switch (rowMediaType)
                        {
                            case MediaType.Book:
                                mediaItem = await ProcessBookRow(csv);
                                break;
                            case MediaType.Movie:
                                mediaItem = await ProcessMovieRow(csv);
                                break;
                            case MediaType.TVShow:
                                mediaItem = await ProcessTvShowRow(csv);
                                break;
                            case MediaType.Article:
                                mediaItem = await ProcessArticleRow(csv);
                                break;
                            case MediaType.Video:
                                mediaItem = await ProcessVideoRow(csv);
                                break;
                            case MediaType.Website:
                                mediaItem = await ProcessWebsiteRow(csv);
                                break;
                            case MediaType.Podcast:
                                // Podcast requires special handling - check if it's a series or episode
                                var podcastTypeStr = GetCsvValue(csv, "PodcastType");
                                if (podcastTypeStr?.Equals("Episode", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    errors.Add($"Row {csv.CurrentIndex}: PodcastEpisode import via CSV not yet supported. Please use the Import Media page.");
                                    errorCount++;
                                    continue;
                                }
                                else
                                {
                                    errors.Add($"Row {csv.CurrentIndex}: PodcastSeries import via CSV not yet supported. Please use the Import Media page.");
                                    errorCount++;
                                    continue;
                                }
                            default:
                                errors.Add($"Row {csv.CurrentIndex}: Unsupported media type {rowMediaType}");
                                errorCount++;
                                continue;
                        }

                        if (mediaItem != null)
                        {
                            // Add to the appropriate DbSet based on the media type
                            if (mediaItem is Book book)
                            {
                                _context.Books.Add(book);
                                importedItems.Add(new
                                {
                                    Id = book.Id,
                                    Title = book.Title,
                                    Author = book.Author,
                                    Thumbnail = book.Thumbnail,
                                    MediaType = "Book"
                                });
                            }
                            else if (mediaItem is Movie movie)
                            {
                                _context.Movies.Add(movie);
                                importedItems.Add(new
                                {
                                    Id = movie.Id,
                                    Title = movie.Title,
                                    Director = movie.Director,
                                    ReleaseYear = movie.ReleaseYear,
                                    Thumbnail = movie.Thumbnail,
                                    MediaType = "Movie"
                                });
                            }
                            else if (mediaItem is TvShow tvShow)
                            {
                                _context.TvShows.Add(tvShow);
                                importedItems.Add(new
                                {
                                    Id = tvShow.Id,
                                    Title = tvShow.Title,
                                    Creator = tvShow.Creator,
                                    FirstAirYear = tvShow.FirstAirYear,
                                    Thumbnail = tvShow.Thumbnail,
                                    MediaType = "TVShow"
                                });
                            }
                            else if (mediaItem is Article article)
                            {
                                _context.Articles.Add(article);
                                importedItems.Add(new
                                {
                                    Id = article.Id,
                                    Title = article.Title,
                                    Author = article.Author,
                                    Link = article.Link,
                                    IsArchived = article.IsArchived,
                                    IsStarred = article.IsStarred,
                                    MediaType = "Article"
                                });
                            }
                            else if (mediaItem is Video video)
                            {
                                _context.Videos.Add(video);
                                importedItems.Add(new
                                {
                                    Id = video.Id,
                                    Title = video.Title,
                                    Platform = video.Platform,
                                    Thumbnail = video.Thumbnail,
                                    MediaType = "Video"
                                });
                            }
                            else if (mediaItem is Website website)
                            {
                                _context.Websites.Add(website);
                                importedItems.Add(new
                                {
                                    Id = website.Id,
                                    Title = website.Title,
                                    Link = website.Link,
                                    Thumbnail = website.Thumbnail,
                                    MediaType = "Website"
                                });
                            }
                            else if (mediaItem is PodcastSeries podcastSeries)
                            {
                                _context.PodcastSeries.Add(podcastSeries);
                                importedItems.Add(new
                                {
                                    Id = podcastSeries.Id,
                                    Title = podcastSeries.Title,
                                    Thumbnail = podcastSeries.Thumbnail,
                                    MediaType = "PodcastSeries"
                                });
                            }
                            else if (mediaItem is PodcastEpisode podcastEpisode)
                            {
                                _context.PodcastEpisodes.Add(podcastEpisode);
                                importedItems.Add(new
                                {
                                    Id = podcastEpisode.Id,
                                    Title = podcastEpisode.Title,
                                    Thumbnail = podcastEpisode.Thumbnail,
                                    MediaType = "PodcastEpisode"
                                });
                            }
                            else
                            {
                                _context.MediaItems.Add(mediaItem);
                                importedItems.Add(new
                                {
                                    Id = mediaItem.Id,
                                    Title = mediaItem.Title,
                                    Thumbnail = mediaItem.Thumbnail,
                                    MediaType = mediaItem.MediaType.ToString()
                                });
                            }
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {csv.CurrentIndex}: {ex.Message}");
                        errorCount++;
                        _logger.LogWarning(ex, "Error processing row {RowIndex}", csv.CurrentIndex);
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync();

                // Make the imported items searchable immediately (best-effort; never fails the import).
                await _importReindexService.ReindexAfterImportAsync(successCount, "CSV upload");

                var result = new
                {
                    Success = true,
                    Message = $"Processed {successCount + errorCount} rows. {successCount} successful, {errorCount} errors.",
                    SuccessCount = successCount,
                    ErrorCount = errorCount,
                    Errors = errors,
                    ImportedItems = importedItems
                };

                _logger.LogInformation("CSV upload completed: {SuccessCount} successful, {ErrorCount} errors", successCount, errorCount);
                _logger.LogInformation("Response result: {@Result}", result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV upload");
                return StatusCode(500, new { error = "Failed to process CSV upload", details = ex.Message });
            }
        }

        /// <summary>
        /// Import books from a Goodreads CSV export file
        /// </summary>
        /// <param name="file">The Goodreads CSV export file</param>
        /// <param name="updateExisting">Whether to update existing books on match (default: true)</param>
        /// <param name="chunkIndex">Optional chunk index for chunked uploads</param>
        /// <param name="totalChunks">Optional total chunks for chunked uploads</param>
        /// <returns>Import result with counts and any errors</returns>
        [HttpPost("goodreads-csv")]
        public async Task<IActionResult> UploadGoodreadsCsv(
            IFormFile file,
            [FromQuery] bool updateExisting = true,
            [FromQuery] int? chunkIndex = null,
            [FromQuery] int? totalChunks = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "File must be a CSV" });
                }

                _logger.LogInformation("Processing Goodreads CSV upload: {FileName}, updateExisting={UpdateExisting}, chunk={ChunkIndex}/{TotalChunks}",
                    file.FileName, updateExisting, chunkIndex, totalChunks);

                using var stream = file.OpenReadStream();
                var result = await _goodreadsImportService.ImportFromCsvAsync(stream, updateExisting);

                // import -> enrich -> embed for the interactive path: derive the MMV Rating enum from
                // the raw Goodreads rating that import just stored (import itself does no conversion),
                // then reindex. Best-effort: a conversion hiccup must not fail an otherwise-good import.
                if (result.SuccessCount > 0)
                {
                    try
                    {
                        await _ratingEnrichmentService.ConvertGoodreadsRatingsAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Goodreads rating conversion after import failed (non-fatal)");
                    }
                }

                // Make the imported books searchable immediately (best-effort; never fails the import).
                // Fires per chunk that actually imported books — the bulk reindex skips unchanged items,
                // so earlier chunks' books aren't re-embedded.
                await _importReindexService.ReindexAfterImportAsync(result.SuccessCount, "Goodreads CSV");

                // Include chunk info in response for frontend progress tracking
                if (chunkIndex.HasValue && totalChunks.HasValue)
                {
                    return Ok(new
                    {
                        chunkIndex = chunkIndex.Value,
                        totalChunks = totalChunks.Value,
                        result
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Goodreads CSV upload");
                return StatusCode(500, new { error = "Failed to process Goodreads CSV upload", details = ex.Message });
            }
        }

        private Task<Book?> ProcessBookRow(CsvReader csv)
        {
            var book = new Book
            {
                Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                MediaType = MediaType.Book,
                Author = GetCsvValue(csv, "Author") ?? "Unknown Author",
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            book.Description = GetCsvValue(csv, "Description");
            book.Link = GetCsvValue(csv, "Link");
            book.Notes = GetCsvValue(csv, "Notes");
            book.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            book.Thumbnail = GetCsvValue(csv, "Thumbnail");
            // Note: Genre is now handled through the navigation property via ProcessTopicsAndGenres
            book.ISBN = GetCsvValue(csv, "ISBN");
            book.ASIN = GetCsvValue(csv, "ASIN");

            // Debug logging for thumbnail
            _logger.LogInformation("Book '{Title}' thumbnail: {Thumbnail}", book.Title, book.Thumbnail ?? "null");
            
            // Parse boolean and enum fields
            if (bool.TryParse(GetCsvValue(csv, "PartOfSeries"), out bool partOfSeries))
                book.PartOfSeries = partOfSeries;

            var formatStr = GetCsvValue(csv, "Format");
            if (!string.IsNullOrEmpty(formatStr) && Enum.TryParse<BookFormat>(formatStr, true, out BookFormat format))
                book.Format = format;

            var ratingStr = GetCsvValue(csv, "Rating");
            if (!string.IsNullOrEmpty(ratingStr) && Enum.TryParse<Rating>(ratingStr, true, out Rating rating))
                book.Rating = rating;

            var ownershipStr = GetCsvValue(csv, "OwnershipStatus");
            if (!string.IsNullOrEmpty(ownershipStr) && Enum.TryParse<OwnershipStatus>(ownershipStr, true, out OwnershipStatus ownership))
                book.OwnershipStatus = ownership;

            // Parse Goodreads rating (1-5 scale)
            var goodreadsRatingStr = GetCsvValue(csv, "GoodreadsRating");
            if (!string.IsNullOrEmpty(goodreadsRatingStr) && decimal.TryParse(goodreadsRatingStr, out decimal goodreadsRating))
            {
                if (goodreadsRating >= 1 && goodreadsRating <= 5)
                {
                    book.GoodreadsRating = goodreadsRating;
                    
                    // If Rating (PLB rating) is not set, auto-convert from Goodreads rating
                    if (!book.Rating.HasValue)
                    {
                        book.Rating = goodreadsRating switch
                        {
                            5 => Rating.SuperLike,
                            4 => Rating.Like,
                            3 => Rating.Neutral,
                            >= 1 and < 3 => Rating.Dislike,
                            _ => null
                        };
                    }
                }
            }

            // Parse dates
            var dateCompletedStr = GetCsvValue(csv, "DateCompleted");
            if (!string.IsNullOrEmpty(dateCompletedStr) && DateTime.TryParse(dateCompletedStr, out DateTime dateCompleted))
                book.DateCompleted = DateTime.SpecifyKind(dateCompleted, DateTimeKind.Utc);

            // Note: Topics and Genres can be assigned later through the UI
            // For now, we'll just create the basic book entity

            return Task.FromResult<Book?>(book);
        }

        private Task<Movie?> ProcessMovieRow(CsvReader csv)
        {
            var movie = new Movie
            {
                Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                MediaType = MediaType.Movie,
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            movie.Description = GetCsvValue(csv, "Description");
            movie.Link = GetCsvValue(csv, "Link");
            movie.Notes = GetCsvValue(csv, "Notes");
            movie.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            movie.Thumbnail = GetCsvValue(csv, "Thumbnail");
            movie.Director = GetCsvValue(csv, "Director");
            movie.Cast = GetCsvValue(csv, "Cast");
            movie.Tagline = GetCsvValue(csv, "Tagline");
            movie.Homepage = GetCsvValue(csv, "Homepage");
            movie.OriginalLanguage = GetCsvValue(csv, "OriginalLanguage");
            movie.OriginalTitle = GetCsvValue(csv, "OriginalTitle");
            movie.ImdbId = GetCsvValue(csv, "ImdbId");
            movie.TmdbId = GetCsvValue(csv, "TmdbId");
            movie.MpaaRating = GetCsvValue(csv, "MpaaRating");

            // Parse numeric fields
            var releaseYearStr = GetCsvValue(csv, "ReleaseYear");
            if (!string.IsNullOrEmpty(releaseYearStr) && int.TryParse(releaseYearStr, out int releaseYear))
                movie.ReleaseYear = releaseYear;

            var runtimeStr = GetCsvValue(csv, "RuntimeMinutes");
            if (!string.IsNullOrEmpty(runtimeStr) && int.TryParse(runtimeStr, out int runtime))
                movie.RuntimeMinutes = runtime;

            var tmdbRatingStr = GetCsvValue(csv, "TmdbRating");
            if (!string.IsNullOrEmpty(tmdbRatingStr) && double.TryParse(tmdbRatingStr, out double tmdbRating))
                movie.TmdbRating = tmdbRating;

            // Parse enums
            var ratingStr = GetCsvValue(csv, "Rating");
            if (!string.IsNullOrEmpty(ratingStr) && Enum.TryParse<Rating>(ratingStr, true, out Rating rating))
                movie.Rating = rating;

            var ownershipStr = GetCsvValue(csv, "OwnershipStatus");
            if (!string.IsNullOrEmpty(ownershipStr) && Enum.TryParse<OwnershipStatus>(ownershipStr, true, out OwnershipStatus ownership))
                movie.OwnershipStatus = ownership;

            // Parse dates
            var dateCompletedStr = GetCsvValue(csv, "DateCompleted");
            if (!string.IsNullOrEmpty(dateCompletedStr) && DateTime.TryParse(dateCompletedStr, out DateTime dateCompleted))
                movie.DateCompleted = DateTime.SpecifyKind(dateCompleted, DateTimeKind.Utc);

            return Task.FromResult<Movie?>(movie);
        }

        private Task<TvShow?> ProcessTvShowRow(CsvReader csv)
        {
            var tvShow = new TvShow
            {
                Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                MediaType = MediaType.TVShow,
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            tvShow.Description = GetCsvValue(csv, "Description");
            tvShow.Link = GetCsvValue(csv, "Link");
            tvShow.Notes = GetCsvValue(csv, "Notes");
            tvShow.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            tvShow.Thumbnail = GetCsvValue(csv, "Thumbnail");
            tvShow.Creator = GetCsvValue(csv, "Creator");
            tvShow.Cast = GetCsvValue(csv, "Cast");
            tvShow.Tagline = GetCsvValue(csv, "Tagline");
            tvShow.Homepage = GetCsvValue(csv, "Homepage");
            tvShow.OriginalLanguage = GetCsvValue(csv, "OriginalLanguage");
            tvShow.OriginalName = GetCsvValue(csv, "OriginalName");
            tvShow.TmdbId = GetCsvValue(csv, "TmdbId");
            tvShow.ContentRating = GetCsvValue(csv, "ContentRating");

            // Parse numeric fields
            var firstAirYearStr = GetCsvValue(csv, "FirstAirYear");
            if (!string.IsNullOrEmpty(firstAirYearStr) && int.TryParse(firstAirYearStr, out int firstAirYear))
                tvShow.FirstAirYear = firstAirYear;

            var lastAirYearStr = GetCsvValue(csv, "LastAirYear");
            if (!string.IsNullOrEmpty(lastAirYearStr) && int.TryParse(lastAirYearStr, out int lastAirYear))
                tvShow.LastAirYear = lastAirYear;

            var numberOfSeasonsStr = GetCsvValue(csv, "NumberOfSeasons");
            if (!string.IsNullOrEmpty(numberOfSeasonsStr) && int.TryParse(numberOfSeasonsStr, out int numberOfSeasons))
                tvShow.NumberOfSeasons = numberOfSeasons;

            var numberOfEpisodesStr = GetCsvValue(csv, "NumberOfEpisodes");
            if (!string.IsNullOrEmpty(numberOfEpisodesStr) && int.TryParse(numberOfEpisodesStr, out int numberOfEpisodes))
                tvShow.NumberOfEpisodes = numberOfEpisodes;

            var tmdbRatingStr = GetCsvValue(csv, "TmdbRating");
            if (!string.IsNullOrEmpty(tmdbRatingStr) && double.TryParse(tmdbRatingStr, out double tmdbRating))
                tvShow.TmdbRating = tmdbRating;

            // Parse enums
            var ratingStr = GetCsvValue(csv, "Rating");
            if (!string.IsNullOrEmpty(ratingStr) && Enum.TryParse<Rating>(ratingStr, true, out Rating rating))
                tvShow.Rating = rating;

            var ownershipStr = GetCsvValue(csv, "OwnershipStatus");
            if (!string.IsNullOrEmpty(ownershipStr) && Enum.TryParse<OwnershipStatus>(ownershipStr, true, out OwnershipStatus ownership))
                tvShow.OwnershipStatus = ownership;

            // Parse dates
            var dateCompletedStr = GetCsvValue(csv, "DateCompleted");
            if (!string.IsNullOrEmpty(dateCompletedStr) && DateTime.TryParse(dateCompletedStr, out DateTime dateCompleted))
                tvShow.DateCompleted = DateTime.SpecifyKind(dateCompleted, DateTimeKind.Utc);

            return Task.FromResult<TvShow?>(tvShow);
        }

        private Task<Article?> ProcessArticleRow(CsvReader csv)
        {
            var article = new Article
            {
                Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                MediaType = MediaType.Article,
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            article.Description = GetCsvValue(csv, "Description");
            article.Link = GetCsvValue(csv, "Url") ?? GetCsvValue(csv, "Link"); // Support both column names
            article.Notes = GetCsvValue(csv, "Notes");
            article.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            article.Thumbnail = GetCsvValue(csv, "Thumbnail");
            article.Author = GetCsvValue(csv, "Author");
            article.Publication = GetCsvValue(csv, "Publication");
            // InstapaperBookmarkId and InstapaperHash fields removed - Readwise is now the only sync source

            // Parse boolean fields
            var isArchivedStr = GetCsvValue(csv, "IsArchived") ?? GetCsvValue(csv, "Archived");
            if (!string.IsNullOrEmpty(isArchivedStr))
            {
                article.IsArchived = isArchivedStr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                    isArchivedStr.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                                    isArchivedStr.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }

            var isStarredStr = GetCsvValue(csv, "IsStarred") ?? GetCsvValue(csv, "Starred");
            if (!string.IsNullOrEmpty(isStarredStr))
            {
                article.IsStarred = isStarredStr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                   isStarredStr.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                                   isStarredStr.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }

            // Parse numeric fields
            var wordCountStr = GetCsvValue(csv, "WordCount");
            if (!string.IsNullOrEmpty(wordCountStr) && int.TryParse(wordCountStr, out int wordCount))
                article.WordCount = wordCount;

            var readingProgressStr = GetCsvValue(csv, "ReadingProgress");
            if (!string.IsNullOrEmpty(readingProgressStr) && int.TryParse(readingProgressStr, out int readingProgress))
                article.ReadingProgress = readingProgress;

            // Parse dates
            var publicationDateStr = GetCsvValue(csv, "PublicationDate");
            if (!string.IsNullOrEmpty(publicationDateStr) && DateTime.TryParse(publicationDateStr, out DateTime publicationDate))
                article.PublicationDate = DateTime.SpecifyKind(publicationDate, DateTimeKind.Utc);

            var dateCompletedStr = GetCsvValue(csv, "DateCompleted");
            if (!string.IsNullOrEmpty(dateCompletedStr) && DateTime.TryParse(dateCompletedStr, out DateTime dateCompleted))
                article.DateCompleted = DateTime.SpecifyKind(dateCompleted, DateTimeKind.Utc);

            // Parse enums
            var ratingStr = GetCsvValue(csv, "Rating");
            if (!string.IsNullOrEmpty(ratingStr) && Enum.TryParse<Rating>(ratingStr, true, out Rating rating))
                article.Rating = rating;

            var ownershipStr = GetCsvValue(csv, "OwnershipStatus");
            if (!string.IsNullOrEmpty(ownershipStr) && Enum.TryParse<OwnershipStatus>(ownershipStr, true, out OwnershipStatus ownership))
                article.OwnershipStatus = ownership;

            return Task.FromResult<Article?>(article);
        }

        private Task<Video?> ProcessVideoRow(CsvReader csv)
        {
            var video = new Video
            {
                Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                MediaType = MediaType.Video,
                Platform = GetCsvValue(csv, "Platform") ?? "YouTube", // Default to YouTube, required field
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            video.Description = GetCsvValue(csv, "Description");
            video.Link = GetCsvValue(csv, "Link");
            video.Notes = GetCsvValue(csv, "Notes");
            video.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            video.Thumbnail = GetCsvValue(csv, "Thumbnail");
            video.ExternalId = GetCsvValue(csv, "VideoId") ?? GetCsvValue(csv, "ExternalId");

            // Parse numeric fields
            var lengthStr = GetCsvValue(csv, "LengthInSeconds") ?? GetCsvValue(csv, "DurationInSeconds");
            if (!string.IsNullOrEmpty(lengthStr) && int.TryParse(lengthStr, out int length))
                video.LengthInSeconds = length;

            // Parse dates
            var dateCompletedStr = GetCsvValue(csv, "DateCompleted");
            if (!string.IsNullOrEmpty(dateCompletedStr) && DateTime.TryParse(dateCompletedStr, out DateTime dateCompleted))
                video.DateCompleted = DateTime.SpecifyKind(dateCompleted, DateTimeKind.Utc);

            // Parse enums
            var ratingStr = GetCsvValue(csv, "Rating");
            if (!string.IsNullOrEmpty(ratingStr) && Enum.TryParse<Rating>(ratingStr, true, out Rating rating))
                video.Rating = rating;

            var ownershipStr = GetCsvValue(csv, "OwnershipStatus");
            if (!string.IsNullOrEmpty(ownershipStr) && Enum.TryParse<OwnershipStatus>(ownershipStr, true, out OwnershipStatus ownership))
                video.OwnershipStatus = ownership;

            return Task.FromResult<Video?>(video);
        }

        private Task<Website?> ProcessWebsiteRow(CsvReader csv)
        {
            var website = new Website
            {
                Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                MediaType = MediaType.Website,
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            website.Description = GetCsvValue(csv, "Description");
            website.Link = GetCsvValue(csv, "Link");
            website.Notes = GetCsvValue(csv, "Notes");
            website.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            website.Thumbnail = GetCsvValue(csv, "Thumbnail");
            website.Domain = GetCsvValue(csv, "Domain");
            website.RssFeedUrl = GetCsvValue(csv, "RssFeedUrl");
            website.Author = GetCsvValue(csv, "Author");
            website.Publication = GetCsvValue(csv, "Publication");

            // Parse dates
            var lastCheckedStr = GetCsvValue(csv, "LastCheckedDate");
            if (!string.IsNullOrEmpty(lastCheckedStr) && DateTime.TryParse(lastCheckedStr, out DateTime lastChecked))
                website.LastCheckedDate = DateTime.SpecifyKind(lastChecked, DateTimeKind.Utc);

            var dateCompletedStr = GetCsvValue(csv, "DateCompleted");
            if (!string.IsNullOrEmpty(dateCompletedStr) && DateTime.TryParse(dateCompletedStr, out DateTime dateCompleted))
                website.DateCompleted = DateTime.SpecifyKind(dateCompleted, DateTimeKind.Utc);

            // Parse enums
            var ratingStr = GetCsvValue(csv, "Rating");
            if (!string.IsNullOrEmpty(ratingStr) && Enum.TryParse<Rating>(ratingStr, true, out Rating rating))
                website.Rating = rating;

            var ownershipStr = GetCsvValue(csv, "OwnershipStatus");
            if (!string.IsNullOrEmpty(ownershipStr) && Enum.TryParse<OwnershipStatus>(ownershipStr, true, out OwnershipStatus ownership))
                website.OwnershipStatus = ownership;

            return Task.FromResult<Website?>(website);
        }

        private static string? GetCsvValue(CsvReader csv, string fieldName)
        {
            try
            {
                return csv.GetField(fieldName)?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static Status? ParseStatus(string? statusStr)
        {
            if (string.IsNullOrEmpty(statusStr))
                return null;

            return Enum.TryParse<Status>(statusStr, true, out Status status) ? status : null;
        }
    }

    public class UploadFromUrlRequest
    {
        public string Url { get; set; } = string.Empty;
    }
}


