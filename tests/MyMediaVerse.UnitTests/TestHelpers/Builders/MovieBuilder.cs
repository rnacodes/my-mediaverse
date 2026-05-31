using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class MovieBuilder : MediaItemBuilder<Movie>
    {
        public MovieBuilder() : base(TestDataFactory.CreateMovie())
        {
        }

        public MovieBuilder WithReleaseYear(int releaseYear)
        {
            Item.ReleaseYear = releaseYear;
            return this;
        }

        public MovieBuilder WithDirector(string director)
        {
            Item.Director = director;
            return this;
        }

        public MovieBuilder WithTmdbId(string tmdbId)
        {
            Item.TmdbId = tmdbId;
            return this;
        }
    }
}
