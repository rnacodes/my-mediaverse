using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Enums;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class ArticleBuilder : MediaItemBuilder<Article>
    {
        public ArticleBuilder() : base(TestDataFactory.CreateArticle())
        {
        }

        public ArticleBuilder WithAuthor(string author)
        {
            Item.Author = author;
            return this;
        }

        public ArticleBuilder WithPublication(string publication)
        {
            Item.Publication = publication;
            return this;
        }

        public ArticleBuilder WithSyncStatus(SyncStatus syncStatus)
        {
            Item.SyncStatus = syncStatus;
            return this;
        }

        public ArticleBuilder WithHighlights(params Highlight[] highlights)
        {
            Item.Highlights = highlights.ToList();
            return this;
        }
    }
}
