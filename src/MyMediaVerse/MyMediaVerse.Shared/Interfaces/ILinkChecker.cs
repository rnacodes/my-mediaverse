namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Probes a URL and reports the HTTP status it finally answers with.
    /// </summary>
    public interface ILinkChecker
    {
        /// <summary>
        /// Returns the final HTTP status code for the URL after following redirects, or 0 when the
        /// host cannot be reached at all (DNS failure, connection refused, timeout). Never throws
        /// for a bad or unreachable URL.
        /// </summary>
        Task<int> CheckAsync(string url, CancellationToken cancellationToken = default);
    }
}
