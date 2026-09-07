namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Monthly budget for screenshot renders. Callers reserve a unit only after a render
    /// succeeded, so failed attempts never count against the cap.
    /// </summary>
    public interface IScreenshotQuota
    {
        /// <summary>Renders left in the current calendar month (never negative).</summary>
        Task<int> RemainingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Records one render against the current month. Returns false, recording nothing,
        /// when the cap has already been reached.
        /// </summary>
        Task<bool> TryReserveAsync(CancellationToken cancellationToken = default);
    }
}
