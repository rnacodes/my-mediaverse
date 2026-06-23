using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.UnitTests.Application
{
    public partial class PodcastServiceTests
    {
        #region UpdatePodcastSeries Tests

        [Fact]
        public async Task UpdatePodcastSeriesAsync_ShouldUpdateEditableFields()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            Context.PodcastSeries.Add(new PodcastSeries
            {
                Id = seriesId,
                Title = "Original Title",
                Publisher = "Original Publisher",
                Status = Status.Uncharted,
                Description = "Original description",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear(); // detach seeded graph to mimic a fresh request context

            var dto = new CreatePodcastSeriesDto
            {
                Title = "Updated Title",
                Publisher = "Updated Publisher",
                Status = Status.Completed,
                Description = "Updated description",
                Rating = Rating.Like,
                Link = "https://example.com/podcast"
            };

            // Act
            var result = await _service.UpdatePodcastSeriesAsync(seriesId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");
            result.Publisher.Should().Be("Updated Publisher");
            result.Status.Should().Be(Status.Completed);
            result.Description.Should().Be("Updated description");
            result.Rating.Should().Be(Rating.Like);
            result.Link.Should().Be("https://example.com/podcast");

            var saved = await Context.PodcastSeries.FindAsync(seriesId);
            saved!.Title.Should().Be("Updated Title");
        }

        // Topic/genre assignment and replacement on update write to the TPH many-to-many
        // join tables, which the InMemory provider can't model for a tracked existing
        // entity (see InMemoryDbTestBase's provider-limits note). That behaviour —
        // including lowercase normalization and replacement — is covered against real
        // Postgres in PodcastControllerIntegrationTests.

        [Fact]
        public async Task UpdatePodcastSeriesAsync_ShouldPreserveSyncAndSubscriptionFields()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var lastSync = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Context.PodcastSeries.Add(new PodcastSeries
            {
                Id = seriesId,
                Title = "Subscribed Podcast",
                Status = Status.Uncharted,
                ExternalId = "listennotes-123",
                IsSubscribed = true,
                LastSyncDate = lastSync,
                TotalEpisodes = 42,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear(); // detach seeded graph to mimic a fresh request context

            // DTO leaves sync fields at their defaults (the edit form does not manage them)
            var dto = new CreatePodcastSeriesDto
            {
                Title = "Renamed Podcast",
                Status = Status.Uncharted
            };

            // Act
            var result = await _service.UpdatePodcastSeriesAsync(seriesId, dto);

            // Assert
            result.Title.Should().Be("Renamed Podcast");
            result.ExternalId.Should().Be("listennotes-123");
            result.IsSubscribed.Should().BeTrue();
            result.LastSyncDate.Should().Be(lastSync);
            result.TotalEpisodes.Should().Be(42);
        }

        [Fact]
        public async Task UpdatePodcastSeriesAsync_ShouldThrow_WhenSeriesDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = new CreatePodcastSeriesDto { Title = "Whatever", Status = Status.Uncharted };

            // Act & Assert
            await _service.Invoking(s => s.UpdatePodcastSeriesAsync(nonExistentId, dto))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*{nonExistentId}*not found*");
        }

        #endregion

        #region UpdatePodcastEpisode Tests

        [Fact]
        public async Task UpdatePodcastEpisodeAsync_ShouldUpdateEditableFields()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            Context.PodcastSeries.Add(new PodcastSeries
            {
                Id = seriesId,
                Title = "Parent Series",
                Status = Status.Uncharted,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });

            var episodeId = Guid.NewGuid();
            Context.PodcastEpisodes.Add(new PodcastEpisode
            {
                Id = episodeId,
                Title = "Original Episode",
                SeriesId = seriesId,
                Status = Status.Uncharted,
                DurationInSeconds = 100,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear(); // detach seeded graph to mimic a fresh request context

            var releaseDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Updated Episode",
                SeriesId = seriesId,
                Status = Status.Completed,
                AudioLink = "https://example.com/ep.mp3",
                ReleaseDate = releaseDate,
                DurationInSeconds = 3600,
                EpisodeNumber = 5,
                SeasonNumber = 2,
                Publisher = "Updated Publisher"
            };

            // Act
            var result = await _service.UpdatePodcastEpisodeAsync(episodeId, dto);

            // Assert
            result.Title.Should().Be("Updated Episode");
            result.Status.Should().Be(Status.Completed);
            result.AudioLink.Should().Be("https://example.com/ep.mp3");
            result.ReleaseDate.Should().Be(releaseDate);
            result.DurationInSeconds.Should().Be(3600);
            result.EpisodeNumber.Should().Be(5);
            result.SeasonNumber.Should().Be(2);
            result.Publisher.Should().Be("Updated Publisher");
        }

        [Fact]
        public async Task UpdatePodcastEpisodeAsync_ShouldKeepExistingSeriesId_EvenIfDtoDiffers()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            var otherSeriesId = Guid.NewGuid();
            Context.PodcastSeries.AddRange(
                new PodcastSeries { Id = seriesId, Title = "Series A", Status = Status.Uncharted, Topics = new List<Topic>(), Genres = new List<Genre>() },
                new PodcastSeries { Id = otherSeriesId, Title = "Series B", Status = Status.Uncharted, Topics = new List<Topic>(), Genres = new List<Genre>() });

            var episodeId = Guid.NewGuid();
            Context.PodcastEpisodes.Add(new PodcastEpisode
            {
                Id = episodeId,
                Title = "Episode",
                SeriesId = seriesId,
                Status = Status.Uncharted,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear(); // detach seeded graph to mimic a fresh request context

            // DTO points at a different series; update must ignore it
            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode Renamed",
                SeriesId = otherSeriesId,
                Status = Status.Uncharted
            };

            // Act
            var result = await _service.UpdatePodcastEpisodeAsync(episodeId, dto);

            // Assert
            result.SeriesId.Should().Be(seriesId);
        }

        [Fact]
        public async Task UpdatePodcastEpisodeAsync_ShouldReplaceTopicsAndGenres_WithoutInheritingFromSeries()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            Context.PodcastSeries.Add(new PodcastSeries
            {
                Id = seriesId,
                Title = "Parent Series",
                Status = Status.Uncharted,
                Topics = new List<Topic> { new Topic { Name = "series-topic" } },
                Genres = new List<Genre> { new Genre { Name = "series-genre" } }
            });

            var episodeId = Guid.NewGuid();
            Context.PodcastEpisodes.Add(new PodcastEpisode
            {
                Id = episodeId,
                Title = "Episode",
                SeriesId = seriesId,
                Status = Status.Uncharted,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear(); // detach seeded graph to mimic a fresh request context

            // DTO provides no topics/genres — unlike create, update must NOT inherit from the series
            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode",
                SeriesId = seriesId,
                Status = Status.Uncharted
            };

            // Act
            var result = await _service.UpdatePodcastEpisodeAsync(episodeId, dto);

            // Assert
            result.Topics.Should().BeEmpty();
            result.Genres.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdatePodcastEpisodeAsync_ShouldPreserveExternalId()
        {
            // Arrange
            var seriesId = Guid.NewGuid();
            Context.PodcastSeries.Add(new PodcastSeries
            {
                Id = seriesId,
                Title = "Parent Series",
                Status = Status.Uncharted,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });

            var episodeId = Guid.NewGuid();
            Context.PodcastEpisodes.Add(new PodcastEpisode
            {
                Id = episodeId,
                Title = "Episode",
                SeriesId = seriesId,
                Status = Status.Uncharted,
                ExternalId = "listennotes-ep-999",
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            });
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear(); // detach seeded graph to mimic a fresh request context

            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Episode Renamed",
                SeriesId = seriesId,
                Status = Status.Uncharted
            };

            // Act
            var result = await _service.UpdatePodcastEpisodeAsync(episodeId, dto);

            // Assert
            result.ExternalId.Should().Be("listennotes-ep-999");
        }

        [Fact]
        public async Task UpdatePodcastEpisodeAsync_ShouldThrow_WhenEpisodeDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = new CreatePodcastEpisodeDto
            {
                Title = "Whatever",
                SeriesId = Guid.NewGuid(),
                Status = Status.Uncharted
            };

            // Act & Assert
            await _service.Invoking(s => s.UpdatePodcastEpisodeAsync(nonExistentId, dto))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*{nonExistentId}*not found*");
        }

        #endregion
    }
}
