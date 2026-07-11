using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.Itunes
{
    /// <summary>
    /// Response envelope from Apple's free iTunes Lookup API (https://itunes.apple.com/lookup).
    /// Field names preserve Apple's external casing.
    /// </summary>
    public class ItunesLookupResponseDto
    {
        [JsonPropertyName("resultCount")]
        public int ResultCount { get; set; }

        [JsonPropertyName("results")]
        public List<ItunesPodcastDto> Results { get; set; } = new();
    }

    /// <summary>
    /// A single podcast entry from the iTunes Lookup API.
    /// </summary>
    public class ItunesPodcastDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("collectionId")]
        public long CollectionId { get; set; }

        [JsonPropertyName("collectionName")]
        public string? CollectionName { get; set; }

        [JsonPropertyName("artistName")]
        public string? ArtistName { get; set; }

        [JsonPropertyName("feedUrl")]
        public string? FeedUrl { get; set; }

        [JsonPropertyName("artworkUrl600")]
        public string? ArtworkUrl600 { get; set; }

        [JsonPropertyName("trackCount")]
        public int? TrackCount { get; set; }

        [JsonPropertyName("collectionViewUrl")]
        public string? CollectionViewUrl { get; set; }

        [JsonPropertyName("primaryGenreName")]
        public string? PrimaryGenreName { get; set; }
    }
}
