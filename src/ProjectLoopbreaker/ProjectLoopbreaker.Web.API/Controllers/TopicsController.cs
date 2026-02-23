using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Infrastructure.Data;
using ProjectLoopbreaker.DTOs;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace ProjectLoopbreaker.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TopicsController : ControllerBase
    {
        private readonly MediaLibraryDbContext _context;

        public TopicsController(MediaLibraryDbContext context)
        {
            _context = context;
        }

        // GET: api/topics
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TopicResponseDto>>> GetAllTopics()
        {
            try
            {
                // Get all topics in a single query
                var topics = await _context.Topics
                    .AsNoTracking()
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                // Get all media item counts using LINQ navigation properties
                var topicCounts = await _context.Topics
                    .Select(t => new { TopicId = t.Id, Count = t.MediaItems.Count })
                    .ToListAsync();
                var countsByTopicId = topicCounts.ToDictionary(tc => tc.TopicId, tc => tc.Count);

                // Build response with counts (no need for individual queries)
                var response = topics.Select(topic => new TopicResponseDto
                {
                    Id = topic.Id,
                    Name = topic.Name,
                    MediaItemIds = Array.Empty<Guid>(), // Not needed for list view
                    MediaItemCount = countsByTopicId.GetValueOrDefault(topic.Id, 0)
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllTopics: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to retrieve topics", details = ex.Message });
            }
        }

        // GET: api/topics/search?query={query}
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<TopicResponseDto>>> SearchTopics([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllTopics();
            }

            try
            {
                var normalizedQuery = query.ToLowerInvariant();
                var topics = await _context.Topics
                    .AsNoTracking()
                    .Where(t => t.Name.ToLower().Contains(normalizedQuery))
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                // Get topic IDs for the filtered topics
                var topicIds = topics.Select(t => t.Id).ToList();

                // Get counts for the filtered topics using LINQ
                var topicCounts = await _context.Topics
                    .Select(t => new { TopicId = t.Id, Count = t.MediaItems.Count })
                    .ToListAsync();
                var countsByTopicId = topicCounts.ToDictionary(tc => tc.TopicId, tc => tc.Count);

                var response = topics.Select(topic => new TopicResponseDto
                {
                    Id = topic.Id,
                    Name = topic.Name,
                    MediaItemIds = Array.Empty<Guid>(),
                    MediaItemCount = countsByTopicId.GetValueOrDefault(topic.Id, 0)
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchTopics: {ex.Message}");
                return StatusCode(500, new { error = "Failed to search topics", details = ex.Message });
            }
        }

        // GET: api/topics/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TopicResponseDto>> GetTopic(Guid id)
        {
            try
            {
                var topic = await _context.Topics
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (topic == null)
                {
                    return NotFound($"Topic with ID {id} not found.");
                }

                // Get media item IDs using LINQ navigation properties
                var mediaItemIds = await _context.MediaItems
                    .Where(m => m.Topics.Any(t => t.Id == id))
                    .Select(m => m.Id)
                    .ToListAsync();

                var response = new TopicResponseDto
                {
                    Id = topic.Id,
                    Name = topic.Name,
                    MediaItemIds = mediaItemIds.ToArray(),
                    MediaItemCount = mediaItemIds.Count
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTopic: {ex.Message}");
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

            var normalizedTopicName = dto.Name.Trim().ToLowerInvariant();

            // Check if topic already exists
            var existingTopic = await _context.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);

            if (existingTopic != null)
            {
                // Get media item IDs using LINQ navigation properties
                var mediaItemIds = await _context.MediaItems
                    .Where(m => m.Topics.Any(t => t.Id == existingTopic.Id))
                    .Select(m => m.Id)
                    .ToListAsync();

                var existingResponse = new TopicResponseDto
                {
                    Id = existingTopic.Id,
                    Name = existingTopic.Name,
                    MediaItemIds = mediaItemIds.ToArray(),
                    MediaItemCount = mediaItemIds.Count
                };
                return Ok(existingResponse);
            }

            var topic = new Topic { Name = normalizedTopicName };
            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();

            var response = new TopicResponseDto
            {
                Id = topic.Id,
                Name = topic.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = 0
            };

            return CreatedAtAction(nameof(GetTopic), new { id = topic.Id }, response);
        }

        // PUT: api/topics/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TopicResponseDto>> UpdateTopic(Guid id, [FromBody] CreateTopicDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest("Topic name is required.");
            }

            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == id);
            if (topic == null)
            {
                return NotFound($"Topic with ID {id} not found.");
            }

            var normalizedTopicName = dto.Name.Trim().ToLowerInvariant();

            // Check if another topic with the new name already exists
            var existingTopic = await _context.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == normalizedTopicName && t.Id != id);

            if (existingTopic != null)
            {
                return BadRequest($"A topic with the name '{dto.Name}' already exists.");
            }

            topic.Name = normalizedTopicName;
            await _context.SaveChangesAsync();

            // Get media item IDs using LINQ navigation properties
            var mediaItemIds = await _context.MediaItems
                .Where(m => m.Topics.Any(t => t.Id == id))
                .Select(m => m.Id)
                .ToListAsync();

            var response = new TopicResponseDto
            {
                Id = topic.Id,
                Name = topic.Name,
                MediaItemIds = mediaItemIds.ToArray(),
                MediaItemCount = mediaItemIds.Count
            };

            return Ok(response);
        }

        // DELETE: api/topics/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTopic(Guid id)
        {
            var topic = await _context.Topics
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null)
            {
                return NotFound($"Topic with ID {id} not found.");
            }

            // The database is configured with cascade delete, so removing the topic
            // will automatically remove all associations in the MediaItemTopics join table
            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/topics/import/json
        [HttpPost("import/json")]
        public async Task<ActionResult<BulkImportResultDto>> ImportTopicsFromJson([FromBody] List<CreateTopicDto> topics)
        {
            var result = new BulkImportResultDto();

            if (topics == null || !topics.Any())
            {
                return BadRequest("No topics provided for import.");
            }

            foreach (var topicDto in topics)
            {
                result.TotalProcessed++;

                try
                {
                    if (string.IsNullOrWhiteSpace(topicDto.Name))
                    {
                        result.Errors.Add($"Topic at index {result.TotalProcessed - 1}: Name is required");
                        result.ErrorCount++;
                        continue;
                    }

                    var normalizedTopicName = topicDto.Name.Trim().ToLowerInvariant();

                    // Check if topic already exists
                    var existingTopic = await _context.Topics
                        .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);

                    if (existingTopic != null)
                    {
                        result.Skipped.Add($"Topic '{topicDto.Name}' already exists");
                        result.SkippedCount++;
                        continue;
                    }

                    var topic = new Topic { Name = normalizedTopicName };
                    _context.Topics.Add(topic);
                    await _context.SaveChangesAsync();

                    result.Imported.Add(new TopicResponseDto
                    {
                        Id = topic.Id,
                        Name = topic.Name,
                        MediaItemIds = Array.Empty<Guid>(),
                        MediaItemCount = 0
                    });
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Topic '{topicDto.Name}': {ex.Message}");
                    result.ErrorCount++;
                }
            }

            return Ok(result);
        }

        // POST: api/topics/import/csv
        [HttpPost("import/csv")]
        public async Task<ActionResult<BulkImportResultDto>> ImportTopicsFromCsv(IFormFile file)
        {
            var result = new BulkImportResultDto();

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
                using var reader = new StreamReader(file.OpenReadStream());
                var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLowerInvariant()
                };
                using var csv = new CsvReader(reader, csvConfig);

                csv.Read();
                csv.ReadHeader();
                var headers = csv.HeaderRecord;

                if (headers == null || !headers.Any(h => h.Equals("Name", StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("CSV file must have a 'Name' column");
                }

                while (csv.Read())
                {
                    result.TotalProcessed++;

                    try
                    {
                        var name = csv.GetField("name");

                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result.Errors.Add($"Row {csv.CurrentIndex}: Name is required");
                            result.ErrorCount++;
                            continue;
                        }

                        var normalizedTopicName = name.Trim().ToLowerInvariant();

                        // Check if topic already exists
                        var existingTopic = await _context.Topics
                            .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);

                        if (existingTopic != null)
                        {
                            result.Skipped.Add($"Topic '{name}' already exists");
                            result.SkippedCount++;
                            continue;
                        }

                        var topic = new Topic { Name = normalizedTopicName };
                        _context.Topics.Add(topic);
                        await _context.SaveChangesAsync();

                        result.Imported.Add(new TopicResponseDto
                        {
                            Id = topic.Id,
                            Name = topic.Name,
                            MediaItemIds = Array.Empty<Guid>(),
                            MediaItemCount = 0
                        });
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {csv.CurrentIndex}: {ex.Message}");
                        result.ErrorCount++;
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing CSV file: {ex.Message}");
            }
        }
    }
}
