using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Options;
using Typesense;
using Typesense.Setup;
using Xunit;

namespace MyMediaVerse.IntegrationTests;

/// <summary>
/// Verifies the Typesense .NET client wire-compatibility against a real Typesense server,
/// pinned to the version running in production. Spins up its own container (like the
/// PostgreSQL integration tests) so it is self-contained and CI-runnable — it does not
/// depend on any externally running Typesense.
///
/// Exercises every client operation the production search service relies on:
/// CreateCollection, RetrieveCollection, ImportDocuments (bulk), UpsertDocument,
/// Search, DeleteDocument, DeleteCollection.
/// </summary>
public class TypesenseClientCompatTests : IAsyncLifetime
{
    private const string TypesenseImage = "typesense/typesense:30.2";
    private const string ApiKey = "test-api-key";
    private const int TypesensePort = 8108;

    private readonly IContainer _container = new ContainerBuilder()
        .WithImage(TypesenseImage)
        .WithPortBinding(TypesensePort, assignRandomHostPort: true)
        // /tmp always exists and is writable; the image does not create the default /data dir.
        .WithCommand("--data-dir", "/tmp", $"--api-key={ApiKey}", "--enable-cors")
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(r => r.ForPort(TypesensePort).ForPath("/health")))
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private sealed class SmokeDoc
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("media_type")] public string MediaType { get; set; } = string.Empty;
    }

    private ITypesenseClient CreateClient()
    {
        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(TypesensePort).ToString();
        var config = new Config(new List<Node> { new Node(host, port, "http") }, ApiKey);
        return new TypesenseClient(Options.Create(config), new HttpClient());
    }

    [Fact]
    public async Task Client_round_trips_against_pinned_server_version()
    {
        var client = CreateClient();
        const string collection = "client_compat_check";

        // CreateCollection (mirrors EnsureCollectionExistsAsync shape)
        var schema = new Schema(collection, new List<Field>
        {
            new Field("id", FieldType.String, false),
            new Field("title", FieldType.String, false),
            new Field("media_type", FieldType.String, true),
        });
        var created = await client.CreateCollection(schema);
        Assert.Equal(collection, created.Name);

        // RetrieveCollection (Typesense does not echo the special "id" field back)
        var retrieved = await client.RetrieveCollection(collection);
        Assert.Equal(collection, retrieved.Name);
        Assert.Contains(retrieved.Fields, f => f.Name == "title");
        Assert.Contains(retrieved.Fields, f => f.Name == "media_type");

        // ImportDocuments (bulk Create — mirrors BulkReindexAllMediaItemsAsync)
        var docs = new List<SmokeDoc>
        {
            new() { Id = "1", Title = "the great test book", MediaType = "Book" },
            new() { Id = "2", Title = "a test movie", MediaType = "Movie" },
        };
        var importResults = await client.ImportDocuments(collection, docs, 40, ImportType.Create);
        Assert.All(importResults, r => Assert.True(r.Success, r.Error));

        // UpsertDocument (single — mirrors IndexMediaItemAsync)
        await client.UpsertDocument(collection, new SmokeDoc { Id = "3", Title = "test upsert", MediaType = "Article" });

        // Search (mirrors SearchAsync)
        var search = await client.Search<SmokeDoc>(collection, new SearchParameters("test", "title")
        {
            PerPage = 20,
            Page = 1,
            SortBy = "_text_match:desc",
        });
        Assert.True(search.Found >= 3, $"expected >=3 hits, got {search.Found}");

        // DeleteDocument
        await client.DeleteDocument<SmokeDoc>(collection, "3");

        // DeleteCollection
        var deleted = await client.DeleteCollection(collection);
        Assert.Equal(collection, deleted.Name);
    }
}
