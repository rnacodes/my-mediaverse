using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;

namespace MyMediaVerse.Application.Helpers
{
    /// <summary>
    /// The one place that decides what a fresh YouTube payload may change on a stored video,
    /// channel or playlist. Values YouTube owns are overwritten whenever YouTube sends one; an
    /// empty incoming value never replaces a stored one. A thumbnail follows YouTube only while
    /// it is empty or already points at a YouTube image host, so an image chosen by hand
    /// survives. Status, rating, notes, topics, genres, dates, the link, mixlists and playlist
    /// membership are never touched.
    /// Each method returns true when any stored value changed. Refresh timestamps are left to
    /// the caller, which also stamps items that turned out to be unchanged.
    /// </summary>
    public static class YouTubeMetadataApplier
    {
        /// <param name="storedChannelId">
        /// The library id of the video's channel when that channel is already stored. Used only
        /// to link a video that has no channel yet; an existing link is never replaced.
        /// </param>
        public static bool Apply(Video video, YouTubeVideoDto youTube, Guid? storedChannelId = null)
        {
            var changed = false;
            var snippet = youTube.Snippet;

            changed |= Overwrite(video.Title, snippet?.Title, v => video.Title = v!);
            changed |= Overwrite(video.Description, snippet?.Description, v => video.Description = v);
            changed |= OverwriteDate(video.PublishedAt, snippet?.PublishedAt, v => video.PublishedAt = v);
            changed |= RefreshThumbnail(video, snippet?.Thumbnails);

            // A live or upcoming video reports no duration; the stored length stays.
            var seconds = YouTubeHelper.ParseDurationToSeconds(youTube.ContentDetails?.Duration);
            if (seconds > 0) changed |= Set(video.LengthInSeconds, seconds, v => video.LengthInSeconds = v);

            if (video.ChannelId == null && storedChannelId.HasValue)
            {
                video.ChannelId = storedChannelId;
                changed = true;
            }

            return changed;
        }

        public static bool Apply(YouTubeChannel channel, YouTubeChannelDto youTube)
        {
            var changed = false;
            var snippet = youTube.Snippet;

            changed |= Overwrite(channel.Title, snippet?.Title, v => channel.Title = v!);
            changed |= Overwrite(channel.Description, snippet?.Description, v => channel.Description = v);
            changed |= Overwrite(channel.CustomUrl, snippet?.CustomUrl, v => channel.CustomUrl = v);
            changed |= Overwrite(channel.Country, snippet?.Country, v => channel.Country = v);
            changed |= OverwriteDate(channel.PublishedAt, snippet?.PublishedAt, v => channel.PublishedAt = v);
            changed |= RefreshThumbnail(channel, snippet?.Thumbnails);

            // A channel that hides its subscriber count sends none; the stored count stays.
            changed |= OverwriteCount(channel.SubscriberCount, youTube.Statistics?.SubscriberCount, v => channel.SubscriberCount = v);
            changed |= OverwriteCount(channel.VideoCount, youTube.Statistics?.VideoCount, v => channel.VideoCount = v);
            changed |= OverwriteCount(channel.ViewCount, youTube.Statistics?.ViewCount, v => channel.ViewCount = v);
            changed |= Overwrite(
                channel.UploadsPlaylistId,
                youTube.ContentDetails?.RelatedPlaylists?.Uploads,
                v => channel.UploadsPlaylistId = v);

            return changed;
        }

        public static bool Apply(YouTubePlaylist playlist, YouTubePlaylistDto youTube)
        {
            var changed = false;
            var snippet = youTube.Snippet;

            changed |= Overwrite(playlist.Title, snippet?.Title, v => playlist.Title = v!);
            changed |= Overwrite(playlist.Description, snippet?.Description, v => playlist.Description = v);
            changed |= OverwriteDate(playlist.PublishedAt, snippet?.PublishedAt, v => playlist.PublishedAt = v);
            changed |= RefreshThumbnail(playlist, snippet?.Thumbnails);
            changed |= Overwrite(playlist.PrivacyStatus, youTube.Status?.PrivacyStatus, v => playlist.PrivacyStatus = v);

            if (youTube.ContentDetails?.ItemCount is int itemCount)
            {
                changed |= Set(playlist.VideoCount, itemCount, v => playlist.VideoCount = v);
            }

            return changed;
        }

        private static bool RefreshThumbnail(BaseMediaItem item, YouTubeThumbnailsDto? thumbnails)
        {
            var incoming = YouTubeHelper.GetBestThumbnailUrl(thumbnails);
            if (string.IsNullOrEmpty(incoming)) return false;

            var isYouTubeOwned = string.IsNullOrEmpty(item.Thumbnail) || YouTubeHelper.IsYouTubeImageUrl(item.Thumbnail);
            return isYouTubeOwned && Set(item.Thumbnail, incoming, v => item.Thumbnail = v);
        }

        private static bool Overwrite(string? current, string? incoming, Action<string?> assign)
            => !string.IsNullOrEmpty(incoming) && Set(current, incoming, assign);

        private static bool OverwriteDate(DateTime? current, DateTime? incoming, Action<DateTime?> assign)
            => incoming.HasValue && Set(current, DateTimeNormalizer.ToUtc(incoming), assign);

        private static bool OverwriteCount(long? current, string? incoming, Action<long?> assign)
            => long.TryParse(incoming, out var count) && Set(current, count, assign);

        private static bool Set<T>(T current, T incoming, Action<T> assign)
        {
            if (EqualityComparer<T>.Default.Equals(current, incoming)) return false;

            assign(incoming);
            return true;
        }
    }
}
