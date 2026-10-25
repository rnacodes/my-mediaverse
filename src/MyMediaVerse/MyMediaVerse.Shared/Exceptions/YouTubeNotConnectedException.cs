namespace MyMediaVerse.Shared.Exceptions
{
    /// <summary>
    /// Thrown when a call needs the owner's YouTube account (such as listing their own
    /// playlists) but no account is connected, or the connection has lapsed.
    /// </summary>
    public class YouTubeNotConnectedException : Exception
    {
        public YouTubeNotConnectedException()
            : base("No YouTube account is connected. Connect it from the Import page and try again.")
        {
        }
    }
}
