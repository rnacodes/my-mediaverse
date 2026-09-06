using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using MyMediaVerse.Shared.Interfaces;
using NSubstitute;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// End-to-end coverage of <c>POST /api/upload/goodreads-csv</c>: the reporting-contract result
    /// shape, shelves → Topics, the Audiobook binding, dedup on re-upload, the rating conversion that
    /// runs after a non-chunked upload, and the best-effort reindex gate. Typesense is always a
    /// substitute in this host; the CSV is built in memory.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class GoodreadsCsvUploadIntegrationTests : IAsyncLifetime
    {
        private const string Endpoint = "/api/upload/goodreads-csv";

        private readonly ApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public GoodreadsCsvUploadIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Upload_TwoRows_CreatesBooksWithTopicsFormatAndContractShape()
        {
            var csv = Csv(
                Row(bookId: "9990001", title: "Smoke Audiobook", author: "Smoke Author", myRating: 4,
                    binding: "Audible Audio", bookshelves: "read fantasy classics", exclusiveShelf: "read"),
                Row(bookId: "9990002", title: "Smoke Paperback", author: "Smoke Author", myRating: 0,
                    binding: "Paperback", bookshelves: "to-read sci-fi", exclusiveShelf: "to-read"));

            var response = await _client.PostAsync(Endpoint, CsvForm(csv));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await ReadResult(response);
            result.Success.Should().BeTrue();
            result.Operation.Should().Be("goodreads-import");
            result.TotalProcessed.Should().Be(2);
            result.CreatedCount.Should().Be(2);
            result.UpdatedCount.Should().Be(0);
            result.ErrorCount.Should().Be(0);
            result.WarningMessage.Should().BeNull();
            result.ReindexTriggered.Should().BeTrue();
            result.StartedAt.Should().NotBe(default);
            result.CompletedAt.Should().NotBeNull();
            result.ImportedBooks.Should().HaveCount(2);

            var audiobook = await GetBook(result.ImportedBooks.Single(b => b.Title == "Smoke Audiobook").Id);
            audiobook.Format.Should().Be(BookFormat.Audiobook);
            audiobook.Status.Should().Be(Status.Completed);
            audiobook.GoodreadsBookId.Should().Be(9990001);
            audiobook.GoodreadsRating.Should().Be(4);
            audiobook.Topics.Should().BeEquivalentTo(new[] { "fantasy", "classics" }, "the status shelf is not a topic");
            audiobook.GoodreadsTags.Should().BeEquivalentTo(new[] { "read", "fantasy", "classics" }, "the raw shelf list is kept");

            var paperback = await GetBook(result.ImportedBooks.Single(b => b.Title == "Smoke Paperback").Id);
            paperback.Format.Should().Be(BookFormat.Physical);
            paperback.Status.Should().Be(Status.Uncharted);
            paperback.GoodreadsRating.Should().BeNull("a My Rating of 0 means unrated");
            paperback.Topics.Should().BeEquivalentTo(new[] { "sci-fi" });
        }

        [Fact]
        public async Task Upload_NonChunked_ConvertsTheStoredGoodreadsRating()
        {
            // A non-chunked upload that imported something runs the rating conversion afterward, so the
            // derived MMV Rating is already present when the page reloads.
            var csv = Csv(Row(bookId: "9990003", title: "Rated Book", author: "Smoke Author", myRating: 4));

            var result = await ReadResult(await _client.PostAsync(Endpoint, CsvForm(csv)));

            var book = await GetBook(result.ImportedBooks.Single().Id);
            book.GoodreadsRating.Should().Be(4);
            book.Rating.Should().Be(Rating.Like);
        }

        [Fact]
        public async Task Upload_SameFileTwice_UpdatesInsteadOfDuplicating()
        {
            var csv = Csv(Row(bookId: "9990004", title: "Dune", author: "Frank Herbert", bookshelves: "fantasy"));

            var first = await ReadResult(await _client.PostAsync(Endpoint, CsvForm(csv)));
            var second = await ReadResult(await _client.PostAsync(Endpoint, CsvForm(csv)));

            first.CreatedCount.Should().Be(1);
            second.CreatedCount.Should().Be(0);
            second.UpdatedCount.Should().Be(1);
            (await GetAllBooks()).Should().ContainSingle(b => b.Title == "Dune");
        }

        [Fact]
        public async Task Upload_UpdateExistingFalse_SkipsMatches()
        {
            var csv = Csv(Row(bookId: "9990005", title: "Dune", author: "Frank Herbert"));
            await _client.PostAsync(Endpoint, CsvForm(csv));

            var second = await ReadResult(await _client.PostAsync($"{Endpoint}?updateExisting=false", CsvForm(csv)));

            second.SkippedCount.Should().Be(1);
            second.UpdatedCount.Should().Be(0);
            second.CreatedCount.Should().Be(0);
            second.ReindexTriggered.Should().BeFalse("nothing was imported");
        }

        [Fact]
        public async Task Upload_Reimport_AddsNewShelfTopic_KeepsExistingTopics()
        {
            var firstCsv = Csv(Row(bookId: "9990006", title: "Hyperion", author: "Dan Simmons", bookshelves: "sci-fi"));
            var secondCsv = Csv(Row(bookId: "9990006", title: "Hyperion", author: "Dan Simmons", bookshelves: "sci-fi space-opera"));

            var first = await ReadResult(await _client.PostAsync(Endpoint, CsvForm(firstCsv)));
            await _client.PostAsync(Endpoint, CsvForm(secondCsv));

            var book = await GetBook(first.ImportedBooks.Single().Id);
            book.Topics.Should().BeEquivalentTo(new[] { "sci-fi", "space-opera" });
        }

        [Fact]
        public async Task Upload_Chunked_WrapsResultInChunkEnvelope()
        {
            var csv = Csv(Row(bookId: "9990007", title: "Chunk One", author: "Smoke Author"));

            var response = await _client.PostAsync($"{Endpoint}?chunkIndex=0&totalChunks=2", CsvForm(csv));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var envelope = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);
            envelope.GetProperty("chunkIndex").GetInt32().Should().Be(0);
            envelope.GetProperty("totalChunks").GetInt32().Should().Be(2);
            envelope.GetProperty("result").GetProperty("createdCount").GetInt32().Should().Be(1);
            envelope.GetProperty("result").GetProperty("success").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Upload_ReindexesMedia_OnlyWhenSomethingWasImported()
        {
            var (client, typesense) = _factory.CreateClientWithSubstitute<ITypesenseService>();
            var csv = Csv(Row(bookId: "9990008", title: "Indexed Book", author: "Smoke Author"));

            var first = await client.PostAsync(Endpoint, CsvForm(csv));
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            await typesense.Received(1).BulkReindexAllMediaItemsAsync();

            // updateExisting=false makes the re-upload a pure skip → no reindex (still 1 total).
            var second = await client.PostAsync($"{Endpoint}?updateExisting=false", CsvForm(csv));
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            await typesense.Received(1).BulkReindexAllMediaItemsAsync();
        }

        [Fact]
        public async Task Upload_NotACsv_ReturnsBadRequestJson()
        {
            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes("hello"));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            form.Add(file, "file", "notes.txt");

            var response = await _client.PostAsync(Endpoint, form);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), _jsonOptions);
            body.TryGetProperty("error", out _).Should().BeTrue("400 bodies are JSON objects, never bare strings");
        }

        [Fact]
        public async Task Upload_NoFile_ReturnsBadRequestJson()
        {
            using var form = new MultipartFormDataContent();

            var response = await _client.PostAsync(Endpoint, form);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Upload_WithoutToken_ReturnsUnauthorized()
        {
            var client = _factory.CreateAnonymousClient();

            var response = await client.PostAsync(Endpoint, CsvForm(Csv(Row(title: "X", author: "Y"))));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #region Helpers

        private async Task<GoodreadsImportResultDto> ReadResult(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GoodreadsImportResultDto>(body, _jsonOptions);
            result.Should().NotBeNull();
            return result!;
        }

        private async Task<BookResponseDto> GetBook(Guid id)
        {
            var response = await _client.GetAsync($"/api/book/{id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var book = JsonSerializer.Deserialize<BookResponseDto>(await response.Content.ReadAsStringAsync(), _jsonOptions);
            book.Should().NotBeNull();
            return book!;
        }

        private async Task<List<BookResponseDto>> GetAllBooks()
        {
            var response = await _client.GetAsync("/api/book");
            var books = JsonSerializer.Deserialize<List<BookResponseDto>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
            return books ?? new List<BookResponseDto>();
        }

        private static MultipartFormDataContent CsvForm(string csv, string fileName = "goodreads_library_export.csv")
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(file, "file", fileName);
            return form;
        }

        private const string Header =
            "Book Id,Title,Author,ISBN,ISBN13,My Rating,Average Rating,Publisher,Binding,Year Published,Original Publication Year,Date Read,Date Added,Bookshelves,Exclusive Shelf,My Review";

        private static string Csv(params string[] rows) => Header + "\n" + string.Join("\n", rows) + "\n";

        // Column order matches Header. Goodreads wraps ISBNs in an Excel formula (="…"); the tests
        // leave them blank so dedup exercises the Book Id and title+author keys.
        private static string Row(
            string title,
            string author,
            string bookId = "",
            int myRating = 0,
            string binding = "Paperback",
            string bookshelves = "",
            string exclusiveShelf = "read") =>
            $"\"{bookId}\",\"{title}\",\"{author}\",\"\",\"\",{myRating},3.90,\"Smoke Press\",\"{binding}\",2021,2021,,2026/09/05,\"{bookshelves}\",\"{exclusiveShelf}\",\"\"";

        #endregion
    }
}
