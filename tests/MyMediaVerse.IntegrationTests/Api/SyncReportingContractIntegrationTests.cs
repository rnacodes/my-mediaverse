using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.DTOs;
using MyMediaVerse.IntegrationTests.Fixtures;
using NSubstitute;

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
    }
}
