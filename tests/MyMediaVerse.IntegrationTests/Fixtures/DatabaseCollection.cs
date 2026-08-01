namespace MyMediaVerse.IntegrationTests.Fixtures
{
    /// <summary>
    /// xUnit collection that shares one <see cref="ApiFactory"/> (and one
    /// <see cref="DemoApiFactory"/> for demo-environment tests) across every test class
    /// tagged with <c>[Collection("Database")]</c>. Without this, every class would spin
    /// up its own Postgres container. Keeping both factories in a single collection also
    /// serializes their startup, which matters because each mutates process-level
    /// environment variables while its host builds.
    /// </summary>
    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<ApiFactory>, ICollectionFixture<DemoApiFactory>
    {
    }
}
