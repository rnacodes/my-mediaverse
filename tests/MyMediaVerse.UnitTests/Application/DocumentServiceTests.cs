using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class DocumentServiceTests : InMemoryDbTestBase
    {
        private readonly ILogger<DocumentService> _mockLogger;
        private readonly IDocumentMappingService _mockMappingService;
        private readonly DocumentService _service;

        public DocumentServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<DocumentService>>();
            _mockMappingService = Substitute.For<IDocumentMappingService>();
            _service = new DocumentService(Context, _mockLogger, _mockMappingService);
        }

        private Document CreateTestDocument(string title = "Test Doc", int? paperlessId = null)
        {
            return new Document
            {
                Title = title,
                MediaType = MediaType.Document,
                PaperlessId = paperlessId,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
        }

        #region GetAllDocumentsAsync Tests

        [Fact]
        public async Task GetAllDocumentsAsync_ShouldReturnAllDocuments()
        {
            // Arrange
            Context.Documents.AddRange(
                CreateTestDocument("Doc 1"),
                CreateTestDocument("Doc 2"),
                CreateTestDocument("Doc 3")
            );
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllDocumentsAsync();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllDocumentsAsync_WhenEmpty_ShouldReturnEmptyList()
        {
            // Act
            var result = await _service.GetAllDocumentsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetDocumentByIdAsync Tests

        [Fact]
        public async Task GetDocumentByIdAsync_WhenExists_ShouldReturnDocument()
        {
            // Arrange
            var doc = CreateTestDocument("Found Doc");
            Context.Documents.Add(doc);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetDocumentByIdAsync(doc.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Found Doc");
        }

        [Fact]
        public async Task GetDocumentByIdAsync_WhenNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetDocumentByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetDocumentByPaperlessIdAsync Tests

        [Fact]
        public async Task GetDocumentByPaperlessIdAsync_WhenExists_ShouldReturnDocument()
        {
            // Arrange
            var doc = CreateTestDocument("Paperless Doc", 42);
            Context.Documents.Add(doc);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetDocumentByPaperlessIdAsync(42);

            // Assert
            result.Should().NotBeNull();
            result!.PaperlessId.Should().Be(42);
        }

        [Fact]
        public async Task GetDocumentByPaperlessIdAsync_WhenNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetDocumentByPaperlessIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateDocumentAsync Tests

        [Fact]
        public async Task CreateDocumentAsync_WithValidDto_ShouldCreateDocument()
        {
            // Arrange
            var dto = new CreateDocumentDto
            {
                Title = "New Document",
                Status = Status.Uncharted,
                OriginalFileName = "report.pdf",
                FileType = "pdf",
                FileSizeBytes = 1024,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateDocumentAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("New Document");
            result.MediaType.Should().Be(MediaType.Document);
            result.OriginalFileName.Should().Be("report.pdf");
            result.FileType.Should().Be("pdf");
            Context.Documents.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateDocumentAsync_WithPaperlessId_ShouldSetLastPaperlessSync()
        {
            // Arrange
            var dto = new CreateDocumentDto
            {
                Title = "Synced Doc",
                Status = Status.Uncharted,
                PaperlessId = 100,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateDocumentAsync(dto);

            // Assert
            result.PaperlessId.Should().Be(100);
            result.LastPaperlessSync.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateDocumentAsync_WithoutPaperlessId_ShouldNotSetLastPaperlessSync()
        {
            // Arrange
            var dto = new CreateDocumentDto
            {
                Title = "No Paperless",
                Status = Status.Uncharted,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateDocumentAsync(dto);

            // Assert
            result.LastPaperlessSync.Should().BeNull();
        }

        [Fact]
        public async Task CreateDocumentAsync_WithTopics_ShouldNormalizeToLowercase()
        {
            // Arrange
            var dto = new CreateDocumentDto
            {
                Title = "Tagged Doc",
                Status = Status.Uncharted,
                Topics = new[] { "Finance", " TAX " },
                Genres = new[] { "Reference" }
            };

            // Act
            var result = await _service.CreateDocumentAsync(dto);

            // Assert
            result.Topics.Select(t => t.Name).Should().Contain("finance");
            result.Topics.Select(t => t.Name).Should().Contain("tax");
            result.Genres.Select(g => g.Name).Should().Contain("reference");
        }

        [Fact]
        public async Task CreateDocumentAsync_ShouldSetDateAddedToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;
            var dto = new CreateDocumentDto
            {
                Title = "Timed Doc",
                Status = Status.Uncharted,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.CreateDocumentAsync(dto);

            // Assert
            result.DateAdded.Should().BeOnOrAfter(before);
        }

        #endregion

        #region UpdateDocumentAsync Tests

        [Fact]
        public async Task UpdateDocumentAsync_WhenExists_ShouldUpdateProperties()
        {
            // Arrange
            var doc = CreateTestDocument("Original");
            Context.Documents.Add(doc);
            await Context.SaveChangesAsync();

            var dto = new CreateDocumentDto
            {
                Title = "Updated",
                Status = Status.ActivelyExploring,
                DocumentType = "Invoice",
                Correspondent = "ACME Corp",
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act
            var result = await _service.UpdateDocumentAsync(doc.Id, dto);

            // Assert
            result.Title.Should().Be("Updated");
            result.Status.Should().Be(Status.ActivelyExploring);
            result.DocumentType.Should().Be("Invoice");
            result.Correspondent.Should().Be("ACME Corp");
        }

        [Fact]
        public async Task UpdateDocumentAsync_WhenNotExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dto = new CreateDocumentDto
            {
                Title = "Updated",
                Status = Status.Uncharted,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateDocumentAsync(Guid.NewGuid(), dto));
        }

        #endregion

        #region DeleteDocumentAsync Tests

        [Fact]
        public async Task DeleteDocumentAsync_WhenExists_ShouldReturnTrueAndRemove()
        {
            // Arrange
            var doc = CreateTestDocument();
            Context.Documents.Add(doc);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteDocumentAsync(doc.Id);

            // Assert
            result.Should().BeTrue();
            Context.Documents.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteDocumentAsync_WhenNotExists_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DeleteDocumentAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IsPaperlessAvailableAsync Tests

        [Fact]
        public async Task IsPaperlessAvailableAsync_WhenClientNull_ShouldReturnFalse()
        {
            // Arrange - service created without paperless client (constructor default)
            // _service already has null paperless client

            // Act
            var result = await _service.IsPaperlessAvailableAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsPaperlessAvailableAsync_WhenClientAvailable_ShouldReturnTrue()
        {
            // Arrange
            var mockPaperlessClient = Substitute.For<IPaperlessApiClient>();
            mockPaperlessClient.IsAvailableAsync().Returns(true);
            var service = new DocumentService(
                Context, _mockLogger, _mockMappingService, mockPaperlessClient);

            // Act
            var result = await service.IsPaperlessAvailableAsync();

            // Assert
            result.Should().BeTrue();
        }

        #endregion
    }
}
