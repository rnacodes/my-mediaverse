using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.IntegrationTests.Helpers;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// The generic CSV upload's Video branch: a row needs a title and a link, takes its id from
    /// its own column or from a YouTube link, and is reported as skipped when the video is
    /// already in the library or appeared earlier in the same file.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class UploadCsvVideoRowIntegrationTests : IAsyncLifetime
    {
        private const string Endpoint = "/api/upload/csv";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public UploadCsvVideoRowIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task UploadCsv_NewYouTubeRow_TakesItsIdFromTheLink()
        {
            var response = await _client.PostAsync(Endpoint, CsvForm(Csv(
                "Video,Never Gonna Give You Up,https://youtu.be/dQw4w9WgXcQ,,")));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("createdCount").GetInt32().Should().Be(1);

            var videos = await GetAllVideos();
            videos.Should().ContainSingle();
            videos[0].Platform.Should().Be("YouTube");
            videos[0].ExternalId.Should().Be("dQw4w9WgXcQ");
            videos[0].Link.Should().Be("https://youtu.be/dQw4w9WgXcQ");
        }

        [Fact]
        public async Task UploadCsv_RowOnAnotherPlatform_KeepsItsOwnIdAndTakesNoneFromTheLink()
        {
            var response = await _client.PostAsync(Endpoint, CsvForm(Csv(
                "Video,With an id,https://vimeo.com/76979871,Vimeo,76979871",
                "Video,Without an id,https://vimeo.com/148751763,Vimeo,")));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadJson(response)).GetProperty("createdCount").GetInt32().Should().Be(2);

            var videos = await GetAllVideos();
            videos.Single(v => v.Title == "With an id").ExternalId.Should().Be("76979871");
            videos.Single(v => v.Title == "Without an id").ExternalId.Should().BeNull();
        }

        [Fact]
        public async Task UploadCsv_VideoAlreadyInTheLibrary_IsSkipped()
        {
            await TestDataSeeder.AddAsync(_factory, new Video
            {
                Title = "Stored Video",
                Platform = "YouTube",
                ExternalId = "dQw4w9WgXcQ",
                Link = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                MediaType = MediaType.Video
            });

            var response = await _client.PostAsync(Endpoint, CsvForm(Csv(
                "Video,Same video under another title,https://www.youtube.com/shorts/dQw4w9WgXcQ,,")));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("createdCount").GetInt32().Should().Be(0);
            result.GetProperty("skippedCount").GetInt32().Should().Be(1);
            result.GetProperty("skipped").GetArrayLength().Should().Be(1);
            (await GetAllVideos()).Should().ContainSingle(v => v.Title == "Stored Video");
        }

        [Fact]
        public async Task UploadCsv_SameVideoTwiceInOneFile_CreatesOneAndSkipsTheOther()
        {
            var csv = Csv(
                "Video,First row,https://www.youtube.com/watch?v=dQw4w9WgXcQ,,",
                "Video,An unrelated video,https://www.youtube.com/watch?v=9bZkp7q19f0,,",
                "Video,Second row,https://example.com/somewhere-else,YouTube,dQw4w9WgXcQ");

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("createdCount").GetInt32().Should().Be(2);
            result.GetProperty("skippedCount").GetInt32().Should().Be(1);
            result.GetProperty("failedCount").GetInt32().Should().Be(0);

            var videos = await GetAllVideos();
            videos.Should().HaveCount(2);
            videos.Should().ContainSingle(v => v.ExternalId == "dQw4w9WgXcQ").Which.Title.Should().Be("First row");
        }

        [Fact]
        public async Task UploadCsv_SameLinkTwiceOnAnotherPlatform_CreatesOneAndSkipsTheOther()
        {
            var csv = Csv(
                "Video,First row,https://vimeo.com/76979871,Vimeo,",
                "Video,Second row,http://www.vimeo.com/76979871/,Vimeo,");

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            var result = await ReadJson(response);
            result.GetProperty("createdCount").GetInt32().Should().Be(1);
            result.GetProperty("skippedCount").GetInt32().Should().Be(1);
            (await GetAllVideos()).Should().ContainSingle(v => v.Title == "First row");
        }

        [Theory]
        [InlineData("Video,,https://www.youtube.com/watch?v=dQw4w9WgXcQ,,")]
        [InlineData("Video,A title but no link,,,")]
        public async Task UploadCsv_RowWithoutATitleOrALink_IsReportedAsAnError_AndTheRestStillImports(string badRow)
        {
            var csv = Csv(badRow, "Video,A complete row,https://www.youtube.com/watch?v=9bZkp7q19f0,,");

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadJson(response);
            result.GetProperty("createdCount").GetInt32().Should().Be(1);
            result.GetProperty("failedCount").GetInt32().Should().Be(1);
            result.GetProperty("errors")[0].GetString().Should().Contain("Title and a Link");
            (await GetAllVideos()).Should().ContainSingle(v => v.Title == "A complete row");
        }

        [Fact]
        public async Task UploadCsv_VideoRow_DoesNotStoreOwnership()
        {
            var csv = "MediaType,Title,Link,OwnershipStatus\n"
                + "Video,Owned?,https://www.youtube.com/watch?v=dQw4w9WgXcQ,Own\n";

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            (await ReadJson(response)).GetProperty("createdCount").GetInt32().Should().Be(1);
            var body = JsonSerializer.Deserialize<JsonElement>(
                await (await _client.GetAsync("/api/video")).Content.ReadAsStringAsync(), _jsonOptions);
            body[0].TryGetProperty("ownershipStatus", out _).Should().BeFalse();
        }

        #region Helpers

        private async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
            JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        private async Task<List<VideoResponseDto>> GetAllVideos()
        {
            var response = await _client.GetAsync("/api/video");
            return JsonSerializer.Deserialize<List<VideoResponseDto>>(await response.Content.ReadAsStringAsync(), _jsonOptions)
                   ?? new List<VideoResponseDto>();
        }

        private static string Csv(params string[] rows) =>
            "MediaType,Title,Link,Platform,VideoId\n" + string.Join("\n", rows) + "\n";

        private static MultipartFormDataContent CsvForm(string csv)
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(file, "file", "videos.csv");
            return form;
        }

        #endregion
    }
}
