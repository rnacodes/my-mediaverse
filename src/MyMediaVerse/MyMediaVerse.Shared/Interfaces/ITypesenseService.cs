using MyMediaVerse.Shared.DTOs.Search;

namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Service interface for Typesense search integration.
    /// Manages document indexing and search operations for the media_items collection.
    /// </summary>
    public interface ITypesenseService
    {
        /// <summary>
        /// True when Typesense auto-embedding is configured (an OpenAI key is present), meaning
        /// semantic search and recommendation features are available.
        /// </summary>
        bool IsAutoEmbeddingEnabled { get; }
        /// <summary>
        /// Ensures the media_items collection exists in Typesense.
        /// Creates the collection if it doesn't exist, or skips if it does.
        /// Should be called once during application startup.
        /// </summary>
        Task EnsureCollectionExistsAsync();

        /// <summary>
        /// Indexes or updates a media item document in Typesense.
        /// Uses upsert operation - creates if new, updates if exists.
        /// </summary>
        /// <param name="id">The unique ID of the media item (from PostgreSQL)</param>
        /// <param name="title">The title of the media item</param>
        /// <param name="mediaType">The type of media (Article, Book, Movie, etc.)</param>
        /// <param name="description">Optional description text</param>
        /// <param name="topics">List of topic names associated with the item</param>
        /// <param name="genres">List of genre names associated with the item</param>
        /// <param name="dateAdded">When the item was added to the library</param>
        /// <param name="status">Current status (Uncharted, ActivelyExploring, etc.)</param>
        /// <param name="rating">Optional rating (SuperLike, Like, etc.)</param>
        /// <param name="thumbnail">Optional thumbnail URL</param>
        /// <param name="additionalFields">Optional dictionary for media-specific fields (e.g., author, director, publisher)</param>
        Task IndexMediaItemAsync(
            Guid id,
            string title,
            string mediaType,
            string? description,
            List<string> topics,
            List<string> genres,
            DateTime dateAdded,
            string status,
            string? rating,
            string? thumbnail,
            Dictionary<string, object>? additionalFields = null);

        /// <summary>
        /// Deletes a media item document from Typesense.
        /// </summary>
        /// <param name="id">The unique ID of the media item to delete</param>
        Task DeleteMediaItemAsync(Guid id);

        /// <summary>
        /// Performs a search across the media_items collection.
        /// </summary>
        /// <param name="query">The search query text</param>
        /// <param name="filters">Optional filter by string (e.g., "mediaType:=Book")</param>
        /// <param name="perPage">Number of results per page (default 20)</param>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="sortBy">
        /// Optional Typesense sort_by expression (e.g. "date_added:desc"). Only expressions
        /// against sortable fields are honored; unrecognized values fall back to the default
        /// relevance-then-recency sort.
        /// </param>
        /// <returns>Search results as dynamic objects</returns>
        Task<object> SearchAsync(string query, string? filters = null, int perPage = 20, int page = 1, string? sortBy = null);

        /// <summary>
        /// Performs a bulk re-index of all media items from the database.
        /// Useful for initial setup or full data synchronization.
        /// </summary>
        Task<int> BulkReindexAllMediaItemsAsync();

        /// <summary>
        /// Re-indexes a single media item by ID.
        /// Loads the entity (and any media-type-specific fields) and upserts the Typesense document.
        /// Returns false if no media item with the given ID exists.
        /// </summary>
        Task<bool> ReindexMediaItemByIdAsync(Guid id);

        /// <summary>
        /// Ensures the mixlists collection exists in Typesense.
        /// Creates the collection if it doesn't exist, or skips if it does.
        /// Should be called during application startup.
        /// </summary>
        Task EnsureMixlistCollectionExistsAsync();

        /// <summary>
        /// Indexes or updates a mixlist document in Typesense.
        /// Uses upsert operation - creates if new, updates if exists.
        /// </summary>
        /// <param name="id">The unique ID of the mixlist</param>
        /// <param name="name">The name of the mixlist</param>
        /// <param name="description">Optional description</param>
        /// <param name="thumbnail">Optional thumbnail URL</param>
        /// <param name="dateCreated">When the mixlist was created</param>
        /// <param name="mediaItemTitles">List of titles of media items in the mixlist</param>
        /// <param name="topics">Aggregated topics from contained media items</param>
        /// <param name="genres">Aggregated genres from contained media items</param>
        Task IndexMixlistAsync(
            Guid id,
            string name,
            string? description,
            string? thumbnail,
            DateTime dateCreated,
            List<string> mediaItemTitles,
            List<string> topics,
            List<string> genres);

        /// <summary>
        /// Deletes a mixlist document from Typesense.
        /// </summary>
        /// <param name="id">The unique ID of the mixlist to delete</param>
        Task DeleteMixlistAsync(Guid id);

        /// <summary>
        /// Performs a search across the mixlists collection.
        /// </summary>
        /// <param name="query">The search query text</param>
        /// <param name="filters">Optional filter string (e.g., "topics:=productivity")</param>
        /// <param name="perPage">Number of results per page (default 20)</param>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="sortBy">
        /// Optional Typesense sort_by expression (e.g. "date_created:desc"). Only expressions
        /// against sortable fields are honored; unrecognized values fall back to the default
        /// relevance-then-recency sort.
        /// </param>
        /// <returns>Search results as dynamic objects</returns>
        Task<object> SearchMixlistsAsync(string query, string? filters = null, int perPage = 20, int page = 1, string? sortBy = null);

        /// <summary>
        /// Performs a bulk re-index of all mixlists from the database.
        /// Useful for initial setup or full data synchronization.
        /// </summary>
        Task<int> BulkReindexAllMixlistsAsync();

        /// <summary>
        /// Re-indexes a single mixlist by ID, including aggregated topics/genres from its media items.
        /// Returns false if no mixlist with the given ID exists.
        /// </summary>
        Task<bool> ReindexMixlistByIdAsync(Guid id);

        /// <summary>
        /// Deletes and recreates the media_items collection, clearing all indexed data.
        /// Use this to completely reset the search index.
        /// </summary>
        Task ResetMediaItemsCollectionAsync();

        /// <summary>
        /// Deletes and recreates the mixlists collection, clearing all indexed data.
        /// Use this to completely reset the mixlist search index.
        /// </summary>
        Task ResetMixlistsCollectionAsync();

        // ============================================
        // Obsidian Notes collection methods
        // ============================================

        /// <summary>
        /// Ensures the obsidian_notes collection exists in Typesense.
        /// Creates the collection if it doesn't exist, or skips if it does.
        /// Should be called during application startup.
        /// </summary>
        Task EnsureNotesCollectionExistsAsync();

        /// <summary>
        /// Indexes or updates an Obsidian note document in Typesense.
        /// Uses upsert operation - creates if new, updates if exists.
        /// </summary>
        Task IndexNoteAsync(
            Guid id,
            string slug,
            string title,
            string? content,
            string? description,
            string vaultName,
            string? sourceUrl,
            List<string> tags,
            DateTime dateImported,
            DateTime? noteDate,
            int linkedMediaCount);

        /// <summary>
        /// Deletes a note document from Typesense.
        /// </summary>
        Task DeleteNoteAsync(Guid id);

        /// <summary>
        /// Performs a search across the obsidian_notes collection.
        /// </summary>
        Task<object> SearchNotesAsync(string query, string? filters = null, int perPage = 20, int page = 1, string? sortBy = null);

        /// <summary>
        /// Performs a bulk re-index of all notes from the database.
        /// </summary>
        Task<int> BulkReindexAllNotesAsync();

        /// <summary>
        /// Re-indexes a single note by ID.
        /// Returns false if no note with the given ID exists.
        /// </summary>
        Task<bool> ReindexNoteByIdAsync(Guid id);

        /// <summary>
        /// Deletes and recreates the obsidian_notes collection.
        /// </summary>
        Task ResetNotesCollectionAsync();

        /// <summary>
        /// Performs a multi-search across media_items, mixlists, and obsidian_notes collections.
        /// Returns unified search results from all collections.
        /// </summary>
        Task<object> MultiSearchAsync(string query, string? filters = null, int perPage = 20, int page = 1);

        // ============================================
        // Hybrid/Semantic Search methods
        // ============================================

        /// <summary>
        /// Performs a hybrid search (keyword + semantic) across the media_items collection.
        /// Uses Typesense's vector search with rank fusion.
        /// </summary>
        /// <param name="query">The search query text</param>
        /// <param name="filters">Optional filter string (e.g., "media_type:=Book")</param>
        /// <param name="alpha">Balance between keyword (0) and vector (1) search. Default 0.5</param>
        /// <param name="perPage">Number of results per page (default 20)</param>
        /// <param name="page">Page number (default 1)</param>
        /// <returns>Search results with hybrid ranking</returns>
        Task<object> HybridSearchMediaAsync(
            string query,
            string? filters = null,
            float alpha = 0.5f,
            int perPage = 20,
            int page = 1);

        /// <summary>
        /// Performs a hybrid search (keyword + semantic) across the obsidian_notes collection.
        /// Uses Typesense's vector search with rank fusion.
        /// </summary>
        /// <param name="query">The search query text</param>
        /// <param name="filters">Optional filter string (e.g., "vault_name:=general")</param>
        /// <param name="alpha">Balance between keyword (0) and vector (1) search. Default 0.5</param>
        /// <param name="perPage">Number of results per page (default 20)</param>
        /// <param name="page">Page number (default 1)</param>
        /// <returns>Search results with hybrid ranking</returns>
        Task<object> HybridSearchNotesAsync(
            string query,
            string? filters = null,
            float alpha = 0.5f,
            int perPage = 20,
            int page = 1);

        /// <summary>
        /// Finds media items nearest to the given media item's stored vector (same-collection
        /// nearest-neighbor). Typesense uses the stored embedding by id, so the caller does not
        /// need to hold the vector, and the source document is automatically excluded.
        /// </summary>
        /// <param name="id">The source media item's ID</param>
        /// <param name="limit">Maximum number of results (default 10)</param>
        /// <param name="filters">Optional Typesense filter_by string (e.g., "media_type:=Book")</param>
        /// <param name="distanceThreshold">Optional maximum vector distance; results beyond it are dropped</param>
        Task<List<MediaVectorHit>> FindSimilarMediaByIdAsync(
            Guid id,
            int limit = 10,
            string? filters = null,
            double? distanceThreshold = null);

        /// <summary>
        /// Finds notes nearest to the given note's stored vector (same-collection nearest-neighbour).
        /// The source document is automatically excluded.
        /// </summary>
        Task<List<NoteVectorHit>> FindSimilarNotesByIdAsync(
            Guid id,
            int limit = 10,
            string? filters = null,
            double? distanceThreshold = null);

        /// <summary>
        /// Finds media items nearest to a raw query vector. Used for averaged ("for you")
        /// recommendations and cross-collection (note -> media) similarity.
        /// </summary>
        Task<List<MediaVectorHit>> VectorSearchMediaAsync(
            float[] embedding,
            string? filters = null,
            int limit = 10,
            double? distanceThreshold = null);

        /// <summary>
        /// Finds notes nearest to a raw query vector. Used for cross-collection (media -> note) similarity.
        /// </summary>
        Task<List<NoteVectorHit>> VectorSearchNotesAsync(
            float[] embedding,
            string? filters = null,
            int limit = 10,
            double? distanceThreshold = null);

        /// <summary>
        /// Runs a semantic/hybrid media search from natural-language text (powers "vibe" search).
        /// Typesense embeds the query text itself via the collection's remote embedder.
        /// </summary>
        Task<List<MediaVectorHit>> SemanticSearchMediaAsync(
            string query,
            string? filters = null,
            int limit = 20);

        /// <summary>
        /// Reads a media item's stored embedding vector back out of Typesense.
        /// Returns null if the document or its embedding is not present.
        /// </summary>
        Task<float[]?> GetMediaEmbeddingAsync(Guid id);

        /// <summary>
        /// Reads a note's stored embedding vector back out of Typesense.
        /// Returns null if the document or its embedding is not present.
        /// </summary>
        Task<float[]?> GetNoteEmbeddingAsync(Guid id);

        /// <summary>
        /// Reads multiple media-item embeddings in a single filtered fetch.
        /// Used to average liked items' vectors for personalized recommendations.
        /// </summary>
        Task<IReadOnlyList<float[]>> GetMediaEmbeddingsAsync(IReadOnlyCollection<Guid> ids);

        // ============================================
        // Highlights collection methods
        // ============================================

        /// <summary>
        /// Ensures the highlights collection exists in Typesense.
        /// Creates the collection if it doesn't exist, or skips if it does.
        /// Should be called during application startup.
        /// </summary>
        Task EnsureHighlightsCollectionExistsAsync();

        /// <summary>
        /// Indexes or updates a highlight document in Typesense.
        /// Uses upsert operation - creates if new, updates if exists.
        /// </summary>
        /// <param name="id">The unique ID of the highlight</param>
        /// <param name="text">The highlight text content</param>
        /// <param name="note">Optional annotation/note</param>
        /// <param name="title">Title of the source</param>
        /// <param name="author">Author of the source</param>
        /// <param name="category">Category (books, articles, etc.)</param>
        /// <param name="tags">List of tags</param>
        /// <param name="sourceUrl">URL of the source</param>
        /// <param name="sourceType">Source type (kindle, instapaper, etc.)</param>
        /// <param name="isFavorite">Whether this is a favorite</param>
        /// <param name="highlightedAt">When the highlight was created</param>
        /// <param name="createdAt">When imported to PLB</param>
        /// <param name="articleId">Linked article ID if any</param>
        /// <param name="bookId">Linked book ID if any</param>
        /// <param name="linkedMediaTitle">Title of linked media for display</param>
        /// <param name="location">Location in source</param>
        /// <param name="imageUrl">Cover image URL</param>
        Task IndexHighlightAsync(
            Guid id,
            string text,
            string? note,
            string? title,
            string? author,
            string? category,
            List<string> tags,
            string? sourceUrl,
            string? sourceType,
            bool isFavorite,
            DateTime? highlightedAt,
            DateTime createdAt,
            Guid? articleId,
            Guid? bookId,
            string? linkedMediaTitle,
            int? location,
            string? imageUrl);

        /// <summary>
        /// Deletes a highlight document from Typesense.
        /// </summary>
        /// <param name="id">The unique ID of the highlight to delete</param>
        Task DeleteHighlightAsync(Guid id);

        /// <summary>
        /// Performs a search across the highlights collection.
        /// </summary>
        /// <param name="query">The search query text</param>
        /// <param name="filters">Optional filter string (e.g., "category:=books", "is_favorite:=true")</param>
        /// <param name="perPage">Number of results per page (default 20)</param>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="sortBy">Optional allowlisted sort expression (e.g., "title:asc")</param>
        /// <returns>Search results as dynamic objects</returns>
        Task<object> SearchHighlightsAsync(string query, string? filters = null, int perPage = 20, int page = 1, string? sortBy = null);

        /// <summary>
        /// Performs a bulk re-index of all highlights from the database.
        /// Useful for initial setup or full data synchronization.
        /// </summary>
        Task<int> BulkReindexAllHighlightsAsync();

        /// <summary>
        /// Re-indexes a single highlight by ID.
        /// Returns false if no highlight with the given ID exists.
        /// </summary>
        Task<bool> ReindexHighlightByIdAsync(Guid id);

        /// <summary>
        /// Deletes and recreates the highlights collection, clearing all indexed data.
        /// Use this to completely reset the highlights search index.
        /// </summary>
        Task ResetHighlightsCollectionAsync();
    }
}
