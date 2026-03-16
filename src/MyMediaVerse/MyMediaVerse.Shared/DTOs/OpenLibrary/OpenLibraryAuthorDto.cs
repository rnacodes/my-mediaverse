using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.OpenLibrary
{
    public class OpenLibraryAuthorDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("personal_name")]
        public string? PersonalName { get; set; }

        [JsonPropertyName("birth_date")]
        public string? BirthDate { get; set; }

        [JsonPropertyName("death_date")]
        public string? DeathDate { get; set; }

        [JsonPropertyName("bio")]
        public object? Bio { get; set; } // Can be string or object with "value" property

        [JsonPropertyName("wikipedia")]
        public string? Wikipedia { get; set; }

        [JsonPropertyName("photos")]
        public int[]? Photos { get; set; }

        [JsonPropertyName("alternate_names")]
        public string[]? AlternateNames { get; set; }

        [JsonPropertyName("created")]
        public OpenLibraryTimestamp? Created { get; set; }

        [JsonPropertyName("last_modified")]
        public OpenLibraryTimestamp? LastModified { get; set; }

        [JsonPropertyName("latest_revision")]
        public int? LatestRevision { get; set; }

        [JsonPropertyName("revision")]
        public int? Revision { get; set; }

        [JsonPropertyName("type")]
        public OpenLibraryTypeReference? Type { get; set; }
    }
}
