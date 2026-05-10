using MyMediaVerse.Domain.Entities;
using System;
using Xunit;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class MovieTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var movie = new Movie { Title = "" };

            // Assert
            Assert.NotEqual(Guid.Empty, movie.Id);
            Assert.Equal("", movie.Title);
            Assert.Null(movie.Description);
            Assert.Null(movie.Thumbnail);
            Assert.Null(movie.Director);
            Assert.Null(movie.Cast);
            Assert.Null(movie.ReleaseYear);
            Assert.Null(movie.RuntimeMinutes);
            Assert.Null(movie.MpaaRating);
            Assert.Null(movie.ImdbId);
            Assert.Null(movie.TmdbId);
            Assert.Null(movie.TmdbRating);
            Assert.Null(movie.TmdbBackdropPath);
            Assert.Null(movie.Tagline);
            Assert.Null(movie.Homepage);
            Assert.Null(movie.OriginalLanguage);
            Assert.Null(movie.OriginalTitle);
            Assert.NotNull(movie.Topics);
            Assert.Empty(movie.Topics);
            Assert.NotNull(movie.Genres);
            Assert.Empty(movie.Genres);
            Assert.NotNull(movie.Mixlists);
            Assert.Empty(movie.Mixlists);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var movie = new Movie { Title = "" };
            var testDate = DateTime.UtcNow;

            // Act
            movie.Title = "Test Movie";
            movie.Description = "A test movie description";
            movie.Thumbnail = "https://example.com/movie.jpg";
            movie.Link = "https://example.com/watch";
            movie.MediaType = MediaType.Movie;
            movie.Status = Status.ActivelyExploring;
            movie.Rating = Rating.SuperLike;
            movie.OwnershipStatus = OwnershipStatus.Own;
            movie.DateAdded = testDate;
            movie.DateCompleted = testDate.AddDays(7);
            movie.Director = "Test Director";
            movie.Cast = "Actor 1, Actor 2, Actor 3";
            movie.ReleaseYear = 2023;
            movie.RuntimeMinutes = 120;
            movie.MpaaRating = "PG-13";
            movie.ImdbId = "tt1234567";
            movie.TmdbId = "12345";
            movie.TmdbRating = 8.5;
            movie.TmdbBackdropPath = "/backdrop.jpg";
            movie.Tagline = "An epic adventure";
            movie.Homepage = "https://example.com/movie";
            movie.OriginalLanguage = "en";
            movie.OriginalTitle = "Test Movie Original";

            // Assert
            Assert.Equal("Test Movie", movie.Title);
            Assert.Equal("A test movie description", movie.Description);
            Assert.Equal("https://example.com/movie.jpg", movie.Thumbnail);
            Assert.Equal("https://example.com/watch", movie.Link);
            Assert.Equal(MediaType.Movie, movie.MediaType);
            Assert.Equal(Status.ActivelyExploring, movie.Status);
            Assert.Equal(Rating.SuperLike, movie.Rating);
            Assert.Equal(OwnershipStatus.Own, movie.OwnershipStatus);
            Assert.Equal(testDate, movie.DateAdded);
            Assert.Equal(testDate.AddDays(7), movie.DateCompleted);
            Assert.Equal("Test Director", movie.Director);
            Assert.Equal("Actor 1, Actor 2, Actor 3", movie.Cast);
            Assert.Equal(2023, movie.ReleaseYear);
            Assert.Equal(120, movie.RuntimeMinutes);
            Assert.Equal("PG-13", movie.MpaaRating);
            Assert.Equal("tt1234567", movie.ImdbId);
            Assert.Equal("12345", movie.TmdbId);
            Assert.Equal(8.5, movie.TmdbRating);
            Assert.Equal("/backdrop.jpg", movie.TmdbBackdropPath);
            Assert.Equal("An epic adventure", movie.Tagline);
            Assert.Equal("https://example.com/movie", movie.Homepage);
            Assert.Equal("en", movie.OriginalLanguage);
            Assert.Equal("Test Movie Original", movie.OriginalTitle);
        }

        [Fact]
        public void ReleaseYear_CanBeSetToPositiveValue()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act
            movie.ReleaseYear = 2024;

            // Assert
            Assert.Equal(2024, movie.ReleaseYear);
        }

        [Fact]
        public void RuntimeMinutes_CanBeSetToPositiveValue()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act
            movie.RuntimeMinutes = 150;

            // Assert
            Assert.Equal(150, movie.RuntimeMinutes);
        }

        [Fact]
        public void TmdbRating_CanBeSetToDecimalValue()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act
            movie.TmdbRating = 7.8;

            // Assert
            Assert.Equal(7.8, movie.TmdbRating);
        }

        [Fact]
        public void GetTmdbBackdropUrl_WithValidPath_ShouldReturnFullUrl()
        {
            // Arrange
            var movie = new Movie { Title = "", TmdbBackdropPath = "/abcdef.jpg" };

            // Act
            var url = movie.GetTmdbBackdropUrl();

            // Assert
            Assert.Equal("https://image.tmdb.org/t/p/w1280/abcdef.jpg", url);
        }

        [Fact]
        public void GetTmdbBackdropUrl_WithCustomSize_ShouldReturnFullUrlWithSize()
        {
            // Arrange
            var movie = new Movie { Title = "", TmdbBackdropPath = "/abcdef.jpg" };

            // Act
            var url = movie.GetTmdbBackdropUrl("w500");

            // Assert
            Assert.Equal("https://image.tmdb.org/t/p/w500/abcdef.jpg", url);
        }

        [Fact]
        public void GetTmdbBackdropUrl_WithNullPath_ShouldReturnNull()
        {
            // Arrange
            var movie = new Movie { Title = "", TmdbBackdropPath = null };

            // Act
            var url = movie.GetTmdbBackdropUrl();

            // Assert
            Assert.Null(url);
        }

        [Fact]
        public void GetTmdbBackdropUrl_WithEmptyPath_ShouldReturnNull()
        {
            // Arrange
            var movie = new Movie { Title = "", TmdbBackdropPath = "" };

            // Act
            var url = movie.GetTmdbBackdropUrl();

            // Assert
            Assert.Null(url);
        }

        [Fact]
        public void NavigationProperties_TopicsCanBeAddedAndRetrieved()
        {
            // Arrange
            var movie = new Movie { Title = "" };
            var topic1 = new Topic { Name = "action" };
            var topic2 = new Topic { Name = "thriller" };

            // Act
            movie.Topics.Add(topic1);
            movie.Topics.Add(topic2);

            // Assert
            Assert.Equal(2, movie.Topics.Count);
            Assert.Contains(topic1, movie.Topics);
            Assert.Contains(topic2, movie.Topics);
        }

        [Fact]
        public void NavigationProperties_GenresCanBeAddedAndRetrieved()
        {
            // Arrange
            var movie = new Movie { Title = "" };
            var genre1 = new Genre { Name = "sci-fi" };
            var genre2 = new Genre { Name = "adventure" };

            // Act
            movie.Genres.Add(genre1);
            movie.Genres.Add(genre2);

            // Assert
            Assert.Equal(2, movie.Genres.Count);
            Assert.Contains(genre1, movie.Genres);
            Assert.Contains(genre2, movie.Genres);
        }

        [Fact]
        public void NavigationProperties_MixlistsCanBeAddedAndRetrieved()
        {
            // Arrange
            var movie = new Movie { Title = "" };
            var mixlist1 = new Mixlist { Name = "Favorite Movies" };
            var mixlist2 = new Mixlist { Name = "Must Watch" };

            // Act
            movie.Mixlists.Add(mixlist1);
            movie.Mixlists.Add(mixlist2);

            // Assert
            Assert.Equal(2, movie.Mixlists.Count);
            Assert.Contains(mixlist1, movie.Mixlists);
            Assert.Contains(mixlist2, movie.Mixlists);
        }

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(movie);
            Assert.NotNull(movie.Id);
            Assert.NotNull(movie.Topics);
            Assert.NotNull(movie.Genres);
            Assert.NotNull(movie.Mixlists);
        }

        [Fact]
        public void ImdbId_CanStoreStandardFormat()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act
            movie.ImdbId = "tt1234567";

            // Assert
            Assert.Equal("tt1234567", movie.ImdbId);
        }

        [Fact]
        public void TmdbId_CanStoreNumericId()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act
            movie.TmdbId = "550"; // Fight Club's TMDB ID

            // Assert
            Assert.Equal("550", movie.TmdbId);
        }

        [Fact]
        public void MpaaRating_CanStoreVariousRatings()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act & Assert
            movie.MpaaRating = "G";
            Assert.Equal("G", movie.MpaaRating);

            movie.MpaaRating = "PG";
            Assert.Equal("PG", movie.MpaaRating);

            movie.MpaaRating = "PG-13";
            Assert.Equal("PG-13", movie.MpaaRating);

            movie.MpaaRating = "R";
            Assert.Equal("R", movie.MpaaRating);

            movie.MpaaRating = "NC-17";
            Assert.Equal("NC-17", movie.MpaaRating);
        }

        [Fact]
        public void Cast_CanStoreCommaSeparatedList()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act
            movie.Cast = "Tom Hanks, Tim Allen, Joan Cusack";

            // Assert
            Assert.Equal("Tom Hanks, Tim Allen, Joan Cusack", movie.Cast);
        }

        [Fact]
        public void Tagline_CanStoreMovieTagline()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act
            movie.Tagline = "In space, no one can hear you scream.";

            // Assert
            Assert.Equal("In space, no one can hear you scream.", movie.Tagline);
        }

        [Fact]
        public void OriginalTitle_CanStoreForeignTitle()
        {
            // Arrange
            var movie = new Movie { Title = "" };

            // Act
            movie.Title = "Spirited Away";
            movie.OriginalTitle = "åƒã¨åƒå°‹ã®ç¥žéš ã—";
            movie.OriginalLanguage = "ja";

            // Assert
            Assert.Equal("Spirited Away", movie.Title);
            Assert.Equal("åƒã¨åƒå°‹ã®ç¥žéš ã—", movie.OriginalTitle);
            Assert.Equal("ja", movie.OriginalLanguage);
        }
    }
}

