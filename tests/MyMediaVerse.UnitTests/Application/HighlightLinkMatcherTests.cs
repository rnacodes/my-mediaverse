using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;
using MyMediaVerse.UnitTests.TestHelpers;
using Xunit;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class HighlightLinkMatcherTests : InMemoryDbTestBase
    {
        private Article AddArticle(string title, string? link)
        {
            var article = TestDataFactory.CreateArticle(title);
            article.Link = link;
            Context.Articles.Add(article);
            return article;
        }

        private Book AddBook(string title, string? author)
        {
            var book = new Book { Id = Guid.NewGuid(), Title = title, Author = author };
            Context.Books.Add(book);
            return book;
        }

        [Fact]
        public async Task ResolveAsync_ExactUrlMatch_ReturnsArticle()
        {
            var article = AddArticle("Test Article", "https://example.com/post");
            await Context.SaveChangesAsync();

            var match = await HighlightLinkMatcher.ResolveAsync(
                Context, new[] { "https://example.com/post" }, null, null, null);

            match.Article.Should().NotBeNull();
            match.Article!.Id.Should().Be(article.Id);
            match.Book.Should().BeNull();
        }

        [Fact]
        public async Task ResolveAsync_UrlMatch_IsCaseInsensitive()
        {
            var article = AddArticle("Test Article", "https://EXAMPLE.com/Post");
            await Context.SaveChangesAsync();

            var match = await HighlightLinkMatcher.ResolveAsync(
                Context, new[] { "https://example.com/post" }, null, null, null);

            match.Article.Should().NotBeNull();
            match.Article!.Id.Should().Be(article.Id);
        }

        [Fact]
        public async Task ResolveAsync_ProtocolMismatch_StillMatches()
        {
            // Stored with http, highlight arrives with https — the anchored
            // variant list must still find it
            var article = AddArticle("Test Article", "http://example.com/post");
            await Context.SaveChangesAsync();

            var match = await HighlightLinkMatcher.ResolveAsync(
                Context, new[] { "https://example.com/post" }, null, null, null);

            match.Article.Should().NotBeNull();
            match.Article!.Id.Should().Be(article.Id);
        }

        [Fact]
        public async Task ResolveAsync_LegacyWwwAndTrailingSlash_StillMatches()
        {
            // Links stored before write-time normalization can carry www. and a
            // trailing slash — both are legitimate stored forms of the same key
            var article = AddArticle("Test Article", "https://www.example.com/post/");
            await Context.SaveChangesAsync();

            var match = await HighlightLinkMatcher.ResolveAsync(
                Context, new[] { "http://example.com/post" }, null, null, null);

            match.Article.Should().NotBeNull();
            match.Article!.Id.Should().Be(article.Id);
        }

        [Fact]
        public async Task ResolveAsync_SuffixOfDifferentDomain_DoesNotMatch()
        {
            // The old unanchored EndsWith would let a lookalike domain claim the
            // highlight; the anchored variant list must not
            AddArticle("Evil Twin", "https://evil-example.com/post");
            await Context.SaveChangesAsync();

            var match = await HighlightLinkMatcher.ResolveAsync(
                Context, new[] { "https://example.com/post" }, null, null, null);

            match.HasMatch.Should().BeFalse();
        }

        [Fact]
        public async Task ResolveAsync_SecondCandidateUrl_IsTried()
        {
            var article = AddArticle("Test Article", "https://example.com/unique");
            await Context.SaveChangesAsync();

            var match = await HighlightLinkMatcher.ResolveAsync(
                Context,
                new[] { "https://example.com/nomatch", "https://example.com/unique" },
                null, null, null);

            match.Article.Should().NotBeNull();
            match.Article!.Id.Should().Be(article.Id);
        }

        [Fact]
        public async Task ResolveAsync_TitleFallback_OnlyForArticleCategory()
        {
            AddArticle("Shared Title", "https://example.com/a");
            await Context.SaveChangesAsync();

            var articleMatch = await HighlightLinkMatcher.ResolveAsync(
                Context, Array.Empty<string?>(), "Shared Title", null, "articles");
            var bookCategoryMatch = await HighlightLinkMatcher.ResolveAsync(
                Context, Array.Empty<string?>(), "Shared Title", null, "books");

            articleMatch.Article.Should().NotBeNull();
            bookCategoryMatch.HasMatch.Should().BeFalse();
        }

        [Fact]
        public async Task ResolveAsync_TitleFallback_IsCaseInsensitive()
        {
            var article = AddArticle("The Great Gatsby", null);
            await Context.SaveChangesAsync();

            var match = await HighlightLinkMatcher.ResolveAsync(
                Context, Array.Empty<string?>(), "the great gatsby", null, "ARTICLES");

            match.Article.Should().NotBeNull();
            match.Article!.Id.Should().Be(article.Id);
        }

        [Fact]
        public async Task ResolveAsync_BookMatch_RequiresTitleAndAuthor()
        {
            var book = AddBook("Meditations", "Marcus Aurelius");
            await Context.SaveChangesAsync();

            var withAuthor = await HighlightLinkMatcher.ResolveAsync(
                Context, Array.Empty<string?>(), "meditations", "MARCUS AURELIUS", "books");
            var withoutAuthor = await HighlightLinkMatcher.ResolveAsync(
                Context, Array.Empty<string?>(), "meditations", null, "books");

            withAuthor.Book.Should().NotBeNull();
            withAuthor.Book!.Id.Should().Be(book.Id);
            withoutAuthor.HasMatch.Should().BeFalse();
        }

        [Fact]
        public async Task ResolveAsync_UrlMatch_TakesPrecedenceOverTitleFallback()
        {
            var byUrl = AddArticle("Different Title", "https://example.com/post");
            AddArticle("Highlight Title", null);
            await Context.SaveChangesAsync();

            var match = await HighlightLinkMatcher.ResolveAsync(
                Context, new[] { "https://example.com/post" }, "Highlight Title", null, "articles");

            match.Article.Should().NotBeNull();
            match.Article!.Id.Should().Be(byUrl.Id);
        }

        [Fact]
        public async Task ResolveAsync_NoMatch_ReturnsEmpty()
        {
            var match = await HighlightLinkMatcher.ResolveAsync(
                Context, new[] { "https://example.com/none" }, "Unknown", "Nobody", "books");

            match.HasMatch.Should().BeFalse();
            match.Article.Should().BeNull();
            match.Book.Should().BeNull();
        }
    }
}
