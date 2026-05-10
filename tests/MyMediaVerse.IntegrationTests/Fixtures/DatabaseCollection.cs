namespace MyMediaVerse.IntegrationTests.Fixtures
{
    /// <summary>
    /// xUnit collection that shares one <see cref="ApiFactory"/> across every test class
    /// tagged with <c>[Collection("Database")]</c>. Without this, every class would spin
    /// up its own Postgres container.
    /// </summary>
    [CollectionDefinition("Database")]
    public class DatabaseCollection : ICollectionFixture<ApiFactory>
    {
    }
}
