using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.GoogleBooks
{
    public class GoogleBooksVolumeDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }  // Google Books Volume ID

        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        [JsonPropertyName("selfLink")]
        public string? SelfLink { get; set; }

        [JsonPropertyName("volumeInfo")]
        public GoogleBooksVolumeInfoDto? VolumeInfo { get; set; }

        [JsonPropertyName("saleInfo")]
        public GoogleBooksSaleInfoDto? SaleInfo { get; set; }
    }
}
