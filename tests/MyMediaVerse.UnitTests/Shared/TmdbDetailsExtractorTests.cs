using AwesomeAssertions;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.UnitTests.Shared
{
    [Trait("Category", "Unit")]
    public class TmdbDetailsExtractorTests
    {
        private static TmdbCreditsDto CreditsWithCast(params string[] names) => new()
        {
            Cast = names.Select((name, index) => new TmdbCastMemberDto { Name = name, Order = index }).ToList()
        };

        [Fact]
        public void GetDirector_ShouldReturnNull_WhenCreditsAreMissing()
        {
            TmdbDetailsExtractor.GetDirector(new TmdbMovieDto()).Should().BeNull();
        }

        [Fact]
        public void GetDirector_ShouldReturnNull_WhenCrewHasNoDirector()
        {
            var movie = new TmdbMovieDto
            {
                Credits = new TmdbCreditsDto { Crew = { new TmdbCrewMemberDto { Name = "Hans Zimmer", Job = "Original Music Composer" } } }
            };

            TmdbDetailsExtractor.GetDirector(movie).Should().BeNull();
        }

        [Fact]
        public void GetDirector_ShouldJoinCoDirectors()
        {
            var movie = new TmdbMovieDto
            {
                Credits = new TmdbCreditsDto
                {
                    Crew =
                    {
                        new TmdbCrewMemberDto { Name = "Lana Wachowski", Job = "Director" },
                        new TmdbCrewMemberDto { Name = "Lilly Wachowski", Job = "Director" },
                        new TmdbCrewMemberDto { Name = "Lana Wachowski", Job = "Writer" }
                    }
                }
            };

            TmdbDetailsExtractor.GetDirector(movie).Should().Be("Lana Wachowski, Lilly Wachowski");
        }

        [Fact]
        public void GetCast_ShouldKeepTopEightByBillingOrder()
        {
            var credits = new TmdbCreditsDto
            {
                // Deliberately out of order: billing order decides, not payload order.
                Cast = Enumerable.Range(0, 12).Reverse()
                    .Select(i => new TmdbCastMemberDto { Name = $"Actor {i}", Order = i }).ToList()
            };

            var cast = TmdbDetailsExtractor.GetCast(credits);

            cast.Should().Be("Actor 0, Actor 1, Actor 2, Actor 3, Actor 4, Actor 5, Actor 6, Actor 7");
        }

        [Fact]
        public void GetCast_ShouldCapOnANameBoundary()
        {
            // Eight 90-character names would need 734 characters; only five whole names fit in 500.
            var names = Enumerable.Range(0, 8).Select(i => new string((char)('a' + i), 90)).ToArray();

            var cast = TmdbDetailsExtractor.GetCast(CreditsWithCast(names));

            cast.Should().NotBeNull();
            cast!.Length.Should().BeLessThanOrEqualTo(TmdbDetailsExtractor.MaxCastLength);
            cast.Split(", ").Should().Equal(names.Take(5));
        }

        [Fact]
        public void GetCast_ShouldReturnNull_WhenCastIsEmpty()
        {
            TmdbDetailsExtractor.GetCast(new TmdbCreditsDto()).Should().BeNull();
            TmdbDetailsExtractor.GetCast(null).Should().BeNull();
        }

        [Fact]
        public void GetCreator_ShouldCapAtColumnLength()
        {
            var show = new TmdbTvShowDto
            {
                CreatedBy =
                {
                    new TmdbCreatedByDto { Name = new string('a', 60) },
                    new TmdbCreatedByDto { Name = new string('b', 60) }
                }
            };

            TmdbDetailsExtractor.GetCreator(show).Should().Be(new string('a', 60));
        }

        [Fact]
        public void GetCreator_ShouldReturnNull_WhenNoCreators()
        {
            TmdbDetailsExtractor.GetCreator(new TmdbTvShowDto()).Should().BeNull();
        }

        [Fact]
        public void GetMpaaRating_ShouldReturnNull_WhenNoUsCertification()
        {
            var movie = new TmdbMovieDto
            {
                ReleaseDates = new TmdbReleaseDatesDto
                {
                    Results =
                    {
                        new TmdbCountryReleaseDatesDto { Iso31661 = "GB", ReleaseDates = { new TmdbReleaseDateDto { Certification = "12A" } } },
                        new TmdbCountryReleaseDatesDto { Iso31661 = "US", ReleaseDates = { new TmdbReleaseDateDto { Certification = "" } } }
                    }
                }
            };

            TmdbDetailsExtractor.GetMpaaRating(movie).Should().BeNull();
        }

        [Fact]
        public void GetMpaaRating_ShouldTakeFirstNonEmptyUsCertification()
        {
            var movie = new TmdbMovieDto
            {
                ReleaseDates = new TmdbReleaseDatesDto
                {
                    Results =
                    {
                        new TmdbCountryReleaseDatesDto
                        {
                            Iso31661 = "US",
                            ReleaseDates = { new TmdbReleaseDateDto { Certification = "" }, new TmdbReleaseDateDto { Certification = "R" } }
                        }
                    }
                }
            };

            TmdbDetailsExtractor.GetMpaaRating(movie).Should().Be("R");
        }

        [Fact]
        public void GetContentRating_ShouldReturnNull_WhenNoUsRating()
        {
            var show = new TmdbTvShowDto
            {
                ContentRatings = new TmdbContentRatingsDto { Results = { new TmdbCountryContentRatingDto { Iso31661 = "DE", Rating = "16" } } }
            };

            TmdbDetailsExtractor.GetContentRating(show).Should().BeNull();
            TmdbDetailsExtractor.GetContentRating(new TmdbTvShowDto()).Should().BeNull();
        }
    }
}
