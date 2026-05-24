using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class VideoBuilder : MediaItemBuilder<Video>
    {
        public VideoBuilder() : base(TestDataFactory.CreateVideo())
        {
        }

        public VideoBuilder WithPlatform(string platform)
        {
            Item.Platform = platform;
            return this;
        }

        public VideoBuilder WithVideoType(VideoType videoType)
        {
            Item.VideoType = videoType;
            return this;
        }

        public VideoBuilder WithChannel(Guid channelId)
        {
            Item.ChannelId = channelId;
            return this;
        }

        public VideoBuilder WithEpisodes(params Video[] episodes)
        {
            Item.Episodes = episodes.ToList();
            return this;
        }
    }
}
