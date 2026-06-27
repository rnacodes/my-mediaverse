using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for AI-powered operations including note description generation.
    /// </summary>
    public class AIService : IAIService
    {
        private readonly IApplicationDbContext _context;
        private readonly IGradientAIClient _gradientClient;
        private readonly ILogger<AIService> _logger;

        // Prompt templates
        private const string NoteDescriptionSystemPrompt = @"You are a helpful assistant that creates concise, informative descriptions for notes in a personal knowledge management system.
Your descriptions should:
- Be 2-3 sentences long
- Capture the main topic and purpose of the note
- Be written in third person
- Focus on what the note is about, not how it's written
- Avoid phrases like 'This note discusses...' - just describe the content directly";

        private const string NoteDescriptionUserPromptTemplate = @"Generate a description for this note:

Title: {0}
Tags: {1}
Content (excerpt):
{2}";

        public AIService(
            IApplicationDbContext context,
            IGradientAIClient gradientClient,
            ILogger<AIService> logger)
        {
            _context = context;
            _gradientClient = gradientClient;
            _logger = logger;
        }

        #region Note Description Generation

        public async Task<string?> GenerateNoteDescriptionAsync(Guid noteId, CancellationToken cancellationToken = default)
        {
            try
            {
                var note = await _context.Notes
                    .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

                if (note == null)
                {
                    _logger.LogWarning("Note {NoteId} not found for description generation", noteId);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(note.Content))
                {
                    _logger.LogWarning("Note {NoteId} has no content for description generation", noteId);
                    return null;
                }

                // Prepare the prompt
                var contentExcerpt = TruncateContent(note.Content, 2000);
                var tagsString = note.Tags?.Count > 0 ? string.Join(", ", note.Tags) : "none";
                var userPrompt = string.Format(NoteDescriptionUserPromptTemplate, note.Title, tagsString, contentExcerpt);

                // Generate description
                var description = await _gradientClient.GenerateTextAsync(
                    userPrompt,
                    NoteDescriptionSystemPrompt,
                    maxTokens: 200,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(description))
                {
                    _logger.LogWarning("Empty description generated for note {NoteId}", noteId);
                    return null;
                }

                // Update the note
                note.AiDescription = description;
                note.AiDescriptionGeneratedAt = DateTime.UtcNow;

                // If there's no manual description, also update the main Description field
                if (!note.IsDescriptionManual && string.IsNullOrWhiteSpace(note.Description))
                {
                    note.Description = description;
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Generated AI description for note {NoteId} ({Title})", noteId, note.Title);
                return description;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating description for note {NoteId}", noteId);
                throw;
            }
        }

        public async Task<AIBatchResultDto> GenerateNoteDescriptionsBatchAsync(int batchSize = 20, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new AIBatchResultDto();

            try
            {
                // Get notes that need descriptions
                var notes = await _context.Notes
                    .Where(n => n.AiDescription == null
                             && n.Content != null
                             && !n.IsDescriptionManual)
                    .OrderBy(n => n.DateImported)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = notes.Count;

                if (notes.Count == 0)
                {
                    _logger.LogInformation("No notes need description generation");
                    stopwatch.Stop();
                    result.DurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                _logger.LogInformation("Starting batch description generation for {Count} notes", notes.Count);

                foreach (var note in notes)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Batch description generation cancelled");
                        break;
                    }

                    try
                    {
                        var description = await GenerateNoteDescriptionAsync(note.Id, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(description))
                        {
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }

                        // Add delay to avoid rate limiting
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Note {note.Id} ({note.Title}): {ex.Message}");
                        _logger.LogWarning(ex, "Failed to generate description for note {NoteId}", note.Id);
                    }
                }

                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Batch description generation completed: {Success} succeeded, {Failed} failed, {Skipped} skipped in {Duration}ms",
                    result.SuccessCount, result.FailedCount, result.SkippedCount, result.DurationMs);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error in batch description generation");
                throw;
            }
        }

        public async Task<int> GetNotesNeedingDescriptionCountAsync()
        {
            return await _context.Notes
                .CountAsync(n => n.AiDescription == null
                              && n.Content != null
                              && !n.IsDescriptionManual);
        }

        #endregion

        #region Status

        public async Task<AIStatusDto> GetStatusAsync()
        {
            var isAvailable = await IsAvailableAsync();

            return new AIStatusDto
            {
                IsAvailable = isAvailable,
                GenerationModel = _gradientClient.GenerationModelName,
                GenerationProvider = "DigitalOcean",
                PendingNoteDescriptions = await GetNotesNeedingDescriptionCountAsync(),
                StatusMessage = isAvailable ? "AI services are available and ready" : "AI services are not configured or unavailable"
            };
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await _gradientClient.IsAvailableAsync();
        }

        #endregion

        #region Helper Methods

        private static string TruncateContent(string content, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Remove excessive whitespace
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();

            if (content.Length <= maxLength)
                return content;

            // Try to truncate at a word boundary
            var truncated = content.Substring(0, maxLength);
            var lastSpace = truncated.LastIndexOf(' ');

            if (lastSpace > maxLength * 0.8) // Don't go back too far
            {
                truncated = truncated.Substring(0, lastSpace);
            }

            return truncated + "...";
        }

        #endregion
    }
}
