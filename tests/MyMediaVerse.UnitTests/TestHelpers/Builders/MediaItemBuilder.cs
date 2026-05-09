using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    /// <summary>
    /// Generic fluent builder for the common <see cref="BaseMediaItem"/> surface
    /// (title, status, rating, topics, genres). Concrete builders (BookBuilder, etc.)
    /// start from a <see cref="TestData.TestDataFactory"/> instance and chain through here.
    /// </summary>
    public class MediaItemBuilder<TItem> where TItem : BaseMediaItem
    {
        protected TItem Item;

        public MediaItemBuilder(TItem item)
        {
            Item = item;
        }

        public MediaItemBuilder<TItem> WithId(Guid id)
        {
            Item.Id = id;
            return this;
        }

        public MediaItemBuilder<TItem> WithTitle(string title)
        {
            Item.Title = title;
            return this;
        }

        public MediaItemBuilder<TItem> WithStatus(Status status)
        {
            Item.Status = status;
            return this;
        }

        public MediaItemBuilder<TItem> WithRating(Rating rating)
        {
            Item.Rating = rating;
            return this;
        }

        public MediaItemBuilder<TItem> WithTopics(params Topic[] topics)
        {
            Item.Topics = topics.ToList();
            return this;
        }

        public MediaItemBuilder<TItem> WithGenres(params Genre[] genres)
        {
            Item.Genres = genres.ToList();
            return this;
        }

        public MediaItemBuilder<TItem> WithMixlists(params Mixlist[] mixlists)
        {
            Item.Mixlists = mixlists.ToList();
            return this;
        }

        public MediaItemBuilder<TItem> WithDateAdded(DateTime dateAdded)
        {
            Item.DateAdded = dateAdded;
            return this;
        }

        public TItem Build() => Item;

        public static implicit operator TItem(MediaItemBuilder<TItem> builder) => builder.Item;
    }
}
