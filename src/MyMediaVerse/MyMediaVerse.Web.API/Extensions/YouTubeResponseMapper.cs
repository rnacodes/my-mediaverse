using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Web.API.Extensions;

/// <summary>
/// Maps stored videos, channels and playlists to their response DTOs. Shared by the video,
/// channel, playlist and YouTube import controllers so each kind has one response shape.
/// </summary>
public static class YouTubeResponseMapper
{
    public static VideoResponseDto ToResponseDto(this Video video)
    {
        return new VideoResponseDto
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description,
            MediaType = video.MediaType,
            Status = video.Status,
            DateAdded = video.DateAdded,
            Link = video.Link,
            Thumbnail = video.GetEffectiveThumbnail(),
            Platform = video.Platform,
            ChannelId = video.ChannelId,
            Channel = video.Channel != null ? new YouTubeChannelInfoDto
            {
                Id = video.Channel.Id,
                Title = video.Channel.Title,
                Thumbnail = video.Channel.Thumbnail,
                ChannelExternalId = video.Channel.ChannelExternalId,
                CustomUrl = video.Channel.CustomUrl,
                SubscriberCount = video.Channel.SubscriberCount
            } : null,
            LengthInSeconds = video.LengthInSeconds,
            ExternalId = video.ExternalId,
            PublishedAt = video.PublishedAt,
            Rating = video.Rating,
            DateCompleted = video.DateCompleted,
            Notes = video.Notes,
            RelatedNotes = video.RelatedNotes,
            Topics = video.Topics.Select(t => t.Name).ToArray(),
            Genres = video.Genres.Select(g => g.Name).ToArray()
        };
    }

    public static YouTubeChannelResponseDto ToResponseDto(this YouTubeChannel channel)
    {
        return new YouTubeChannelResponseDto
        {
            Id = channel.Id,
            Title = channel.Title,
            Description = channel.Description,
            Link = channel.Link,
            Thumbnail = channel.Thumbnail,
            ChannelExternalId = channel.ChannelExternalId,
            CustomUrl = channel.CustomUrl,
            SubscriberCount = channel.SubscriberCount,
            VideoCount = channel.VideoCount,
            ViewCount = channel.ViewCount,
            UploadsPlaylistId = channel.UploadsPlaylistId,
            Country = channel.Country,
            PublishedAt = channel.PublishedAt,
            LastSyncedAt = channel.LastSyncedAt,
            MediaType = channel.MediaType,
            Status = channel.Status,
            DateAdded = channel.DateAdded,
            DateCompleted = channel.DateCompleted,
            Rating = channel.Rating,
            Notes = channel.Notes,
            RelatedNotes = channel.RelatedNotes,
            Topics = channel.Topics.Select(t => t.Name).ToArray(),
            Genres = channel.Genres.Select(g => g.Name).ToArray(),
            MixlistIds = channel.Mixlists.Select(m => m.Id).ToArray(),
            VideoCountInDb = channel.Videos.Count
        };
    }

    public static YouTubePlaylistResponseDto ToResponseDto(this YouTubePlaylist playlist, bool includeVideos = false)
    {
        var dto = new YouTubePlaylistResponseDto
        {
            Id = playlist.Id,
            Title = playlist.Title,
            Description = playlist.Description,
            Link = playlist.Link,
            Thumbnail = playlist.Thumbnail,
            PlaylistExternalId = playlist.PlaylistExternalId,
            VideoCount = playlist.VideoCount,
            PublishedAt = playlist.PublishedAt,
            LastSyncedAt = playlist.LastSyncedAt,
            PrivacyStatus = playlist.PrivacyStatus,
            MediaType = playlist.MediaType,
            Status = playlist.Status,
            DateAdded = playlist.DateAdded,
            Rating = playlist.Rating,
            Notes = playlist.Notes,
            Topics = playlist.Topics?.Select(t => t.Name).ToList() ?? new List<string>(),
            Genres = playlist.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
            MixlistIds = playlist.Mixlists?.Select(m => m.Id).ToArray() ?? Array.Empty<Guid>()
        };

        if (includeVideos && playlist.PlaylistVideos != null)
        {
            dto.Videos = playlist.PlaylistVideos
                .OrderBy(pv => pv.Position)
                .Select(pv => new VideoInfoDto
                {
                    Id = pv.Video.Id,
                    Title = pv.Video.Title,
                    Thumbnail = pv.Video.GetEffectiveThumbnail(),
                    LengthInSeconds = pv.Video.LengthInSeconds,
                    Position = pv.Position,
                    ExternalId = pv.Video.ExternalId
                })
                .ToList();
        }

        return dto;
    }

    /// <summary>
    /// The response DTO for whatever an import produced. Each DTO carries its
    /// <c>mediaType</c>, which tells the caller which of the three it received.
    /// </summary>
    public static object ToImportResponseDto(this BaseMediaItem item) => item switch
    {
        Video video => video.ToResponseDto(),
        YouTubeChannel channel => channel.ToResponseDto(),
        YouTubePlaylist playlist => playlist.ToResponseDto(),
        _ => throw new InvalidOperationException($"An import produced an unexpected item type: {item.GetType().Name}")
    };
}
