using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class MixlistBuilder
    {
        private readonly Mixlist _mixlist;

        public MixlistBuilder()
        {
            _mixlist = TestDataFactory.CreateMixlist();
        }

        public MixlistBuilder WithId(Guid id)
        {
            _mixlist.Id = id;
            return this;
        }

        public MixlistBuilder WithName(string name)
        {
            _mixlist.Name = name;
            return this;
        }

        public MixlistBuilder WithDescription(string description)
        {
            _mixlist.Description = description;
            return this;
        }

        public MixlistBuilder WithMediaItems(params BaseMediaItem[] items)
        {
            _mixlist.MediaItems = items.ToList();
            return this;
        }

        public MixlistBuilder WithTopics(params Topic[] topics)
        {
            _mixlist.Topics = topics.ToList();
            return this;
        }

        public MixlistBuilder WithGenres(params Genre[] genres)
        {
            _mixlist.Genres = genres.ToList();
            return this;
        }

        public Mixlist Build() => _mixlist;

        public static implicit operator Mixlist(MixlistBuilder builder) => builder._mixlist;
    }
}
