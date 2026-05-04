using FluentAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    public class BookTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var book = new Book { Title = "", Author = "" };

            // Assert
            book.Id.Should().NotBeEmpty();
            book.Title.Should().Be("");
            book.Author.Should().Be("");
            book.ISBN.Should().BeNull();
            book.ASIN.Should().BeNull();
            book.Format.Should().Be(BookFormat.Digital);
            book.PartOfSeries.Should().BeFalse();
            book.GoodreadsRating.Should().BeNull();
            book.AverageRating.Should().BeNull();
            book.YearPublished.Should().BeNull();
            book.OriginalPublicationYear.Should().BeNull();
            book.DateRead.Should().BeNull();
            book.MyReview.Should().BeNull();
            book.Publisher.Should().BeNull();
            book.GoodreadsTags.Should().NotBeNull().And.BeEmpty();
            book.ReadwiseBookId.Should().BeNull();
            book.LastReadwiseSync.Should().BeNull();
            book.Highlights.Should().NotBeNull().And.BeEmpty();
            book.Topics.Should().NotBeNull().And.BeEmpty();
            book.Genres.Should().NotBeNull().And.BeEmpty();
            book.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();
            var testDate = DateTime.UtcNow;

            // Act
            book.Title = "The Great Gatsby";
            book.Author = "F. Scott Fitzgerald";
            book.Description = "A classic novel about the American Dream";
            book.ISBN = "978-0743273565";
            book.ASIN = "B000FC0PDA";
            book.Format = BookFormat.Physical;
            book.PartOfSeries = false;
            book.GoodreadsRating = 3.93m;
            book.AverageRating = 4.2m;
            book.YearPublished = 2004;
            book.OriginalPublicationYear = 1925;
            book.DateRead = testDate;
            book.MyReview = "A masterpiece of American literature.";
            book.Publisher = "Scribner";
            book.ReadwiseBookId = 12345;
            book.LastReadwiseSync = testDate;

            // Assert
            book.Title.Should().Be("The Great Gatsby");
            book.Author.Should().Be("F. Scott Fitzgerald");
            book.Description.Should().Be("A classic novel about the American Dream");
            book.ISBN.Should().Be("978-0743273565");
            book.ASIN.Should().Be("B000FC0PDA");
            book.Format.Should().Be(BookFormat.Physical);
            book.PartOfSeries.Should().BeFalse();
            book.GoodreadsRating.Should().Be(3.93m);
            book.AverageRating.Should().Be(4.2m);
            book.YearPublished.Should().Be(2004);
            book.OriginalPublicationYear.Should().Be(1925);
            book.DateRead.Should().Be(testDate);
            book.MyReview.Should().Be("A masterpiece of American literature.");
            book.Publisher.Should().Be("Scribner");
            book.ReadwiseBookId.Should().Be(12345);
            book.LastReadwiseSync.Should().Be(testDate);
        }

        [Theory]
        [InlineData(BookFormat.Digital)]
        [InlineData(BookFormat.Physical)]
        public void Format_ShouldAcceptAllValidValues(BookFormat format)
        {
            // Arrange
            var book = TestDataFactory.CreateBook();

            // Act
            book.Format = format;

            // Assert
            book.Format.Should().Be(format);
        }

        [Fact]
        public void ISBN_CanStoreIsbn10()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();

            // Act
            book.ISBN = "0743273567";

            // Assert
            book.ISBN.Should().Be("0743273567");
        }

        [Fact]
        public void ISBN_CanStoreIsbn13()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();

            // Act
            book.ISBN = "978-0743273565";

            // Assert
            book.ISBN.Should().Be("978-0743273565");
        }

        [Fact]
        public void GoodreadsTags_CanStoreMultipleTags()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();

            // Act
            book.GoodreadsTags.Add("fiction");
            book.GoodreadsTags.Add("classic");
            book.GoodreadsTags.Add("american-literature");

            // Assert
            book.GoodreadsTags.Should().HaveCount(3);
            book.GoodreadsTags.Should().Contain(new[] { "fiction", "classic", "american-literature" });
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_HighlightsCanBeAddedAndRetrieved()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();
            var highlight1 = new Highlight { Id = Guid.NewGuid(), ReadwiseId = 1, Text = "A memorable quote" };
            var highlight2 = new Highlight { Id = Guid.NewGuid(), ReadwiseId = 2, Text = "Another great passage" };

            // Act
            book.Highlights.Add(highlight1);
            book.Highlights.Add(highlight2);

            // Assert
            book.Highlights.Should().HaveCount(2);
            book.Highlights.Should().Contain(highlight1);
            book.Highlights.Should().Contain(highlight2);
        }

        [Fact]
        public void NavigationProperties_TopicsCanBeAddedAndRetrieved()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();
            var topic = TestDataFactory.CreateTopic("literature");

            // Act
            book.Topics.Add(topic);

            // Assert
            book.Topics.Should().ContainSingle().Which.Name.Should().Be("literature");
        }

        [Fact]
        public void NavigationProperties_GenresCanBeAddedAndRetrieved()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();
            var genre = TestDataFactory.CreateGenre("fiction");

            // Act
            book.Genres.Add(genre);

            // Assert
            book.Genres.Should().ContainSingle().Which.Name.Should().Be("fiction");
        }

        [Fact]
        public void NavigationProperties_MixlistsCanBeAddedAndRetrieved()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();
            var mixlist = TestDataFactory.CreateMixlist("Must Read Books");

            // Act
            book.Mixlists.Add(mixlist);

            // Assert
            book.Mixlists.Should().ContainSingle().Which.Name.Should().Be("Must Read Books");
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange
            var book = TestDataFactory.CreateBook();

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(book);
            book.MediaType.Should().Be(MediaType.Book);
            book.Status.Should().Be(Status.Uncharted);
        }

        #endregion
    }
}
