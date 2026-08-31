using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
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
            result.CreatedCount.Should().Be(1);
            result.UpdatedCount.Should().Be(0);
            result.SkippedCount.Should().Be(0);
            result.VaultName.Should().Be("general");
            result.Success.Should().BeTrue();
            result.Operation.Should().Be("notes-sync");
            result.ErrorMessage.Should().BeNull();
            result.WarningMessage.Should().BeNull();
            result.CompletedAt.Should().NotBeNull();
            result.Duration.Should().NotBeNull();
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
            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(1);
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
        public async Task SyncFromQuartzVaultAsync_WhenAuthError_ShouldReturnFatalResult()
        {
            // Arrange — a total failure is reported in the result itself so the API layer
            // can return a real error status with the same body shape as a success
            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Throws(new UnauthorizedAccessException("Invalid token"));

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com", "bad-token");

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("authentication failed");
            result.CompletedAt.Should().BeNull();
            result.VaultName.Should().Be("general");
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WhenVaultUnreachable_ShouldReturnFatalResult()
        {
            // Arrange
            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Throws(new HttpRequestException("Connection refused"));

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to reach the vault");
            result.CompletedAt.Should().BeNull();
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WhenSomeNotesFail_ShouldWarnButStaySuccessful()
        {
            // Arrange — one processable note and one malformed entry that throws mid-loop
            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Content about stoicism",
                    Tags = new List<string> { "philosophy" }
                },
                ["philosophy/broken"] = null!
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert — per-item failures never flip Success; they surface as a warning
            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(1);
            result.FailedCount.Should().Be(1);
            result.Errors.Should().ContainSingle(e => e.Contains("philosophy/broken"));
            result.WarningMessage.Should().Contain("1 of 2 notes failed");
            result.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_ShouldLowercaseSlugsOnImport()
        {
            // Arrange — Quartz can publish mixed-case slugs; MMV stores them lowercase
            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["Philosophy/Stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Content about stoicism",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert — slug lowered in the DB, but the published URL keeps its original casing
            var note = Context.Notes.Single();
            note.Slug.Should().Be("philosophy/stoicism");
            note.SourceUrl.Should().Be("https://vault.example.com/Philosophy/Stoicism");
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WithMixedCaseSlug_ShouldUpdateExistingLowercaseNote()
        {
            // Arrange — the same note must not duplicate when the vault reports a different casing
            var existingNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            existingNote.ContentHash = "old-hash";
            Context.Notes.Add(existingNote);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["Philosophy/Stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism Updated",
                    Content = "Updated content",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert
            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(1);
            Context.Notes.Should().HaveCount(1);
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WithRemoveOrphans_ShouldDeleteNotesMissingFromIndex()
        {
            // Arrange — one note still published, one removed from the vault
            var keptNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            keptNote.ContentHash = "kept-hash";
            var orphanNote = CreateTestNote("philosophy/deleted-note", "Deleted Note", "general");
            Context.Notes.AddRange(keptNote, orphanNote);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Content",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com", removeOrphans: true);

            // Assert — orphan gone from DB and search index; published note untouched
            result.OrphansRemoved.Should().Be(1);
            result.RemovedSlugs.Should().ContainSingle().Which.Should().Be("philosophy/deleted-note");
            Context.Notes.Should().ContainSingle(n => n.Slug == "philosophy/stoicism");
            await _mockTypesenseService.Received(1).DeleteNoteAsync(orphanNote.Id);
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WithoutRemoveOrphans_ShouldKeepNotesMissingFromIndex()
        {
            // Arrange
            var orphanNote = CreateTestNote("philosophy/deleted-note", "Deleted Note", "general");
            Context.Notes.Add(orphanNote);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Content",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert — default sync never deletes
            result.OrphansRemoved.Should().Be(0);
            Context.Notes.Should().HaveCount(2);
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WithRemoveOrphansAndEmptyIndex_ShouldRefuseToWipeVault()
        {
            // Arrange — empty index + existing notes looks like a bad publish, not an emptied vault
            Context.Notes.Add(CreateTestNote("philosophy/stoicism", "Stoicism", "general"));
            await Context.SaveChangesAsync();

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(new Dictionary<string, QuartzNoteDto>());

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com", removeOrphans: true);

            // Assert — the skip is a warning (suspect run), not a per-item error or a failure
            result.OrphansRemoved.Should().Be(0);
            result.Success.Should().BeTrue();
            result.Errors.Should().BeEmpty();
            result.WarningMessage.Should().Contain("Orphan removal skipped");
            Context.Notes.Should().HaveCount(1);
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_WithRemoveOrphans_ShouldMatchSlugsCaseInsensitively()
        {
            // Arrange — DB slug is lowercase, vault publishes mixed case; not an orphan
            var note = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            note.ContentHash = "old-hash";
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["Philosophy/Stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Content",
                    Tags = new List<string> { "philosophy" }
                }
            };

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com", removeOrphans: true);

            // Assert
            result.OrphansRemoved.Should().Be(0);
            Context.Notes.Should().HaveCount(1);
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
            result.CreatedCount.Should().Be(0);
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

        #region Tags → Topics (retro pass, derive-on-sync)

        [Fact]
        public async Task SyncFromQuartzVaultAsync_NewNote_DerivesTopicsFromTags()
        {
            // Arrange
            var contentIndex = new Dictionary<string, QuartzNoteDto>
            {
                ["philosophy/stoicism"] = new QuartzNoteDto
                {
                    Title = "Stoicism",
                    Content = "Content",
                    Tags = new List<string> { " Philosophy ", "STOICISM" }
                }
            };
            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(contentIndex);

            // Act
            await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert — topic links mirror the normalized tags
            var note = await Context.Notes.Include(n => n.Topics).SingleAsync();
            note.Tags.Should().BeEquivalentTo(new[] { "philosophy", "stoicism" });
            note.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "philosophy", "stoicism" });
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_UnchangedNote_StillDerivesTopics()
        {
            // Arrange — same content hash, so the unchanged branch runs; this is the
            // branch that backfills topic links on notes that predate the migration
            var existingNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            existingNote.Content = "Content";
            existingNote.Tags = new List<string> { "philosophy" };
            Context.Notes.Add(existingNote);
            await Context.SaveChangesAsync();

            var dto = new QuartzNoteDto
            {
                Title = "Stoicism",
                Content = "Content",
                Tags = new List<string> { "philosophy" }
            };
            // Make the stored hash match what the sync will compute so the note is "unchanged"
            existingNote.ContentHash = ComputeHashForTest(dto.Content);
            await Context.SaveChangesAsync();

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(new Dictionary<string, QuartzNoteDto> { ["philosophy/stoicism"] = dto });

            // Act
            var result = await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert
            result.SkippedCount.Should().Be(1);
            var note = await Context.Notes.Include(n => n.Topics).SingleAsync();
            note.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "philosophy" });
        }

        [Fact]
        public async Task SyncFromQuartzVaultAsync_TagRemovedInObsidian_RemovesTopicLink()
        {
            // Arrange — Obsidian is source of truth: a removed tag drops its topic link
            var existingNote = CreateTestNote("philosophy/stoicism", "Stoicism", "general");
            existingNote.ContentHash = "old-hash";
            existingNote.Tags = new List<string> { "philosophy", "removed" };
            existingNote.Topics.Add(new Topic { Name = "philosophy" });
            existingNote.Topics.Add(new Topic { Name = "removed" });
            Context.Notes.Add(existingNote);
            await Context.SaveChangesAsync();

            _mockQuartzClient.GetContentIndexAsync(Arg.Any<string>(), Arg.Any<string?>())
                .Returns(new Dictionary<string, QuartzNoteDto>
                {
                    ["philosophy/stoicism"] = new QuartzNoteDto
                    {
                        Title = "Stoicism",
                        Content = "New content",
                        Tags = new List<string> { "philosophy" }
                    }
                });

            // Act
            await _service.SyncFromQuartzVaultAsync("general", "https://vault.example.com");

            // Assert — link removed, Topic entity itself survives
            var note = await Context.Notes.Include(n => n.Topics).SingleAsync();
            note.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "philosophy" });
            Context.Topics.Any(t => t.Name == "removed").Should().BeTrue();
        }

        private static string ComputeHashForTest(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes);
        }

        #endregion
    }
}
