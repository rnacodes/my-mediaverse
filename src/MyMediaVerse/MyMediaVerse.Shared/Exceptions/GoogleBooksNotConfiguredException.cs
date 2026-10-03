namespace MyMediaVerse.Shared.Exceptions
{
    /// <summary>
    /// Thrown when a Google Books call is attempted without an API key. Google rejects
    /// keyless requests with a 429 that no amount of waiting or retrying will clear, so the
    /// client refuses to send the request and callers can report the real cause instead.
    /// </summary>
    public class GoogleBooksNotConfiguredException : Exception
    {
        public GoogleBooksNotConfiguredException()
            : base("Google Books API key is not configured. Set the GOOGLE_BOOKS_API_KEY environment variable or GoogleBooks:ApiKey in configuration.")
        {
        }
    }
}
