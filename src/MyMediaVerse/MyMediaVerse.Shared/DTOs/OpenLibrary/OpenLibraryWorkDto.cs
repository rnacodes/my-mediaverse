using System.Text.Json.Serialization;

namespace MyMediaVerse.Shared.DTOs.OpenLibrary
{
    public class OpenLibraryWorkDto
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("authors")]
        public OpenLibraryAuthorReference[]? Authors { get; set; }

        [JsonPropertyName("description")]
        public object? Description { get; set; } // Can be string or object with "value" property

        [JsonPropertyName("subjects")]
        public string[]? Subjects { get; set; }

        [JsonPropertyName("subject_places")]
        public string[]? SubjectPlaces { get; set; }

        [JsonPropertyName("subject_times")]
        public string[]? SubjectTimes { get; set; }

        [JsonPropertyName("subject_people")]
        public string[]? SubjectPeople { get; set; }

        [JsonPropertyName("covers")]
        public int[]? Covers { get; set; }

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
