using MyMediaVerse.Domain.Entities;
using System;
using Xunit;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class TvShowTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var tvShow = new TvShow { Title = "" };

            // Assert
            Assert.NotEqual(Guid.Empty, tvShow.Id);
            Assert.Equal("", tvShow.Title);
            Assert.Null(tvShow.Description);
            Assert.Null(tvShow.Thumbnail);
            Assert.Null(tvShow.Creator);
            Assert.Null(tvShow.Cast);
            Assert.Null(tvShow.FirstAirYear);
            Assert.Null(tvShow.LastAirYear);
            Assert.Null(tvShow.NumberOfSeasons);
            Assert.Null(tvShow.NumberOfEpisodes);
            Assert.Null(tvShow.ContentRating);
            Assert.Null(tvShow.TmdbId);
            Assert.Null(tvShow.TmdbRating);
            Assert.Null(tvShow.TmdbPosterPath);
            Assert.Null(tvShow.Tagline);
            Assert.Null(tvShow.Homepage);
            Assert.Null(tvShow.OriginalLanguage);
            Assert.Null(tvShow.OriginalName);
            Assert.NotNull(tvShow.Topics);
            Assert.Empty(tvShow.Topics);
            Assert.NotNull(tvShow.Genres);
            Assert.Empty(tvShow.Genres);
            Assert.NotNull(tvShow.Mixlists);
            Assert.Empty(tvShow.Mixlists);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };
            var testDate = DateTime.UtcNow;

            // Act
            tvShow.Title = "Test TV Show";
            tvShow.Description = "A test TV show description";
            tvShow.Thumbnail = "https://example.com/tvshow.jpg";
            tvShow.Link = "https://example.com/watch";
            tvShow.MediaType = MediaType.TVShow;
            tvShow.Status = Status.ActivelyExploring;
            tvShow.Rating = Rating.SuperLike;
            tvShow.OwnershipStatus = OwnershipStatus.Streamed;
            tvShow.DateAdded = testDate;
            tvShow.DateCompleted = testDate.AddDays(30);
            tvShow.Creator = "Test Creator";
            tvShow.Cast = "Actor A, Actor B, Actor C";
            tvShow.FirstAirYear = 2020;
            tvShow.LastAirYear = 2023;
            tvShow.NumberOfSeasons = 3;
            tvShow.NumberOfEpisodes = 30;
            tvShow.ContentRating = "TV-14";
            tvShow.TmdbId = "54321";
            tvShow.TmdbRating = 8.9;
            tvShow.TmdbPosterPath = "/poster.jpg";
            tvShow.Tagline = "The best show ever";
            tvShow.Homepage = "https://example.com/tvshow";
            tvShow.OriginalLanguage = "en";
            tvShow.OriginalName = "Test TV Show Original";

            // Assert
            Assert.Equal("Test TV Show", tvShow.Title);
            Assert.Equal("A test TV show description", tvShow.Description);
            Assert.Equal("https://example.com/tvshow.jpg", tvShow.Thumbnail);
            Assert.Equal("https://example.com/watch", tvShow.Link);
            Assert.Equal(MediaType.TVShow, tvShow.MediaType);
            Assert.Equal(Status.ActivelyExploring, tvShow.Status);
            Assert.Equal(Rating.SuperLike, tvShow.Rating);
            Assert.Equal(OwnershipStatus.Streamed, tvShow.OwnershipStatus);
            Assert.Equal(testDate, tvShow.DateAdded);
            Assert.Equal(testDate.AddDays(30), tvShow.DateCompleted);
            Assert.Equal("Test Creator", tvShow.Creator);
            Assert.Equal("Actor A, Actor B, Actor C", tvShow.Cast);
            Assert.Equal(2020, tvShow.FirstAirYear);
            Assert.Equal(2023, tvShow.LastAirYear);
            Assert.Equal(3, tvShow.NumberOfSeasons);
            Assert.Equal(30, tvShow.NumberOfEpisodes);
            Assert.Equal("TV-14", tvShow.ContentRating);
            Assert.Equal("54321", tvShow.TmdbId);
            Assert.Equal(8.9, tvShow.TmdbRating);
            Assert.Equal("/poster.jpg", tvShow.TmdbPosterPath);
            Assert.Equal("The best show ever", tvShow.Tagline);
            Assert.Equal("https://example.com/tvshow", tvShow.Homepage);
            Assert.Equal("en", tvShow.OriginalLanguage);
            Assert.Equal("Test TV Show Original", tvShow.OriginalName);
        }

        [Fact]
        public void FirstAirYear_CanBeSetToPositiveValue()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act
            tvShow.FirstAirYear = 2024;

            // Assert
            Assert.Equal(2024, tvShow.FirstAirYear);
        }

        [Fact]
        public void LastAirYear_CanBeSetToPositiveValue()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act
            tvShow.LastAirYear = 2026;

            // Assert
            Assert.Equal(2026, tvShow.LastAirYear);
        }

        [Fact]
        public void NumberOfSeasons_CanBeSetToPositiveValue()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act
            tvShow.NumberOfSeasons = 5;

            // Assert
            Assert.Equal(5, tvShow.NumberOfSeasons);
        }

        [Fact]
        public void NumberOfEpisodes_CanBeSetToPositiveValue()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act
            tvShow.NumberOfEpisodes = 50;

            // Assert
            Assert.Equal(50, tvShow.NumberOfEpisodes);
        }

        [Fact]
        public void TmdbRating_CanBeSetToDecimalValue()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act
            tvShow.TmdbRating = 9.2;

            // Assert
            Assert.Equal(9.2, tvShow.TmdbRating);
        }

        [Fact]
        public void GetTmdbPosterUrl_WithValidPath_ShouldReturnFullUrl()
        {
            // Arrange
            var tvShow = new TvShow { Title = "", TmdbPosterPath = "/poster123.jpg" };

            // Act
            var url = tvShow.GetTmdbPosterUrl();

            // Assert
            Assert.Equal("https://image.tmdb.org/t/p/w500/poster123.jpg", url);
        }

        [Fact]
        public void GetTmdbPosterUrl_WithCustomSize_ShouldReturnFullUrlWithSize()
        {
            // Arrange
            var tvShow = new TvShow { Title = "", TmdbPosterPath = "/poster123.jpg" };

            // Act
            var url = tvShow.GetTmdbPosterUrl("w300");

            // Assert
            Assert.Equal("https://image.tmdb.org/t/p/w300/poster123.jpg", url);
        }

        [Fact]
        public void GetTmdbPosterUrl_WithNullPath_ShouldReturnNull()
        {
            // Arrange
            var tvShow = new TvShow { Title = "", TmdbPosterPath = null };

            // Act
            var url = tvShow.GetTmdbPosterUrl();

            // Assert
            Assert.Null(url);
        }

        [Fact]
        public void GetTmdbPosterUrl_WithEmptyPath_ShouldReturnNull()
        {
            // Arrange
            var tvShow = new TvShow { Title = "", TmdbPosterPath = "" };

            // Act
            var url = tvShow.GetTmdbPosterUrl();

            // Assert
            Assert.Null(url);
        }

        [Fact]
        public void GetEffectiveThumbnail_WithTmdbPoster_ShouldReturnTmdbUrl()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                TmdbPosterPath = "/poster123.jpg",
                Thumbnail = "https://example.com/thumb.jpg"
            };

            // Act
            var thumbnail = tvShow.GetEffectiveThumbnail();

            // Assert
            Assert.Equal("https://image.tmdb.org/t/p/w500/poster123.jpg", thumbnail);
        }

        [Fact]
        public void GetEffectiveThumbnail_WithoutTmdbPoster_ShouldReturnBaseThumbnail()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                TmdbPosterPath = null,
                Thumbnail = "https://example.com/thumb.jpg"
            };

            // Act
            var thumbnail = tvShow.GetEffectiveThumbnail();

            // Assert
            Assert.Equal("https://example.com/thumb.jpg", thumbnail);
        }

        [Fact]
        public void GetEffectiveThumbnail_WithNeitherTmdbNorThumbnail_ShouldReturnNull()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                TmdbPosterPath = null,
                Thumbnail = null
            };

            // Act
            var thumbnail = tvShow.GetEffectiveThumbnail();

            // Assert
            Assert.Null(thumbnail);
        }

        [Fact]
        public void GetAirYears_WithBothYears_ShouldReturnRange()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                FirstAirYear = 2015,
                LastAirYear = 2020
            };

            // Act
            var airYears = tvShow.GetAirYears();

            // Assert
            Assert.Equal("2015-2020", airYears);
        }

        [Fact]
        public void GetAirYears_WithSameYear_ShouldReturnSingleYear()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                FirstAirYear = 2020,
                LastAirYear = 2020
            };

            // Act
            var airYears = tvShow.GetAirYears();

            // Assert
            Assert.Equal("2020", airYears);
        }

        [Fact]
        public void GetAirYears_WithOnlyFirstYear_ShouldReturnYearWithDash()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                FirstAirYear = 2020,
                LastAirYear = null
            };

            // Act
            var airYears = tvShow.GetAirYears();

            // Assert
            Assert.Equal("2020-", airYears); // Still airing
        }

        [Fact]
        public void GetAirYears_WithNoYears_ShouldReturnNull()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                FirstAirYear = null,
                LastAirYear = null
            };

            // Act
            var airYears = tvShow.GetAirYears();

            // Assert
            Assert.Null(airYears);
        }

        [Fact]
        public void GetEpisodeCount_WithSeasonsAndEpisodes_ShouldReturnFormattedString()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                NumberOfSeasons = 3,
                NumberOfEpisodes = 30
            };

            // Act
            var episodeCount = tvShow.GetEpisodeCount();

            // Assert
            Assert.Equal("3 seasons, 30 episodes", episodeCount);
        }

        [Fact]
        public void GetEpisodeCount_WithSingleSeason_ShouldUseSingularForm()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                NumberOfSeasons = 1,
                NumberOfEpisodes = 10
            };

            // Act
            var episodeCount = tvShow.GetEpisodeCount();

            // Assert
            Assert.Equal("1 season, 10 episodes", episodeCount);
        }

        [Fact]
        public void GetEpisodeCount_WithSingleEpisode_ShouldUseSingularForm()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                NumberOfSeasons = 1,
                NumberOfEpisodes = 1
            };

            // Act
            var episodeCount = tvShow.GetEpisodeCount();

            // Assert
            Assert.Equal("1 season, 1 episode", episodeCount);
        }

        [Fact]
        public void GetEpisodeCount_WithOnlySeasons_ShouldReturnSeasonsOnly()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                NumberOfSeasons = 5,
                NumberOfEpisodes = null
            };

            // Act
            var episodeCount = tvShow.GetEpisodeCount();

            // Assert
            Assert.Equal("5 seasons", episodeCount);
        }

        [Fact]
        public void GetEpisodeCount_WithOnlyEpisodes_ShouldReturnEpisodesOnly()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                NumberOfSeasons = null,
                NumberOfEpisodes = 42
            };

            // Act
            var episodeCount = tvShow.GetEpisodeCount();

            // Assert
            Assert.Equal("42 episodes", episodeCount);
        }

        [Fact]
        public void GetEpisodeCount_WithNoData_ShouldReturnNull()
        {
            // Arrange
            var tvShow = new TvShow
            {
                Title = "",
                NumberOfSeasons = null,
                NumberOfEpisodes = null
            };

            // Act
            var episodeCount = tvShow.GetEpisodeCount();

            // Assert
            Assert.Null(episodeCount);
        }

        [Fact]
        public void NavigationProperties_TopicsCanBeAddedAndRetrieved()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };
            var topic1 = new Topic { Name = "drama" };
            var topic2 = new Topic { Name = "mystery" };

            // Act
            tvShow.Topics.Add(topic1);
            tvShow.Topics.Add(topic2);

            // Assert
            Assert.Equal(2, tvShow.Topics.Count);
            Assert.Contains(topic1, tvShow.Topics);
            Assert.Contains(topic2, tvShow.Topics);
        }

        [Fact]
        public void NavigationProperties_GenresCanBeAddedAndRetrieved()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };
            var genre1 = new Genre { Name = "crime" };
            var genre2 = new Genre { Name = "thriller" };

            // Act
            tvShow.Genres.Add(genre1);
            tvShow.Genres.Add(genre2);

            // Assert
            Assert.Equal(2, tvShow.Genres.Count);
            Assert.Contains(genre1, tvShow.Genres);
            Assert.Contains(genre2, tvShow.Genres);
        }

        [Fact]
        public void NavigationProperties_MixlistsCanBeAddedAndRetrieved()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };
            var mixlist1 = new Mixlist { Name = "Favorite Shows" };
            var mixlist2 = new Mixlist { Name = "Binge Watch" };

            // Act
            tvShow.Mixlists.Add(mixlist1);
            tvShow.Mixlists.Add(mixlist2);

            // Assert
            Assert.Equal(2, tvShow.Mixlists.Count);
            Assert.Contains(mixlist1, tvShow.Mixlists);
            Assert.Contains(mixlist2, tvShow.Mixlists);
        }

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(tvShow);
            Assert.NotNull(tvShow.Id);
            Assert.NotNull(tvShow.Topics);
            Assert.NotNull(tvShow.Genres);
            Assert.NotNull(tvShow.Mixlists);
        }

        [Fact]
        public void ContentRating_CanStoreVariousRatings()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act & Assert
            tvShow.ContentRating = "TV-Y";
            Assert.Equal("TV-Y", tvShow.ContentRating);

            tvShow.ContentRating = "TV-PG";
            Assert.Equal("TV-PG", tvShow.ContentRating);

            tvShow.ContentRating = "TV-14";
            Assert.Equal("TV-14", tvShow.ContentRating);

            tvShow.ContentRating = "TV-MA";
            Assert.Equal("TV-MA", tvShow.ContentRating);
        }

        [Fact]
        public void Cast_CanStoreCommaSeparatedList()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act
            tvShow.Cast = "Bryan Cranston, Aaron Paul, Anna Gunn";

            // Assert
            Assert.Equal("Bryan Cranston, Aaron Paul, Anna Gunn", tvShow.Cast);
        }

        [Fact]
        public void Tagline_CanStoreShowTagline()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act
            tvShow.Tagline = "Winter is coming.";

            // Assert
            Assert.Equal("Winter is coming.", tvShow.Tagline);
        }

        [Fact]
        public void OriginalName_CanStoreForeignTitle()
        {
            // Arrange
            var tvShow = new TvShow { Title = "" };

            // Act
            tvShow.Title = "Money Heist";
            tvShow.OriginalName = "La casa de papel";
            tvShow.OriginalLanguage = "es";

            // Assert
            Assert.Equal("Money Heist", tvShow.Title);
            Assert.Equal("La casa de papel", tvShow.OriginalName);
            Assert.Equal("es", tvShow.OriginalLanguage);
        }
    }
}

