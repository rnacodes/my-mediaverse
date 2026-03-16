using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public class AIServiceTests : InMemoryDbTestBase
    {
        private readonly Mock<IGradientAIClient> _mockGradientClient;
        private readonly Mock<ITypeSenseService> _mockTypeSenseService;
        private readonly Mock<ILogger<AIService>> _mockLogger;
        private readonly AIService _service;

        public AIServiceTests()
        {
            _mockGradientClient = new Mock<IGradientAIClient>();
            _mockTypeSenseService = new Mock<ITypeSenseService>();
            _mockLogger = new Mock<ILogger<AIService>>();
            _service = new AIService(Context, _mockGradientClient.Object, _mockTypeSenseService.Object, _mockLogger.Object);
        }

        #region IsAvailableAsync

        [Fact]
        public async Task IsAvailableAsync_ClientAvailable_ReturnsTrue()
        {
            _mockGradientClient.Setup(c => c.IsAvailableAsync()).ReturnsAsync(true);

            var result = await _service.IsAvailableAsync();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAvailableAsync_ClientUnavailable_ReturnsFalse()
        {
            _mockGradientClient.Setup(c => c.IsAvailableAsync()).ReturnsAsync(false);

            var result = await _service.IsAvailableAsync();

            result.Should().BeFalse();
        }

        #endregion

        #region GenerateNoteDescriptionAsync

        [Fact]
        public async Task GenerateNoteDescriptionAsync_NoteNotFound_ReturnsNull()
        {
            var result = await _service.GenerateNoteDescriptionAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GenerateNoteDescriptionAsync_ValidNote_GeneratesDescription()
        {
            var note = TestDataFactory.CreateNote("Test Note", "test-note", "general");
            note.Content = "This is a detailed note about machine learning concepts and neural networks.";
            note.Tags = new List<string> { "ai", "machine-learning" };
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            _mockGradientClient.Setup(c => c.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("A comprehensive exploration of machine learning fundamentals.");

            var result = await _service.GenerateNoteDescriptionAsync(note.Id);

            result.Should().NotBeNullOrEmpty();
            result.Should().Be("A comprehensive exploration of machine learning fundamentals.");
        }

        [Fact]
        public async Task GenerateNoteDescriptionAsync_EmptyContent_ReturnsNull()
        {
            var note = TestDataFactory.CreateNote("Empty Note");
            note.Content = null;
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            var result = await _service.GenerateNoteDescriptionAsync(note.Id);

            // Service returns null when note has no content
            result.Should().BeNull();
        }

        [Fact]
        public async Task GenerateNoteDescriptionAsync_SavesDescription_ToDatabase()
        {
            var note = TestDataFactory.CreateNote("Test Note");
            note.Content = "Important content about technology";
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            _mockGradientClient.Setup(c => c.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Generated description");

            await _service.GenerateNoteDescriptionAsync(note.Id);

            var updatedNote = Context.Notes.First(n => n.Id == note.Id);
            updatedNote.AiDescription.Should().Be("Generated description");
        }

        #endregion

        #region GenerateNoteDescriptionsBatchAsync

        [Fact]
        public async Task GenerateNoteDescriptionsBatchAsync_NoNotesNeeding_ReturnsZeroProcessed()
        {
            var result = await _service.GenerateNoteDescriptionsBatchAsync();

            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(0);
        }

        [Fact]
        public async Task GenerateNoteDescriptionsBatchAsync_WithNotes_ProcessesBatch()
        {
            var note = TestDataFactory.CreateNote("Need Description");
            note.Content = "Some content";
            note.AiDescription = null;
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            _mockGradientClient.Setup(c => c.GenerateTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Generated description");

            var result = await _service.GenerateNoteDescriptionsBatchAsync(batchSize: 10);

            result.Should().NotBeNull();
            result.TotalProcessed.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GenerateNoteDescriptionsBatchAsync_RespectsCancellationToken()
        {
            var note = TestDataFactory.CreateNote("Test");
            note.Content = "Content";
            note.AiDescription = null;
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Pre-cancelled token causes OperationCanceledException at EF query level
            Func<Task> act = async () => await _service.GenerateNoteDescriptionsBatchAsync(cancellationToken: cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region GetNotesNeedingDescriptionCountAsync

        [Fact]
        public async Task GetNotesNeedingDescriptionCountAsync_NoNotes_ReturnsZero()
        {
            var result = await _service.GetNotesNeedingDescriptionCountAsync();

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetNotesNeedingDescriptionCountAsync_WithNotesNeedingDescription_ReturnsCount()
        {
            var noteWithDescription = TestDataFactory.CreateNote("Has Description");
            noteWithDescription.AiDescription = "Already has one";
            noteWithDescription.Content = "Content";

            var noteWithoutDescription = TestDataFactory.CreateNote("No Description");
            noteWithoutDescription.AiDescription = null;
            noteWithoutDescription.Content = "Some content";

            Context.Notes.AddRange(noteWithDescription, noteWithoutDescription);
            await Context.SaveChangesAsync();

            var result = await _service.GetNotesNeedingDescriptionCountAsync();

            result.Should().Be(1);
        }

        #endregion

        #region GetStatusAsync

        [Fact]
        public async Task GetStatusAsync_WithInMemoryDb_ThrowsDueToRawSql()
        {
            _mockGradientClient.Setup(c => c.IsAvailableAsync()).ReturnsAsync(true);
            _mockGradientClient.Setup(c => c.EmbeddingModelName).Returns("text-embedding-3-large");
            _mockGradientClient.Setup(c => c.EmbeddingDimensions).Returns(1024);
            _mockGradientClient.Setup(c => c.GenerationModelName).Returns("gradient-model");

            // GetStatusAsync uses raw SQL for embedding counts, which InMemory provider doesn't support
            Func<Task> act = async () => await _service.GetStatusAsync();

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        #endregion
    }
}
