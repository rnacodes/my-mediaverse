namespace MyMediaVerse.Shared.Exceptions
{
    /// <summary>
    /// Thrown when the YouTube Data API returns a 403 indicating the daily quota has been
    /// exhausted (reason "quotaExceeded" / "dailyLimitExceeded"). This is distinct from a
    /// transient rate limit (429), which is retried automatically. Callers (e.g. a bulk
    /// playlist import) should treat this as a clean stop signal and resume once the quota
    /// resets, rather than retrying immediately.
    /// </summary>
    public class YouTubeQuotaExceededException : Exception
    {
        /// <summary>
        /// The YouTube error reason that triggered this exception (e.g. "quotaExceeded").
        /// </summary>
        public string? Reason { get; }

        public YouTubeQuotaExceededException(string message, string? reason = null)
            : base(message)
        {
            Reason = reason;
        }
    }
}
