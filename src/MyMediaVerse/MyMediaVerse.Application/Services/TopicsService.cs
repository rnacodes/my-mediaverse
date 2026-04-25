using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    public class TopicsService : ITopicsService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TopicsService> _logger;

        public TopicsService(IApplicationDbContext context, ILogger<TopicsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<TopicResponseDto>> GetAllTopicsAsync()
        {
            var topics = await _context.Topics
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    Count = t.MediaItems.Count
                })
                .ToListAsync();

            return topics.Select(t => new TopicResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = t.Count
            }).ToList();
        }

        public async Task<IReadOnlyList<TopicResponseDto>> SearchTopicsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllTopicsAsync();
            }

            var normalizedQuery = query.ToLowerInvariant();
            var topics = await _context.Topics
                .AsNoTracking()
                .Where(t => t.Name.ToLower().Contains(normalizedQuery))
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    Count = t.MediaItems.Count
                })
                .ToListAsync();

            return topics.Select(t => new TopicResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = t.Count
            }).ToList();
        }

        public async Task<TopicResponseDto?> GetTopicAsync(Guid id)
        {
            var topic = await _context.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null)
            {
                return null;
            }

            var mediaItemIds = await _context.MediaItems
                .Where(m => m.Topics.Any(t => t.Id == id))
                .Select(m => m.Id)
                .ToListAsync();

            return new TopicResponseDto
            {
                Id = topic.Id,
                Name = topic.Name,
                MediaItemIds = mediaItemIds.ToArray(),
                MediaItemCount = mediaItemIds.Count
            };
        }

        public async Task<(TopicResponseDto Topic, bool Created)> CreateTopicAsync(CreateTopicDto dto)
        {
            var normalizedTopicName = dto.Name.Trim().ToLowerInvariant();

            var existingTopic = await _context.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);

            if (existingTopic != null)
            {
                var mediaItemIds = await _context.MediaItems
                    .Where(m => m.Topics.Any(t => t.Id == existingTopic.Id))
                    .Select(m => m.Id)
                    .ToListAsync();

                return (new TopicResponseDto
                {
                    Id = existingTopic.Id,
                    Name = existingTopic.Name,
                    MediaItemIds = mediaItemIds.ToArray(),
                    MediaItemCount = mediaItemIds.Count
                }, false);
            }

            var topic = new Topic { Name = normalizedTopicName };
            _context.Add(topic);
            await _context.SaveChangesAsync();

            return (new TopicResponseDto
            {
                Id = topic.Id,
                Name = topic.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = 0
            }, true);
        }

        public async Task<TopicResponseDto?> UpdateTopicAsync(Guid id, CreateTopicDto dto)
        {
            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == id);
            if (topic == null)
            {
                return null;
            }

            var normalizedTopicName = dto.Name.Trim().ToLowerInvariant();
            var conflict = await _context.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == normalizedTopicName && t.Id != id);

            if (conflict != null)
            {
                throw new InvalidOperationException($"A topic with the name '{dto.Name}' already exists.");
            }

            topic.Name = normalizedTopicName;
            await _context.SaveChangesAsync();

            var mediaItemIds = await _context.MediaItems
                .Where(m => m.Topics.Any(t => t.Id == id))
                .Select(m => m.Id)
                .ToListAsync();

            return new TopicResponseDto
            {
                Id = topic.Id,
                Name = topic.Name,
                MediaItemIds = mediaItemIds.ToArray(),
                MediaItemCount = mediaItemIds.Count
            };
        }

        public async Task<bool> DeleteTopicAsync(Guid id)
        {
            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == id);
            if (topic == null)
            {
                return false;
            }

            // Cascade delete in the database removes the join-table associations.
            _context.Remove(topic);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BulkImportResultDto> ImportTopicsFromJsonAsync(IReadOnlyList<CreateTopicDto> topics)
        {
            var result = new BulkImportResultDto();
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

                    var imported = await ImportSingleTopicAsync(topicDto.Name, result);
                    if (imported != null)
                    {
                        result.Imported.Add(imported);
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing topic '{Name}'", topicDto.Name);
                    result.Errors.Add($"Topic '{topicDto.Name}': {ex.Message}");
                    result.ErrorCount++;
                }
            }

            return result;
        }

        public async Task<BulkImportResultDto> ImportTopicsFromCsvAsync(Stream csvStream)
        {
            var result = new BulkImportResultDto();
            using var reader = new StreamReader(csvStream);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLowerInvariant()
            };
            using var csv = new CsvReader(reader, csvConfig);

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;
            if (headers == null || !headers.Any(h => h.Equals("Name", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("CSV file must have a 'Name' column");
            }

            while (csv.Read())
            {
                result.TotalProcessed++;
                var name = csv.GetField("name");

                try
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.Errors.Add($"Row {csv.CurrentIndex}: Name is required");
                        result.ErrorCount++;
                        continue;
                    }

                    var imported = await ImportSingleTopicAsync(name, result);
                    if (imported != null)
                    {
                        result.Imported.Add(imported);
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing topic from CSV row {Row}", csv.CurrentIndex);
                    result.Errors.Add($"Row {csv.CurrentIndex}: {ex.Message}");
                    result.ErrorCount++;
                }
            }

            return result;
        }

        private async Task<TopicResponseDto?> ImportSingleTopicAsync(string name, BulkImportResultDto result)
        {
            var normalizedTopicName = name.Trim().ToLowerInvariant();
            var existingTopic = await _context.Topics
                .FirstOrDefaultAsync(t => t.Name == normalizedTopicName);

            if (existingTopic != null)
            {
                result.Skipped.Add($"Topic '{name}' already exists");
                result.SkippedCount++;
                return null;
            }

            var topic = new Topic { Name = normalizedTopicName };
            _context.Add(topic);
            await _context.SaveChangesAsync();

            return new TopicResponseDto
            {
                Id = topic.Id,
                Name = topic.Name,
                MediaItemIds = Array.Empty<Guid>(),
                MediaItemCount = 0
            };
        }
    }
}
