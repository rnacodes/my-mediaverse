namespace MyMediaVerse.Shared.Exceptions
{
    /// <summary>
    /// Thrown when a YouTube call is attempted without an API key. Raised on first use rather
    /// than at startup, so the app still runs without a key and only YouTube features fail.
    /// </summary>
    public class YouTubeNotConfiguredException : Exception
    {
        public YouTubeNotConfiguredException()
            : base("YouTube API key is not configured. Set the YOUTUBE_API_KEY environment variable or ApiKeys:YouTube in configuration.")
        {
        }
    }
}
