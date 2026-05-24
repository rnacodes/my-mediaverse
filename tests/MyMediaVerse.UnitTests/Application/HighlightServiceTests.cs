using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;
using Xunit;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class HighlightServiceTests : InMemoryDbTestBase
    {
        private readonly IReadwiseApiClient _mockReadwiseClient;
        private readonly ILogger<HighlightService> _mockLogger;
        private readonly HighlightService _service;

        public HighlightServiceTests()
        {
            _mockReadwiseClient = Substitute.For<IReadwiseApiClient>();
            _mockLogger = Substitute.For<ILogger<HighlightService>>();

            _service = new HighlightService(Context, _mockReadwiseClient, _mockLogger);
        }

        [Fact]
        public async Task GetHighlightByIdAsync_ValidId_ReturnsHighlight()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            var highlight = new Highlight
            {
                Id = highlightId,
                Text = "Test highlight text",
                ReadwiseId = 123
            };

            Context.Highlights.Add(highlight);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetHighlightByIdAsync(highlightId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(highlightId);
            result.Text.Should().Be("Test highlight text");
        }

        [Fact]
        public async Task GetHighlightsByArticleIdAsync_ValidArticleId_ReturnsHighlights()
        {
            // Arrange
            var articleId = Guid.NewGuid();
            var highlights = new List<Highlight>
            {
                new Highlight { Id = Guid.NewGuid(), ArticleId = articleId, Text = "Highlight 1", ReadwiseId = 1 },
                new Highlight { Id = Guid.NewGuid(), ArticleId = articleId, Text = "Highlight 2", ReadwiseId = 2 }
            };

            Context.Highlights.AddRange(highlights);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetHighlightsByArticleIdAsync(articleId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(h => h.ArticleId.Should().Be(articleId));
        }

        [Fact]
        public async Task GetHighlightsByBookIdAsync_ValidBookId_ReturnsHighlights()
        {
            // Arrange
            var bookId = Guid.NewGuid();
            var highlights = new List<Highlight>
            {
                new Highlight { Id = Guid.NewGuid(), BookId = bookId, Text = "Book Highlight 1", ReadwiseId = 1 },
                new Highlight { Id = Guid.NewGuid(), BookId = bookId, Text = "Book Highlight 2", ReadwiseId = 2 },
                new Highlight { Id = Guid.NewGuid(), BookId = bookId, Text = "Book Highlight 3", ReadwiseId = 3 }
            };

            Context.Highlights.AddRange(highlights);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetHighlightsByBookIdAsync(bookId);

            // Assert
            result.Should().HaveCount(3);
            result.Should().AllSatisfy(h => h.BookId.Should().Be(bookId));
        }

        [Fact]
        public async Task GetHighlightsByTagAsync_ValidTag_ReturnsMatchingHighlights()
        {
            // Arrange
            var tag = "important";
            var highlights = new List<Highlight>
            {
                new Highlight { Id = Guid.NewGuid(), Text = "Tagged highlight", Tags = "important,review", ReadwiseId = 1 },
                new Highlight { Id = Guid.NewGuid(), Text = "Another tagged highlight", Tags = "important", ReadwiseId = 2 },
                new Highlight { Id = Guid.NewGuid(), Text = "Untagged highlight", Tags = "", ReadwiseId = 3 }
            };

            Context.Highlights.AddRange(highlights);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetHighlightsByTagAsync(tag);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(h => h.Tags.Should().Contain(tag));
        }

        [Fact]
        public async Task CreateHighlightAsync_ValidData_CreatesHighlight()
        {
            // Arrange
            var createDto = new CreateHighlightDto
            {
                Text = "New highlight",
                Note = "Test note",
                Tags = new List<string> { "test" }
            };

            // Act
            var result = await _service.CreateHighlightAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().Be("New highlight");
            result.Note.Should().Be("Test note");
            
            var dbHighlight = await Context.Highlights.FindAsync(result.Id);
            dbHighlight.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateHighlightAsync_ValidData_UpdatesHighlight()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            var existingHighlight = new Highlight
            {
                Id = highlightId,
                Text = "Original text",
                Note = "Original note",
                ReadwiseId = 123
            };

            Context.Highlights.Add(existingHighlight);
            await Context.SaveChangesAsync();

            var updateDto = new CreateHighlightDto
            {
                Text = "Updated text",
                Note = "Updated note"
            };

            // Act
            var result = await _service.UpdateHighlightAsync(highlightId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Text.Should().Be("Updated text");
            result.Note.Should().Be("Updated note");
            
            var dbHighlight = await Context.Highlights.FindAsync(highlightId);
            dbHighlight.Should().NotBeNull();
            dbHighlight.Text.Should().Be("Updated text");
        }

        [Fact]
        public async Task DeleteHighlightAsync_ValidId_DeletesHighlight()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            var highlight = new Highlight { Id = highlightId, Text = "To be deleted", ReadwiseId = 123 };

            Context.Highlights.Add(highlight);
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteHighlightAsync(highlightId);

            // Assert
            result.Should().BeTrue();
            
            var dbHighlight = await Context.Highlights.FindAsync(highlightId);
            dbHighlight.Should().BeNull();
        }

        [Fact]
        public async Task DeleteHighlightAsync_InvalidId_ReturnsFalse()
        {
            // Arrange
            // No data

            // Act
            var result = await _service.DeleteHighlightAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }
    }
}
