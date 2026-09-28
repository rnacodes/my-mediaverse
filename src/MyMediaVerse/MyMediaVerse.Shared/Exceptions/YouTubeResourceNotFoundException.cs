namespace MyMediaVerse.Shared.Exceptions
{
    /// <summary>
    /// Thrown when YouTube has no video, channel, or playlist for the id that was asked for:
    /// it was deleted, made private, or never existed. This is an answer from YouTube, not a
    /// fault, so callers report it as "not found" and keep it apart from configuration and
    /// transport errors.
    /// </summary>
    public class YouTubeResourceNotFoundException : Exception
    {
        /// <summary>What was looked up: "video", "channel", or "playlist".</summary>
        public string ResourceType { get; }

        /// <summary>The YouTube id, handle, or username that returned nothing.</summary>
        public string Identifier { get; }

        public YouTubeResourceNotFoundException(string resourceType, string identifier)
            : base($"YouTube has no {resourceType} for '{identifier}'.")
        {
            ResourceType = resourceType;
            Identifier = identifier;
        }
    }
}
