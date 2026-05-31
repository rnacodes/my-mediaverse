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
        #region LinkToMediaItemAsync Tests

        [Fact]
        public async Task LinkToMediaItemAsync_ShouldCreateLink()
        {
            // Arrange
            var note = CreateTestNote();
            var book = new Book
            {
                Title = "Test Book",
                Author = "Author",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Notes.Add(note);
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            // Act
            await _service.LinkToMediaItemAsync(note.Id, book.Id, "related to book");

            // Assert
            Context.MediaItemNotes.Should().HaveCount(1);
            var link = Context.MediaItemNotes.First();
            link.NoteId.Should().Be(note.Id);
            link.MediaItemId.Should().Be(book.Id);
            link.LinkDescription.Should().Be("related to book");
        }

        [Fact]
        public async Task LinkToMediaItemAsync_WhenAlreadyLinked_ShouldNotDuplicate()
        {
            // Arrange
            var note = CreateTestNote();
            var book = new Book
            {
                Title = "Test Book",
                Author = "Author",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Notes.Add(note);
            Context.Books.Add(book);
            Context.MediaItemNotes.Add(new MediaItemNote
            {
                NoteId = note.Id,
                MediaItemId = book.Id,
                LinkedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            // Act - link again
            await _service.LinkToMediaItemAsync(note.Id, book.Id);

            // Assert - should still be 1
            Context.MediaItemNotes.Should().HaveCount(1);
        }

        [Fact]
        public async Task LinkToMediaItemAsync_WhenNoteNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var book = new Book
            {
                Title = "Test",
                Author = "Author",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.LinkToMediaItemAsync(Guid.NewGuid(), book.Id));
        }

        [Fact]
        public async Task LinkToMediaItemAsync_WhenMediaItemNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var note = CreateTestNote();
            Context.Notes.Add(note);
            await Context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.LinkToMediaItemAsync(note.Id, Guid.NewGuid()));
        }

        #endregion

        #region UnlinkFromMediaItemAsync Tests

        [Fact]
        public async Task UnlinkFromMediaItemAsync_WhenLinked_ShouldRemoveLink()
        {
            // Arrange
            var note = CreateTestNote();
            var book = new Book
            {
                Title = "Test",
                Author = "Author",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.Notes.Add(note);
            Context.Books.Add(book);
            Context.MediaItemNotes.Add(new MediaItemNote
            {
                NoteId = note.Id,
                MediaItemId = book.Id,
                LinkedAt = DateTime.UtcNow
            });
            await Context.SaveChangesAsync();

            // Act
            await _service.UnlinkFromMediaItemAsync(note.Id, book.Id);

            // Assert
            Context.MediaItemNotes.Should().BeEmpty();
        }

        [Fact]
        public async Task UnlinkFromMediaItemAsync_WhenNotLinked_ShouldThrowKeyNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UnlinkFromMediaItemAsync(Guid.NewGuid(), Guid.NewGuid()));
        }

        #endregion
    }
}
