using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class TvShowBuilder : MediaItemBuilder<TvShow>
    {
        public TvShowBuilder() : base(TestDataFactory.CreateTvShow())
        {
        }

        public TvShowBuilder WithFirstAirYear(int firstAirYear)
        {
            Item.FirstAirYear = firstAirYear;
            return this;
        }

        public TvShowBuilder WithCreator(string creator)
        {
            Item.Creator = creator;
            return this;
        }

        public TvShowBuilder WithTmdbId(string tmdbId)
        {
            Item.TmdbId = tmdbId;
            return this;
        }

        public TvShowBuilder WithEpisodes(params TvShowEpisode[] episodes)
        {
            Item.Episodes = episodes.ToList();
            return this;
        }
    }
}
