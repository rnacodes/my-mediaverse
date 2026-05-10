using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class PodcastBuilder : MediaItemBuilder<Podcast>
    {
        public PodcastBuilder() : base(TestDataFactory.CreatePodcastSeries())
        {
        }

        public static PodcastBuilder Episode(Guid? parentId = null)
        {
            var episode = TestDataFactory.CreatePodcastEpisode(parentId: parentId);
            return new PodcastBuilder(episode);
        }

        private PodcastBuilder(Podcast podcast) : base(podcast)
        {
        }

        public PodcastBuilder WithPublisher(string publisher)
        {
            Item.Publisher = publisher;
            return this;
        }

        public PodcastBuilder WithType(PodcastType type)
        {
            Item.PodcastType = type;
            return this;
        }

        public PodcastBuilder WithEpisodes(params Podcast[] episodes)
        {
            Item.Episodes = episodes.ToList();
            return this;
        }

        public PodcastBuilder WithParent(Guid parentId)
        {
            Item.ParentPodcastId = parentId;
            return this;
        }
    }
}
