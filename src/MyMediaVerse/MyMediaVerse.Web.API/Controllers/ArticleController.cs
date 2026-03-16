using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class ArticleController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly IArticleMappingService _articleMappingService;
        private readonly IReaderService? _readerService;
        private readonly IArticleDeduplicationService? _deduplicationService;
        private readonly IWebsiteScraperService? _websiteScraperService;
        private readonly ILogger<ArticleController> _logger;

        public ArticleController(
            IArticleService articleService,
            IArticleMappingService articleMappingService,
            ILogger<ArticleController> logger,
            IReaderService? readerService = null,
            IArticleDeduplicationService? deduplicationService = null,
            IWebsiteScraperService? websiteScraperService = null)
        {
            _articleService = articleService;
            _articleMappingService = articleMappingService;
            _logger = logger;
            _readerService = readerService;
            _deduplicationService = deduplicationService;
            _websiteScraperService = websiteScraperService;
        }

        // GET: api/article
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArticleResponseDto>>> GetAllArticles()
        {
            try
            {
                var articles = await _articleService.GetAllArticlesAsync();
                var response = await _articleMappingService.MapToResponseDtoAsync(articles);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all articles");
                return StatusCode(500, new { error = "Failed to retrieve articles", details = ex.Message });
            }
        }

        // GET: api/article/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleResponseDto>> GetArticle(Guid id)
        {
            try
            {
                var article = await _articleService.GetArticleByIdAsync(id);
                if (article == null)
                {
                    return NotFound($"Article with ID {id} not found.");
                }

                var response = await _articleMappingService.MapToResponseDtoAsync(article);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve article", details = ex.Message });
            }
        }

        // GET: api/article/by-author/{author}
        [HttpGet("by-author/{author}")]
        public async Task<ActionResult<IEnumerable<ArticleResponseDto>>> GetArticlesByAuthor(string author)
        {
            try
            {
                var articles = await _articleService.GetArticlesByAuthorAsync(author);
                var response = await _articleMappingService.MapToResponseDtoAsync(articles);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving articles by author: {Author}", author);
                return StatusCode(500, new { error = "Failed to retrieve articles by author", details = ex.Message });
            }
        }

        // GET: api/article/archived
        [HttpGet("archived")]
        public async Task<ActionResult<IEnumerable<ArticleResponseDto>>> GetArchivedArticles()
        {
            try
            {
                var articles = await _articleService.GetArchivedArticlesAsync();
                var response = await _articleMappingService.MapToResponseDtoAsync(articles);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving archived articles");
                return StatusCode(500, new { error = "Failed to retrieve archived articles", details = ex.Message });
            }
        }

        // GET: api/article/starred
        [HttpGet("starred")]
        public async Task<ActionResult<IEnumerable<ArticleResponseDto>>> GetStarredArticles()
        {
            try
            {
                var articles = await _articleService.GetStarredArticlesAsync();
                var response = await _articleMappingService.MapToResponseDtoAsync(articles);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving starred articles");
                return StatusCode(500, new { error = "Failed to retrieve starred articles", details = ex.Message });
            }
        }

        // POST: api/article
        [HttpPost]
        public async Task<IActionResult> CreateArticle([FromBody] CreateArticleDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Article data is required");
                }

                var article = await _articleService.CreateArticleAsync(dto);
                var response = await _articleMappingService.MapToResponseDtoAsync(article);

                return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating article");
                return StatusCode(500, new { error = "Failed to create article", details = ex.Message });
            }
        }

        // PUT: api/article/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticle(Guid id, [FromBody] CreateArticleDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Article data is required");
                }

                var article = await _articleService.UpdateArticleAsync(id, dto);
                var response = await _articleMappingService.MapToResponseDtoAsync(article);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Article with ID {Id} not found for update", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update article", details = ex.Message });
            }
        }

        // DELETE: api/article/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticle(Guid id)
        {
            try
            {
                var deleted = await _articleService.DeleteArticleAsync(id);
                if (!deleted)
                {
                    return NotFound($"Article with ID {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to delete article", details = ex.Message });
            }
        }

        // GET: api/article/{id}/content
        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetArticleContent(Guid id)
        {
            try
            {
                var content = await _articleService.GetArticleContentAsync(id);
                if (content == null)
                {
                    return NotFound($"Content for article with ID {id} not found.");
                }

                return Ok(new { content });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving content for article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve article content", details = ex.Message });
            }
        }

        // POST: api/article/{id}/content
        [HttpPost("{id}/content")]
        public async Task<IActionResult> UpdateArticleContent(Guid id, [FromBody] ArticleContentUpdateDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.HtmlContent))
                {
                    return BadRequest("HTML content is required");
                }

                var success = await _articleService.UpdateArticleContentAsync(id, dto.HtmlContent);
                if (!success)
                {
                    return NotFound($"Article with ID {id} not found or S3 storage not configured.");
                }

                return Ok(new { message = "Article content updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating content for article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update article content", details = ex.Message });
            }
        }

        // PUT: api/article/{id}/sync-status
        [HttpPut("{id}/sync-status")]
        public async Task<IActionResult> UpdateArticleSyncStatus(Guid id, [FromBody] ArticleSyncStatusUpdateDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Sync status data is required");
                }

                var article = await _articleService.UpdateArticleSyncStatusAsync(id, dto.IsArchived, dto.IsStarred);
                var response = await _articleMappingService.MapToResponseDtoAsync(article);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Article with ID {Id} not found for sync status update", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating sync status for article with ID {Id}", id);
                return StatusCode(500, new { error = "Failed to update article sync status", details = ex.Message });
            }
        }

        // POST: api/article/sync-reader
        [HttpPost("sync-reader")]
        public async Task<ActionResult<ReaderSyncResultDto>> SyncFromReader([FromQuery] string? location = null)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                _logger.LogInformation("Starting Reader document sync (location: {Location})", location ?? "all");
                var result = await _readerService.SyncDocumentsAsync(location);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing from Reader");
                return StatusCode(500, new { error = "Failed to sync from Reader", details = ex.Message });
            }
        }

        // POST: api/article/{id}/fetch-content
        [HttpPost("{id}/fetch-content")]
        public async Task<IActionResult> FetchArticleContent(Guid id)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                var success = await _readerService.FetchAndStoreArticleContentAsync(id);
                if (!success)
                {
                    return NotFound(new { error = "Article not found or content unavailable" });
                }
                return Ok(new { message = "Content fetched and stored successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching content for article {Id}", id);
                return StatusCode(500, new { error = "Failed to fetch content", details = ex.Message });
            }
        }

        // POST: api/article/bulk-fetch-content
        [HttpPost("bulk-fetch-content")]
        public async Task<IActionResult> BulkFetchContent([FromQuery] int batchSize = 50)
        {
            try
            {
                if (_readerService == null)
                {
                    return StatusCode(500, new { error = "Reader service not configured" });
                }

                var count = await _readerService.BulkFetchArticleContentsAsync(batchSize);
                
                var message = count > 0 
                    ? $"Successfully fetched content for {count} article{(count == 1 ? "" : "s")}"
                    : "No articles found to fetch content for. Sync documents from Readwise Reader first.";
                    
                return Ok(new { fetchedCount = count, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk content fetch");
                return StatusCode(500, new { error = "Failed to bulk fetch content", details = ex.Message });
            }
        }

        // POST: api/article/scrape-preview
        [HttpPost("scrape-preview")]
        public async Task<IActionResult> ScrapePreview([FromBody] ArticleScrapeRequestDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Url))
                {
                    return BadRequest(new { error = "URL is required" });
                }

                if (_websiteScraperService == null)
                {
                    return StatusCode(500, new { error = "Website scraper service not configured" });
                }

                _logger.LogInformation("Scraping article metadata from URL: {Url}", dto.Url);
                var scrapedData = await _websiteScraperService.ScrapeWebsiteAsync(dto.Url);
                return Ok(scrapedData);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid URL for scrape preview: {Url}", dto?.Url);
                return BadRequest(new { error = "Invalid URL", details = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch URL for scrape preview: {Url}", dto?.Url);
                return BadRequest(new { error = "Failed to fetch URL", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping article metadata from URL: {Url}", dto?.Url);
                return StatusCode(500, new { error = "Failed to scrape article metadata", details = ex.Message });
            }
        }

    }

    // Helper DTOs for specific endpoints
    public class ArticleScrapeRequestDto
    {
        public required string Url { get; set; }
    }
    public class ArticleContentUpdateDto
    {
        public required string HtmlContent { get; set; }
    }

    public class ArticleSyncStatusUpdateDto
    {
        public bool IsArchived { get; set; }
        public bool IsStarred { get; set; }
    }
    
    // Deduplication endpoints added at the end of ArticleController
    public partial class ArticleController
    {
        // POST: api/article/deduplicate
        [HttpPost("deduplicate")]
        public async Task<ActionResult<DeduplicationResultDto>> DeduplicateArticles()
        {
            try
            {
                if (_deduplicationService == null)
                {
                    return StatusCode(500, new { error = "Deduplication service not configured" });
                }

                _logger.LogInformation("Starting article deduplication");
                var result = await _deduplicationService.FindAndMergeDuplicatesAsync();

                if (result.Success)
                {
                    _logger.LogInformation("Deduplication completed successfully. Merged {Count} articles", 
                        result.MergedCount);
                    return Ok(result);
                }
                else
                {
                    _logger.LogError("Deduplication failed: {Error}", result.ErrorMessage);
                    return StatusCode(500, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during article deduplication");
                return StatusCode(500, new { 
                    error = "Failed to deduplicate articles", 
                    details = ex.Message 
                });
            }
        }

        // GET: api/article/duplicates
        [HttpGet("duplicates")]
        public async Task<ActionResult<List<DuplicateGroupDto>>> FindDuplicates()
        {
            try
            {
                if (_deduplicationService == null)
                {
                    return StatusCode(500, new { error = "Deduplication service not configured" });
                }

                _logger.LogInformation("Finding duplicate articles");
                var duplicates = await _deduplicationService.FindDuplicatesAsync();

                return Ok(new 
                { 
                    count = duplicates.Count,
                    totalDuplicates = duplicates.Sum(g => g.Articles.Count - 1),
                    groups = duplicates 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding duplicate articles");
                return StatusCode(500, new { 
                    error = "Failed to find duplicates", 
                    details = ex.Message 
                });
            }
        }
    }
}
