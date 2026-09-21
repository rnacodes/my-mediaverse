using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The from-tmdb import doors, end to end against a stubbed TMDB service: genres and credits
    /// in the details payload must reach the stored item, with genres in library form.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class TmdbImportGenresIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public TmdbImportGenresIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task ImportMovieFromTmdb_ShouldStoreLowercaseGenresAndCredits()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetMovieDetailsAsync(27205, Arg.Any<string>()).Returns(new TmdbMovieDto
                {
                    Id = 27205,
                    Title = "Inception",
                    ReleaseDate = "2010-07-15",
                    Genres = new List<TmdbGenreDto>
                    {
                        new() { Id = 28, Name = "Action" },
                        new() { Id = 878, Name = "Science Fiction" }
                    },
                    Credits = new TmdbCreditsDto
                    {
                        Cast = { new TmdbCastMemberDto { Name = "Leonardo DiCaprio", Order = 0 } },
                        Crew = { new TmdbCrewMemberDto { Name = "Christopher Nolan", Job = "Director" } }
                    },
                    ReleaseDates = new TmdbReleaseDatesDto
                    {
                        Results = { new TmdbCountryReleaseDatesDto { Iso31661 = "US", ReleaseDates = { new TmdbReleaseDateDto { Certification = "PG-13" } } } }
                    }
                }));

            var response = await client.PostAsync("/api/movie/from-tmdb/27205", null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var movie = await response.Content.ReadFromJsonAsync<MovieResponseDto>(_jsonOptions);
            movie!.Genres.Should().BeEquivalentTo(new[] { "action", "science fiction" });
            movie.Director.Should().Be("Christopher Nolan");
            movie.Cast.Should().Be("Leonardo DiCaprio");
            movie.MpaaRating.Should().Be("PG-13");
        }

        [Fact]
        public async Task ImportTvShowFromTmdb_ShouldSplitCompoundGenres_AndStoreCreator()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetTvShowDetailsAsync(1399, Arg.Any<string>()).Returns(new TmdbTvShowDto
                {
                    Id = 1399,
                    Name = "Game of Thrones",
                    FirstAirDate = "2011-04-17",
                    Genres = new List<TmdbGenreDto>
                    {
                        new() { Id = 10765, Name = "Sci-Fi & Fantasy" },
                        new() { Id = 10759, Name = "Action & Adventure" }
                    },
                    CreatedBy = new List<TmdbCreatedByDto> { new() { Name = "David Benioff" } },
                    ContentRatings = new TmdbContentRatingsDto
                    {
                        Results = { new TmdbCountryContentRatingDto { Iso31661 = "US", Rating = "TV-MA" } }
                    }
                }));

            var response = await client.PostAsync("/api/tvshow/from-tmdb/1399", null);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var show = await response.Content.ReadFromJsonAsync<TvShowResponseDto>(_jsonOptions);
            show!.Genres.Should().BeEquivalentTo(new[] { "science fiction", "fantasy", "action", "adventure" });
            show.Creator.Should().Be("David Benioff");
            show.ContentRating.Should().Be("TV-MA");
        }

        [Fact]
        public async Task ImportMovieFromTmdb_Twice_ShouldReturnCreatedThenOk_WithOneTmdbCall()
        {
            var (client, tmdb) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetMovieDetailsAsync(603, Arg.Any<string>()).Returns(new TmdbMovieDto
                {
                    Id = 603,
                    Title = "The Matrix",
                    ReleaseDate = "1999-03-31"
                }));

            var first = await client.PostAsync("/api/movie/from-tmdb/603", null);
            var second = await client.PostAsync("/api/movie/from-tmdb/603", null);

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var created = await first.Content.ReadFromJsonAsync<MovieResponseDto>(_jsonOptions);
            var existing = await second.Content.ReadFromJsonAsync<MovieResponseDto>(_jsonOptions);
            existing!.Id.Should().Be(created!.Id);

            // The stored id answers the second request without going back to TMDB.
            await tmdb.Received(1).GetMovieDetailsAsync(603, Arg.Any<string>());
        }

        [Fact]
        public async Task ImportTvShowFromTmdb_Twice_ShouldReturnCreatedThenOk_WithOneTmdbCall()
        {
            var (client, tmdb) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetTvShowDetailsAsync(95396, Arg.Any<string>()).Returns(new TmdbTvShowDto
                {
                    Id = 95396,
                    Name = "Severance",
                    FirstAirDate = "2022-02-17"
                }));

            var first = await client.PostAsync("/api/tvshow/from-tmdb/95396", null);
            var second = await client.PostAsync("/api/tvshow/from-tmdb/95396", null);

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var created = await first.Content.ReadFromJsonAsync<TvShowResponseDto>(_jsonOptions);
            var existing = await second.Content.ReadFromJsonAsync<TvShowResponseDto>(_jsonOptions);
            existing!.Id.Should().Be(created!.Id);
            await tmdb.Received(1).GetTvShowDetailsAsync(95396, Arg.Any<string>());
        }

        [Fact]
        public async Task CreateMovie_WithAStoredTmdbId_ShouldReturnOkWithTheStoredMovie()
        {
            var client = _factory.CreateClient();
            var dto = new CreateMovieDto { Title = "Inception", ReleaseYear = 2010, TmdbId = "27205", MediaType = Domain.Entities.MediaType.Movie };

            var first = await client.PostAsJsonAsync("/api/movie", dto);
            dto.Title = "Inception (duplicate attempt)";
            dto.ReleaseYear = 2011;
            var second = await client.PostAsJsonAsync("/api/movie", dto);

            first.StatusCode.Should().Be(HttpStatusCode.Created);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var stored = await second.Content.ReadFromJsonAsync<MovieResponseDto>(_jsonOptions);
            stored!.Title.Should().Be("Inception");
        }

        [Theory]
        [InlineData("/api/tmdb/import/movie/27205")]
        [InlineData("/api/tmdb/import/tv/1399")]
        public async Task RetiredImportRoutes_ShouldNoLongerExist(string route)
        {
            var response = await _factory.CreateClient().PostAsync(route, null);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        }
    }
}
