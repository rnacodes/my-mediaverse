using Microsoft.AspNetCore.Mvc;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopicsController : ControllerBase
    {
        private readonly ITopicsService _topicsService;
        private readonly ILogger<TopicsController> _logger;

        public TopicsController(ITopicsService topicsService, ILogger<TopicsController> logger)
        {
            _topicsService = topicsService;
            _logger = logger;
        }

        // GET: api/topics
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TopicResponseDto>>> GetAllTopics()
        {
            try
            {
                var topics = await _topicsService.GetAllTopicsAsync();
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving topics");
                return StatusCode(500, new { error = "Failed to retrieve topics", details = ex.Message });
            }
        }

        // GET: api/topics/search?query={query}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<TopicResponseDto>>> SearchTopics([FromQuery] string query)
        {
            try
            {
                var topics = await _topicsService.SearchTopicsAsync(query);
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching topics");
                return StatusCode(500, new { error = "Failed to search topics", details = ex.Message });
            }
        }

        // GET: api/topics/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TopicResponseDto>> GetTopic(Guid id)
        {
            try
            {
                var topic = await _topicsService.GetTopicAsync(id);
                if (topic == null)
                {
                    return NotFound($"Topic with ID {id} not found.");
                }
                return Ok(topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving topic {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve topic", details = ex.Message });
            }
        }

        // POST: api/topics
        [HttpPost]
        public async Task<ActionResult<TopicResponseDto>> CreateTopic([FromBody] CreateTopicDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Topic name is required.");
            }

            var (topic, created) = await _topicsService.CreateTopicAsync(dto);
            return created
                ? CreatedAtAction(nameof(GetTopic), new { id = topic.Id }, topic)
                : Ok(topic);
        }

        // PUT: api/topics/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TopicResponseDto>> UpdateTopic(Guid id, [FromBody] CreateTopicDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Topic name is required.");
            }

            try
            {
                var topic = await _topicsService.UpdateTopicAsync(id, dto);
                if (topic == null)
                {
                    return NotFound($"Topic with ID {id} not found.");
                }
                return Ok(topic);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/topics/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTopic(Guid id)
        {
            var deleted = await _topicsService.DeleteTopicAsync(id);
            if (!deleted)
            {
                return NotFound($"Topic with ID {id} not found.");
            }
            return NoContent();
        }

        // POST: api/topics/import/json
        [HttpPost("import/json")]
        public async Task<ActionResult<BulkImportResultDto>> ImportTopicsFromJson([FromBody] List<CreateTopicDto> topics)
        {
            if (topics == null || !topics.Any())
            {
                return BadRequest("No topics provided for import.");
            }

            var result = await _topicsService.ImportTopicsFromJsonAsync(topics);
            return Ok(result);
        }

        // POST: api/topics/import/csv
        [HttpPost("import/csv")]
        public async Task<ActionResult<BulkImportResultDto>> ImportTopicsFromCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("File must be a CSV");
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _topicsService.ImportTopicsFromCsvAsync(stream);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV file");
                return StatusCode(500, $"Error processing CSV file: {ex.Message}");
            }
        }
    }
}
