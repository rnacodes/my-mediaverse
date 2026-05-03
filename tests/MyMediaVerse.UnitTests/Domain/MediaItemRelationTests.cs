using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    public class MediaItemRelationTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var relation = new MediaItemRelation();

            // Assert
            relation.SourceMediaItemId.Should().Be(Guid.Empty);
            relation.RelatedMediaItemId.Should().Be(Guid.Empty);
            relation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            relation.Source.Should().Be(RelationSource.ManuallyAdded);
            relation.SimilarityScore.Should().BeNull();
            relation.Note.Should().BeNull();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var sourceId = Guid.NewGuid();
            var relatedId = Guid.NewGuid();
            var testDate = DateTime.UtcNow;

            // Act
            var relation = new MediaItemRelation
            {
                SourceMediaItemId = sourceId,
                RelatedMediaItemId = relatedId,
                CreatedAt = testDate,
                Source = RelationSource.AiRecommended,
                SimilarityScore = 0.95,
                Note = "Both explore themes of consciousness"
            };

            // Assert
            relation.SourceMediaItemId.Should().Be(sourceId);
            relation.RelatedMediaItemId.Should().Be(relatedId);
            relation.CreatedAt.Should().Be(testDate);
            relation.Source.Should().Be(RelationSource.AiRecommended);
            relation.SimilarityScore.Should().Be(0.95);
            relation.Note.Should().Be("Both explore themes of consciousness");
        }

        [Theory]
        [InlineData(RelationSource.AiRecommended)]
        [InlineData(RelationSource.ManuallyAdded)]
        public void Source_ShouldAcceptAllValidValues(RelationSource source)
        {
            // Arrange
            var relation = new MediaItemRelation();

            // Act
            relation.Source = source;

            // Assert
            relation.Source.Should().Be(source);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_CanLinkTwoMediaItems()
        {
            // Arrange
            var movie = TestDataFactory.CreateMovie("Inception");
            var book = TestDataFactory.CreateBook("The Dream Machine");

            // Act
            var relation = new MediaItemRelation
            {
                SourceMediaItemId = movie.Id,
                SourceMediaItem = movie,
                RelatedMediaItemId = book.Id,
                RelatedMediaItem = book,
                Source = RelationSource.ManuallyAdded,
                Note = "Both about dreams"
            };

            // Assert
            relation.SourceMediaItem.Title.Should().Be("Inception");
            relation.RelatedMediaItem.Title.Should().Be("The Dream Machine");
        }

        [Fact]
        public void Relation_IsDirectional_SourceAndRelatedAreDifferentRoles()
        {
            // Arrange
            var movieA = TestDataFactory.CreateMovie("Movie A");
            var movieB = TestDataFactory.CreateMovie("Movie B");

            // Act
            var relation = new MediaItemRelation
            {
                SourceMediaItemId = movieA.Id,
                SourceMediaItem = movieA,
                RelatedMediaItemId = movieB.Id,
                RelatedMediaItem = movieB
            };

            // Assert - source and related are different roles
            relation.SourceMediaItemId.Should().NotBe(relation.RelatedMediaItemId);
            relation.SourceMediaItem.Should().NotBeSameAs(relation.RelatedMediaItem);
        }

        #endregion

        #region AI Recommendation Tests

        [Fact]
        public void AiRecommendation_ShouldIncludeSimilarityScore()
        {
            // Arrange & Act
            var relation = new MediaItemRelation
            {
                SourceMediaItemId = Guid.NewGuid(),
                RelatedMediaItemId = Guid.NewGuid(),
                Source = RelationSource.AiRecommended,
                SimilarityScore = 0.87
            };

            // Assert
            relation.Source.Should().Be(RelationSource.AiRecommended);
            relation.SimilarityScore.Should().Be(0.87);
        }

        [Fact]
        public void ManualRelation_ShouldHaveNullSimilarityScore()
        {
            // Arrange & Act
            var relation = new MediaItemRelation
            {
                SourceMediaItemId = Guid.NewGuid(),
                RelatedMediaItemId = Guid.NewGuid(),
                Source = RelationSource.ManuallyAdded
            };

            // Assert
            relation.Source.Should().Be(RelationSource.ManuallyAdded);
            relation.SimilarityScore.Should().BeNull();
        }

        #endregion
    }
}
