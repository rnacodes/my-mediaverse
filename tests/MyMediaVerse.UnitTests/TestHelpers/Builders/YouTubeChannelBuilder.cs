using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class YouTubeChannelBuilder : MediaItemBuilder<YouTubeChannel>
    {
        public YouTubeChannelBuilder() : base(TestDataFactory.CreateYouTubeChannel())
        {
        }

        public YouTubeChannelBuilder WithChannelExternalId(string channelExternalId)
        {
            Item.ChannelExternalId = channelExternalId;
            return this;
        }

        public YouTubeChannelBuilder WithSubscriberCount(long subscriberCount)
        {
            Item.SubscriberCount = subscriberCount;
            return this;
        }

        public YouTubeChannelBuilder WithVideos(params Video[] videos)
        {
            Item.Videos = videos.ToList();
            return this;
        }
    }
}
