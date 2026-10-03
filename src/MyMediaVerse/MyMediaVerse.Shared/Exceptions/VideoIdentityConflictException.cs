namespace MyMediaVerse.Shared.Exceptions
{
    /// <summary>
    /// Thrown when a video is given an id that another video on the same platform already
    /// holds. Each video id belongs to one library item.
    /// </summary>
    public class VideoIdentityConflictException : Exception
    {
        public string Platform { get; }
        public string ExternalId { get; }

        public VideoIdentityConflictException(string platform, string externalId)
            : base($"Another video already has the {platform} id '{externalId}'.")
        {
            Platform = platform;
            ExternalId = externalId;
        }
    }
}
