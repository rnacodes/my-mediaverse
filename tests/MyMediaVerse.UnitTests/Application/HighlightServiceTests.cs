using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.Readwise;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestData;
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

            var updateDto = new UpdateHighlightDto
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
        public async Task UpdateHighlightAsync_PartialUpdate_LeavesOmittedFieldsUnchanged()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            var articleId = Guid.NewGuid();
            Context.Highlights.Add(new Highlight
            {
                Id = highlightId,
                Text = "Original text",
                Note = "Original note",
                Title = "Original Title",
                Author = "Original Author",
                Tags = "philosophy,stoicism",
                ArticleId = articleId,
                ReadwiseId = 123
            });
            await Context.SaveChangesAsync();

            // Act — only the note is sent; everything else must survive
            var result = await _service.UpdateHighlightAsync(highlightId, new UpdateHighlightDto
            {
                Note = "New note"
            });

            // Assert
            result.Note.Should().Be("New note");
            result.Text.Should().Be("Original text");
            result.Title.Should().Be("Original Title");
            result.Author.Should().Be("Original Author");
            result.Tags.Should().Be("philosophy,stoicism");
            result.ArticleId.Should().Be(articleId);
        }

        [Fact]
        public async Task UpdateHighlightAsync_EmptyTagList_ClearsTags()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            Context.Highlights.Add(new Highlight
            {
                Id = highlightId,
                Text = "Text",
                Tags = "philosophy,stoicism"
            });
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateHighlightAsync(highlightId, new UpdateHighlightDto
            {
                Tags = new List<string>()
            });

            // Assert
            result.Tags.Should().BeNull();
        }

        [Fact]
        public async Task UpdateHighlightAsync_EmptyText_Throws()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            Context.Highlights.Add(new Highlight { Id = highlightId, Text = "Text" });
            await Context.SaveChangesAsync();

            // Act & Assert
            await _service.Invoking(s => s.UpdateHighlightAsync(highlightId, new UpdateHighlightDto { Text = "   " }))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task UpdateHighlightAsync_MetadataFields_AreUpdatedAndNormalized()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            Context.Highlights.Add(new Highlight { Id = highlightId, Text = "Text" });
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateHighlightAsync(highlightId, new UpdateHighlightDto
            {
                Title = "New Title",
                Author = "New Author",
                Category = "Books",
                Tags = new List<string> { " Philosophy ", "STOICISM" }
            });

            // Assert
            result.Title.Should().Be("New Title");
            result.Author.Should().Be("New Author");
            result.Category.Should().Be("books");
            result.Tags.Should().Be("philosophy,stoicism");
        }

        [Fact]
        public async Task SetHighlightLinkAsync_ToBook_SetsBookAndClearsArticle()
        {
            // Arrange
            var article = new Article { Id = Guid.NewGuid(), Title = "Article" };
            var book = new Book { Id = Guid.NewGuid(), Title = "Book", Author = "Author" };
            var highlightId = Guid.NewGuid();
            Context.Articles.Add(article);
            Context.Books.Add(book);
            Context.Highlights.Add(new Highlight { Id = highlightId, Text = "Text", ArticleId = article.Id });
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.SetHighlightLinkAsync(highlightId, null, book.Id);

            // Assert
            result.BookId.Should().Be(book.Id);
            result.ArticleId.Should().BeNull();
        }

        [Fact]
        public async Task SetHighlightLinkAsync_BothTargets_Throws()
        {
            // Act & Assert
            await _service.Invoking(s => s.SetHighlightLinkAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SetHighlightLinkAsync_NoTargets_Unlinks()
        {
            // Arrange
            var article = new Article { Id = Guid.NewGuid(), Title = "Article" };
            var highlightId = Guid.NewGuid();
            Context.Articles.Add(article);
            Context.Highlights.Add(new Highlight { Id = highlightId, Text = "Text", ArticleId = article.Id });
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.SetHighlightLinkAsync(highlightId, null, null);

            // Assert
            result.ArticleId.Should().BeNull();
            result.BookId.Should().BeNull();
        }

        [Fact]
        public async Task SetHighlightLinkAsync_MissingTarget_Throws()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            Context.Highlights.Add(new Highlight { Id = highlightId, Text = "Text" });
            await Context.SaveChangesAsync();

            // Act & Assert
            await _service.Invoking(s => s.SetHighlightLinkAsync(highlightId, null, Guid.NewGuid()))
                .Should().ThrowAsync<InvalidOperationException>();
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

        [Fact]
        public async Task SyncHighlightsFromReadwiseAsync_ClientThrows_ReportsFailure()
        {
            // Arrange
            _service.ExportPageDelayMs = 0;
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .ThrowsAsync(new UnauthorizedAccessException("Readwise API token is invalid or expired."));

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert — an API failure must not look like a successful empty sync
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("invalid or expired");
            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(0);
        }

        [Fact]
        public async Task SyncHighlightsFromReadwiseAsync_EmptyExport_SucceedsWithoutWarning()
        {
            // Arrange
            _service.ExportPageDelayMs = 0;
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(new ReadwiseExportResponse());

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.WarningMessage.Should().BeNull();
            result.CreatedCount.Should().Be(0);
        }

        [Fact]
        public async Task SyncHighlightsFromReadwiseAsync_ImportsHighlightsFromExport()
        {
            // Arrange
            _service.ExportPageDelayMs = 0;
            var page = new ReadwiseExportResponse
            {
                nextPageCursor = null,
                results = new List<ReadwiseExportBookDto>
                {
                    new ReadwiseExportBookDto
                    {
                        user_book_id = 7,
                        title = "Sync Book",
                        author = "Sync Author",
                        category = "books",
                        highlights = new List<ReadwiseExportHighlightDto>
                        {
                            new ReadwiseExportHighlightDto { id = 42, text = "Synced highlight" }
                        }
                    }
                }
            };
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(page);

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(1);
            result.WarningMessage.Should().BeNull();
            var saved = await Context.Highlights.SingleAsync(h => h.ReadwiseId == 42);
            saved.Text.Should().Be("Synced highlight");
            saved.Title.Should().Be("Sync Book");
        }

        [Fact]
        public async Task SyncHighlightsFromReadwiseAsync_LinksBookHighlightOnCreate()
        {
            // Arrange
            _service.ExportPageDelayMs = 0;
            var book = new Book { Id = Guid.NewGuid(), Title = "Meditations", Author = "Marcus Aurelius" };
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var page = new ReadwiseExportResponse
            {
                results = new List<ReadwiseExportBookDto>
                {
                    new ReadwiseExportBookDto
                    {
                        user_book_id = 9,
                        title = "MEDITATIONS",
                        author = "marcus aurelius",
                        category = "books",
                        highlights = new List<ReadwiseExportHighlightDto>
                        {
                            new ReadwiseExportHighlightDto { id = 77, text = "Memento mori" }
                        }
                    }
                }
            };
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(page);

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.LinkedCount.Should().Be(1);
            var saved = await Context.Highlights.SingleAsync(h => h.ReadwiseId == 77);
            saved.BookId.Should().Be(book.Id);
        }

        [Fact]
        public async Task BulkCreateHighlightsAsync_AutoLinksBookByTitleAndAuthor()
        {
            // Arrange
            var book = new Book { Id = Guid.NewGuid(), Title = "Meditations", Author = "Marcus Aurelius" };
            Context.Books.Add(book);
            await Context.SaveChangesAsync();

            var dtos = new List<CreateHighlightDto>
            {
                new CreateHighlightDto
                {
                    Text = "Memento mori",
                    Title = "Meditations",
                    Author = "Marcus Aurelius",
                    Category = "books"
                }
            };

            // Act
            var result = await _service.BulkCreateHighlightsAsync(dtos);

            // Assert
            result.Created.Should().Be(1);
            result.Linked.Should().Be(1);
            var saved = await Context.Highlights.SingleAsync(h => h.Text == "Memento mori");
            saved.BookId.Should().Be(book.Id);
        }

        [Fact]
        public async Task BulkCreateHighlightsAsync_ExplicitLink_SkipsAutoLinking()
        {
            // Arrange — caller-provided link must be respected, not second-guessed
            var explicitBookId = Guid.NewGuid();
            Context.Books.Add(new Book { Id = explicitBookId, Title = "Chosen Book", Author = "Someone" });
            Context.Articles.Add(TestDataFactory.CreateArticle("Decoy Article"));
            await Context.SaveChangesAsync();

            var dtos = new List<CreateHighlightDto>
            {
                new CreateHighlightDto
                {
                    Text = "Explicitly linked",
                    Title = "Decoy Article",
                    Category = "articles",
                    BookId = explicitBookId
                }
            };

            // Act
            var result = await _service.BulkCreateHighlightsAsync(dtos);

            // Assert
            result.Created.Should().Be(1);
            result.Linked.Should().Be(0);
            var saved = await Context.Highlights.SingleAsync(h => h.Text == "Explicitly linked");
            saved.BookId.Should().Be(explicitBookId);
            saved.ArticleId.Should().BeNull();
        }

        [Fact]
        public async Task SyncHighlightsFromReadwiseAsync_PageCapReached_SurfacesWarning()
        {
            // Arrange — every page reports another page after it, so the sync
            // must stop at the safety limit and say so
            _service.ExportPageDelayMs = 0;
            var neverEndingPage = new ReadwiseExportResponse
            {
                nextPageCursor = "more",
                results = new List<ReadwiseExportBookDto>
                {
                    new ReadwiseExportBookDto
                    {
                        title = "Book",
                        highlights = new List<ReadwiseExportHighlightDto>()
                    }
                }
            };
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(neverEndingPage);

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.WarningMessage.Should().NotBeNull();
            result.WarningMessage.Should().Contain("safety limit");
            await _mockReadwiseClient.Received(100).GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>());
        }
    }
}
