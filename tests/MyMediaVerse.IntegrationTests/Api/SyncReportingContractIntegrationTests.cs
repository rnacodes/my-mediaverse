using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using NSubstitute;
using MyMediaVerse.Shared.DTOs.TMDB;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Verifies the HTTP semantics of the sync reporting contract: a completed run
    /// (warnings included) returns 200 with the result body, while an aborted run
    /// returns 500 with the same body shape so callers never parse two formats.
    /// </summary>
    [Trait("Category", "Integration")]
    [Collection("Database")]
    public class SyncReportingContractIntegrationTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;

        public SyncReportingContractIntegrationTests(ApiFactory factory)
        {
            _factory = factory;
        }

        public Task InitializeAsync() => _factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content).RootElement.Clone();
        }

        #region Note sync (single vault)

        [Fact]
        public async Task NoteSyncVault_WhenRunSucceeds_ShouldReturnOkWithResult()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<INoteService>(svc =>
                svc.SyncFromQuartzVaultAsync("general", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>())
                    .Returns(new NoteSyncResultDto
                    {
                        Success = true,
                        VaultName = "general",
                        CreatedCount = 2,
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/note/sync/general?url=https://vault.example.com", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("operation").GetString().Should().Be("notes-sync");
            body.GetProperty("createdCount").GetInt32().Should().Be(2);
        }

        [Fact]
        public async Task NoteSyncVault_WhenRunAborts_ShouldReturn500WithResultBody()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<INoteService>(svc =>
                svc.SyncFromQuartzVaultAsync("general", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>())
                    .Returns(new NoteSyncResultDto
                    {
                        Success = false,
                        VaultName = "general",
                        ErrorMessage = "Failed to reach the vault: connection refused",
                        StartedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/note/sync/general?url=https://vault.example.com", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeFalse();
            body.GetProperty("errorMessage").GetString().Should().Contain("Failed to reach the vault");
        }

        [Fact]
        public async Task NoteSyncVault_WhenRunCompletesWithWarning_ShouldStillReturnOk()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<INoteService>(svc =>
                svc.SyncFromQuartzVaultAsync("general", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>())
                    .Returns(new NoteSyncResultDto
                    {
                        Success = true,
                        VaultName = "general",
                        WarningMessage = "Orphan removal skipped: the published content index is empty.",
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/note/sync/general?url=https://vault.example.com", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("warningMessage").GetString().Should().Contain("Orphan removal skipped");
        }

        #endregion

        #region Note sync (all vaults envelope)

        [Fact]
        public async Task NoteSyncAll_WhenAllVaultsSucceed_ShouldReturnOkWithSuccessTrue()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<INoteService>(svc =>
                svc.SyncAllVaultsAsync(Arg.Any<bool>())
                    .Returns(new List<NoteSyncResultDto>
                    {
                        new() { Success = true, VaultName = "general" },
                        new() { Success = true, VaultName = "programming" }
                    }));

            var response = await client.PostAsync("/api/note/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("results").GetArrayLength().Should().Be(2);
        }

        [Fact]
        public async Task NoteSyncAll_WhenAnyVaultFails_ShouldReturn500WithEnvelope()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<INoteService>(svc =>
                svc.SyncAllVaultsAsync(Arg.Any<bool>())
                    .Returns(new List<NoteSyncResultDto>
                    {
                        new() { Success = true, VaultName = "general" },
                        new() { Success = false, VaultName = "programming", ErrorMessage = "Vault authentication failed" }
                    }));

            var response = await client.PostAsync("/api/note/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeFalse();
            body.GetProperty("results").GetArrayLength().Should().Be(2);
        }

        #endregion

        #region Readwise unified sync

        [Fact]
        public async Task ReadwiseSync_WhenRunSucceeds_ShouldReturnOk()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IReadwiseSyncService>(svc =>
                svc.SyncAllAsync(Arg.Any<bool>())
                    .Returns(new ReadwiseSyncAllResultDto
                    {
                        Success = true,
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/readwise/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("operation").GetString().Should().Be("readwise-sync");
        }

        [Fact]
        public async Task ReadwiseSync_WhenServiceReportsFailure_ShouldReturn500WithResultBody()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IReadwiseSyncService>(svc =>
                svc.SyncAllAsync(Arg.Any<bool>())
                    .Returns(new ReadwiseSyncAllResultDto
                    {
                        Success = false,
                        ErrorMessage = "Readwise API returned 401",
                        StartedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/readwise/sync", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeFalse();
            body.GetProperty("errorMessage").GetString().Should().Contain("401");
        }

        #endregion

        #region Reader document sync

        [Fact]
        public async Task ReaderSync_WhenServiceReportsFailure_ShouldReturn500WithResultBody()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IReaderService>(svc =>
                svc.SyncDocumentsAsync(Arg.Any<string?>(), Arg.Any<DateTime?>())
                    .Returns(new ReaderSyncResultDto
                    {
                        Success = false,
                        ErrorMessage = "Reader API unreachable",
                        StartedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/article/sync-reader", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeFalse();
            body.GetProperty("errorMessage").GetString().Should().Contain("unreachable");
        }

        [Fact]
        public async Task ReaderSync_WhenRunSucceeds_ShouldReturnOk()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IReaderService>(svc =>
                svc.SyncDocumentsAsync(Arg.Any<string?>(), Arg.Any<DateTime?>())
                    .Returns(new ReaderSyncResultDto
                    {
                        Success = true,
                        CreatedCount = 3,
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/article/sync-reader", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("operation").GetString().Should().Be("reader-sync");
            body.GetProperty("createdCount").GetInt32().Should().Be(3);
        }

        #endregion

        #region TMDB refresh

        [Fact]
        public async Task TmdbRefreshStale_WhenRunCompletesWithItemFailures_ShouldStillReturnOk()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IMovieTvEnrichmentService>(svc =>
                svc.RefreshStaleAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(new MovieTvRefreshResultDto
                    {
                        FailedCount = 1,
                        Errors = { "TMDB no longer has id 1 for 'Removed Upstream'" },
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/movietvenrichment/refresh-stale", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("operation").GetString().Should().Be("tmdb-refresh-stale");
            body.GetProperty("failedCount").GetInt32().Should().Be(1);
            body.GetProperty("reindexTriggered").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task TmdbRefreshStale_WhenRunAborts_ShouldReturn500WithResultBody()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<IMovieTvEnrichmentService>(svc =>
                svc.RefreshStaleAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(new MovieTvRefreshResultDto
                    {
                        Success = false,
                        ErrorMessage = "TMDB refresh run failed: database unavailable",
                        StartedAt = DateTime.UtcNow
                    }));

            var response = await client.PostAsync("/api/movietvenrichment/refresh-stale", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeFalse();
            body.GetProperty("operation").GetString().Should().Be("tmdb-refresh-stale");
            body.GetProperty("errorMessage").GetString().Should().Contain("database unavailable");
        }

        #endregion

        #region TMDB episode import

        // The 404 pre-check runs against the real database, so the show must exist even though the
        // import service itself is substituted.
        private static async Task<Guid> CreateShowAsync(HttpClient client)
        {
            var dto = new CreateTvShowDto
            {
                Title = "Contract Show",
                TmdbId = "424242",
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted
            };
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var content = new StringContent(JsonSerializer.Serialize(dto, options), System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/tvshow", content);
            response.EnsureSuccessStatusCode();
            return (await ReadBodyAsync(response)).GetProperty("id").GetGuid();
        }

        [Fact]
        public async Task TvEpisodeImport_WhenRunCompletesWithSeasonFailures_ShouldStillReturnOk()
        {
            var (client, _, reindex) = _factory.CreateClientWithSubstitutes<ITvEpisodeImportService, IImportReindexService>(svc =>
                svc.ImportFromTmdbAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(call => new TvEpisodeImportResultDto
                    {
                        ShowId = call.Arg<Guid>(),
                        ShowTitle = "Contract Show",
                        CreatedCount = 9,
                        FailedCount = 1,
                        SeasonsProcessed = 1,
                        Errors = { "TMDB has no season 2 for 'Contract Show'." },
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    }));
            var showId = await CreateShowAsync(client);

            var response = await client.PostAsync($"/api/tvshow/{showId}/episodes/from-tmdb", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.GetProperty("operation").GetString().Should().Be("tv-episodes-from-tmdb");
            body.GetProperty("showId").GetGuid().Should().Be(showId);
            body.GetProperty("createdCount").GetInt32().Should().Be(9);
            body.GetProperty("failedCount").GetInt32().Should().Be(1);
            body.GetProperty("totalProcessed").GetInt32().Should().Be(9);
            body.GetProperty("errors").GetArrayLength().Should().Be(1);
            body.GetProperty("reindexTriggered").GetBoolean().Should().BeTrue();
            await reindex.Received(1).ReindexAfterImportAsync(9, Arg.Any<string>());
        }

        [Fact]
        public async Task TvEpisodeImport_WhenNothingChanged_ShouldNotReindex()
        {
            var (client, _, reindex) = _factory.CreateClientWithSubstitutes<ITvEpisodeImportService, IImportReindexService>(svc =>
                svc.ImportFromTmdbAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(new TvEpisodeImportResultDto
                    {
                        SkippedCount = 10,
                        SeasonsProcessed = 1,
                        StartedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    }));
            var showId = await CreateShowAsync(client);

            var response = await client.PostAsync($"/api/tvshow/{showId}/episodes/from-tmdb", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await ReadBodyAsync(response);
            body.GetProperty("skippedCount").GetInt32().Should().Be(10);
            body.GetProperty("reindexTriggered").GetBoolean().Should().BeFalse();
            await reindex.DidNotReceive().ReindexAfterImportAsync(Arg.Any<int>(), Arg.Any<string>());
        }

        [Fact]
        public async Task TvEpisodeImport_WhenRunAborts_ShouldReturn500WithResultBody()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<ITvEpisodeImportService>(svc =>
                svc.ImportFromTmdbAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(new TvEpisodeImportResultDto
                    {
                        Success = false,
                        ShowTitle = "Contract Show",
                        ErrorMessage = "'Contract Show' has no usable TMDB id; import the show from TMDB or set its TMDB id first.",
                        StartedAt = DateTime.UtcNow
                    }));
            var showId = await CreateShowAsync(client);

            var response = await client.PostAsync($"/api/tvshow/{showId}/episodes/from-tmdb", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeFalse();
            body.GetProperty("operation").GetString().Should().Be("tv-episodes-from-tmdb");
            body.GetProperty("errorMessage").GetString().Should().Contain("no usable TMDB id");
            // Null properties are omitted from responses; an aborted run must not report a completion time.
            (body.TryGetProperty("completedAt", out var completedAt) && completedAt.ValueKind != JsonValueKind.Null)
                .Should().BeFalse();
        }

        [Fact]
        public async Task TvEpisodeImport_WhenTheServiceThrows_ShouldReturn500WithResultBody()
        {
            var (client, _) = _factory.CreateClientWithSubstitute<ITvEpisodeImportService>(svc =>
                svc.ImportFromTmdbAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns<TvEpisodeImportResultDto>(_ => throw new InvalidOperationException("boom")));
            var showId = await CreateShowAsync(client);

            var response = await client.PostAsync($"/api/tvshow/{showId}/episodes/from-tmdb", null);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ReadBodyAsync(response);
            body.GetProperty("success").GetBoolean().Should().BeFalse();
            body.GetProperty("operation").GetString().Should().Be("tv-episodes-from-tmdb");
            body.GetProperty("errorMessage").GetString().Should().Contain("boom");
        }

        #endregion
    }
}
