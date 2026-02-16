using FluentAssertions;
using ProjectLoopbreaker.Application.Services;
using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Shared.DTOs.Paperless;
using ProjectLoopbreaker.UnitTests.TestData;

namespace ProjectLoopbreaker.UnitTests.Application
{
    public class DocumentMappingServiceTests
    {
        private readonly DocumentMappingService _service;

        public DocumentMappingServiceTests()
        {
            _service = new DocumentMappingService();
        }

        #region MapToResponseDto

        [Fact]
        public void MapToResponseDto_ValidDocument_MapsAllProperties()
        {
            var document = TestDataFactory.CreateDocument("Invoice #123", 42, "Invoice", "ACME Corp");
            document.Description = "Monthly invoice";
            document.PageCount = 3;
            document.FileSizeBytes = 1048576; // 1 MB
            document.FileType = "pdf";
            document.PaperlessUrl = "https://paperless.local/documents/42/";
            document.IsArchived = false;
            document.Topics.Add(new Topic { Name = "finance" });
            document.Genres.Add(new Genre { Name = "invoice" });

            var result = _service.MapToResponseDto(document);

            result.Should().NotBeNull();
            result.Id.Should().Be(document.Id);
            result.Title.Should().Be("Invoice #123");
            result.PaperlessId.Should().Be(42);
            result.DocumentType.Should().Be("Invoice");
            result.Correspondent.Should().Be("ACME Corp");
            result.Description.Should().Be("Monthly invoice");
            result.PageCount.Should().Be(3);
            result.FileType.Should().Be("pdf");
            result.PaperlessUrl.Should().Be("https://paperless.local/documents/42/");
            result.IsArchived.Should().BeFalse();
            result.Topics.Should().Contain("finance");
            result.Genres.Should().Contain("invoice");
            result.FormattedFileSize.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void MapToResponseDto_NullTopicsAndGenres_DefaultsToEmptyArrays()
        {
            var document = TestDataFactory.CreateDocument();
            document.Topics = null;
            document.Genres = null;

            var result = _service.MapToResponseDto(document);

            result.Topics.Should().NotBeNull();
            result.Topics.Should().BeEmpty();
            result.Genres.Should().NotBeNull();
            result.Genres.Should().BeEmpty();
        }

        #endregion

        #region MapToResponseDtos

        [Fact]
        public void MapToResponseDtos_MultipleDocuments_MapsAll()
        {
            var documents = TestDataFactory.CreateDocuments(3);

            var result = _service.MapToResponseDtos(documents);

            result.Should().HaveCount(3);
        }

        [Fact]
        public void MapToResponseDtos_EmptyCollection_ReturnsEmpty()
        {
            var documents = new List<Document>();

            var result = _service.MapToResponseDtos(documents);

            result.Should().BeEmpty();
        }

        #endregion

        #region MapFromPaperlessAsync

        [Fact]
        public async Task MapFromPaperlessAsync_ValidDocument_MapsCorrectly()
        {
            var paperlessDoc = new PaperlessDocumentDto
            {
                Id = 42,
                Title = "Invoice from ACME",
                Content = "OCR content here",
                Tags = new List<int> { 1, 2 },
                DocumentType = 10,
                Correspondent = 5,
                OriginalFileName = "invoice.pdf",
                ArchiveSerialNumber = "100",
                Created = new DateTime(2024, 3, 15),
                PageCount = 2
            };

            var tagLookup = new Dictionary<int, string> { { 1, "finance" }, { 2, "quarterly" } };
            var docTypeLookup = new Dictionary<int, string> { { 10, "Invoice" } };
            var correspondentLookup = new Dictionary<int, string> { { 5, "ACME Corp" } };
            var baseUrl = "https://paperless.local";

            var result = await _service.MapFromPaperlessAsync(
                paperlessDoc, tagLookup, docTypeLookup, correspondentLookup, baseUrl);

            result.Title.Should().Be("Invoice from ACME");
            result.PaperlessId.Should().Be(42);
            result.OriginalFileName.Should().Be("invoice.pdf");
            result.ArchiveSerialNumber.Should().Be("100");
            result.DocumentType.Should().Be("Invoice");
            result.Correspondent.Should().Be("ACME Corp");
            result.OcrContent.Should().Be("OCR content here");
            result.PageCount.Should().Be(2);
            result.FileType.Should().Be("pdf");
            result.PaperlessUrl.Should().Be("https://paperless.local/documents/42/");
            result.Thumbnail.Should().Be("https://paperless.local/api/documents/42/thumb/");
            result.MediaType.Should().Be(MediaType.Document);
            result.Status.Should().Be(Status.Uncharted);
        }

        [Fact]
        public async Task MapFromPaperlessAsync_MissingLookupEntries_HandlesGracefully()
        {
            var paperlessDoc = new PaperlessDocumentDto
            {
                Id = 1,
                Title = "Test",
                Tags = new List<int> { 99 }, // Non-existent tag
                DocumentType = 99, // Non-existent type
                Correspondent = 99 // Non-existent correspondent
            };

            var tagLookup = new Dictionary<int, string>();
            var docTypeLookup = new Dictionary<int, string>();
            var correspondentLookup = new Dictionary<int, string>();

            var result = await _service.MapFromPaperlessAsync(
                paperlessDoc, tagLookup, docTypeLookup, correspondentLookup, "https://paperless.local");

            result.DocumentType.Should().BeNull();
            result.Correspondent.Should().BeNull();
            result.PaperlessTags.Should().BeEmpty();
        }

        [Fact]
        public async Task MapFromPaperlessAsync_EmptyBaseUrl_UrlsAreNull()
        {
            var paperlessDoc = new PaperlessDocumentDto
            {
                Id = 1,
                Title = "Test",
                Tags = new List<int>()
            };

            var result = await _service.MapFromPaperlessAsync(
                paperlessDoc,
                new Dictionary<int, string>(),
                new Dictionary<int, string>(),
                new Dictionary<int, string>(),
                "");

            result.PaperlessUrl.Should().BeNull();
            result.Thumbnail.Should().BeNull();
        }

        [Fact]
        public async Task MapFromPaperlessAsync_GeneratesDescription()
        {
            var paperlessDoc = new PaperlessDocumentDto
            {
                Id = 1,
                Title = "Test",
                Tags = new List<int>(),
                DocumentType = 1,
                Correspondent = 1,
                PageCount = 5,
                Created = new DateTime(2024, 3, 15)
            };

            var docTypeLookup = new Dictionary<int, string> { { 1, "Invoice" } };
            var correspondentLookup = new Dictionary<int, string> { { 1, "ACME" } };

            var result = await _service.MapFromPaperlessAsync(
                paperlessDoc,
                new Dictionary<int, string>(),
                docTypeLookup,
                correspondentLookup,
                "https://paperless.local");

            result.Description.Should().Contain("Type: Invoice");
            result.Description.Should().Contain("From: ACME");
            result.Description.Should().Contain("5 page(s)");
        }

        #endregion

        #region MapPaperlessTagsToTopicsAndGenres

        [Fact]
        public void MapPaperlessTagsToTopicsAndGenres_TopicPrefix_ClassifiesAsTopic()
        {
            var tags = new[] { "topic:artificial intelligence" };

            var (topics, genres) = _service.MapPaperlessTagsToTopicsAndGenres(tags);

            topics.Should().Contain("artificial intelligence");
            genres.Should().BeEmpty();
        }

        [Fact]
        public void MapPaperlessTagsToTopicsAndGenres_GenrePrefix_ClassifiesAsGenre()
        {
            var tags = new[] { "genre:fiction" };

            var (topics, genres) = _service.MapPaperlessTagsToTopicsAndGenres(tags);

            topics.Should().BeEmpty();
            genres.Should().Contain("fiction");
        }

        [Fact]
        public void MapPaperlessTagsToTopicsAndGenres_KnownGenre_ClassifiesAsGenre()
        {
            var tags = new[] { "History", "Biography", "Science" };

            var (topics, genres) = _service.MapPaperlessTagsToTopicsAndGenres(tags);

            genres.Should().HaveCount(3);
            genres.Should().Contain("history");
            genres.Should().Contain("biography");
            genres.Should().Contain("science");
        }

        [Fact]
        public void MapPaperlessTagsToTopicsAndGenres_UnknownTag_DefaultsToTopic()
        {
            var tags = new[] { "machine learning", "project notes" };

            var (topics, genres) = _service.MapPaperlessTagsToTopicsAndGenres(tags);

            topics.Should().HaveCount(2);
            topics.Should().Contain("machine learning");
            topics.Should().Contain("project notes");
            genres.Should().BeEmpty();
        }

        [Fact]
        public void MapPaperlessTagsToTopicsAndGenres_NormalizesToLowercase()
        {
            var tags = new[] { "TOPIC:AI", "GENRE:Thriller", "UNKNOWN TAG" };

            var (topics, genres) = _service.MapPaperlessTagsToTopicsAndGenres(tags);

            topics.Should().Contain("ai");
            topics.Should().Contain("unknown tag");
            genres.Should().Contain("thriller");
        }

        [Fact]
        public void MapPaperlessTagsToTopicsAndGenres_DuplicateTags_Deduplicates()
        {
            var tags = new[] { "topic:ai", "topic:AI", "TOPIC:ai" };

            var (topics, genres) = _service.MapPaperlessTagsToTopicsAndGenres(tags);

            topics.Should().HaveCount(1);
            topics.Should().Contain("ai");
        }

        [Fact]
        public void MapPaperlessTagsToTopicsAndGenres_EmptyPrefixValue_Skipped()
        {
            var tags = new[] { "topic:", "genre:" };

            var (topics, genres) = _service.MapPaperlessTagsToTopicsAndGenres(tags);

            topics.Should().BeEmpty();
            genres.Should().BeEmpty();
        }

        #endregion

        #region Lookup Builders

        [Fact]
        public void BuildTagLookup_ValidTags_BuildsDictionary()
        {
            var tags = new[]
            {
                new PaperlessTagDto { Id = 1, Name = "Finance" },
                new PaperlessTagDto { Id = 2, Name = "Legal" }
            };

            var result = _service.BuildTagLookup(tags);

            result.Should().HaveCount(2);
            result[1].Should().Be("Finance");
            result[2].Should().Be("Legal");
        }

        [Fact]
        public void BuildDocumentTypeLookup_ValidTypes_BuildsDictionary()
        {
            var types = new[]
            {
                new PaperlessDocumentTypeDto { Id = 1, Name = "Invoice" },
                new PaperlessDocumentTypeDto { Id = 2, Name = "Receipt" }
            };

            var result = _service.BuildDocumentTypeLookup(types);

            result.Should().HaveCount(2);
            result[1].Should().Be("Invoice");
        }

        [Fact]
        public void BuildCorrespondentLookup_ValidCorrespondents_BuildsDictionary()
        {
            var correspondents = new[]
            {
                new PaperlessCorrespondentDto { Id = 1, Name = "ACME Corp" },
                new PaperlessCorrespondentDto { Id = 2, Name = "Test Inc" }
            };

            var result = _service.BuildCorrespondentLookup(correspondents);

            result.Should().HaveCount(2);
            result[1].Should().Be("ACME Corp");
        }

        #endregion
    }
}
