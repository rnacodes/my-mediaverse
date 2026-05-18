using FluentAssertions;
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
