using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.GoogleBooks
{
    public class GoogleBooksSaleInfoDto
    {
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("saleability")]
        public string? Saleability { get; set; }  // "FOR_SALE", "FREE", "NOT_FOR_SALE"

        [JsonPropertyName("isEbook")]
        public bool? IsEbook { get; set; }

        [JsonPropertyName("buyLink")]
        public string? BuyLink { get; set; }
    }
}
