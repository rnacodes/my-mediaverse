using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Models;
using MyMediaVerse.Infrastructure.Services.Search;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using Typesense;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Pins the values the search index receives for TV. A show and its episodes share the TVShow
    /// media type, so the TV-only fields are what let search tell them apart, list one show's
    /// episodes in order, and find an episode by its show's name.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseTvDocumentTests : InMemoryDbTestBase
    {
        private readonly ITypesenseClient _client = Substitute.For<ITypesenseClient>();
        private readonly TypesenseService _service;
        private MediaItemDocument? _indexed;

        public TypesenseTvDocumentTests()
        {
            var config = new ConfigurationBuilder().Build();
            _service = new TypesenseService(_client, Context, NullLogger<TypesenseService>.Instance, config);

            _client.UpsertDocument(Arg.Any<string>(), Arg.Do<MediaItemDocument>(d => _indexed = d))
                .Returns(call => call.Arg<MediaItemDocument>());
        }

        private async Task<TvShow> SeedShow()
        {
            var show = new TvShow
            {
                Id = Guid.NewGuid(),
                Title = "Game of Thrones",
                Creator = "David Benioff, D. B. Weiss",
                FirstAirYear = 2011,
                MediaType = MediaType.TVShow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.TvShows.Add(show);
            await Context.SaveChangesAsync();
            return show;
        }

        [Fact]
        public async Task Show_IsIndexedAsShow_WithCreatorAndYear_AndNoEpisodeFields()
        {
            var show = await SeedShow();

            var found = await _service.ReindexMediaItemByIdAsync(show.Id);

            found.Should().BeTrue();
            _indexed.Should().NotBeNull();
            _indexed!.MediaType.Should().Be("TVShow");
            _indexed.TvType.Should().Be(TvDocumentTypes.Show);
            _indexed.Creator.Should().Be("David Benioff, D. B. Weiss");
            _indexed.ReleaseYear.Should().Be(2011);
            _indexed.ShowId.Should().BeNull();
            _indexed.ShowTitle.Should().BeNull();
            _indexed.SeasonNumber.Should().BeNull();
            _indexed.EpisodeNumber.Should().BeNull();
        }

        [Fact]
        public async Task Episode_IsIndexedAsEpisode_WithItsParentShowAndNumbers_NotAsAShow()
        {
            var show = await SeedShow();
            var episode = new TvShowEpisode
            {
                Id = Guid.NewGuid(),
                Title = "Winter Is Coming",
                MediaType = MediaType.TVShow,
                ShowId = show.Id,
                SeasonNumber = 1,
                EpisodeNumber = 1,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.TvShowEpisodes.Add(episode);
            await Context.SaveChangesAsync();

            var found = await _service.ReindexMediaItemByIdAsync(episode.Id);

            found.Should().BeTrue();
            _indexed.Should().NotBeNull();
            _indexed!.Id.Should().Be(episode.Id.ToString());
            _indexed.MediaType.Should().Be("TVShow");
            _indexed.TvType.Should().Be(TvDocumentTypes.Episode);
            _indexed.ShowId.Should().Be(show.Id.ToString());
            _indexed.ShowTitle.Should().Be("Game of Thrones");
            _indexed.SeasonNumber.Should().Be(1);
            _indexed.EpisodeNumber.Should().Be(1);
            _indexed.Creator.Should().BeNull("the show's creator belongs to the show document");
            _indexed.ReleaseYear.Should().BeNull();
        }

        [Fact]
        public async Task Episode_WithoutNumbers_StillCarriesItsTypeAndShow()
        {
            var show = await SeedShow();
            var episode = new TvShowEpisode
            {
                Id = Guid.NewGuid(),
                Title = "Untitled special",
                MediaType = MediaType.TVShow,
                ShowId = show.Id,
                Topics = new List<Topic>(),
                Genres = new List<Genre>()
            };
            Context.TvShowEpisodes.Add(episode);
            await Context.SaveChangesAsync();

            await _service.ReindexMediaItemByIdAsync(episode.Id);

            _indexed!.TvType.Should().Be(TvDocumentTypes.Episode);
            _indexed.ShowId.Should().Be(show.Id.ToString());
            _indexed.SeasonNumber.Should().BeNull();
            _indexed.EpisodeNumber.Should().BeNull();
        }
    }
}
