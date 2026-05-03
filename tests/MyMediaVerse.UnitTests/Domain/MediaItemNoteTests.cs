using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    public class MediaItemNoteTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var mediaItemNote = new MediaItemNote();

            // Assert
            mediaItemNote.MediaItemId.Should().Be(Guid.Empty);
            mediaItemNote.NoteId.Should().Be(Guid.Empty);
            mediaItemNote.LinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            mediaItemNote.LinkDescription.Should().BeNull();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var mediaItemId = Guid.NewGuid();
            var noteId = Guid.NewGuid();
            var testDate = DateTime.UtcNow;

            // Act
            var mediaItemNote = new MediaItemNote
            {
                MediaItemId = mediaItemId,
                NoteId = noteId,
                LinkedAt = testDate,
                LinkDescription = "Mentioned in the introduction"
            };

            // Assert
            mediaItemNote.MediaItemId.Should().Be(mediaItemId);
            mediaItemNote.NoteId.Should().Be(noteId);
            mediaItemNote.LinkedAt.Should().Be(testDate);
            mediaItemNote.LinkDescription.Should().Be("Mentioned in the introduction");
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_CanLinkMediaItemAndNote()
        {
            // Arrange
            var book = TestDataFactory.CreateBook("Deep Work");
            var note = new Note
            {
                Slug = "productivity-notes",
                Title = "Productivity Notes",
                VaultName = "general"
            };

            // Act
            var link = new MediaItemNote
            {
                MediaItemId = book.Id,
                MediaItem = book,
                NoteId = note.Id,
                Note = note,
                LinkDescription = "Book referenced in note"
            };

            // Assert
            link.MediaItem.Title.Should().Be("Deep Work");
            link.Note.Title.Should().Be("Productivity Notes");
            link.LinkDescription.Should().Be("Book referenced in note");
        }

        [Fact]
        public void LinkDescription_CanBeNull()
        {
            // Arrange & Act
            var link = new MediaItemNote
            {
                MediaItemId = Guid.NewGuid(),
                NoteId = Guid.NewGuid(),
                LinkDescription = null
            };

            // Assert
            link.LinkDescription.Should().BeNull();
        }

        #endregion
    }
}
