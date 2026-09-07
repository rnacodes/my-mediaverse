namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Looks up archived copies of a page in the Internet Archive's Wayback Machine.
    /// </summary>
    public interface IWaybackMachineClient
    {
        /// <summary>
        /// Returns the URL of the most recent successful snapshot of the page
        /// (<c>https://web.archive.org/web/{timestamp}/{url}</c>), or null when the archive has
        /// none or cannot be reached. Never throws for a missing or unreachable archive.
        /// </summary>
        Task<string?> FindLatestSnapshotAsync(string url, CancellationToken cancellationToken = default);
    }
}
