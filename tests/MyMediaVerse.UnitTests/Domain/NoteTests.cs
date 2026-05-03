using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.Domain
{
    public class NoteTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var note = new Note { Slug = "test-note", Title = "Test Note", VaultName = "general" };

            // Assert
            note.Id.Should().NotBeEmpty();
            note.Slug.Should().Be("test-note");
            note.Title.Should().Be("Test Note");
            note.VaultName.Should().Be("general");
            note.Content.Should().BeNull();
            note.Description.Should().BeNull();
            note.SourceUrl.Should().BeNull();
            note.Tags.Should().NotBeNull().And.BeEmpty();
            note.NoteDate.Should().BeNull();
            note.DateImported.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            note.LastSyncedAt.Should().BeNull();
            note.ContentHash.Should().BeNull();
            note.AiDescription.Should().BeNull();
            note.AiDescriptionGeneratedAt.Should().BeNull();
            note.IsDescriptionManual.Should().BeFalse();
            note.Embedding.Should().BeNull();
            note.EmbeddingGeneratedAt.Should().BeNull();
            note.EmbeddingModel.Should().BeNull();
            note.MediaItemNotes.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var note = new Note { Slug = "test", Title = "Test", VaultName = "general" };
            var testDate = DateTime.UtcNow;

            // Act
            note.Slug = "philosophy-of-mind";
            note.Title = "Philosophy of Mind";
            note.Content = "# Philosophy of Mind\n\nExploring consciousness...";
            note.Description = "Notes on consciousness and mental states";
            note.VaultName = "general";
            note.SourceUrl = "https://quartz.example.com/philosophy-of-mind";
            note.NoteDate = testDate.AddDays(-30);
            note.LastSyncedAt = testDate;
            note.ContentHash = "abc123def456";
            note.AiDescription = "A comprehensive exploration of consciousness theories";
            note.AiDescriptionGeneratedAt = testDate;
            note.IsDescriptionManual = false;
            note.EmbeddingModel = "text-embedding-3-large";
            note.EmbeddingGeneratedAt = testDate;

            // Assert
            note.Slug.Should().Be("philosophy-of-mind");
            note.Title.Should().Be("Philosophy of Mind");
            note.Content.Should().StartWith("# Philosophy of Mind");
            note.Description.Should().Be("Notes on consciousness and mental states");
            note.VaultName.Should().Be("general");
            note.SourceUrl.Should().Be("https://quartz.example.com/philosophy-of-mind");
            note.NoteDate.Should().Be(testDate.AddDays(-30));
            note.LastSyncedAt.Should().Be(testDate);
            note.ContentHash.Should().Be("abc123def456");
            note.AiDescription.Should().Be("A comprehensive exploration of consciousness theories");
            note.AiDescriptionGeneratedAt.Should().Be(testDate);
            note.IsDescriptionManual.Should().BeFalse();
            note.EmbeddingModel.Should().Be("text-embedding-3-large");
            note.EmbeddingGeneratedAt.Should().Be(testDate);
        }

        [Theory]
        [InlineData("general")]
        [InlineData("programming")]
        public void VaultName_ShouldAcceptKnownValues(string vaultName)
        {
            // Arrange
            var note = new Note { Slug = "test", Title = "Test", VaultName = vaultName };

            // Assert
            note.VaultName.Should().Be(vaultName);
        }

        [Fact]
        public void Tags_CanStoreMultipleTags()
        {
            // Arrange
            var note = new Note { Slug = "test", Title = "Test", VaultName = "general" };

            // Act
            note.Tags.Add("philosophy");
            note.Tags.Add("consciousness");
            note.Tags.Add("mind");

            // Assert
            note.Tags.Should().HaveCount(3);
            note.Tags.Should().Contain(new[] { "philosophy", "consciousness", "mind" });
        }

        [Fact]
        public void IsDescriptionManual_WhenTrue_IndicatesManualOverride()
        {
            // Arrange
            var note = new Note { Slug = "test", Title = "Test", VaultName = "general" };

            // Act
            note.IsDescriptionManual = true;
            note.Description = "My custom description";

            // Assert
            note.IsDescriptionManual.Should().BeTrue();
            note.Description.Should().Be("My custom description");
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void MediaItemNotes_CanBeAddedAndRetrieved()
        {
            // Arrange
            var note = new Note { Slug = "test", Title = "Test", VaultName = "general" };
            var book = new Book { Title = "Test Book", Author = "Author" };
            var mediaItemNote = new MediaItemNote
            {
                MediaItemId = book.Id,
                MediaItem = book,
                NoteId = note.Id,
                Note = note,
                LinkDescription = "Referenced in chapter 3"
            };

            // Act
            note.MediaItemNotes.Add(mediaItemNote);

            // Assert
            note.MediaItemNotes.Should().ContainSingle();
            note.MediaItemNotes.First().LinkDescription.Should().Be("Referenced in chapter 3");
        }

        #endregion

        #region Id Tests

        [Fact]
        public void Id_ShouldBeUniqueAcrossInstances()
        {
            // Arrange & Act
            var note1 = new Note { Slug = "note-1", Title = "Note 1", VaultName = "general" };
            var note2 = new Note { Slug = "note-2", Title = "Note 2", VaultName = "general" };

            // Assert
            note1.Id.Should().NotBe(note2.Id);
        }

        #endregion
    }
}
