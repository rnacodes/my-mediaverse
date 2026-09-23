using System.Text.Json.Serialization;

namespace MyMediaVerse.Infrastructure.Models
{
    /// <summary>
    /// Typesense document model for media items.
    /// This mirrors the schema defined in Typesense and represents how data is indexed.
    /// All fields must match the collection schema exactly.
    /// </summary>
    public class MediaItemDocument
    {
        /// <summary>
        /// Document ID in Typesense (matches PostgreSQL MediaItems.Id)
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// Title of the media item (searchable, sortable)
        /// </summary>
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        /// <summary>
        /// Type of media: Article, Book, Movie, TVShow, Video, Podcast, Website, Channel, Playlist
        /// Facetable for filtering by type
        /// </summary>
        [JsonPropertyName("media_type")]
        public required string MediaType { get; set; }

        /// <summary>
        /// Description or summary of the media item (searchable)
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// List of topic names (facetable for filtering)
        /// </summary>
        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new List<string>();

        /// <summary>
        /// List of genre names (facetable for filtering)
        /// </summary>
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new List<string>();

        /// <summary>
        /// When the item was added to the library (Unix timestamp in seconds)
        /// Used for sorting by recency
        /// </summary>
        [JsonPropertyName("date_added")]
        public long DateAdded { get; set; }

        /// <summary>
        /// Current status: Uncharted, ActivelyExploring, Completed, Abandoned
        /// Facetable for filtering by status
        /// </summary>
        [JsonPropertyName("status")]
        public required string Status { get; set; }

        /// <summary>
        /// Optional user rating: SuperLike, Like, Neutral, Dislike
        /// Facetable for filtering by rating
        /// </summary>
        [JsonPropertyName("rating")]
        public string? Rating { get; set; }

        /// <summary>
        /// Thumbnail image URL (not searchable, just for display)
        /// </summary>
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        /// <summary>
        /// Author name (for Books and Articles) - searchable and facetable
        /// </summary>
        [JsonPropertyName("author")]
        public string? Author { get; set; }

        /// <summary>
        /// Director name (for Movies) - searchable and facetable
        /// </summary>
        [JsonPropertyName("director")]
        public string? Director { get; set; }

        /// <summary>
        /// Creator name (for TV Shows) - searchable and facetable
        /// </summary>
        [JsonPropertyName("creator")]
        public string? Creator { get; set; }

        /// <summary>
        /// Publisher/Host (for Podcasts) - searchable and facetable
        /// </summary>
        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        /// <summary>
        /// Release year (for Movies and TV Shows) - facetable for filtering
        /// </summary>
        [JsonPropertyName("release_year")]
        public int? ReleaseYear { get; set; }

        /// <summary>
        /// Platform (for Videos, e.g., YouTube, Vimeo) - facetable
        /// </summary>
        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        /// <summary>
        /// Series ID (for Podcast Episodes) - used to distinguish episodes from series in routing
        /// </summary>
        [JsonPropertyName("series_id")]
        public string? SeriesId { get; set; }

        /// <summary>
        /// ISBN-13 (for Books) - exact-match searchable so a scanned or pasted ISBN finds the book
        /// </summary>
        [JsonPropertyName("isbn")]
        public string? Isbn { get; set; }

        /// <summary>
        /// The user's Goodreads star rating, 1-5 (for Books) - facetable, shown on list and card views
        /// </summary>
        [JsonPropertyName("goodreads_rating")]
        public double? GoodreadsRating { get; set; }

        /// <summary>
        /// Site domain (for Websites, e.g. "theverge.com") - facetable, groups bookmarks by source
        /// </summary>
        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        /// <summary>
        /// Whether the website has a known RSS feed (for Websites) - facetable
        /// </summary>
        [JsonPropertyName("has_rss")]
        public bool? HasRss { get; set; }

        /// <summary>
        /// HTTP status from the last link check (for Websites; 0 = unreachable) - lets lists flag broken links
        /// </summary>
        [JsonPropertyName("link_status")]
        public int? LinkStatus { get; set; }

        /// <summary>
        /// "Series" or "Episode" (for Podcasts) - both are media items of type Podcast, so this is what
        /// separates a show from its episodes in search
        /// </summary>
        [JsonPropertyName("podcast_type")]
        public string? PodcastType { get; set; }

        /// <summary>
        /// Title of the parent show (for podcast episodes) - makes a search for the show return its episodes
        /// </summary>
        [JsonPropertyName("series_title")]
        public string? SeriesTitle { get; set; }

        /// <summary>
        /// Whether the show is subscribed (for podcast series) - facetable, so lists can show only followed shows
        /// </summary>
        [JsonPropertyName("is_subscribed")]
        public bool? IsSubscribed { get; set; }

        /// <summary>
        /// Where a podcast series' stored metadata came from (rss, apple, podcastindex, ...) - drives the
        /// attribution shown beside it
        /// </summary>
        [JsonPropertyName("metadata_source")]
        public string? MetadataSource { get; set; }

        /// <summary>
        /// "Show" or "Episode" (for TV) - both are media items of type TVShow, so this is what separates
        /// a show from its episodes in search
        /// </summary>
        [JsonPropertyName("tv_type")]
        public string? TvType { get; set; }

        /// <summary>
        /// Parent show ID (for TV episodes) - used to list one show's episodes and route to the show
        /// </summary>
        [JsonPropertyName("show_id")]
        public string? ShowId { get; set; }

        /// <summary>
        /// Title of the parent show (for TV episodes) - makes a search for the show return its episodes
        /// </summary>
        [JsonPropertyName("show_title")]
        public string? ShowTitle { get; set; }

        /// <summary>
        /// Season number (for TV episodes) - orders an episode list; season 0 holds specials
        /// </summary>
        [JsonPropertyName("season_number")]
        public int? SeasonNumber { get; set; }

        /// <summary>
        /// Episode number within its season (for TV episodes)
        /// </summary>
        [JsonPropertyName("episode_number")]
        public int? EpisodeNumber { get; set; }

        /// <summary>
        /// Text composed for semantic embedding. Typesense auto-embeds this via the collection's
        /// embedding field, so keyword and vector search stay sourced from one place. Serialized
        /// on write; ignored when search hits are deserialized back (no setter).
        /// Topics/genres are sorted so an unchanged item always produces a byte-identical string;
        /// that lets Typesense skip re-embedding (and the paid embedding call) when nothing changed.
        /// </summary>
        [JsonPropertyName("embedding_source")]
        public string EmbeddingSource => string.Join("\n", new[]
        {
            Title,
            MediaType,
            Description,
            Topics.Count > 0 ? string.Join(", ", Topics.OrderBy(t => t, StringComparer.Ordinal)) : null,
            Genres.Count > 0 ? string.Join(", ", Genres.OrderBy(g => g, StringComparer.Ordinal)) : null,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
