using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class BookBuilder : MediaItemBuilder<Book>
    {
        public BookBuilder() : base(TestDataFactory.CreateBook())
        {
        }

        public BookBuilder WithAuthor(string author)
        {
            Item.Author = author;
            return this;
        }

        public BookBuilder WithFormat(BookFormat format)
        {
            Item.Format = format;
            return this;
        }

        public BookBuilder WithIsbn(string isbn)
        {
            Item.ISBN = isbn;
            return this;
        }

        public BookBuilder WithHighlights(params Highlight[] highlights)
        {
            Item.Highlights = highlights.ToList();
            return this;
        }
    }
}
