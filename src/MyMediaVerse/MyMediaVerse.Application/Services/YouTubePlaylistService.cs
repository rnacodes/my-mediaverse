using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Helpers;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.DTOs.YouTube;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    public class YouTubePlaylistService : IYouTubePlaylistService
    {
        private readonly IApplicationDbContext _context;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly IYouTubeMappingService _mappingService;
        private readonly IMediaService _mediaService;
        private readonly ILogger<YouTubePlaylistService> _logger;

        // YouTube accepts at most 50 ids per videos request.
        public const int VideoDetailsBatchSize = 50;

        public YouTubePlaylistService(
            IApplicationDbContext context,
            IYouTubeApiClient youTubeApiClient,
            IYouTubeMappingService mappingService,
            IMediaService mediaService,
            ILogger<YouTubePlaylistService> logger)
        {
            _context = context;
            _youTubeApiClient = youTubeApiClient;
            _mappingService = mappingService;
            _mediaService = mediaService;
            _logger = logger;
        }

        public async Task<YouTubePlaylist?> GetPlaylistByIdAsync(Guid id, bool includeVideos = false)
        {
            var query = _context.YouTubePlaylists
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Mixlists)
                .AsQueryable();

            if (includeVideos)
            {
                query = query
                    .Include(p => p.PlaylistVideos)
                    .ThenInclude(pv => pv.Video);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<YouTubePlaylist?> GetPlaylistByExternalIdAsync(string externalId, bool includeVideos = false)
        {
            var query = _context.YouTubePlaylists
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Mixlists)
                .AsQueryable();

            if (includeVideos)
            {
                query = query
                    .Include(p => p.PlaylistVideos)
                    .ThenInclude(pv => pv.Video);
            }

            return await query.FirstOrDefaultAsync(p => p.PlaylistExternalId == externalId);
        }

        public async Task<List<YouTubePlaylist>> GetAllPlaylistsAsync()
        {
            return await _context.YouTubePlaylists
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .Include(p => p.Mixlists)
                .ToListAsync();
        }

        public async Task<List<Video>> GetPlaylistVideosAsync(Guid playlistId)
        {
            var playlist = await _context.YouTubePlaylists
                .Include(p => p.PlaylistVideos)
                .ThenInclude(pv => pv.Video)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return new List<Video>();

            return playlist.PlaylistVideos
                .OrderBy(pv => pv.Position)
                .Select(pv => pv.Video)
                .ToList();
        }

        public async Task<YouTubePlaylistCreationResult> ImportPlaylistFromYouTubeAsync(string playlistExternalId)
        {
            try
            {
                _logger.LogInformation($"Importing YouTube playlist: {playlistExternalId}");

                // Check if playlist already exists
                var existingPlaylist = await GetPlaylistByExternalIdAsync(playlistExternalId, includeVideos: false);
                if (existingPlaylist != null)
                {
                    _logger.LogInformation($"Playlist {playlistExternalId} already exists, returning existing playlist");
                    return new YouTubePlaylistCreationResult(existingPlaylist, false);
                }

                // Get playlist details from YouTube API
                var playlistDto = await _youTubeApiClient.GetPlaylistDetailsAsync(playlistExternalId);
                if (playlistDto == null)
                {
                    throw new YouTubeResourceNotFoundException("playlist", playlistExternalId);
                }

                // Create playlist entity
                var playlist = _mappingService.MapPlaylistToYouTubePlaylistEntity(playlistDto);

                // Save playlist (without importing videos - similar to podcast series)
                var savedPlaylist = await SavePlaylistAsync(playlist, updateIfExists: false);

                _logger.LogInformation($"Successfully imported playlist {savedPlaylist.Title} (videos not auto-imported - use selective import)");

                // The save hands back a different row when another request imported the
                // playlist first.
                return new YouTubePlaylistCreationResult(savedPlaylist, ReferenceEquals(savedPlaylist, playlist));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing YouTube playlist: {playlistExternalId}");
                throw;
            }
        }

        public async Task<YouTubePlaylistSyncResult> SyncPlaylistVideosAsync(Guid playlistId)
        {
            var playlist = await GetPlaylistByIdAsync(playlistId, includeVideos: true);
            if (playlist == null)
                throw new InvalidOperationException($"Playlist with ID {playlistId} not found");

            _logger.LogInformation("Syncing playlist: {Title}", playlist.Title);

            // The playlist's own details come first, so its sync stamp means its title,
            // description and thumbnail are current too, not only its video list.
            var playlistDto = await _youTubeApiClient.GetPlaylistDetailsAsync(playlist.PlaylistExternalId);
            if (playlistDto == null)
            {
                throw new YouTubeResourceNotFoundException("playlist", playlist.PlaylistExternalId);
            }

            YouTubeMetadataApplier.Apply(playlist, playlistDto);

            var playlistItems = await _youTubeApiClient.GetAllPlaylistItemsAsync(playlist.PlaylistExternalId);

            // Deleted and private videos are left out. A playlist can also list the same video
            // twice; the first occurrence decides its position, because a video is linked to a
            // playlist only once.
            var itemsByVideoId = new Dictionary<string, YouTubePlaylistItemDto>(StringComparer.Ordinal);
            var orderedVideoIds = new List<string>();
            foreach (var item in playlistItems.Where(item => !IsDeletedOrPrivateVideo(item)))
            {
                var videoId = GetVideoId(item);
                if (!string.IsNullOrEmpty(videoId) && itemsByVideoId.TryAdd(videoId, item))
                {
                    orderedVideoIds.Add(videoId);
                }
            }

            // Unlink the videos that left the playlist. The video rows stay: they may sit in a
            // mixlist, another playlist, or under a channel.
            var linksByVideoId = new Dictionary<string, YouTubePlaylistVideo>(StringComparer.Ordinal);
            var linkedVideoIds = new HashSet<Guid>();
            var unlinkedCount = 0;
            foreach (var link in playlist.PlaylistVideos?.ToList() ?? new List<YouTubePlaylistVideo>())
            {
                var videoId = link.Video == null ? null : GetStoredYouTubeId(link.Video);
                if (link.Video != null && (videoId == null || !itemsByVideoId.ContainsKey(videoId)))
                {
                    _context.Remove(link);
                    unlinkedCount++;
                    _logger.LogInformation("Unlinked video '{Title}' from playlist (no longer in the YouTube playlist)", link.Video.Title);
                    continue;
                }

                linkedVideoIds.Add(link.VideoId);
                if (videoId != null)
                {
                    linksByVideoId.TryAdd(videoId, link);
                }
            }

            var idsToAdd = orderedVideoIds.Where(id => !linksByVideoId.ContainsKey(id)).ToList();
            var videosByYouTubeId = await FindStoredVideosAsync(idsToAdd);
            var reusedIds = videosByYouTubeId.Keys.ToHashSet(StringComparer.Ordinal);

            var newIds = idsToAdd.Where(id => !videosByYouTubeId.ContainsKey(id)).ToList();
            var createdCount = 0;
            if (newIds.Count > 0)
            {
                var newVideos = await BuildNewVideosAsync(newIds, itemsByVideoId);
                foreach (var video in newVideos)
                {
                    _context.Add(video);
                    videosByYouTubeId[video.ExternalId!] = video;
                    createdCount++;
                }
            }

            var linkedCount = 0;
            foreach (var videoId in idsToAdd)
            {
                // No entry means YouTube returned no details for the video, so it was not added.
                if (!videosByYouTubeId.TryGetValue(videoId, out var video) || !linkedVideoIds.Add(video.Id))
                {
                    continue;
                }

                var item = itemsByVideoId[videoId];
                _context.Add(new YouTubePlaylistVideo
                {
                    YouTubePlaylistId = playlist.Id,
                    VideoId = video.Id,
                    Video = video,
                    Position = item.Snippet?.Position,
                    VideoPublishedAt = DateTimeNormalizer.ToUtc(item.Snippet?.PublishedAt)
                });

                if (reusedIds.Contains(videoId))
                {
                    linkedCount++;
                }
            }

            // Follow YouTube's order for the videos that were already linked.
            var positionsUpdated = 0;
            foreach (var (videoId, link) in linksByVideoId)
            {
                var position = itemsByVideoId[videoId].Snippet?.Position;
                if (position.HasValue && link.Position != position)
                {
                    link.Position = position;
                    positionsUpdated++;
                }
            }

            playlist.LastSyncedAt = DateTime.UtcNow;
            playlist.VideoCount = itemsByVideoId.Count;
            _context.Update(playlist);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Synced playlist {Title}: {Created} created, {Linked} linked, {Unlinked} unlinked, {Moved} moved, {Available} available on YouTube",
                playlist.Title, createdCount, linkedCount, unlinkedCount, positionsUpdated, itemsByVideoId.Count);

            var synced = await GetPlaylistByIdAsync(playlistId, includeVideos: true) ?? playlist;
            return new YouTubePlaylistSyncResult(synced, createdCount, linkedCount, unlinkedCount, positionsUpdated);
        }

        private static string? GetVideoId(YouTubePlaylistItemDto item) =>
            item.Snippet?.ResourceId?.VideoId ?? item.ContentDetails?.VideoId;

        /// <summary>
        /// The YouTube id a stored video is known by: its own id, or for a row saved before ids
        /// were stored, the id held by its link.
        /// </summary>
        private static string? GetStoredYouTubeId(Video video) =>
            VideoDuplicateFinder.NormalizeExternalId(video.ExternalId)
            ?? VideoDuplicateFinder.ExtractYouTubeId(video.Link);

        /// <summary>
        /// Finds the videos already in the library for the given YouTube ids, keyed by that id.
        /// A row saved before ids were stored is found through its link and given its id.
        /// </summary>
        private async Task<Dictionary<string, Video>> FindStoredVideosAsync(List<string> youTubeIds)
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
        private async Task<List<Video>> BuildNewVideosAsync(
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
        /// Helper method to check if a playlist item represents a deleted or private video
        /// </summary>
        private static bool IsDeletedOrPrivateVideo(YouTubePlaylistItemDto item)
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

        public async Task<bool> AddVideoToPlaylistAsync(Guid playlistId, Guid videoId, int? position = null)
        {
            var playlist = await GetPlaylistByIdAsync(playlistId);
            if (playlist == null)
                return false;

            var video = await _context.Videos.FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null)
                return false;

            // Check if already exists - we need to query through the playlist's navigation property
            var playlistWithVideos = await _context.YouTubePlaylists
                .Include(p => p.PlaylistVideos)
                .FirstOrDefaultAsync(p => p.Id == playlistId);
            
            if (playlistWithVideos == null)
                return false;

            var existing = playlistWithVideos.PlaylistVideos
                .FirstOrDefault(pv => pv.VideoId == videoId);

            if (existing != null)
                return true; // Already exists

            var playlistVideo = new YouTubePlaylistVideo
            {
                YouTubePlaylistId = playlistId,
                VideoId = videoId,
                Position = position
            };

            _context.Add(playlistVideo);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveVideoFromPlaylistAsync(Guid playlistId, Guid videoId)
        {
            var playlistWithVideos = await _context.YouTubePlaylists
                .Include(p => p.PlaylistVideos)
                .FirstOrDefaultAsync(p => p.Id == playlistId);
            
            if (playlistWithVideos == null)
                return false;

            var playlistVideo = playlistWithVideos.PlaylistVideos
                .FirstOrDefault(pv => pv.VideoId == videoId);

            if (playlistVideo == null)
                return false;

            _context.Remove(playlistVideo);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<YouTubePlaylist> SavePlaylistAsync(YouTubePlaylist playlist, bool updateIfExists = false)
        {
            var existingPlaylist = await _context.YouTubePlaylists
                .Include(p => p.Topics)
                .Include(p => p.Genres)
                .FirstOrDefaultAsync(p => p.PlaylistExternalId == playlist.PlaylistExternalId);

            if (existingPlaylist != null)
            {
                if (updateIfExists)
                {
                    // Update existing playlist
                    existingPlaylist.Title = playlist.Title;
                    existingPlaylist.Description = playlist.Description;
                    existingPlaylist.Link = playlist.Link;
                    existingPlaylist.Thumbnail = playlist.Thumbnail;
                    existingPlaylist.VideoCount = playlist.VideoCount;
                    existingPlaylist.PrivacyStatus = playlist.PrivacyStatus;
                    existingPlaylist.LastSyncedAt = DateTime.UtcNow;

                    _context.Update(existingPlaylist);
                    await _context.SaveChangesAsync();

                    return existingPlaylist;
                }
                else
                {
                    return existingPlaylist;
                }
            }

            // Topics and Genres will be handled via the navigation properties
            // EF Core will automatically track and manage these relationships
            try
            {
                _context.Add(playlist);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Another request saved the same playlist first: drop the rejected row from
                // tracking and hand back the one that won.
                _context.ClearChangeTracker();

                var winner = await GetPlaylistByExternalIdAsync(playlist.PlaylistExternalId);
                if (winner == null)
                {
                    throw;
                }

                _logger.LogInformation(ex, "Playlist {PlaylistId} was saved by another request first", playlist.PlaylistExternalId);
                return winner;
            }

            return playlist;
        }

        public async Task<bool> DeletePlaylistAsync(Guid id)
        {
            // Only a playlist id is accepted here; any other media item is left alone.
            if (!await _context.YouTubePlaylists.AnyAsync(p => p.Id == id))
                return false;

            // The shared delete detaches mixlists, topics, and genres, cleans up a stored
            // thumbnail, and removes the item from the search index. The playlist's videos
            // stay in the library; only their links to this playlist go.
            return await _mediaService.DeleteMediaItemAsync(id);
        }
    }
}

