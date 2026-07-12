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
    public partial class NoteServiceTests
    {
        #region SyncFromQuartzVaultAsync Tests

        [Fact]
        public async Task SyncFromQuartzVaultAsync_ShouldImportNewNotes()
        {
            // Arrange
            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Description = "Notes on stoicism",
                    Content = "Content about stoicism",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert
            result.Imported.Should().Be(1);
            result.Updated.Should().Be(0);
            result.Unchanged.Should().Be(0);
            result.VaultName.Should().Be("general");
            Context.Notes.Should().HaveCount(1);
            Context.Notes.First().VaultName.Should().Be("general");
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_ShouldUpdateChangedNotes()
        {
            // Arrange
            var existingNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            existingNote.ContentHash = "old-hash";
            Context.Notes.Add(existingNote);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism Updated",
                    Content = "Updated content about stoicism",
                    Tags = new List<string> { "philosophy", "updated" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert
            result.Imported.Should().Be(0);
            result.Updated.Should().Be(1);
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WhenContentChanged_ShouldClearStaleAiDescription()
        {
            // Arrange — an existing note carrying an AI summary from a previous batch run
            var existingNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            existingNote.ContentHash = "old-hash";
            existingNote.AiDescription = "Stale AI summary";
            existingNote.AiDescriptionGeneratedAt = DateTime.UtcNow.AddDays(-7);
            Context.Notes.Add(existingNote);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Completely rewritten content",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert — stale summary cleared so batch regen (selects AiDescription == null) reprocesses it
            var updated = Context.Notes.First(n => n.Slug == "philosophy/stoicism");
            updated.AiDescription.Should().BeNull();
            updated.AiDescriptionGeneratedAt.Should().BeNull();
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WhenContentChanged_ShouldResetSyncedDescription()
        {
            // Arrange — description came from sync (not hand-edited)
            var existingNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            existingNote.ContentHash = "old-hash";
            existingNote.Description = "Old synced description";
            existingNote.IsDescriptionManual = false;
            Context.Notes.Add(existingNote);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Description = "New synced description",
                    Content = "Rewritten content",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert
            var updated = Context.Notes.First(n => n.Slug == "philosophy/stoicism");
            updated.Description.Should().Be("New synced description");
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WhenContentChanged_ShouldPreserveManualDescription()
        {
            // Arrange — user hand-edited the description; content then changes in the vault
            var existingNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            existingNote.ContentHash = "old-hash";
            existingNote.Description = "Hand-written description";
            existingNote.IsDescriptionManual = true;
            existingNote.AiDescription = "Stale AI summary";
            Context.Notes.Add(existingNote);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Description = null,
                    Content = "Rewritten content",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert — manual description survives; stale AI summary is still cleared
            var updated = Context.Notes.First(n => n.Slug == "philosophy/stoicism");
            updated.Description.Should().Be("Hand-written description");
            updated.AiDescription.Should().BeNull();
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_ShouldLowercaseTagsOnImport()
        {
            // Arrange
            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Content",
                    Tags = new List<string> { "Philosophy", "STOICISM", "  Ethics  " }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert — trimmed + lowercased, invariant owned by MMV not Quartz
            var note = Context.Notes.First(n => n.Slug == "philosophy/stoicism");
            note.Tags.Should().BeEquivalentTo(new[] { "philosophy", "stoicism", "ethics" });
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_ShouldLowercaseTagsOnUpdate()
        {
            // Arrange
            var existingNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            existingNote.ContentHash = "old-hash";
            Context.Notes.Add(existingNote);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Updated content",
                    Tags = new List<string> { "Philosophy", "Updated" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert
            var note = Context.Notes.First(n => n.Slug == "philosophy/stoicism");
            note.Tags.Should().BeEquivalentTo(new[] { "philosophy", "updated" });
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WhenAuthError_ShouldReturnResultWithError()
        {
            // Arrange
            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Throws(new UnauthorizedAccessException("Invalid token"));

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com", "bad-token");

            // Assert
            result.Errors.Should().NotBeEmpty();
            result.Errors.First().Should().Contain("Authentication error");
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WithEmptyVault_ShouldReturnZeroCounts()
        {
            // Arrange
            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(new Dictionary<string, QuartzNoteDto>());

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert
            result.TotalProcessed.Should().Be(0);
            result.Imported.Should().Be(0);
        }

        #endregion

        #region GetSyncStatusAsync Tests

        [Fact]
        public async Task GetSyncStatusAsync_ShouldReturnCorrectCounts()
        {
            // Arrange
            Context.Notes.AddRange(
                CreateTestNote("note-1", "Note 1", "general"),
                CreateTestNote("note-2", "Note 2", "general"),
                CreateTestNote("note-3", "Note 3", "programming")
            );
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetSyncStatusAsync();

            // Assert
            result.TotalNotesGeneral.Should().Be(2);
            result.TotalNotesProgramming.Should().Be(1);
        }

        #endregion
    }
}
