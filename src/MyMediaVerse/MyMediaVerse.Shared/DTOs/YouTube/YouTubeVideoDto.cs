using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.YouTube
{
    public class YouTubeVideoDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubeVideoSnippetDto? Snippet { get; set; }

        [JsonPropertyName("contentDetails")]
        public YouTubeVideoContentDetailsDto? ContentDetails { get; set; }

        [JsonPropertyName("statistics")]
        public YouTubeVideoStatisticsDto? Statistics { get; set; }

        [JsonPropertyName("status")]
        public YouTubeVideoStatusDto? Status { get; set; }
    }

    public class YouTubeVideoSnippetDto
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

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("categoryId")]
        public string? CategoryId { get; set; }

        [JsonPropertyName("liveBroadcastContent")]
        public string? LiveBroadcastContent { get; set; }

        [JsonPropertyName("defaultLanguage")]
        public string? DefaultLanguage { get; set; }

        [JsonPropertyName("defaultAudioLanguage")]
        public string? DefaultAudioLanguage { get; set; }
    }

    public class YouTubeVideoContentDetailsDto
    {
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        [JsonPropertyName("dimension")]
        public string? Dimension { get; set; }

        [JsonPropertyName("definition")]
        public string? Definition { get; set; }

        [JsonPropertyName("caption")]
        public string? Caption { get; set; }

        [JsonPropertyName("licensedContent")]
        public bool? LicensedContent { get; set; }

        [JsonPropertyName("projection")]
        public string? Projection { get; set; }
    }

    public class YouTubeVideoStatisticsDto
    {
        [JsonPropertyName("viewCount")]
        public string? ViewCount { get; set; }

        [JsonPropertyName("likeCount")]
        public string? LikeCount { get; set; }

        [JsonPropertyName("favoriteCount")]
        public string? FavoriteCount { get; set; }

        [JsonPropertyName("commentCount")]
        public string? CommentCount { get; set; }
    }

    public class YouTubeVideoStatusDto
    {
        [JsonPropertyName("uploadStatus")]
        public string? UploadStatus { get; set; }

        [JsonPropertyName("privacyStatus")]
        public string? PrivacyStatus { get; set; }

        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("embeddable")]
        public bool? Embeddable { get; set; }

        [JsonPropertyName("publicStatsViewable")]
        public bool? PublicStatsViewable { get; set; }
    }

    public class YouTubeThumbnailsDto
    {
        [JsonPropertyName("default")]
        public YouTubeThumbnailDto? Default { get; set; }

        [JsonPropertyName("medium")]
        public YouTubeThumbnailDto? Medium { get; set; }

        [JsonPropertyName("high")]
        public YouTubeThumbnailDto? High { get; set; }

        [JsonPropertyName("standard")]
        public YouTubeThumbnailDto? Standard { get; set; }

        [JsonPropertyName("maxres")]
        public YouTubeThumbnailDto? Maxres { get; set; }
    }

    public class YouTubeThumbnailDto
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }
    }
}
