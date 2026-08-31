using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private readonly ITypesenseService _mockTypesenseService;
        private readonly ILogger<HighlightService> _mockLogger;
        private readonly HighlightService _service;

        public HighlightServiceTests()
        {
            _mockReadwiseClient = Substitute.For<IReadwiseApiClient>();
            _mockTypesenseService = Substitute.For<ITypesenseService>();
            _mockLogger = Substitute.For<ILogger<HighlightService>>();

            _service = new HighlightService(Context, _mockReadwiseClient, _mockTypesenseService, _mockLogger);
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
        public async Task GetHighlightsByTagAsync_MatchesWholeTagsOnly_NotSubstrings()
        {
            // Arrange — "art" must not match "articles" or "cart"
            Context.Highlights.AddRange(
                new Highlight { Id = Guid.NewGuid(), Text = "Substring trap", Tags = "articles", ReadwiseId = 10 },
                new Highlight { Id = Guid.NewGuid(), Text = "Suffix trap", Tags = "cart,design", ReadwiseId = 11 },
                new Highlight { Id = Guid.NewGuid(), Text = "Exact match", Tags = "art", ReadwiseId = 12 });
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetHighlightsByTagAsync("art");

            // Assert
            result.Should().ContainSingle().Which.Text.Should().Be("Exact match");
        }

        [Theory]
        [InlineData("first", "first,middle,last")]  // first position
        [InlineData("middle", "first,middle,last")] // middle position
        [InlineData("last", "first,middle,last")]   // last position
        [InlineData("only", "only")]                // single tag
        public async Task GetHighlightsByTagAsync_MatchesTagInAnyPosition(string tag, string storedTags)
        {
            // Arrange
            Context.Highlights.Add(new Highlight { Id = Guid.NewGuid(), Text = "Positional", Tags = storedTags, ReadwiseId = 20 });
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetHighlightsByTagAsync(tag);

            // Assert
            result.Should().ContainSingle();
        }

        [Fact]
        public async Task GetHighlightsByTagAsync_NormalizesCaseAndWhitespace()
        {
            // Arrange — stored tags are lowercase; lookups may arrive messy
            Context.Highlights.Add(new Highlight { Id = Guid.NewGuid(), Text = "Cased", Tags = "stoicism", ReadwiseId = 21 });
            await Context.SaveChangesAsync();

            // Act
            var result = await _service.GetHighlightsByTagAsync("  Stoicism ");

            // Assert
            result.Should().ContainSingle();
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
            await _mockTypesenseService.DidNotReceive().DeleteHighlightAsync(Arg.Any<Guid>());
        }

        [Fact]
        public async Task BulkDeleteHighlightsAsync_DeletesOnlyMatchingIds_AndCleansIndex()
        {
            // Arrange — two to delete, one unknown id, one bystander
            var keep = new Highlight { Id = Guid.NewGuid(), Text = "Keep me", ReadwiseId = 200 };
            var doomedA = new Highlight { Id = Guid.NewGuid(), Text = "Doomed A", ReadwiseId = 201 };
            var doomedB = new Highlight { Id = Guid.NewGuid(), Text = "Doomed B", ReadwiseId = 202 };
            Context.Highlights.AddRange(keep, doomedA, doomedB);
            await Context.SaveChangesAsync();

            // Act — unknown ids are skipped, not errors
            var deletedCount = await _service.BulkDeleteHighlightsAsync(
                new List<Guid> { doomedA.Id, doomedB.Id, Guid.NewGuid() });

            // Assert
            deletedCount.Should().Be(2);
            (await Context.Highlights.CountAsync()).Should().Be(1);
            (await Context.Highlights.SingleAsync()).Id.Should().Be(keep.Id);
            await _mockTypesenseService.Received(1).DeleteHighlightAsync(doomedA.Id);
            await _mockTypesenseService.Received(1).DeleteHighlightAsync(doomedB.Id);
            await _mockTypesenseService.DidNotReceive().DeleteHighlightAsync(keep.Id);
        }

        [Fact]
        public async Task BulkDeleteHighlightsAsync_NoMatches_ReturnsZero()
        {
            var deletedCount = await _service.BulkDeleteHighlightsAsync(new List<Guid> { Guid.NewGuid() });

            deletedCount.Should().Be(0);
            await _mockTypesenseService.DidNotReceive().DeleteHighlightAsync(Arg.Any<Guid>());
        }

        [Fact]
        public async Task BulkDeleteHighlightsAsync_SearchIndexFailure_StillDeletesRows()
        {
            var doomed = new Highlight { Id = Guid.NewGuid(), Text = "Doomed", ReadwiseId = 203 };
            Context.Highlights.Add(doomed);
            await Context.SaveChangesAsync();
            _mockTypesenseService.DeleteHighlightAsync(Arg.Any<Guid>())
                .ThrowsAsync(new HttpRequestException("typesense unreachable"));

            var deletedCount = await _service.BulkDeleteHighlightsAsync(new List<Guid> { doomed.Id });

            deletedCount.Should().Be(1);
            (await Context.Highlights.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task DeleteHighlightAsync_RemovesSearchDocumentBestEffort()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            Context.Highlights.Add(new Highlight { Id = highlightId, Text = "Indexed", ReadwiseId = 124 });
            await Context.SaveChangesAsync();

            // Act
            await _service.DeleteHighlightAsync(highlightId);

            // Assert
            await _mockTypesenseService.Received(1).DeleteHighlightAsync(highlightId);
        }

        [Fact]
        public async Task DeleteHighlightAsync_SearchIndexFailure_StillDeletesRow()
        {
            // Arrange — Typesense being down must not block the DB delete
            var highlightId = Guid.NewGuid();
            Context.Highlights.Add(new Highlight { Id = highlightId, Text = "Indexed", ReadwiseId = 125 });
            await Context.SaveChangesAsync();
            _mockTypesenseService.DeleteHighlightAsync(Arg.Any<Guid>())
                .ThrowsAsync(new HttpRequestException("typesense unreachable"));

            // Act
            var result = await _service.DeleteHighlightAsync(highlightId);

            // Assert
            result.Should().BeTrue();
            (await Context.Highlights.FindAsync(highlightId)).Should().BeNull();
        }

        [Fact]
        public async Task CleanAllHighlightTextAsync_PagesThroughTheWholeTable()
        {
            // Arrange — more rows than one 200-row page, dirty rows scattered across pages
            for (var i = 0; i < 205; i++)
            {
                Context.Highlights.Add(new Highlight
                {
                    Id = Guid.NewGuid(),
                    ReadwiseId = 1000 + i,
                    Text = i % 100 == 0 ? "<div>dirty text</div>" : $"clean text {i}"
                });
            }
            await Context.SaveChangesAsync();

            // Act
            var cleanedCount = await _service.CleanAllHighlightTextAsync();

            // Assert — rows 0, 100, and 200 were dirty; 200 sits on the second page
            cleanedCount.Should().Be(3);
            (await Context.Highlights.CountAsync(h => h.Text.Contains("<div>"))).Should().Be(0);
            (await Context.Highlights.CountAsync()).Should().Be(205);
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

        private static ReadwiseExportResponse SinglePage(params ReadwiseExportBookDto[] books) =>
            new() { nextPageCursor = null, results = books.ToList() };

        [Fact]
        public async Task SyncHighlights_DeletedInReadwise_RemovesExistingRow()
        {
            // Arrange — we already imported ReadwiseId 42; Readwise now reports it deleted
            _service.ExportPageDelayMs = 0;
            Context.Highlights.Add(new Highlight { Id = Guid.NewGuid(), ReadwiseId = 42, Text = "Old copy" });
            await Context.SaveChangesAsync();

            var page = SinglePage(new ReadwiseExportBookDto
            {
                user_book_id = 7,
                title = "Sync Book",
                highlights = new List<ReadwiseExportHighlightDto>
                {
                    new ReadwiseExportHighlightDto { id = 42, text = "Old copy", is_deleted = true }
                }
            });
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>()).Returns(page);

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.DeletedCount.Should().Be(1);
            result.CreatedCount.Should().Be(0);
            result.UpdatedCount.Should().Be(0);
            (await Context.Highlights.AnyAsync(h => h.ReadwiseId == 42)).Should().BeFalse();
            await _mockTypesenseService.Received(1).DeleteHighlightAsync(Arg.Any<Guid>());
        }

        [Fact]
        public async Task SyncHighlights_DiscardedInReadwise_RemovesExistingRow()
        {
            // Arrange
            _service.ExportPageDelayMs = 0;
            Context.Highlights.Add(new Highlight { Id = Guid.NewGuid(), ReadwiseId = 43, Text = "Discarded copy" });
            await Context.SaveChangesAsync();

            var page = SinglePage(new ReadwiseExportBookDto
            {
                user_book_id = 7,
                title = "Sync Book",
                highlights = new List<ReadwiseExportHighlightDto>
                {
                    new ReadwiseExportHighlightDto { id = 43, text = "Discarded copy", is_discard = true }
                }
            });
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>()).Returns(page);

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.DeletedCount.Should().Be(1);
            (await Context.Highlights.AnyAsync(h => h.ReadwiseId == 43)).Should().BeFalse();
        }

        [Fact]
        public async Task SyncHighlights_TombstoneForUnknownHighlight_IsIgnoredWithoutCreating()
        {
            // Arrange — deleted in Readwise, never imported here
            _service.ExportPageDelayMs = 0;
            var page = SinglePage(new ReadwiseExportBookDto
            {
                user_book_id = 7,
                title = "Sync Book",
                highlights = new List<ReadwiseExportHighlightDto>
                {
                    new ReadwiseExportHighlightDto { id = 44, text = "Never imported", is_deleted = true }
                }
            });
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>()).Returns(page);

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(0);
            result.DeletedCount.Should().Be(0);
            (await Context.Highlights.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task SyncHighlights_BookDeletedInReadwise_RemovesItsHighlights()
        {
            // Arrange — the whole source is tombstoned; nested highlights go with it
            _service.ExportPageDelayMs = 0;
            Context.Highlights.Add(new Highlight { Id = Guid.NewGuid(), ReadwiseId = 45, Text = "From deleted book" });
            Context.Highlights.Add(new Highlight { Id = Guid.NewGuid(), ReadwiseId = 46, Text = "Also from deleted book" });
            await Context.SaveChangesAsync();

            var page = SinglePage(new ReadwiseExportBookDto
            {
                user_book_id = 8,
                title = "Deleted Book",
                is_deleted = true,
                highlights = new List<ReadwiseExportHighlightDto>
                {
                    new ReadwiseExportHighlightDto { id = 45, text = "From deleted book" },
                    new ReadwiseExportHighlightDto { id = 46, text = "Also from deleted book" }
                }
            });
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>()).Returns(page);

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.DeletedCount.Should().Be(2);
            (await Context.Highlights.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task SyncHighlights_LiveHighlightAlongsideTombstone_IsStillSynced()
        {
            // Arrange — one live, one deleted in the same source
            _service.ExportPageDelayMs = 0;
            var page = SinglePage(new ReadwiseExportBookDto
            {
                user_book_id = 7,
                title = "Sync Book",
                highlights = new List<ReadwiseExportHighlightDto>
                {
                    new ReadwiseExportHighlightDto { id = 47, text = "Still alive" },
                    new ReadwiseExportHighlightDto { id = 48, text = "Gone", is_deleted = true }
                }
            });
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>()).Returns(page);

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert
            result.CreatedCount.Should().Be(1);
            result.DeletedCount.Should().Be(0);
            (await Context.Highlights.SingleAsync()).ReadwiseId.Should().Be(47);
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
        public async Task BulkCreateHighlightsAsync_ReUpload_UpdatesInPlaceInsteadOfDuplicating()
        {
            // Arrange — the same file uploaded twice, the second time with an edited note
            var dtos = new List<CreateHighlightDto>
            {
                new CreateHighlightDto { Text = "First highlight", Title = "Some Book", Note = "v1" },
                new CreateHighlightDto { Text = "Second highlight", Title = "Some Book" }
            };
            await _service.BulkCreateHighlightsAsync(dtos);

            dtos[0].Note = "v2";

            // Act
            var result = await _service.BulkCreateHighlightsAsync(dtos);

            // Assert — no new rows; the note edit landed
            result.Created.Should().Be(0);
            result.Updated.Should().Be(2);
            (await Context.Highlights.CountAsync()).Should().Be(2);
            (await Context.Highlights.SingleAsync(h => h.Text == "First highlight")).Note.Should().Be("v2");
        }

        [Fact]
        public async Task BulkCreateHighlightsAsync_DuplicateWithinBatch_ImportsFirstAndSkipsRest()
        {
            var dtos = new List<CreateHighlightDto>
            {
                new CreateHighlightDto { Text = "Same text", Title = "Same Book" },
                new CreateHighlightDto { Text = "Same text", Title = "Same Book" },
                new CreateHighlightDto { Text = "Same text", Title = "Same Book" }
            };

            var result = await _service.BulkCreateHighlightsAsync(dtos);

            result.Created.Should().Be(1);
            result.Skipped.Should().Be(2);
            (await Context.Highlights.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task BulkCreateHighlightsAsync_MatchesReadwiseRow_PreservesReadwiseIdAndLink()
        {
            // Arrange — the highlight already exists from a Readwise sync, linked to a book
            var book = new Book { Id = Guid.NewGuid(), Title = "Meditations", Author = "Marcus Aurelius" };
            Context.Books.Add(book);
            Context.Highlights.Add(new Highlight
            {
                Id = Guid.NewGuid(),
                ReadwiseId = 555,
                Text = "Memento mori",
                Title = "Meditations",
                BookId = book.Id
            });
            await Context.SaveChangesAsync();

            var dtos = new List<CreateHighlightDto>
            {
                new CreateHighlightDto { Text = "Memento mori", Title = "MEDITATIONS", Note = "from markdown" }
            };

            // Act — title matching is case-insensitive
            var result = await _service.BulkCreateHighlightsAsync(dtos);

            // Assert — one row, still Readwise-owned, still linked, note added
            result.Created.Should().Be(0);
            result.Updated.Should().Be(1);
            var saved = await Context.Highlights.SingleAsync();
            saved.ReadwiseId.Should().Be(555);
            saved.BookId.Should().Be(book.Id);
            saved.Note.Should().Be("from markdown");
        }

        [Fact]
        public async Task BulkCreateHighlightsAsync_SameTitleDifferentText_CreatesNewRow()
        {
            Context.Highlights.Add(new Highlight { Id = Guid.NewGuid(), Text = "Old text", Title = "Some Book" });
            await Context.SaveChangesAsync();

            var result = await _service.BulkCreateHighlightsAsync(new List<CreateHighlightDto>
            {
                new CreateHighlightDto { Text = "New text", Title = "Some Book" }
            });

            result.Created.Should().Be(1);
            result.Updated.Should().Be(0);
            (await Context.Highlights.CountAsync()).Should().Be(2);
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

        #region Tags → Topics (retro pass)

        private ReadwiseExportResponse SinglePageExport(params ReadwiseExportHighlightDto[] highlights) =>
            new ReadwiseExportResponse
            {
                nextPageCursor = null,
                results = new List<ReadwiseExportBookDto>
                {
                    new ReadwiseExportBookDto
                    {
                        user_book_id = 7,
                        title = "Topic Book",
                        author = "Topic Author",
                        category = "books",
                        highlights = highlights.ToList()
                    }
                }
            };

        [Fact]
        public async Task SyncHighlights_NewHighlightWithTags_DerivesNormalizedTopics()
        {
            // Arrange — tags arrive untrimmed and mixed-case
            _service.ExportPageDelayMs = 0;
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(SinglePageExport(new ReadwiseExportHighlightDto
                {
                    id = 42,
                    text = "Text",
                    tags = new List<ReadwiseExportTagDto>
                    {
                        new ReadwiseExportTagDto { name = " Philosophy " },
                        new ReadwiseExportTagDto { name = "STOICISM" }
                    }
                }));

            // Act
            await _service.SyncHighlightsFromReadwiseAsync();

            // Assert — tags string normalized, topic links mirror it
            var saved = await Context.Highlights.Include(h => h.Topics).SingleAsync(h => h.ReadwiseId == 42);
            saved.Tags.Should().Be("philosophy,stoicism");
            saved.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "philosophy", "stoicism" });
        }

        [Fact]
        public async Task SyncHighlights_ExistingHighlight_TopicsReplacedToMatchTags()
        {
            // Arrange — Readwise is source of truth for tags: a removed tag drops its topic link
            var oldTopic = new Topic { Name = "oldtag" };
            var highlight = new Highlight { Id = Guid.NewGuid(), ReadwiseId = 42, Text = "Text", Tags = "oldtag" };
            highlight.Topics.Add(oldTopic);
            Context.Highlights.Add(highlight);
            await Context.SaveChangesAsync();

            _service.ExportPageDelayMs = 0;
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(SinglePageExport(new ReadwiseExportHighlightDto
                {
                    id = 42,
                    text = "Text",
                    tags = new List<ReadwiseExportTagDto> { new ReadwiseExportTagDto { name = "newtag" } }
                }));

            // Act
            await _service.SyncHighlightsFromReadwiseAsync();

            // Assert — link replaced; the Topic entity itself survives
            var saved = await Context.Highlights.Include(h => h.Topics).SingleAsync(h => h.ReadwiseId == 42);
            saved.Tags.Should().Be("newtag");
            saved.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "newtag" });
            Context.Topics.Any(t => t.Name == "oldtag").Should().BeTrue();
        }

        [Fact]
        public async Task SyncHighlights_SharedNewTag_CreatesOneTopicAndReusesExisting()
        {
            // Arrange — one pre-existing topic, one brand-new tag shared by two highlights
            Context.Topics.Add(new Topic { Name = "philosophy" });
            await Context.SaveChangesAsync();

            _service.ExportPageDelayMs = 0;
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(SinglePageExport(
                    new ReadwiseExportHighlightDto
                    {
                        id = 1,
                        text = "First",
                        tags = new List<ReadwiseExportTagDto>
                        {
                            new ReadwiseExportTagDto { name = "philosophy" },
                            new ReadwiseExportTagDto { name = "brand-new" }
                        }
                    },
                    new ReadwiseExportHighlightDto
                    {
                        id = 2,
                        text = "Second",
                        tags = new List<ReadwiseExportTagDto> { new ReadwiseExportTagDto { name = "brand-new" } }
                    }));

            // Act
            await _service.SyncHighlightsFromReadwiseAsync();

            // Assert — no duplicate Topic rows for either name
            Context.Topics.Count(t => t.Name == "philosophy").Should().Be(1);
            Context.Topics.Count(t => t.Name == "brand-new").Should().Be(1);
        }

        [Fact]
        public async Task SyncHighlights_MalformedHighlightedAt_StoresNullInsteadOfAborting()
        {
            // Arrange
            _service.ExportPageDelayMs = 0;
            _mockReadwiseClient.GetExportAsync(Arg.Any<string?>(), Arg.Any<string?>())
                .Returns(SinglePageExport(new ReadwiseExportHighlightDto
                {
                    id = 42,
                    text = "Text",
                    highlighted_at = "not-a-date"
                }));

            // Act
            var result = await _service.SyncHighlightsFromReadwiseAsync();

            // Assert — the highlight imports; only the date is dropped
            result.Success.Should().BeTrue();
            result.CreatedCount.Should().Be(1);
            var saved = await Context.Highlights.SingleAsync(h => h.ReadwiseId == 42);
            saved.HighlightedAt.Should().BeNull();
        }

        [Fact]
        public async Task CreateHighlightAsync_Tags_NormalizedAndTopicsDerived()
        {
            // Act
            var result = await _service.CreateHighlightAsync(new CreateHighlightDto
            {
                Text = "Text",
                Tags = new List<string> { " Philosophy ", "STOICISM", "philosophy" }
            });

            // Assert
            result.Tags.Should().Be("philosophy,stoicism");
            var saved = await Context.Highlights.Include(h => h.Topics).SingleAsync(h => h.Id == result.Id);
            saved.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "philosophy", "stoicism" });
        }

        [Fact]
        public async Task UpdateHighlightAsync_Tags_ReconcileTopicLinks()
        {
            // Arrange
            var highlightId = Guid.NewGuid();
            var highlight = new Highlight { Id = highlightId, Text = "Text", Tags = "oldtag" };
            highlight.Topics.Add(new Topic { Name = "oldtag" });
            Context.Highlights.Add(highlight);
            await Context.SaveChangesAsync();

            // Act
            await _service.UpdateHighlightAsync(highlightId, new UpdateHighlightDto
            {
                Tags = new List<string> { "newtag" }
            });

            // Assert
            var saved = await Context.Highlights.Include(h => h.Topics).SingleAsync(h => h.Id == highlightId);
            saved.Tags.Should().Be("newtag");
            saved.Topics.Select(t => t.Name).Should().BeEquivalentTo(new[] { "newtag" });
        }

        [Fact]
        public async Task BulkCreateHighlightsAsync_SetsContractFields_AndDerivesTopics()
        {
            // Act
            var result = await _service.BulkCreateHighlightsAsync(new List<CreateHighlightDto>
            {
                new CreateHighlightDto { Text = "One", Title = "Bulk Source", Tags = new List<string> { " Shared " } },
                new CreateHighlightDto { Text = "Two", Title = "Bulk Source", Tags = new List<string> { "shared" } }
            });

            // Assert — contract fields plus topic derivation across the batch
            result.Success.Should().BeTrue();
            result.Operation.Should().Be("highlight-bulk-import");
            result.Created.Should().Be(2);
            result.CompletedAt.Should().NotBeNull();
            result.Duration.Should().NotBeNull();
            result.TotalProcessed.Should().Be(2);
            Context.Topics.Count(t => t.Name == "shared").Should().Be(1);
            var saved = await Context.Highlights.Include(h => h.Topics).ToListAsync();
            saved.Should().OnlyContain(h => h.Topics.Any(t => t.Name == "shared"));
        }

        [Fact]
        public async Task BackfillHighlightTopicsAsync_NormalizesLegacyTagsAndDerivesTopics_Idempotently()
        {
            // Arrange — legacy rows written before trimming was consistent
            Context.Highlights.AddRange(
                new Highlight { Id = Guid.NewGuid(), Text = "A", Tags = " Philosophy ,stoicism" },
                new Highlight { Id = Guid.NewGuid(), Text = "B", Tags = "philosophy" },
                new Highlight { Id = Guid.NewGuid(), Text = "C", Tags = null });
            await Context.SaveChangesAsync();

            // Act
            var firstRun = await _service.BackfillHighlightTopicsAsync();
            var secondRun = await _service.BackfillHighlightTopicsAsync();

            // Assert
            firstRun.Should().Be(2);
            secondRun.Should().Be(0);
            Context.Topics.Count(t => t.Name == "philosophy").Should().Be(1);
            var tagged = await Context.Highlights.Include(h => h.Topics).Where(h => h.Tags != null).ToListAsync();
            tagged.Single(h => h.Text == "A").Tags.Should().Be("philosophy,stoicism");
            tagged.Single(h => h.Text == "A").Topics.Should().HaveCount(2);
            tagged.Single(h => h.Text == "B").Topics.Should().HaveCount(1);
        }

        #endregion
    }
}
