using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Data;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
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
        private readonly ITmdbService _tmdbService;
        private readonly IMovieService _movieService;
        private readonly IMovieMappingService _movieMappingService;
        private readonly ITvShowService _tvShowService;
        private readonly ITvShowMappingService _tvShowMappingService;

        public UploadController(
            MediaLibraryDbContext context,
            ILogger<UploadController> logger,
            IThumbnailStorageService thumbnailStorage,
            IAmazonS3? s3Client,
            IConfiguration configuration,
            IGoodreadsImportService goodreadsImportService,
            IImportReindexService importReindexService,
            IBookRatingEnrichmentService ratingEnrichmentService,
            ITmdbService tmdbService,
            IMovieService movieService,
            IMovieMappingService movieMappingService,
            ITvShowService tvShowService,
            ITvShowMappingService tvShowMappingService)
        {
            _context = context;
            _logger = logger;
            _thumbnailStorage = thumbnailStorage;
            _s3Client = s3Client;
            _configuration = configuration;
            _goodreadsImportService = goodreadsImportService;
            _importReindexService = importReindexService;
            _ratingEnrichmentService = ratingEnrichmentService;
            _tmdbService = tmdbService;
            _movieService = movieService;
            _movieMappingService = movieMappingService;
            _tvShowService = tvShowService;
            _tvShowMappingService = tvShowMappingService;
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
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be a CSV" });
            }

            // A media type sent with the file applies to every row; without one, each row names its own.
            MediaType? fixedMediaType = null;
            if (!string.IsNullOrEmpty(mediaType))
            {
                if (!Enum.TryParse<MediaType>(mediaType, true, out var parsedType))
                {
                    return BadRequest(new { error = $"Invalid media type: {mediaType}. Supported types: Book, Movie, TVShow, Article, Video, Website" });
                }
                fixedMediaType = parsedType;
            }

            var result = new CsvUploadResultDto { StartedAt = DateTime.UtcNow };
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord == null)
                {
                    return BadRequest(new { error = "CSV file must have headers" });
                }

                var hasMediaTypeColumn = csv.HeaderRecord.Any(h => h.Equals("MediaType", StringComparison.OrdinalIgnoreCase));
                if (!hasMediaTypeColumn && !fixedMediaType.HasValue)
                {
                    return BadRequest(new { error = "CSV file must include a 'MediaType' column, or you must specify a media type parameter" });
                }

                _logger.LogInformation("Processing CSV upload, media type: {MediaType}", fixedMediaType?.ToString() ?? "per row");

                var topicResolver = new TopicResolver(_context);
                var genreResolver = new GenreResolver(_context);
                var unknownChannelRows = new List<int>();

                while (csv.Read())
                {
                    var row = csv.Parser.Row;
                    try
                    {
                        MediaType rowMediaType;
                        if (fixedMediaType.HasValue)
                        {
                            rowMediaType = fixedMediaType.Value;
                        }
                        else
                        {
                            var mediaTypeStr = GetCsvValue(csv, "MediaType");
                            if (string.IsNullOrEmpty(mediaTypeStr))
                            {
                                throw new InvalidOperationException("MediaType column is empty");
                            }

                            if (!Enum.TryParse<MediaType>(mediaTypeStr, true, out rowMediaType))
                            {
                                throw new InvalidOperationException($"Invalid media type '{mediaTypeStr}'");
                            }
                        }

                        // Each arm returns null for an item that is already in the library.
                        BaseMediaItem? mediaItem;
                        string alreadyStored;
                        switch (rowMediaType)
                        {
                            case MediaType.Book:
                                mediaItem = await ProcessBookRow(csv);
                                alreadyStored = "a book with this ISBN/ASIN or title+author already exists";
                                break;
                            case MediaType.Movie:
                                mediaItem = await ProcessMovieRow(csv);
                                alreadyStored = "a movie with this TMDB id or title and year already exists";
                                break;
                            case MediaType.TVShow:
                                mediaItem = await ProcessTvShowRow(csv);
                                alreadyStored = "a TV show with this TMDB id or title and year already exists";
                                break;
                            case MediaType.Article:
                                mediaItem = await ProcessArticleRow(csv);
                                alreadyStored = "an article with this URL already exists";
                                break;
                            case MediaType.Video:
                                mediaItem = await ProcessVideoRow(csv, unknownChannelRows);
                                alreadyStored = "a video with this ID or link already exists";
                                break;
                            case MediaType.Website:
                                mediaItem = await ProcessWebsiteRow(csv);
                                alreadyStored = "a website with this URL already exists";
                                break;
                            case MediaType.Podcast:
                                throw new InvalidOperationException("Podcast import via CSV is not supported. Please use the Import Media page.");
                            default:
                                throw new InvalidOperationException($"Unsupported media type {rowMediaType}");
                        }

                        if (mediaItem == null)
                        {
                            result.Skipped.Add($"Row {row}: {alreadyStored}; skipped");
                            result.SkippedCount++;
                            continue;
                        }

                        // Movie and TV rows are saved by their own services. Every other row is saved
                        // here, one at a time, so a repeat later in the same file finds it and is skipped.
                        if (_context.Entry(mediaItem).State == EntityState.Detached)
                        {
                            await ApplyTagColumnsAsync(mediaItem, csv, topicResolver, genreResolver);
                            _context.Add(mediaItem);
                            await SaveRowAsync(mediaItem);
                        }

                        result.ImportedItems.Add(new CsvImportedItemDto
                        {
                            Id = mediaItem.Id,
                            Title = mediaItem.Title,
                            MediaType = mediaItem.MediaType.ToString()
                        });
                        result.CreatedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {row}: {ex.Message}");
                        result.FailedCount++;
                        _logger.LogWarning(ex, "Error processing CSV row {Row}", row);
                    }
                }

                if (unknownChannelRows.Count > 0)
                {
                    result.WarningMessage = $"{unknownChannelRows.Count} video row(s) named a channel that is not in the library and were stored without one (rows {string.Join(", ", unknownChannelRows)}).";
                }

                // Make the imported items searchable immediately (best-effort; never fails the import).
                result.ReindexTriggered = result.CreatedCount > 0;
                await _importReindexService.ReindexAfterImportAsync(result.CreatedCount, "CSV upload");

                result.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("CSV upload completed: {Created} created, {Skipped} skipped, {Failed} failed",
                    result.CreatedCount, result.SkippedCount, result.FailedCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV upload");
                result.Success = false;
                result.ErrorMessage = $"Failed to process CSV upload: {ex.Message}";
                return StatusCode(500, result);
            }
        }

        // Saves one row. A row the database rejects leaves tracking on its own, so the rows after it
        // can still save.
        private async Task SaveRowAsync(BaseMediaItem item)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                item.Topics.Clear();
                item.Genres.Clear();
                _context.ChangeTracker.DetectChanges();
                _context.Entry(item).State = EntityState.Detached;
                throw;
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
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be a CSV" });
            }

            var startedAt = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Processing Goodreads CSV upload: {FileName}, updateExisting={UpdateExisting}, chunk={ChunkIndex}/{TotalChunks}",
                    file.FileName, updateExisting, chunkIndex, totalChunks);

                using var stream = file.OpenReadStream();
                var result = await _goodreadsImportService.ImportFromCsvAsync(stream, updateExisting);

                if (!result.Success)
                {
                    // Fatal per the reporting contract: 500 with the result body (bare, even for a
                    // chunked call, so the client's failure path reads one shape).
                    return StatusCode(500, result);
                }

                // import -> enrich -> embed for the interactive path: derive the MMV Rating enum from
                // the raw Goodreads rating that import just stored (import itself does no conversion),
                // then reindex. Best-effort: a conversion hiccup must not fail an otherwise-good import.
                //
                // ConvertGoodreadsRatingsAsync scans the whole rated library, so run it once per upload
                // rather than per chunk: on the final chunk of a chunked upload (earlier chunks' books
                // are covered by the same full-library pass), or on a non-chunked upload that imported
                // something.
                var isChunked = chunkIndex.HasValue && totalChunks.HasValue;
                var isFinalChunk = isChunked && chunkIndex!.Value == totalChunks!.Value - 1;
                if (isFinalChunk || (!isChunked && result.SuccessCount > 0))
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
                result.ReindexTriggered = result.SuccessCount > 0;
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
                return StatusCode(500, new GoodreadsImportResultDto
                {
                    Success = false,
                    ErrorMessage = "Failed to process Goodreads CSV upload",
                    StartedAt = startedAt
                });
            }
        }

        private async Task<Book?> ProcessBookRow(CsvReader csv)
        {
            var title = GetCsvValue(csv, "Title") ?? "Unknown Title";
            var author = GetCsvValue(csv, "Author") ?? "Unknown Author";
            var rawIsbn = GetCsvValue(csv, "ISBN");
            var asin = GetCsvValue(csv, "ASIN");

            // Dedup like the Article branch: skip rows that match an existing book
            // (by ISBN/ASIN or title+author) instead of silently inserting a duplicate.
            var existing = await BookDuplicateFinder.FindExistingAsync(_context.Books, new BookIdentity
            {
                Isbn = rawIsbn,
                Asin = asin,
                Title = title,
                Author = author
            });
            if (existing != null)
            {
                _logger.LogInformation("CSV row {RowIndex}: book already exists for '{Title}' by {Author} (ID: {Id}); skipping",
                    csv.CurrentIndex, title, author, existing.Id);
                return null;
            }

            var book = new Book
            {
                Title = title,
                MediaType = MediaType.Book,
                Author = author,
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            book.Description = GetCsvValue(csv, "Description");
            book.Link = GetCsvValue(csv, "Link");
            book.Notes = GetCsvValue(csv, "Notes");
            book.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            book.Thumbnail = GetCsvValue(csv, "Thumbnail");
            book.ISBN = IsbnNormalizer.Normalize(rawIsbn) ?? rawIsbn;
            book.ASIN = asin;

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
                        book.Rating = RatingConverter.ConvertGoodreadsRatingToPLBRating(goodreadsRating);
                    }
                }
            }

            // Parse dates (invariant culture so a CSV parses the same on any host locale)
            var dateCompletedStr = GetCsvValue(csv, "DateCompleted");
            if (!string.IsNullOrEmpty(dateCompletedStr) &&
                DateTime.TryParse(dateCompletedStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateCompleted))
                book.DateCompleted = DateTime.SpecifyKind(dateCompleted, DateTimeKind.Utc);

            book.Publisher = GetCsvValue(csv, "Publisher");
            book.YearPublished = ParseInt(GetCsvValue(csv, "YearPublished"));
            book.DateRead = ParseDate(GetCsvValue(csv, "DateRead"));
            book.MyReview = GetCsvValue(csv, "MyReview");

            return book;
        }

        // Returns null when the movie is already in the library. A row with a TMDB id takes its
        // details from TMDB; a row without one is stored as typed.
        private async Task<Movie?> ProcessMovieRow(CsvReader csv)
        {
            CreateMovieDto dto;
            var fromTmdb = int.TryParse(GetCsvValue(csv, "TmdbId"), out var tmdbId);
            if (fromTmdb)
            {
                if (await _movieService.GetMovieByTmdbIdAsync(tmdbId.ToString()) != null)
                    return null;

                var tmdbMovie = await FromTmdbAsync(_tmdbService.GetMovieDetailsAsync(tmdbId), tmdbId);
                dto = TmdbCreateDtoMapper.ToCreateDto(await _movieMappingService.MapFromTmdbAsync(tmdbMovie));
            }
            else
            {
                dto = new CreateMovieDto
                {
                    Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                    Description = GetCsvValue(csv, "Description"),
                    Link = GetCsvValue(csv, "Link"),
                    Thumbnail = GetCsvValue(csv, "Thumbnail"),
                    Director = GetCsvValue(csv, "Director"),
                    Cast = GetCsvValue(csv, "Cast"),
                    Tagline = GetCsvValue(csv, "Tagline"),
                    Homepage = GetCsvValue(csv, "Homepage"),
                    OriginalLanguage = GetCsvValue(csv, "OriginalLanguage"),
                    OriginalTitle = GetCsvValue(csv, "OriginalTitle"),
                    ImdbId = GetCsvValue(csv, "ImdbId"),
                    TmdbId = GetCsvValue(csv, "TmdbId"),
                    MpaaRating = GetCsvValue(csv, "MpaaRating"),
                    ReleaseYear = ParseInt(GetCsvValue(csv, "ReleaseYear")),
                    RuntimeMinutes = ParseInt(GetCsvValue(csv, "RuntimeMinutes")),
                    TmdbRating = ParseDouble(GetCsvValue(csv, "TmdbRating"))
                };
            }

            // The owner's own columns apply either way.
            dto.Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted;
            dto.Rating = ParseEnum<Rating>(GetCsvValue(csv, "Rating"));
            dto.OwnershipStatus = ParseEnum<OwnershipStatus>(GetCsvValue(csv, "OwnershipStatus"));
            dto.DateCompleted = ParseDate(GetCsvValue(csv, "DateCompleted"));
            dto.Notes = GetCsvValue(csv, "Notes");
            dto.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            dto.Topics = SplitTagColumn(GetCsvValue(csv, "Topics")).ToArray();
            dto.Genres = dto.Genres.Concat(SplitTagColumn(GetCsvValue(csv, "Genres"))).Distinct().ToArray();

            var result = await _movieService.CreateMovieAsync(dto, fromTmdb);
            return result.Created ? result.Movie : null;
        }

        // Returns null when the show is already in the library. Same two paths as a movie row.
        private async Task<TvShow?> ProcessTvShowRow(CsvReader csv)
        {
            CreateTvShowDto dto;
            var fromTmdb = int.TryParse(GetCsvValue(csv, "TmdbId"), out var tmdbId);
            if (fromTmdb)
            {
                if (await _tvShowService.GetTvShowByTmdbIdAsync(tmdbId.ToString()) != null)
                    return null;

                var tmdbTvShow = await FromTmdbAsync(_tmdbService.GetTvShowDetailsAsync(tmdbId), tmdbId);
                dto = TmdbCreateDtoMapper.ToCreateDto(await _tvShowMappingService.MapFromTmdbAsync(tmdbTvShow));
            }
            else
            {
                dto = new CreateTvShowDto
                {
                    Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                    Description = GetCsvValue(csv, "Description"),
                    Link = GetCsvValue(csv, "Link"),
                    Thumbnail = GetCsvValue(csv, "Thumbnail"),
                    Creator = GetCsvValue(csv, "Creator"),
                    Cast = GetCsvValue(csv, "Cast"),
                    Tagline = GetCsvValue(csv, "Tagline"),
                    Homepage = GetCsvValue(csv, "Homepage"),
                    OriginalLanguage = GetCsvValue(csv, "OriginalLanguage"),
                    OriginalName = GetCsvValue(csv, "OriginalName"),
                    TmdbId = GetCsvValue(csv, "TmdbId"),
                    ContentRating = GetCsvValue(csv, "ContentRating"),
                    FirstAirYear = ParseInt(GetCsvValue(csv, "FirstAirYear")),
                    LastAirYear = ParseInt(GetCsvValue(csv, "LastAirYear")),
                    NumberOfSeasons = ParseInt(GetCsvValue(csv, "NumberOfSeasons")),
                    NumberOfEpisodes = ParseInt(GetCsvValue(csv, "NumberOfEpisodes")),
                    TmdbRating = ParseDouble(GetCsvValue(csv, "TmdbRating"))
                };
            }

            dto.Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted;
            dto.Rating = ParseEnum<Rating>(GetCsvValue(csv, "Rating"));
            dto.OwnershipStatus = ParseEnum<OwnershipStatus>(GetCsvValue(csv, "OwnershipStatus"));
            dto.DateCompleted = ParseDate(GetCsvValue(csv, "DateCompleted"));
            dto.Notes = GetCsvValue(csv, "Notes");
            dto.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            dto.Topics = SplitTagColumn(GetCsvValue(csv, "Topics")).ToArray();
            dto.Genres = dto.Genres.Concat(SplitTagColumn(GetCsvValue(csv, "Genres"))).Distinct().ToArray();

            var result = await _tvShowService.CreateTvShowAsync(dto, fromTmdb);
            return result.Created ? result.TvShow : null;
        }

        // A TMDB failure fails the row with a message that names the id, not the whole upload.
        private static async Task<T> FromTmdbAsync<T>(Task<T> lookup, int tmdbId) where T : class
        {
            T? found;
            try
            {
                found = await lookup;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"TMDB lookup for id {tmdbId} failed: {ex.Message}", ex);
            }

            return found ?? throw new InvalidOperationException($"TMDB has nothing with id {tmdbId}");
        }

        // Returns null when an article with the row's URL already exists.
        private async Task<Article?> ProcessArticleRow(CsvReader csv)
        {
            var rawLink = GetCsvValue(csv, "Url") ?? GetCsvValue(csv, "Link"); // Support both column names
            var normalizedLink = string.IsNullOrWhiteSpace(rawLink) ? rawLink : UrlNormalizer.Normalize(rawLink);

            var existing = await ArticleDuplicateFinder.FindExistingAsync(_context.Articles, null, rawLink);
            if (existing != null)
            {
                _logger.LogInformation("CSV row {RowIndex}: article already exists for {Url} (ID: {Id}); skipping",
                    csv.CurrentIndex, normalizedLink, existing.Id);
                return null;
            }

            var article = new Article
            {
                Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                MediaType = MediaType.Article,
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            article.Description = GetCsvValue(csv, "Description");
            article.Link = normalizedLink;
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

            return article;
        }

        private async Task<Video?> ProcessVideoRow(CsvReader csv, List<int> unknownChannelRows)
        {
            var title = GetCsvValue(csv, "Title");
            var link = GetCsvValue(csv, "Link");
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
            {
                throw new InvalidOperationException("a video row needs both a Title and a Link");
            }

            var platform = GetCsvValue(csv, "Platform");
            if (string.IsNullOrWhiteSpace(platform))
            {
                platform = VideoDuplicateFinder.YouTubePlatform; // Default to YouTube, required field
            }

            // The id comes from its own column, else from the link when the link is a YouTube one.
            var externalId = VideoDuplicateFinder.NormalizeExternalId(
                GetCsvValue(csv, "VideoId") ?? GetCsvValue(csv, "ExternalId"));
            if (externalId == null && platform.Equals(VideoDuplicateFinder.YouTubePlatform, StringComparison.OrdinalIgnoreCase))
            {
                externalId = VideoDuplicateFinder.ExtractYouTubeId(link);
            }

            var existing = await VideoDuplicateFinder.FindExistingAsync(_context.Videos, new VideoIdentity
            {
                Platform = platform,
                ExternalId = externalId,
                Link = link,
                Title = title
            });
            if (existing != null)
            {
                _logger.LogInformation("CSV row {RowIndex}: video already exists (ID: {Id}); skipping",
                    csv.CurrentIndex, existing.Id);
                return null;
            }

            var video = new Video
            {
                Title = title,
                MediaType = MediaType.Video,
                Platform = platform,
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            video.Description = GetCsvValue(csv, "Description");
            video.Link = link;
            video.Notes = GetCsvValue(csv, "Notes");
            video.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            video.Thumbnail = GetCsvValue(csv, "Thumbnail");
            video.ExternalId = externalId;

            // ChannelId is the YouTube channel id. Channels are curated, so an upload links to a
            // stored one and never creates one.
            var channelExternalId = GetCsvValue(csv, "ChannelId");
            if (!string.IsNullOrEmpty(channelExternalId))
            {
                var channel = await _context.YouTubeChannels
                    .FirstOrDefaultAsync(c => c.ChannelExternalId == channelExternalId);
                if (channel != null)
                    video.ChannelId = channel.Id;
                else
                    unknownChannelRows.Add(csv.Parser.Row);
            }

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

            return video;
        }

        private async Task<Website?> ProcessWebsiteRow(CsvReader csv)
        {
            var rawLink = GetCsvValue(csv, "Url") ?? GetCsvValue(csv, "Link"); // Support both column names
            var normalizedLink = string.IsNullOrWhiteSpace(rawLink) ? rawLink : UrlNormalizer.Normalize(rawLink);

            var existing = await WebsiteDuplicateFinder.FindExistingAsync(_context.Websites, rawLink);
            if (existing != null)
            {
                _logger.LogInformation("CSV row {RowIndex}: website already exists for {Url} (ID: {Id}); skipping",
                    csv.CurrentIndex, normalizedLink, existing.Id);
                return null;
            }

            var website = new Website
            {
                Title = GetCsvValue(csv, "Title") ?? "Unknown Title",
                MediaType = MediaType.Website,
                DateAdded = DateTime.UtcNow,
                Status = ParseStatus(GetCsvValue(csv, "Status")) ?? Status.Uncharted
            };

            // Optional fields
            website.Description = GetCsvValue(csv, "Description");
            website.Link = normalizedLink;
            website.UrlKey = string.IsNullOrWhiteSpace(rawLink) ? null : UrlNormalizer.GetComparisonKey(rawLink);
            website.Notes = GetCsvValue(csv, "Notes");
            website.RelatedNotes = GetCsvValue(csv, "RelatedNotes");
            website.Thumbnail = GetCsvValue(csv, "Thumbnail");
            var extractedDomain = UrlNormalizer.ExtractDomain(rawLink);
            website.Domain = string.IsNullOrEmpty(extractedDomain) ? GetCsvValue(csv, "Domain") : extractedDomain;
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

            return website;
        }

        /// <summary>
        /// Reads the optional Topics and Genres columns (semicolon-separated, pipe tolerated) onto a
        /// row's new item. Names are trimmed and lowercased like every other tag path.
        /// </summary>
        private static async Task ApplyTagColumnsAsync(BaseMediaItem item, CsvReader csv, TopicResolver topics, GenreResolver genres)
        {
            foreach (var name in SplitTagColumn(GetCsvValue(csv, "Topics")))
            {
                if (item.Topics.Any(t => t.Name == name)) continue;
                var topic = await topics.GetOrCreateAsync(name);
                if (topic != null) item.Topics.Add(topic);
            }

            foreach (var name in SplitTagColumn(GetCsvValue(csv, "Genres")))
            {
                if (item.Genres.Any(g => g.Name == name)) continue;
                var genre = await genres.GetOrCreateAsync(name);
                if (genre != null) item.Genres.Add(genre);
            }
        }

        private static IEnumerable<string> SplitTagColumn(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? Enumerable.Empty<string>()
                : value.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(v => v.ToLowerInvariant())
                    .Where(v => v.Length > 0)
                    .Distinct(StringComparer.Ordinal);

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

        private static int? ParseInt(string? value) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static double? ParseDouble(string? value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static TEnum? ParseEnum<TEnum>(string? value) where TEnum : struct, Enum =>
            !string.IsNullOrEmpty(value) && Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : null;

        // Invariant culture so a CSV parses the same on any host locale.
        private static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : null;

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


