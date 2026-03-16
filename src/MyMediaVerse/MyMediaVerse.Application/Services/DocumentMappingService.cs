using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.Paperless;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Service for mapping between Paperless-ngx DTOs and MyMediaVerse Document entities.
    /// </summary>
    public class DocumentMappingService : IDocumentMappingService
    {
        // Known genres for automatic classification
        private static readonly HashSet<string> KnownGenres = new(StringComparer.OrdinalIgnoreCase)
        {
            "fiction", "non-fiction", "nonfiction", "biography", "autobiography",
            "history", "science", "technology", "business", "finance",
            "personal", "legal", "medical", "financial", "academic",
            "reference", "manual", "guide", "tutorial", "contract",
            "invoice", "receipt", "tax", "insurance", "banking"
        };

        // ============================================
        // Entity to DTO mappings
        // ============================================

        public DocumentResponseDto MapToResponseDto(Document document)
        {
            return new DocumentResponseDto
            {
                Id = document.Id,
                Title = document.Title,
                MediaType = document.MediaType,
                Link = document.Link,
                Notes = document.Notes,
                DateAdded = document.DateAdded,
                Status = document.Status,
                DateCompleted = document.DateCompleted,
                Rating = document.Rating,
                OwnershipStatus = document.OwnershipStatus,
                Description = document.Description,
                RelatedNotes = document.RelatedNotes,
                Thumbnail = document.Thumbnail,
                Topics = document.Topics?.Select(t => t.Name).ToArray() ?? Array.Empty<string>(),
                Genres = document.Genres?.Select(g => g.Name).ToArray() ?? Array.Empty<string>(),
                PaperlessId = document.PaperlessId,
                OriginalFileName = document.OriginalFileName,
                ArchiveSerialNumber = document.ArchiveSerialNumber,
                DocumentType = document.DocumentType,
                Correspondent = document.Correspondent,
                DocumentDate = document.DocumentDate,
                PageCount = document.PageCount,
                FileType = document.FileType,
                FileSizeBytes = document.FileSizeBytes,
                FormattedFileSize = document.GetFormattedFileSize(),
                PaperlessTags = document.GetPaperlessTags().ToArray(),
                PaperlessUrl = document.PaperlessUrl,
                LastPaperlessSync = document.LastPaperlessSync,
                IsArchived = document.IsArchived
            };
        }

        public IEnumerable<DocumentResponseDto> MapToResponseDtos(IEnumerable<Document> documents)
        {
            return documents.Select(MapToResponseDto);
        }

        // ============================================
        // Paperless to Entity mappings
        // ============================================

        public Task<CreateDocumentDto> MapFromPaperlessAsync(
            PaperlessDocumentDto paperlessDocument,
            IReadOnlyDictionary<int, string> tagLookup,
            IReadOnlyDictionary<int, string> documentTypeLookup,
            IReadOnlyDictionary<int, string> correspondentLookup,
            string paperlessBaseUrl)
        {
            // Resolve tag IDs to names
            var tagNames = paperlessDocument.Tags
                .Where(tagId => tagLookup.ContainsKey(tagId))
                .Select(tagId => tagLookup[tagId])
                .ToList();

            // Resolve document type ID to name
            string? documentTypeName = null;
            if (paperlessDocument.DocumentType.HasValue &&
                documentTypeLookup.TryGetValue(paperlessDocument.DocumentType.Value, out var typeName))
            {
                documentTypeName = typeName;
            }

            // Resolve correspondent ID to name
            string? correspondentName = null;
            if (paperlessDocument.Correspondent.HasValue &&
                correspondentLookup.TryGetValue(paperlessDocument.Correspondent.Value, out var corrName))
            {
                correspondentName = corrName;
            }

            // Map tags to topics and genres
            var (topics, genres) = MapPaperlessTagsToTopicsAndGenres(tagNames);

            // Extract file type from original filename
            var fileType = !string.IsNullOrEmpty(paperlessDocument.OriginalFileName)
                ? Path.GetExtension(paperlessDocument.OriginalFileName)?.TrimStart('.').ToLowerInvariant()
                : null;

            // Build Paperless URL
            var paperlessUrl = !string.IsNullOrEmpty(paperlessBaseUrl)
                ? $"{paperlessBaseUrl.TrimEnd('/')}/documents/{paperlessDocument.Id}/"
                : null;

            // Build thumbnail URL
            var thumbnailUrl = !string.IsNullOrEmpty(paperlessBaseUrl)
                ? $"{paperlessBaseUrl.TrimEnd('/')}/api/documents/{paperlessDocument.Id}/thumb/"
                : null;

            var dto = new CreateDocumentDto
            {
                Title = paperlessDocument.Title,
                MediaType = MediaType.Document,
                Status = Status.Uncharted,
                PaperlessId = paperlessDocument.Id,
                OriginalFileName = paperlessDocument.OriginalFileName,
                ArchiveSerialNumber = paperlessDocument.ArchiveSerialNumber,
                DocumentType = documentTypeName,
                Correspondent = correspondentName,
                OcrContent = paperlessDocument.Content,
                DocumentDate = paperlessDocument.Created,
                PageCount = paperlessDocument.PageCount,
                FileType = fileType,
                PaperlessTags = tagNames.ToArray(),
                PaperlessUrl = paperlessUrl,
                Thumbnail = thumbnailUrl,
                Topics = topics.ToArray(),
                Genres = genres.ToArray(),
                IsArchived = false,
                Description = GenerateDescription(paperlessDocument, documentTypeName, correspondentName)
            };

            return Task.FromResult(dto);
        }

        public (List<string> Topics, List<string> Genres) MapPaperlessTagsToTopicsAndGenres(IEnumerable<string> paperlessTags)
        {
            var topics = new List<string>();
            var genres = new List<string>();

            foreach (var tag in paperlessTags)
            {
                var normalizedTag = tag.Trim().ToLowerInvariant();

                // Check for explicit prefixes
                if (normalizedTag.StartsWith("topic:"))
                {
                    var topicName = normalizedTag.Substring(6).Trim();
                    if (!string.IsNullOrEmpty(topicName))
                        topics.Add(topicName);
                }
                else if (normalizedTag.StartsWith("genre:"))
                {
                    var genreName = normalizedTag.Substring(6).Trim();
                    if (!string.IsNullOrEmpty(genreName))
                        genres.Add(genreName);
                }
                // Check if it's a known genre
                else if (KnownGenres.Contains(normalizedTag))
                {
                    genres.Add(normalizedTag);
                }
                // Default: treat as topic
                else
                {
                    topics.Add(normalizedTag);
                }
            }

            return (topics.Distinct().ToList(), genres.Distinct().ToList());
        }

        // ============================================
        // Lookup building helpers
        // ============================================

        public IReadOnlyDictionary<int, string> BuildTagLookup(IEnumerable<PaperlessTagDto> tags)
        {
            return tags.ToDictionary(t => t.Id, t => t.Name);
        }

        public IReadOnlyDictionary<int, string> BuildDocumentTypeLookup(IEnumerable<PaperlessDocumentTypeDto> documentTypes)
        {
            return documentTypes.ToDictionary(t => t.Id, t => t.Name);
        }

        public IReadOnlyDictionary<int, string> BuildCorrespondentLookup(IEnumerable<PaperlessCorrespondentDto> correspondents)
        {
            return correspondents.ToDictionary(c => c.Id, c => c.Name);
        }

        // ============================================
        // Private helpers
        // ============================================

        private static string GenerateDescription(
            PaperlessDocumentDto document,
            string? documentType,
            string? correspondent)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(documentType))
                parts.Add($"Type: {documentType}");

            if (!string.IsNullOrEmpty(correspondent))
                parts.Add($"From: {correspondent}");

            if (document.PageCount.HasValue && document.PageCount > 0)
                parts.Add($"{document.PageCount} page(s)");

            if (document.Created != default)
                parts.Add($"Dated: {document.Created:MMM d, yyyy}");

            return parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
        }
    }
}
