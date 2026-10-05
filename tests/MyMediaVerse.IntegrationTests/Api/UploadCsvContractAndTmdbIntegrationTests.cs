using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The generic CSV upload's result shape, its type-per-file mode, and the Movie / TV show
    /// branches: a row with a TMDB id is filled from TMDB, a row without one is stored as typed,
    /// and either is skipped when the library already holds it.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class UploadCsvContractAndTmdbIntegrationTests : IAsyncLifetime
    {
        private const string Endpoint = "/api/upload/csv";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public UploadCsvContractAndTmdbIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task UploadCsv_ReturnsTheReportingContractShape()
        {
            var response = await _client.PostAsync(Endpoint, CsvForm(
                "Title,Author\nDune,Frank Herbert\n", mediaType: "Book"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            result.GetProperty("operation").GetString().Should().Be("csv-upload");
            result.GetProperty("createdCount").GetInt32().Should().Be(1);
            result.GetProperty("skippedCount").GetInt32().Should().Be(0);
            result.GetProperty("failedCount").GetInt32().Should().Be(0);
            result.GetProperty("totalProcessed").GetInt32().Should().Be(1);
            result.GetProperty("importedItems")[0].GetProperty("mediaType").GetString().Should().Be("Book");
            result.TryGetProperty("startedAt", out _).Should().BeTrue();
            result.TryGetProperty("completedAt", out _).Should().BeTrue();
        }

        [Fact]
        public async Task UploadCsv_NoMediaTypeColumnAndNoMediaTypeSent_AnswersAnErrorObject()
        {
            var response = await _client.PostAsync(Endpoint, CsvForm("Title\nDune\n"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(response)).GetProperty("error").GetString().Should().Contain("MediaType");
        }

        [Fact]
        public async Task UploadCsv_UnknownColumnsAndBlankOptionalCells_ImportWithoutErrors()
        {
            var csv = "Title,Author,Shelf Color,ISBN,YearPublished\n"
                + "Dune,Frank Herbert,orange,,\n";

            var result = await ReadJson(await _client.PostAsync(Endpoint, CsvForm(csv, mediaType: "Book")));

            result.GetProperty("createdCount").GetInt32().Should().Be(1);
            result.GetProperty("failedCount").GetInt32().Should().Be(0);
        }

        [Fact]
        public async Task UploadCsv_BookRow_StoresPublisherYearDateReadAndReview()
        {
            var csv = "Title,Author,Publisher,YearPublished,DateRead,MyReview\n"
                + "Dune,Frank Herbert,Chilton Books,1965,2024-03-09,Worth the hype\n";

            await _client.PostAsync(Endpoint, CsvForm(csv, mediaType: "Book"));

            var books = JsonSerializer.Deserialize<List<BookResponseDto>>(
                await (await _client.GetAsync("/api/book")).Content.ReadAsStringAsync(), _jsonOptions)!;
            var book = books.Should().ContainSingle().Subject;
            book.Publisher.Should().Be("Chilton Books");
            book.YearPublished.Should().Be(1965);
            book.DateRead!.Value.Date.Should().Be(new DateTime(2024, 3, 9));
            book.MyReview.Should().Be("Worth the hype");
        }

        [Fact]
        public async Task UploadCsv_VideoRow_LinksAStoredChannel_AndWarnsAboutAnUnknownOne()
        {
            var channel = new YouTubeChannel
            {
                Title = "Tale Foundry",
                ChannelExternalId = "UC-known",
                MediaType = MediaType.Channel
            };
            await TestDataSeeder.AddAsync(_factory, channel);

            var csv = "Title,Link,ChannelId\n"
                + "Known channel,https://www.youtube.com/watch?v=dQw4w9WgXcQ,UC-known\n"
                + "Unknown channel,https://www.youtube.com/watch?v=9bZkp7q19f0,UC-missing\n";

            var result = await ReadJson(await _client.PostAsync(Endpoint, CsvForm(csv, mediaType: "Video")));

            result.GetProperty("createdCount").GetInt32().Should().Be(2);
            result.GetProperty("warningMessage").GetString().Should().Contain("1 video row");

            var videos = JsonSerializer.Deserialize<List<VideoResponseDto>>(
                await (await _client.GetAsync("/api/video")).Content.ReadAsStringAsync(), _jsonOptions)!;
            videos.Single(v => v.Title == "Known channel").ChannelId.Should().Be(channel.Id);
            videos.Single(v => v.Title == "Unknown channel").ChannelId.Should().BeNull();
        }

        [Fact]
        public async Task UploadCsv_MovieRowWithATmdbId_IsFilledFromTmdb_AndSkippedOnASecondUpload()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetMovieDetailsAsync(27205, Arg.Any<string>()).Returns(new TmdbMovieDto
                {
                    Id = 27205,
                    Title = "Inception",
                    ReleaseDate = "2010-07-15",
                    Genres = new List<TmdbGenreDto> { new() { Id = 878, Name = "Science Fiction" } }
                }));
            var csv = "Title,TmdbId,Status,Rating,Notes,Genres\n"
                + "whatever I typed,27205,Completed,Like,Saw it twice,heist\n";

            var first = await ReadJson(await client.PostAsync(Endpoint, CsvForm(csv, mediaType: "Movie")));
            var second = await ReadJson(await client.PostAsync(Endpoint, CsvForm(csv, mediaType: "Movie")));

            first.GetProperty("createdCount").GetInt32().Should().Be(1, ErrorsOf(first));
            second.GetProperty("createdCount").GetInt32().Should().Be(0);
            second.GetProperty("skippedCount").GetInt32().Should().Be(1);

            var movie = (await GetAll<MovieResponseDto>("/api/movie")).Should().ContainSingle().Subject;
            movie.Title.Should().Be("Inception");
            movie.TmdbId.Should().Be("27205");
            movie.ReleaseYear.Should().Be(2010);
            movie.Status.Should().Be(Status.Completed);
            movie.Rating.Should().Be(Rating.Like);
            movie.Notes.Should().Be("Saw it twice");
            movie.Genres.Should().BeEquivalentTo(new[] { "science fiction", "heist" });
        }

        [Fact]
        public async Task UploadCsv_MovieRowWhoseTmdbLookupFails_FailsThatRow_AndTheRestStillImports()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetMovieDetailsAsync(1, Arg.Any<string>()).ThrowsAsync(new HttpRequestException("404 from TMDB")));
            var csv = "Title,TmdbId,ReleaseYear\nBroken,1,\nTyped Movie,,1999\n";

            var result = await ReadJson(await client.PostAsync(Endpoint, CsvForm(csv, mediaType: "Movie")));

            result.GetProperty("createdCount").GetInt32().Should().Be(1);
            result.GetProperty("failedCount").GetInt32().Should().Be(1);
            result.GetProperty("errors")[0].GetString().Should().Contain("TMDB lookup for id 1 failed");
            (await GetAll<MovieResponseDto>("/api/movie")).Should().ContainSingle(m => m.Title == "Typed Movie");
        }

        [Fact]
        public async Task UploadCsv_MovieRowWithoutATmdbId_IsStoredAsTyped_AndSkippedOnASecondUpload()
        {
            var csv = "Title,ReleaseYear,Director\nThe Matrix,1999,The Wachowskis\n";

            var first = await ReadJson(await _client.PostAsync(Endpoint, CsvForm(csv, mediaType: "Movie")));
            var second = await ReadJson(await _client.PostAsync(Endpoint, CsvForm(csv, mediaType: "Movie")));

            first.GetProperty("createdCount").GetInt32().Should().Be(1, ErrorsOf(first));
            second.GetProperty("skippedCount").GetInt32().Should().Be(1);
            var movie = (await GetAll<MovieResponseDto>("/api/movie")).Should().ContainSingle().Subject;
            movie.Director.Should().Be("The Wachowskis");
            movie.ReleaseYear.Should().Be(1999);
        }

        [Fact]
        public async Task UploadCsv_TvShowRowWithATmdbId_IsFilledFromTmdb_AndSkippedOnASecondUpload()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<ITmdbService>(mock =>
                mock.GetTvShowDetailsAsync(1399, Arg.Any<string>()).Returns(new TmdbTvShowDto
                {
                    Id = 1399,
                    Name = "Game of Thrones",
                    FirstAirDate = "2011-04-17"
                }));
            var csv = "Title,TmdbId\n,1399\n";

            var first = await ReadJson(await client.PostAsync(Endpoint, CsvForm(csv, mediaType: "TVShow")));
            var second = await ReadJson(await client.PostAsync(Endpoint, CsvForm(csv, mediaType: "TVShow")));

            first.GetProperty("createdCount").GetInt32().Should().Be(1, ErrorsOf(first));
            second.GetProperty("skippedCount").GetInt32().Should().Be(1);
            var show = (await GetAll<TvShowResponseDto>("/api/tvshow")).Should().ContainSingle().Subject;
            show.Title.Should().Be("Game of Thrones");
            show.FirstAirYear.Should().Be(2011);
        }

        [Fact]
        public async Task UploadCsv_TvShowRowWithoutATmdbId_IsStoredAsTyped_AndSkippedOnASecondUpload()
        {
            var csv = "Title,FirstAirYear,Creator\nSeverance,2022,Dan Erickson\n";

            var first = await ReadJson(await _client.PostAsync(Endpoint, CsvForm(csv, mediaType: "TVShow")));
            var second = await ReadJson(await _client.PostAsync(Endpoint, CsvForm(csv, mediaType: "TVShow")));

            first.GetProperty("createdCount").GetInt32().Should().Be(1, ErrorsOf(first));
            second.GetProperty("skippedCount").GetInt32().Should().Be(1);
            (await GetAll<TvShowResponseDto>("/api/tvshow")).Should().ContainSingle()
                .Which.Creator.Should().Be("Dan Erickson");
        }

        #region Helpers

        private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        private async Task<List<T>> GetAll<T>(string url) =>
            JsonSerializer.Deserialize<List<T>>(await (await _client.GetAsync(url)).Content.ReadAsStringAsync(), _jsonOptions)
            ?? new List<T>();

        private static string ErrorsOf(JsonElement result) =>
            result.TryGetProperty("errors", out var errors) ? string.Join(" | ", errors.EnumerateArray().Select(e => e.GetString())) : "";

        private static MultipartFormDataContent CsvForm(string csv, string? mediaType = null)
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(file, "file", "upload.csv");
            if (mediaType != null) form.Add(new StringContent(mediaType), "mediaType");
            return form;
        }

        #endregion
    }
}
