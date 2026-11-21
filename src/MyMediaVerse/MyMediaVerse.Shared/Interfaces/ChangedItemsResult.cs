namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// The outcome of an import, sync or enrichment run: the <paramref name="Report"/> returned to
    /// the caller, and <paramref name="ChangedIds"/>, the media items the run created or changed.
    /// The ids stay server-side; they tell the search reindex which documents to refresh, so it
    /// never has to rebuild the whole library.
    /// </summary>
    public sealed record ChangedItemsResult<TReport>(TReport Report, IReadOnlyList<Guid> ChangedIds);
}
