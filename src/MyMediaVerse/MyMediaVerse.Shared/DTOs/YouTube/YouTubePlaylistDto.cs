using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.YouTube
{
    public class YouTubePlaylistDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubePlaylistSnippetDto? Snippet { get; set; }

        [JsonPropertyName("status")]
        public YouTubePlaylistStatusDto? Status { get; set; }

        [JsonPropertyName("contentDetails")]
        public YouTubePlaylistContentDetailsDto? ContentDetails { get; set; }
    }

    public class YouTubePlaylistSnippetDto
    {
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("channelId")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("thumbnails")]
        public YouTubeThumbnailsDto? Thumbnails { get; set; }

        [JsonPropertyName("channelTitle")]
        public string? ChannelTitle { get; set; }

        [JsonPropertyName("defaultLanguage")]
        public string? DefaultLanguage { get; set; }
    }

    public class YouTubePlaylistStatusDto
    {
        [JsonPropertyName("privacyStatus")]
        public string? PrivacyStatus { get; set; }
    }

    public class YouTubePlaylistContentDetailsDto
    {
        [JsonPropertyName("itemCount")]
        public int? ItemCount { get; set; }
    }

    public class YouTubePlaylistItemDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubePlaylistItemSnippetDto? Snippet { get; set; }

        [JsonPropertyName("contentDetails")]
        public YouTubePlaylistItemContentDetailsDto? ContentDetails { get; set; }
    }

    public class YouTubePlaylistItemSnippetDto
    {
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("channelId")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("thumbnails")]
        public YouTubeThumbnailsDto? Thumbnails { get; set; }

        [JsonPropertyName("channelTitle")]
        public string? ChannelTitle { get; set; }

        [JsonPropertyName("playlistId")]
        public string? PlaylistId { get; set; }

        [JsonPropertyName("position")]
        public int? Position { get; set; }

        [JsonPropertyName("resourceId")]
        public YouTubeResourceIdDto? ResourceId { get; set; }
    }

    public class YouTubePlaylistItemContentDetailsDto
    {
        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }

        [JsonPropertyName("startAt")]
        public string? StartAt { get; set; }

        [JsonPropertyName("endAt")]
        public string? EndAt { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("videoPublishedAt")]
        public DateTime? VideoPublishedAt { get; set; }
    }

    public class YouTubeResourceIdDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }
    }
}
