using System.Net;

namespace MyMediaVerse.Shared.Exceptions
{
    /// <summary>Why a podcast feed could not be read.</summary>
    public enum PodcastFeedFailureReason
    {
        /// <summary>The URL is not an absolute http or https URL.</summary>
        InvalidUrl,
        /// <summary>The server answered with a non-success status code.</summary>
        HttpError,
        /// <summary>The host could not be reached (DNS, connection, TLS).</summary>
        Unreachable,
        /// <summary>The feed did not arrive within the time limit.</summary>
        Timeout,
        /// <summary>The feed is larger than the size cap.</summary>
        TooLarge,
        /// <summary>The body is not well-formed XML, or it declares a DTD.</summary>
        InvalidXml,
        /// <summary>The XML is not an RSS feed with a channel.</summary>
        NotAFeed
    }

    /// <summary>
    /// Thrown when a podcast feed cannot be fetched or parsed. The message is safe to show to users;
    /// <see cref="Reason"/> lets enrichment decide when to retry.
    /// </summary>
    public class PodcastFeedException : Exception
    {
        public PodcastFeedFailureReason Reason { get; }

        /// <summary>The HTTP status when <see cref="Reason"/> is <see cref="PodcastFeedFailureReason.HttpError"/>.</summary>
        public HttpStatusCode? StatusCode { get; }

        public PodcastFeedException(
            PodcastFeedFailureReason reason, string message, HttpStatusCode? statusCode = null, Exception? innerException = null)
            : base(message, innerException)
        {
            Reason = reason;
            StatusCode = statusCode;
        }
    }
}
