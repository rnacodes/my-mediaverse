using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.YouTube
{
    public class YouTubeSearchResultDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }

        [JsonPropertyName("prevPageToken")]
        public string? PrevPageToken { get; set; }

        [JsonPropertyName("regionCode")]
        public string? RegionCode { get; set; }

        [JsonPropertyName("pageInfo")]
        public YouTubePageInfoDto? PageInfo { get; set; }

        [JsonPropertyName("items")]
        public List<YouTubeSearchItemDto>? Items { get; set; }
    }

    public class YouTubePageInfoDto
    {
        [JsonPropertyName("totalResults")]
        public int? TotalResults { get; set; }

        [JsonPropertyName("resultsPerPage")]
        public int? ResultsPerPage { get; set; }
    }

    public class YouTubeSearchItemDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("id")]
        public YouTubeSearchItemIdDto? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubeSearchItemSnippetDto? Snippet { get; set; }
    }

    public class YouTubeSearchItemIdDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }

        [JsonPropertyName("channelId")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("playlistId")]
        public string? PlaylistId { get; set; }
    }

    public class YouTubeSearchItemSnippetDto
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

        [JsonPropertyName("liveBroadcastContent")]
        public string? LiveBroadcastContent { get; set; }

        [JsonPropertyName("publishTime")]
        public DateTime? PublishTime { get; set; }
    }

    // Response DTOs for API endpoints
    public class YouTubeVideoListResponseDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("items")]
        public List<YouTubeVideoDto>? Items { get; set; }

        [JsonPropertyName("pageInfo")]
        public YouTubePageInfoDto? PageInfo { get; set; }
    }

    public class YouTubePlaylistListResponseDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }

        [JsonPropertyName("prevPageToken")]
        public string? PrevPageToken { get; set; }

        [JsonPropertyName("items")]
        public List<YouTubePlaylistDto>? Items { get; set; }

        [JsonPropertyName("pageInfo")]
        public YouTubePageInfoDto? PageInfo { get; set; }
    }

    public class YouTubePlaylistItemListResponseDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }

        [JsonPropertyName("prevPageToken")]
        public string? PrevPageToken { get; set; }

        [JsonPropertyName("items")]
        public List<YouTubePlaylistItemDto>? Items { get; set; }

        [JsonPropertyName("pageInfo")]
        public YouTubePageInfoDto? PageInfo { get; set; }
    }

    public class YouTubeChannelListResponseDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("items")]
        public List<YouTubeChannelDto>? Items { get; set; }

        [JsonPropertyName("pageInfo")]
        public YouTubePageInfoDto? PageInfo { get; set; }
    }
}
