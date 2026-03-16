using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.YouTube
{
    public class YouTubeChannelDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("snippet")]
        public YouTubeChannelSnippetDto? Snippet { get; set; }

        [JsonPropertyName("contentDetails")]
        public YouTubeChannelContentDetailsDto? ContentDetails { get; set; }

        [JsonPropertyName("statistics")]
        public YouTubeChannelStatisticsDto? Statistics { get; set; }

        [JsonPropertyName("brandingSettings")]
        public YouTubeChannelBrandingSettingsDto? BrandingSettings { get; set; }
    }

    public class YouTubeChannelSnippetDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("customUrl")]
        public string? CustomUrl { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("thumbnails")]
        public YouTubeThumbnailsDto? Thumbnails { get; set; }

        [JsonPropertyName("defaultLanguage")]
        public string? DefaultLanguage { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }

    public class YouTubeChannelContentDetailsDto
    {
        [JsonPropertyName("relatedPlaylists")]
        public YouTubeRelatedPlaylistsDto? RelatedPlaylists { get; set; }
    }

    public class YouTubeRelatedPlaylistsDto
    {
        [JsonPropertyName("likes")]
        public string? Likes { get; set; }

        [JsonPropertyName("favorites")]
        public string? Favorites { get; set; }

        [JsonPropertyName("uploads")]
        public string? Uploads { get; set; }

        [JsonPropertyName("watchHistory")]
        public string? WatchHistory { get; set; }

        [JsonPropertyName("watchLater")]
        public string? WatchLater { get; set; }
    }

    public class YouTubeChannelStatisticsDto
    {
        [JsonPropertyName("viewCount")]
        public string? ViewCount { get; set; }

        [JsonPropertyName("subscriberCount")]
        public string? SubscriberCount { get; set; }

        [JsonPropertyName("hiddenSubscriberCount")]
        public bool? HiddenSubscriberCount { get; set; }

        [JsonPropertyName("videoCount")]
        public string? VideoCount { get; set; }
    }

    public class YouTubeChannelBrandingSettingsDto
    {
        [JsonPropertyName("channel")]
        public YouTubeChannelBrandingChannelDto? Channel { get; set; }

        [JsonPropertyName("image")]
        public YouTubeChannelBrandingImageDto? Image { get; set; }
    }

    public class YouTubeChannelBrandingChannelDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("keywords")]
        public string? Keywords { get; set; }

        [JsonPropertyName("defaultLanguage")]
        public string? DefaultLanguage { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }

    public class YouTubeChannelBrandingImageDto
    {
        [JsonPropertyName("bannerImageUrl")]
        public string? BannerImageUrl { get; set; }

        [JsonPropertyName("bannerMobileImageUrl")]
        public string? BannerMobileImageUrl { get; set; }

        [JsonPropertyName("bannerTabletLowImageUrl")]
        public string? BannerTabletLowImageUrl { get; set; }

        [JsonPropertyName("bannerTabletImageUrl")]
        public string? BannerTabletImageUrl { get; set; }

        [JsonPropertyName("bannerTabletHdImageUrl")]
        public string? BannerTabletHdImageUrl { get; set; }

        [JsonPropertyName("bannerTabletExtraHdImageUrl")]
        public string? BannerTabletExtraHdImageUrl { get; set; }

        [JsonPropertyName("bannerMobileLowImageUrl")]
        public string? BannerMobileLowImageUrl { get; set; }

        [JsonPropertyName("bannerMobileMediumHdImageUrl")]
        public string? BannerMobileMediumHdImageUrl { get; set; }

        [JsonPropertyName("bannerMobileHdImageUrl")]
        public string? BannerMobileHdImageUrl { get; set; }

        [JsonPropertyName("bannerMobileExtraHdImageUrl")]
        public string? BannerMobileExtraHdImageUrl { get; set; }

        [JsonPropertyName("bannerTvImageUrl")]
        public string? BannerTvImageUrl { get; set; }

        [JsonPropertyName("bannerTvLowImageUrl")]
        public string? BannerTvLowImageUrl { get; set; }

        [JsonPropertyName("bannerTvMediumImageUrl")]
        public string? BannerTvMediumImageUrl { get; set; }

        [JsonPropertyName("bannerTvHighImageUrl")]
        public string? BannerTvHighImageUrl { get; set; }
    }
}
