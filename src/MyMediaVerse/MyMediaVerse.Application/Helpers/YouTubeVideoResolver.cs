using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Helpers
{
    /// <summary>
    /// Turns a batch of YouTube playlist items into library videos: the ones already stored are
    /// found by their YouTube id (a row saved before ids were stored is found through its link),
    /// the rest are fetched in batches of 50 and mapped. Shared by the playlist mirror and the
    /// channel "import latest uploads" action. A channel is never created here.
    /// </summary>
    public sealed class YouTubeVideoResolver
    {
        private readonly IApplicationDbContext _context;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly IYouTubeMappingService _mappingService;

        // YouTube accepts at most 50 ids per videos request.
        public const int VideoDetailsBatchSize = 50;

        public YouTubeVideoResolver(
            IApplicationDbContext context,
            IYouTubeApiClient youTubeApiClient,
            IYouTubeMappingService mappingService)
        {
            _context = context;
            _youTubeApiClient = youTubeApiClient;
            _mappingService = mappingService;
        }

        public static string? GetVideoId(YouTubePlaylistItemDto item) =>
            item.Snippet?.ResourceId?.VideoId ?? item.ContentDetails?.VideoId;

        /// <summary>
        /// The YouTube id a stored video is known by: its own id, or for a row saved before ids
        /// were stored, the id held by its link.
        /// </summary>
        public static string? GetStoredYouTubeId(Video video) =>
            VideoDuplicateFinder.NormalizeExternalId(video.ExternalId)
            ?? VideoDuplicateFinder.ExtractYouTubeId(video.Link);

        /// <summary>
        /// Finds the videos already in the library for the given YouTube ids, keyed by that id.
        /// A row saved before ids were stored is found through its link and given its id.
        /// </summary>
        public async Task<Dictionary<string, Video>> FindStoredVideosAsync(List<string> youTubeIds)
        {
            var found = new Dictionary<string, Video>(StringComparer.Ordinal);
            if (youTubeIds.Count == 0)
            {
                return found;
            }

            var youTubeLower = VideoDuplicateFinder.YouTubePlatform.ToLower();
            var stored = await _context.Videos
                .Where(v => v.ExternalId != null && youTubeIds.Contains(v.ExternalId) && v.Platform.ToLower() == youTubeLower)
                .ToListAsync();
            foreach (var video in stored)
            {
                found.TryAdd(video.ExternalId!, video);
            }

            if (found.Count == youTubeIds.Count)
            {
                return found;
            }

            // The substring test narrows the candidates; the extraction confirms the id.
            var legacyRows = await _context.Videos
                .Where(v => (v.ExternalId == null || v.ExternalId == "") && v.Link != null && v.Link.Contains("youtu"))
                .ToListAsync();
            var wanted = youTubeIds.ToHashSet(StringComparer.Ordinal);
            foreach (var video in legacyRows)
            {
                var youTubeId = VideoDuplicateFinder.ExtractYouTubeId(video.Link);
                if (youTubeId != null && wanted.Contains(youTubeId) && found.TryAdd(youTubeId, video))
                {
                    VideoDuplicateFinder.AbsorbIdentity(video, new VideoIdentity
                    {
                        Platform = VideoDuplicateFinder.YouTubePlatform,
                        ExternalId = youTubeId
                    });
                }
            }

            return found;
        }

        /// <summary>
        /// Builds the videos that are new to the library. A video YouTube returns no details for
        /// is left out. Each new video is linked to its channel when that channel is already
        /// stored; a channel is never created here.
        /// </summary>
        public async Task<List<Video>> BuildNewVideosAsync(
            List<string> newIds,
            Dictionary<string, YouTubePlaylistItemDto> itemsByVideoId)
        {
            var details = new List<YouTubeVideoDto>();
            foreach (var batch in newIds.Chunk(VideoDetailsBatchSize))
            {
                details.AddRange(await _youTubeApiClient.GetVideosAsync(batch.ToList()) ?? new List<YouTubeVideoDto>());
            }

            var detailsById = new Dictionary<string, YouTubeVideoDto>(StringComparer.Ordinal);
            foreach (var detail in details.Where(detail => !string.IsNullOrEmpty(detail.Id)))
            {
                detailsById.TryAdd(detail.Id!, detail);
            }

            var itemsWithDetails = newIds
                .Where(detailsById.ContainsKey)
                .Select(id => itemsByVideoId[id])
                .ToList();
            if (itemsWithDetails.Count == 0)
            {
                return new List<Video>();
            }

            var channelExternalIds = detailsById.Values
                .Select(detail => detail.Snippet?.ChannelId)
                .Where(channelId => !string.IsNullOrEmpty(channelId))
                .Distinct()
                .ToList();
            var storedChannels = await _context.YouTubeChannels
                .Where(c => channelExternalIds.Contains(c.ChannelExternalId))
                .Select(c => new { c.ChannelExternalId, c.Id })
                .ToListAsync();
            var channelIds = storedChannels.ToDictionary(c => c.ChannelExternalId, c => c.Id, StringComparer.Ordinal);

            var videos = _mappingService.MapPlaylistItemsToVideoEntities(itemsWithDetails, details)
                .Where(video => !string.IsNullOrEmpty(video.ExternalId))
                .ToList();
            foreach (var video in videos)
            {
                video.Status = Status.Uncharted;

                var channelExternalId = detailsById[video.ExternalId!].Snippet?.ChannelId;
                if (channelExternalId != null && channelIds.TryGetValue(channelExternalId, out var channelId))
                {
                    video.ChannelId = channelId;
                }
            }

            return videos;
        }

        /// <summary>
        /// True for the placeholder a playlist shows for a deleted or private video.
        /// </summary>
        public static bool IsDeletedOrPrivateVideo(YouTubePlaylistItemDto item)
        {
            var title = item.Snippet?.Title ?? string.Empty;
            var titleLower = title.ToLowerInvariant();

            // Check for common deleted/private video indicators
            if (titleLower == "deleted video" ||
                titleLower == "private video" ||
                titleLower == "[deleted video]" ||
                titleLower == "[private video]")
            {
                return true;
            }

            // Check if the video has no channel info (often indicates deleted)
            var channelTitle = item.Snippet?.ChannelTitle ?? string.Empty;
            var videoId = GetVideoId(item);

            if (string.IsNullOrEmpty(channelTitle) && string.IsNullOrEmpty(videoId))
            {
                return true;
            }

            return false;
        }
    }
}
