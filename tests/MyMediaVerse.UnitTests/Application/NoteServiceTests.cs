using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.Obsidian;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public partial class NoteServiceTests : InMemoryDbTestBase
    {
        private readonly IQuartzApiClient _mockQuartzClient;
        private readonly IConfiguration _mockConfiguration;
        private readonly ITypesenseService _mockTypesenseService;
        private readonly ILogger<NoteService> _mockLogger;
        private readonly NoteService _service;

        public NoteServiceTests()
        {
            _mockQuartzClient = Substitute.For<IQuartzApiClient>();
            _mockConfiguration = Substitute.For<IConfiguration>();
            _mockTypesenseService = Substitute.For<ITypesenseService>();
            _mockLogger = Substitute.For<ILogger<NoteService>>();
            _service = new NoteService(Context, _mockQuartzClient, _mockConfiguration, _mockTypesenseService, _mockLogger);
        }

        private Note CreateTestNote(string slug = "test-note", string title = "Test Note", string vaultName = "general")
        {
            return new Note
            {
                Slug = slug,
                Title = title,
                VaultName = vaultName,
                Content = "Test content",
                DateImported = DateTime.UtcNow,
                Tags = new List<string> { "tag1", "tag2" },
                MediaItemNotes = new List<MediaItemNote>()
            };
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WhenExists_ShouldReturnNote()
        {
            // Arrange
            var note = CreateTestNote();
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(note.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Test Note");
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetBySlugAndVaultAsync Tests

        [Fact]
        public async Task GetBySlugAndVaultAsync_WhenExists_ShouldReturnNote()
        {
            // Arrange
            var note = CreateTestNote("my-slug", "My Note", "general");
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetBySlugAndVaultAsync("my-slug", "General");

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("My Note");
        }

        [Fact]
        public async Task GetBySlugAndVaultAsync_WithMixedCaseSlug_ShouldReturnNote()
        {
            // Arrange — slugs are stored lowercase; lookups must be case-insensitive
            var note = CreateTestNote("my-slug", "My Note", "general");
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetBySlugAndVaultAsync("My-Slug", "general");

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("My Note");
        }

        [Fact]
        public async Task GetBySlugAndVaultAsync_WithDifferentVault_ShouldReturnNull()
        {
            // Arrange
            var note = CreateTestNote("my-slug", "My Note", "general");
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetBySlugAndVaultAsync("my-slug", "Programming");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllNotes()
        {
            // Arrange
            Context.Notes.AddRange(
                CreateTestNote("note-1", "Note 1"),
                CreateTestNote("note-2", "Note 2")
            );
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAsync_WithVaultFilter_ShouldReturnFilteredNotes()
        {
            // Arrange
            Context.Notes.AddRange(
                CreateTestNote("note-1", "Note 1", "general"),
                CreateTestNote("note-2", "Note 2", "programming")
            );
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync("General");

            // Assert
            result.Should().HaveCount(1);
            result.First().VaultName.Should().Be("general");
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldCreateNote()
        {
            // Arrange
            var dto = new CreateNoteDto
            {
                Slug = "New-Note",
                Title = "New Note",
                VaultName = "General",
                Content = "Some content",
                Tags = new List<string> { "tag1" }
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("New Note");
            result.Slug.Should().Be("new-note"); // lowercase
            result.VaultName.Should().Be("general"); // lowercase
            result.ContentHash.Should().NotBeNull();
            result.DateImported.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            Context.Notes.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateAsync_WithNullTags_ShouldDefaultToEmptyList()
        {
            // Arrange
            var dto = new CreateNoteDto
            {
                Slug = "no-tags",
                Title = "No Tags Note",
                VaultName = "general",
                Tags = null!
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.Tags.Should().NotBeNull();
            result.Tags.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_ShouldComputeContentHash()
        {
            // Arrange
            var dto = new CreateNoteDto
            {
                Slug = "hashed",
                Title = "Hashed Note",
                VaultName = "general",
                Content = "content for hashing"
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.ContentHash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateAsync_WithNullContent_ShouldHaveNullContentHash()
        {
            // Arrange
            var dto = new CreateNoteDto
            {
                Slug = "no-content",
                Title = "No Content",
                VaultName = "general",
                Content = null
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            result.ContentHash.Should().BeNull();
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTitle()
        {
            // Arrange
            var note = CreateTestNote();
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            var dto = new UpdateNoteDto { Title = "Updated Title" };

            // Act
            var result = await _service.UpdateAsync(note.Id, dto);

            // Assert
            result.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task UpdateAsync_WithDescription_ShouldSetIsDescriptionManual()
        {
            // Arrange
            var note = CreateTestNote();
            note.IsDescriptionManual = false;
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            var dto = new UpdateNoteDto { Description = "Manual description" };

            // Act
            var result = await _service.UpdateAsync(note.Id, dto);

            // Assert
            result.Description.Should().Be("Manual description");
            result.IsDescriptionManual.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_WithContent_ShouldUpdateContentHash()
        {
            // Arrange
            var note = CreateTestNote();
            note.ContentHash = "old-hash";
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            var dto = new UpdateNoteDto { Content = "New content" };

            // Act
            var result = await _service.UpdateAsync(note.Id, dto);

            // Assert
            result.Content.Should().Be("New content");
            result.ContentHash.Should().NotBe("old-hash");
        }

        [Fact]
        public async Task UpdateAsync_WhenNotExists_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var dto = new UpdateNoteDto { Title = "Updated" };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldSetLastSyncedAt()
        {
            // Arrange
            var note = CreateTestNote();
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            var dto = new UpdateNoteDto { Title = "Updated" };

            // Act
            var result = await _service.UpdateAsync(note.Id, dto);

            // Assert
            result.LastSyncedAt.Should().NotBeNull();
            result.LastSyncedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WhenExists_ShouldRemoveNote()
        {
            // Arrange
            var note = CreateTestNote();
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            // Act
            await _service.DeleteAsync(note.Id);

            // Assert
            Context.Notes.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAsync_WhenNotExists_ShouldThrowKeyNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.DeleteAsync(Guid.NewGuid()));
        }

        #endregion
    }
}
