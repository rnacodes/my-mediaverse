using MyMediaVerse.Domain.Entities;
using System.Text.Json.Serialization;

namespace MyMediaVerse.DTOs
{
    public class BookResponseDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("mediaType")]
        public MediaType MediaType { get; set; }
        
        [JsonPropertyName("status")]
        public Status Status { get; set; }
        
        [JsonPropertyName("dateAdded")]
        public DateTime DateAdded { get; set; }
        
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        
        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;
        
        [JsonPropertyName("isbn")]
        public string? ISBN { get; set; }
        
        [JsonPropertyName("asin")]
        public string? ASIN { get; set; }
        
        [JsonPropertyName("format")]
        public BookFormat Format { get; set; }
        
        [JsonPropertyName("partOfSeries")]
        public bool PartOfSeries { get; set; }
        
        [JsonPropertyName("rating")]
        public Rating? Rating { get; set; }
        
        [JsonPropertyName("ownershipStatus")]
        public OwnershipStatus? OwnershipStatus { get; set; }
        
        [JsonPropertyName("dateCompleted")]
        public DateTime? DateCompleted { get; set; }
        
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
        
        [JsonPropertyName("relatedNotes")]
        public string? RelatedNotes { get; set; }
        
        [JsonPropertyName("topics")]
        public string[] Topics { get; set; } = Array.Empty<string>();
        
        [JsonPropertyName("genres")]
        public string[] Genres { get; set; } = Array.Empty<string>();
        
        [JsonPropertyName("goodreadsRating")]
        public decimal? GoodreadsRating { get; set; }

        [JsonPropertyName("averageRating")]
        public decimal? AverageRating { get; set; }

        [JsonPropertyName("yearPublished")]
        public int? YearPublished { get; set; }

        [JsonPropertyName("originalPublicationYear")]
        public int? OriginalPublicationYear { get; set; }

        [JsonPropertyName("dateRead")]
        public DateTime? DateRead { get; set; }

        [JsonPropertyName("myReview")]
        public string? MyReview { get; set; }

        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("goodreadsTags")]
        public List<string> GoodreadsTags { get; set; } = new();

        // External identities and the enrichment stamp. Read-only on the wire: imports and
        // enrichment fill these in, and they are never edited through the book form.

        [JsonPropertyName("readwiseBookId")]
        public int? ReadwiseBookId { get; set; }

        [JsonPropertyName("goodreadsBookId")]
        public long? GoodreadsBookId { get; set; }

        [JsonPropertyName("googleVolumeId")]
        public string? GoogleVolumeId { get; set; }

        [JsonPropertyName("openLibraryKey")]
        public string? OpenLibraryKey { get; set; }

        [JsonPropertyName("enrichedAt")]
        public DateTime? EnrichedAt { get; set; }
    }
}
