using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.GoogleBooks
{
    public class GoogleBooksImageLinksDto
    {
        [JsonPropertyName("smallThumbnail")]
        public string? SmallThumbnail { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("small")]
        public string? Small { get; set; }

        [JsonPropertyName("medium")]
        public string? Medium { get; set; }

        [JsonPropertyName("large")]
        public string? Large { get; set; }

        [JsonPropertyName("extraLarge")]
        public string? ExtraLarge { get; set; }

        /// <summary>
        /// Gets the best available thumbnail URL, preferring larger sizes.
        /// Converts HTTP to HTTPS for security.
        /// </summary>
        public string? GetBestThumbnail()
        {
            var url = ExtraLarge ?? Large ?? Medium ?? Thumbnail ?? Small ?? SmallThumbnail;
            if (string.IsNullOrEmpty(url)) return null;

            // Convert HTTP to HTTPS
            return url.Replace("http://", "https://");
        }
    }
}
