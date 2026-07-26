using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Models;
using MyMediaVerse.Shared.DTOs.Search;
using MyMediaVerse.Shared.Interfaces;
using Typesense;
using Typesense.Setup;

namespace MyMediaVerse.Infrastructure.Services.Search
{
    /// <summary>
    /// Service for managing Typesense search indexing and querying.
    /// Handles CRUD synchronization between PostgreSQL and Typesense.
    /// </summary>
    public class TypesenseService : ITypesenseService
    {
        private readonly ITypesenseClient _typesenseClient;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TypesenseService> _logger;
        private readonly string _mediaCollectionName;
        private readonly string _mixlistCollectionName;
        private readonly string _notesCollectionName;
        private readonly string _highlightsCollectionName;

        // Auto-embedding configuration. When an OpenAI key is present, each collection gains an
        // `embedding` field that Typesense populates by calling OpenAI on write, and search becomes
        // hybrid (keyword + vector). With no key, collections and search stay keyword-only.
        private readonly bool _autoEmbeddingEnabled;
        private readonly string? _openAiApiKey;
        private readonly string _embeddingModelName;
        private readonly int _embeddingDimensions;

        public TypesenseService(
            ITypesenseClient typesenseClient,
            IApplicationDbContext context,
            ILogger<TypesenseService> logger,
            IConfiguration configuration)
        {
            _typesenseClient = typesenseClient;
            _context = context;
            _logger = logger;

            // Get the collection prefix from configuration (e.g., "demo_" for demo site)
            // Use IsNullOrEmpty check so that an empty-string env var falls through to appsettings
            var envPrefix = Environment.GetEnvironmentVariable("TYPESENSE_COLLECTION_PREFIX");
            var collectionPrefix = !string.IsNullOrEmpty(envPrefix)
                ? envPrefix
                : configuration["Typesense:CollectionPrefix"] ?? string.Empty;
            var prefixSource = !string.IsNullOrEmpty(envPrefix) ? "TYPESENSE_COLLECTION_PREFIX env var" : "appsettings";

            // Dynamically set collection names with prefix
            _mediaCollectionName = $"{collectionPrefix}media_items";
            _mixlistCollectionName = $"{collectionPrefix}mixlists";
            _notesCollectionName = $"{collectionPrefix}obsidian_notes";
            _highlightsCollectionName = $"{collectionPrefix}highlights";

            _logger.LogInformation(
                "Typesense collections configured with prefix '{Prefix}' (source: {Source}): {MediaCollection}, {MixlistCollection}, {NotesCollection}, {HighlightsCollection}",
                collectionPrefix, prefixSource, _mediaCollectionName, _mixlistCollectionName, _notesCollectionName, _highlightsCollectionName);

            // Resolve the OpenAI key (env var first, then appsettings) to decide whether Typesense
            // auto-embedding is available. The model name is sent to Typesense in the provider/model
            // form it expects (e.g. "openai/text-embedding-3-large").
            _openAiApiKey = ResolveSetting(configuration, "OpenAI:ApiKey", "OPENAI_API_KEY");
            var embeddingModel = ResolveSetting(configuration, "OpenAI:EmbeddingModel", "OPENAI_EMBEDDING_MODEL")
                ?? "text-embedding-3-large";
            _embeddingModelName = embeddingModel.Contains('/') ? embeddingModel : $"openai/{embeddingModel}";
            var dimensionsSetting = ResolveSetting(configuration, "Typesense:EmbeddingDimensions", "TYPESENSE_EMBEDDING_DIMENSIONS");
            _embeddingDimensions = int.TryParse(dimensionsSetting, out var dims) ? dims : 3072;
            _autoEmbeddingEnabled = !string.IsNullOrEmpty(_openAiApiKey);

            if (_autoEmbeddingEnabled)
            {
                _logger.LogInformation(
                    "Typesense auto-embedding enabled. Model={Model}, Dimensions={Dimensions}",
                    _embeddingModelName, _embeddingDimensions);
            }
            else
            {
                _logger.LogWarning("OpenAI API key not configured. Typesense collections and search will be keyword-only.");
            }
        }

        /// <summary>
        /// Returns the first non-empty value from the named environment variable, then the configuration key.
        /// Mirrors the env-var-OR-config pattern used during service registration (that helper lives in the
        /// Web.API layer, which Infrastructure must not reference).
        /// </summary>
        private static string? ResolveSetting(IConfiguration configuration, string configKey, string envVarName)
        {
            var fromEnv = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(fromEnv))
                return fromEnv;

            var fromConfig = configuration[configKey];
            return string.IsNullOrEmpty(fromConfig) ? null : fromConfig;
        }

        /// <summary>
        /// Appends the auto-embedding fields to a collection's field list when an OpenAI key is configured.
        /// <c>embedding_source</c> holds the text composed by the document model; Typesense reads it on
        /// write, calls OpenAI, and stores the resulting vector in <c>embedding</c>. With no key this is
        /// a no-op and the collection stays keyword-only. Must be called before constructing the Schema,
        /// since Schema.Fields is read-only once built.
        /// </summary>
        private void AddEmbeddingFields(List<Field> fields)
        {
            if (!_autoEmbeddingEnabled)
                return;

            fields.Add(new Field("embedding_source", FieldType.String, false, optional: true));
            fields.Add(new Field(
                "embedding",
                FieldType.FloatArray,
                new AutoEmbeddingConfig(
                    new Collection<string> { "embedding_source" },
                    new ModelConfig(_embeddingModelName) { ApiKey = _openAiApiKey }))
            {
                NumberOfDimensions = _embeddingDimensions
            });
        }

        /// <summary>
        /// Returns the comma-separated <c>query_by</c> field list for a collection's keyword fields,
        /// appending the <c>embedding</c> field when auto-embedding is enabled so search runs hybrid
        /// (keyword + vector rank fusion).
        /// </summary>
        private string BuildQueryBy(string keywordFields) =>
            _autoEmbeddingEnabled ? $"{keywordFields},embedding" : keywordFields;

        /// <summary>
        /// Minimal projection used to list the IDs currently in a collection without pulling whole
        /// documents (or their large embedding vectors) over the wire. <c>Id</c> is intentionally
        /// not <c>required</c> so an id-only export still deserializes.
        /// </summary>
        internal sealed class IdProjection
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;
        }

        /// <summary>
        /// Computes the "orphan" IDs to remove from a collection: documents present in the index
        /// (<paramref name="indexedIds"/>) whose source row no longer exists in PostgreSQL
        /// (<paramref name="liveIds"/>). Pure set difference, exposed for unit testing.
        /// </summary>
        internal static List<string> ComputeOrphanDocumentIds(IEnumerable<string> indexedIds, IEnumerable<string> liveIds)
        {
            var liveSet = new HashSet<string>(liveIds, StringComparer.Ordinal);
            return indexedIds
                .Where(id => !string.IsNullOrEmpty(id) && !liveSet.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Reconciles deletes for a collection: lists the IDs currently indexed, removes any whose
        /// source row is gone from PostgreSQL (<paramref name="liveIds"/>), and returns how many were
        /// removed. Because the bulk reindex upserts in place (it no longer drops the collection),
        /// rows deleted in Postgres would otherwise linger as ghost search hits; this clears them on
        /// every reindex. Fail-safe: if listing the current IDs fails, the delete step is skipped
        /// entirely so a partial read can never trigger a mass delete.
        /// </summary>
        internal async Task<int> ReconcileDeletedDocumentsAsync(string collectionName, IEnumerable<string> liveIds)
        {
            List<IdProjection> indexed;
            try
            {
                indexed = await _typesenseClient.ExportDocuments<IdProjection>(
                    collectionName,
                    new ExportParameters { IncludeFields = "id" });
            }
            catch (Exception ex)
            {
                // Never delete on a partial/failed read of the current index.
                _logger.LogWarning(ex,
                    "Orphan reconciliation skipped for '{Collection}': could not list current document IDs.",
                    collectionName);
                return 0;
            }

            var orphanIds = ComputeOrphanDocumentIds(indexed.Select(d => d.Id), liveIds);
            if (orphanIds.Count == 0)
                return 0;

            var removed = 0;
            foreach (var orphanId in orphanIds)
            {
                try
                {
                    await _typesenseClient.DeleteDocument<IdProjection>(collectionName, orphanId);
                    removed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete orphan document {Id} from '{Collection}' during reconciliation.",
                        orphanId, collectionName);
                }
            }

            _logger.LogInformation(
                "Orphan reconciliation removed {Removed} stale document(s) from '{Collection}'.",
                removed, collectionName);
            return removed;
        }

        /// <summary>
        /// Ensures the media_items collection exists with proper schema.
        /// Called once during application startup.
        /// </summary>
        public async Task EnsureCollectionExistsAsync()
        {
            try
            {
                // Try to retrieve the collection to check if it exists
                await _typesenseClient.RetrieveCollection(_mediaCollectionName);
                _logger.LogInformation("Typesense collection '{CollectionName}' already exists.", _mediaCollectionName);
            }
            catch (TypesenseApiNotFoundException)
            {
                // Collection doesn't exist, create it
                _logger.LogInformation("Creating Typesense collection '{CollectionName}'...", _mediaCollectionName);

                var fields = new List<Field>
                {
                    new Field("id", FieldType.String, false), // Not facet, primary key
                    new Field("title", FieldType.String, false) { Sort = true }, // Searchable, sortable for Title (A-Z)
                    new Field("media_type", FieldType.String, true), // Facetable for filtering
                    new Field("description", FieldType.String, false, optional: true), // Searchable, optional
                    new Field("topics", FieldType.StringArray, true), // Facetable array
                    new Field("genres", FieldType.StringArray, true), // Facetable array
                    new Field("date_added", FieldType.Int64, false), // Sortable timestamp
                    new Field("status", FieldType.String, true), // Facetable
                    new Field("rating", FieldType.String, true, optional: true), // Facetable, optional
                    new Field("thumbnail", FieldType.String, false, optional: true, index: false), // Not searchable, not indexed
                    new Field("author", FieldType.String, true, optional: true), // Searchable and facetable
                    new Field("director", FieldType.String, true, optional: true), // Searchable and facetable
                    new Field("creator", FieldType.String, true, optional: true), // Searchable and facetable
                    new Field("publisher", FieldType.String, true, optional: true), // Searchable and facetable
                    new Field("release_year", FieldType.Int32, true, optional: true), // Facetable
                    new Field("platform", FieldType.String, true, optional: true), // Facetable
                    new Field("series_id", FieldType.String, false, optional: true, index: false) // For podcast episode routing
                };

                AddEmbeddingFields(fields);

                var schema = new Schema(_mediaCollectionName, fields)
                {
                    DefaultSortingField = "date_added" // Sort by most recently added by default
                };

                await _typesenseClient.CreateCollection(schema);
                _logger.LogInformation("Successfully created Typesense collection '{CollectionName}'.", _mediaCollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring Typesense collection exists.");
                throw;
            }
        }

        /// <summary>
        /// Indexes or updates a single media item in Typesense.
        /// Uses upsert operation for efficiency.
        /// </summary>
        public async Task IndexMediaItemAsync(
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
            Dictionary<string, object>? additionalFields = null)
        {
            try
            {
                var document = new MediaItemDocument
                {
                    Id = id.ToString(),
                    Title = title,
                    MediaType = mediaType,
                    Description = description,
                    Topics = topics ?? new List<string>(),
                    Genres = genres ?? new List<string>(),
                    DateAdded = ((DateTimeOffset)dateAdded).ToUnixTimeSeconds(),
                    Status = status,
                    Rating = rating,
                    Thumbnail = thumbnail
                };

                // Add media-specific fields if provided
                if (additionalFields != null)
                {
                    if (additionalFields.TryGetValue("author", out var author))
                        document.Author = author?.ToString();
                    
                    if (additionalFields.TryGetValue("director", out var director))
                        document.Director = director?.ToString();
                    
                    if (additionalFields.TryGetValue("creator", out var creator))
                        document.Creator = creator?.ToString();
                    
                    if (additionalFields.TryGetValue("publisher", out var publisher))
                        document.Publisher = publisher?.ToString();
                    
                    if (additionalFields.TryGetValue("release_year", out var releaseYear) && releaseYear != null)
                        document.ReleaseYear = Convert.ToInt32(releaseYear);
                    
                    if (additionalFields.TryGetValue("platform", out var platform))
                        document.Platform = platform?.ToString();

                    if (additionalFields.TryGetValue("series_id", out var seriesId))
                        document.SeriesId = seriesId?.ToString();
                }

                // Upsert: creates if new, updates if exists
                await _typesenseClient.UpsertDocument<MediaItemDocument>(_mediaCollectionName, document);
                
                _logger.LogDebug("Successfully indexed media item {Id} ({Title}) in Typesense.", id, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing media item {Id} in Typesense.", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a media item from the Typesense index.
        /// </summary>
        public async Task DeleteMediaItemAsync(Guid id)
        {
            try
            {
                await _typesenseClient.DeleteDocument<MediaItemDocument>(_mediaCollectionName, id.ToString());
                _logger.LogDebug("Successfully deleted media item {Id} from Typesense.", id);
            }
            catch (TypesenseApiNotFoundException)
            {
                _logger.LogWarning("Media item {Id} not found in Typesense (may have already been deleted).", id);
                // Don't throw - it's fine if the document doesn't exist
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting media item {Id} from Typesense.", id);
                throw;
            }
        }

        // Fields the frontend sort dropdown is allowed to sort by, per collection.
        internal static readonly HashSet<string> MediaSortableFields = new(StringComparer.Ordinal)
        {
            "date_added",
            "title"
        };

        internal static readonly HashSet<string> MixlistSortableFields = new(StringComparer.Ordinal)
        {
            "date_created",
            "media_item_count",
            "name"
        };

        internal static readonly HashSet<string> NotesSortableFields = new(StringComparer.Ordinal)
        {
            "title"
        };

        internal static readonly HashSet<string> HighlightsSortableFields = new(StringComparer.Ordinal)
        {
            "title"
        };

        /// <summary>
        /// Pure check that a requested "field:direction" sort expression targets an allowlisted
        /// sortable field with a valid direction. Guards against sending Typesense an unsupported
        /// or malformed sort_by (which would 400) or an injection attempt.
        /// </summary>
        internal static bool IsAllowedSortExpression(string? requested, HashSet<string> allowedFields)
        {
            if (string.IsNullOrWhiteSpace(requested))
            {
                return false;
            }

            var parts = requested.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            var fieldOk = allowedFields.Contains(parts[0]);
            var directionOk =
                string.Equals(parts[1], "asc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(parts[1], "desc", StringComparison.OrdinalIgnoreCase);

            return fieldOk && directionOk;
        }

        /// <summary>
        /// Returns the requested sort expression when it is allowlisted, otherwise the default.
        /// </summary>
        private string ResolveSortBy(string? requested, string defaultSort, HashSet<string> allowedFields)
        {
            if (string.IsNullOrWhiteSpace(requested))
            {
                return defaultSort;
            }

            if (IsAllowedSortExpression(requested, allowedFields))
            {
                return requested;
            }

            _logger.LogWarning("Ignoring unsupported sort_by '{SortBy}'; falling back to default sort.", requested);
            return defaultSort;
        }

        /// <summary>
        /// Searches the media_items collection in Typesense.
        /// </summary>
        public async Task<object> SearchAsync(string query, string? filters = null, int perPage = 20, int page = 1, string? sortBy = null)
        {
            try
            {
                // Default: relevance first, then recency. An explicit, allowlisted sortBy overrides it.
                var resolvedSort = ResolveSortBy(sortBy, "_text_match:desc,date_added:desc", MediaSortableFields);

                // Create search parameters with query and queryBy fields
                var searchParameters = new SearchParameters(
                    query,
                    // Search across these fields (plus the embedding field when hybrid search is enabled)
                    BuildQueryBy("title,description,author,director,creator,publisher")
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = resolvedSort
                };

                // Never return the raw embedding vector to callers - it's large and not displayable
                if (_autoEmbeddingEnabled)
                {
                    searchParameters.ExcludeFields = "embedding";
                    // Remote embedders (OpenAI auto-embedding) reject prefix search; it must be
                    // disabled explicitly or every hybrid query fails with a 400.
                    searchParameters.Prefix = false;
                }

                // Add filters if provided (e.g., "media_type:=Book")
                if (!string.IsNullOrEmpty(filters))
                {
                    searchParameters.FilterBy = filters;
                }

                var searchResult = await _typesenseClient.Search<MediaItemDocument>(_mediaCollectionName, searchParameters);
                
                _logger.LogDebug("Search for '{Query}' returned {Count} results.", query, searchResult.Found);
                
                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Typesense for query '{Query}'.", query);
                throw;
            }
        }

        /// <summary>
        /// Re-indexes all media items from PostgreSQL into Typesense.
        /// Useful for initial setup or full synchronization.
        /// </summary>
        public async Task<int> BulkReindexAllMediaItemsAsync()
        {
            try
            {
                _logger.LogInformation("Starting bulk re-index of all media items...");

                // Fetch all media items with their topics and genres
                var mediaItems = await _context.MediaItems
                    .Include(m => m.Topics)
                    .Include(m => m.Genres)
                    .AsNoTracking()
                    .ToListAsync();

                var documents = new List<MediaItemDocument>();

                foreach (var item in mediaItems)
                {
                    var additionalFields = new Dictionary<string, object>();

                    // Extract media-specific fields based on type
                    switch (item.MediaType.ToString())
                    {
                        case "Article":
                            var article = await _context.Articles.AsNoTracking()
                                .FirstOrDefaultAsync(a => a.Id == item.Id);
                            if (article?.Author != null)
                                additionalFields["author"] = article.Author;
                            break;

                        case "Book":
                            var book = await _context.Books.AsNoTracking()
                                .FirstOrDefaultAsync(b => b.Id == item.Id);
                            if (book?.Author != null)
                                additionalFields["author"] = book.Author;
                            break;

                        case "Movie":
                            var movie = await _context.Movies.AsNoTracking()
                                .FirstOrDefaultAsync(m => m.Id == item.Id);
                            if (movie?.Director != null)
                                additionalFields["director"] = movie.Director;
                            if (movie?.ReleaseYear != null)
                                additionalFields["release_year"] = movie.ReleaseYear.Value;
                            break;

                        case "TVShow":
                            var tvShow = await _context.TvShows.AsNoTracking()
                                .FirstOrDefaultAsync(t => t.Id == item.Id);
                            if (tvShow?.Creator != null)
                                additionalFields["creator"] = tvShow.Creator;
                            if (tvShow?.FirstAirYear != null)
                                additionalFields["release_year"] = tvShow.FirstAirYear.Value;
                            break;

                        case "Podcast":
                            // Check if it's a podcast episode first (episodes have SeriesId)
                            var episode = await _context.PodcastEpisodes.AsNoTracking()
                                .FirstOrDefaultAsync(e => e.Id == item.Id);
                            if (episode != null)
                            {
                                additionalFields["series_id"] = episode.SeriesId.ToString();
                                if (episode.Publisher != null)
                                    additionalFields["publisher"] = episode.Publisher;
                            }
                            else
                            {
                                // It's a podcast series
                                var podcast = await _context.PodcastSeries.AsNoTracking()
                                    .FirstOrDefaultAsync(p => p.Id == item.Id);
                                if (podcast?.Publisher != null)
                                    additionalFields["publisher"] = podcast.Publisher;
                            }
                            break;

                        case "Video":
                            var video = await _context.Videos.AsNoTracking()
                                .FirstOrDefaultAsync(v => v.Id == item.Id);
                            if (video?.Platform != null)
                                additionalFields["platform"] = video.Platform;
                            break;
                    }

                    var document = new MediaItemDocument
                    {
                        Id = item.Id.ToString(),
                        Title = item.Title,
                        MediaType = item.MediaType.ToString(),
                        Description = item.Description,
                        Topics = item.Topics.Select(t => t.Name).ToList(),
                        Genres = item.Genres.Select(g => g.Name).ToList(),
                        DateAdded = ((DateTimeOffset)item.DateAdded).ToUnixTimeSeconds(),
                        Status = item.Status.ToString(),
                        Rating = item.Rating?.ToString(),
                        Thumbnail = item.Thumbnail
                    };

                    // Apply additional fields
                    if (additionalFields.TryGetValue("author", out var author))
                        document.Author = author.ToString();
                    if (additionalFields.TryGetValue("director", out var director))
                        document.Director = director.ToString();
                    if (additionalFields.TryGetValue("creator", out var creator))
                        document.Creator = creator.ToString();
                    if (additionalFields.TryGetValue("publisher", out var publisher))
                        document.Publisher = publisher.ToString();
                    if (additionalFields.TryGetValue("release_year", out var releaseYear))
                        document.ReleaseYear = Convert.ToInt32(releaseYear);
                    if (additionalFields.TryGetValue("platform", out var platform))
                        document.Platform = platform.ToString();
                    if (additionalFields.TryGetValue("series_id", out var seriesId))
                        document.SeriesId = seriesId.ToString();

                    documents.Add(document);
                }

                // Ensure the collection exists without dropping it. Upsert-in-place keeps the live
                // index searchable throughout and lets Typesense skip re-embedding unchanged docs
                // (the embedding_source text is stable). Rows deleted in Postgres are reconciled
                // away below; use the explicit reset endpoint for a destructive full rebuild.
                await EnsureCollectionExistsAsync();

                var successCount = 0;
                if (documents.Count == 0)
                {
                    _logger.LogInformation("No media items found to index.");
                }
                else
                {
                    // Upsert so existing docs are updated in place; unchanged docs avoid a needless re-embed.
                    var importResults = await _typesenseClient.ImportDocuments<MediaItemDocument>(
                        _mediaCollectionName,
                        documents,
                        40, // Batch size
                        ImportType.Upsert
                    );

                    successCount = importResults.Count(r => r.Success);
                    var failureCount = importResults.Count(r => !r.Success);

                    _logger.LogInformation(
                        "Bulk re-index complete. Success: {SuccessCount}, Failures: {FailureCount}",
                        successCount,
                        failureCount
                    );

                    if (failureCount > 0)
                    {
                        _logger.LogWarning("Some documents failed to index. Check Typesense logs for details.");
                    }
                }

                // Remove any indexed documents whose source rows no longer exist in Postgres so
                // deleted items stop appearing as ghost search hits.
                await ReconcileDeletedDocumentsAsync(_mediaCollectionName, documents.Select(d => d.Id));

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk re-index of media items.");
                throw;
            }
        }

        /// <summary>
        /// Re-indexes a single media item by ID, applying any media-type-specific fields.
        /// </summary>
        public async Task<bool> ReindexMediaItemByIdAsync(Guid id)
        {
            var item = await _context.MediaItems
                .Include(m => m.Topics)
                .Include(m => m.Genres)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null)
            {
                _logger.LogInformation("ReindexMediaItemByIdAsync: media item {Id} not found.", id);
                return false;
            }

            var additionalFields = new Dictionary<string, object>();

            switch (item.MediaType.ToString())
            {
                case "Article":
                    var article = await _context.Articles.AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == item.Id);
                    if (article?.Author != null)
                        additionalFields["author"] = article.Author;
                    break;

                case "Book":
                    var book = await _context.Books.AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Id == item.Id);
                    if (book?.Author != null)
                        additionalFields["author"] = book.Author;
                    break;

                case "Movie":
                    var movie = await _context.Movies.AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == item.Id);
                    if (movie?.Director != null)
                        additionalFields["director"] = movie.Director;
                    if (movie?.ReleaseYear != null)
                        additionalFields["release_year"] = movie.ReleaseYear.Value;
                    break;

                case "TVShow":
                    var tvShow = await _context.TvShows.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == item.Id);
                    if (tvShow?.Creator != null)
                        additionalFields["creator"] = tvShow.Creator;
                    if (tvShow?.FirstAirYear != null)
                        additionalFields["release_year"] = tvShow.FirstAirYear.Value;
                    break;

                case "Podcast":
                    var episode = await _context.PodcastEpisodes.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.Id == item.Id);
                    if (episode != null)
                    {
                        additionalFields["series_id"] = episode.SeriesId.ToString();
                        if (episode.Publisher != null)
                            additionalFields["publisher"] = episode.Publisher;
                    }
                    else
                    {
                        var podcast = await _context.PodcastSeries.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == item.Id);
                        if (podcast?.Publisher != null)
                            additionalFields["publisher"] = podcast.Publisher;
                    }
                    break;

                case "Video":
                    var video = await _context.Videos.AsNoTracking()
                        .FirstOrDefaultAsync(v => v.Id == item.Id);
                    if (video?.Platform != null)
                        additionalFields["platform"] = video.Platform;
                    break;
            }

            await IndexMediaItemAsync(
                item.Id,
                item.Title,
                item.MediaType.ToString(),
                item.Description,
                item.Topics.Select(t => t.Name).ToList(),
                item.Genres.Select(g => g.Name).ToList(),
                item.DateAdded,
                item.Status.ToString(),
                item.Rating?.ToString(),
                item.Thumbnail,
                additionalFields);

            return true;
        }

        /// <summary>
        /// Ensures the mixlists collection exists with proper schema.
        /// Called during application startup.
        /// </summary>
        public async Task EnsureMixlistCollectionExistsAsync()
        {
            try
            {
                // Try to retrieve the collection to check if it exists
                await _typesenseClient.RetrieveCollection(_mixlistCollectionName);
                _logger.LogInformation("Typesense collection '{CollectionName}' already exists.", _mixlistCollectionName);
            }
            catch (TypesenseApiNotFoundException)
            {
                // Collection doesn't exist, create it
                _logger.LogInformation("Creating Typesense collection '{CollectionName}'...", _mixlistCollectionName);

                var fields = new List<Field>
                {
                    new Field("id", FieldType.String, false), // Primary key
                    new Field("name", FieldType.String, false) { Sort = true }, // Searchable, sortable for Title (A-Z)
                    new Field("description", FieldType.String, false, optional: true), // Searchable, optional
                    new Field("thumbnail", FieldType.String, false, optional: true, index: false), // Not searchable
                    new Field("date_created", FieldType.Int64, false), // Sortable timestamp
                    new Field("media_item_count", FieldType.Int32, false), // Sortable/facetable
                    new Field("media_item_titles", FieldType.StringArray, false, optional: true), // Searchable array
                    new Field("topics", FieldType.StringArray, true, optional: true), // Facetable array
                    new Field("genres", FieldType.StringArray, true, optional: true) // Facetable array
                };

                AddEmbeddingFields(fields);

                var schema = new Schema(_mixlistCollectionName, fields)
                {
                    DefaultSortingField = "date_created" // Sort by most recently created by default
                };

                await _typesenseClient.CreateCollection(schema);
                _logger.LogInformation("Successfully created Typesense collection '{CollectionName}'.", _mixlistCollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring Typesense mixlist collection exists.");
                throw;
            }
        }

        /// <summary>
        /// Indexes or updates a single mixlist in Typesense.
        /// Uses upsert operation for efficiency.
        /// </summary>
        public async Task IndexMixlistAsync(
            Guid id,
            string name,
            string? description,
            string? thumbnail,
            DateTime dateCreated,
            List<string> mediaItemTitles,
            List<string> topics,
            List<string> genres)
        {
            try
            {
                var document = new MixlistDocument
                {
                    Id = id.ToString(),
                    Name = name,
                    Description = description,
                    Thumbnail = thumbnail,
                    DateCreated = ((DateTimeOffset)dateCreated).ToUnixTimeSeconds(),
                    MediaItemCount = mediaItemTitles.Count,
                    MediaItemTitles = mediaItemTitles ?? new List<string>(),
                    Topics = topics ?? new List<string>(),
                    Genres = genres ?? new List<string>()
                };

                // Upsert: creates if new, updates if exists
                await _typesenseClient.UpsertDocument<MixlistDocument>(_mixlistCollectionName, document);
                
                _logger.LogDebug("Successfully indexed mixlist {Id} ({Name}) in Typesense.", id, name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing mixlist {Id} in Typesense.", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a mixlist from the Typesense index.
        /// </summary>
        public async Task DeleteMixlistAsync(Guid id)
        {
            try
            {
                await _typesenseClient.DeleteDocument<MixlistDocument>(_mixlistCollectionName, id.ToString());
                _logger.LogDebug("Successfully deleted mixlist {Id} from Typesense.", id);
            }
            catch (TypesenseApiNotFoundException)
            {
                _logger.LogWarning("Mixlist {Id} not found in Typesense (may have already been deleted).", id);
                // Don't throw - it's fine if the document doesn't exist
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mixlist {Id} from Typesense.", id);
                throw;
            }
        }

        /// <summary>
        /// Searches the mixlists collection in Typesense.
        /// </summary>
        public async Task<object> SearchMixlistsAsync(string query, string? filters = null, int perPage = 20, int page = 1, string? sortBy = null)
        {
            try
            {
                // Default: relevance first, then recency. An explicit, allowlisted sortBy overrides it.
                var resolvedSort = ResolveSortBy(sortBy, "_text_match:desc,date_created:desc", MixlistSortableFields);

                // Create search parameters with query and queryBy fields
                var searchParameters = new SearchParameters(
                    query,
                    // Search across these fields (plus the embedding field when hybrid search is enabled)
                    BuildQueryBy("name,description,media_item_titles")
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = resolvedSort
                };

                // Never return the raw embedding vector to callers - it's large and not displayable
                if (_autoEmbeddingEnabled)
                {
                    searchParameters.ExcludeFields = "embedding";
                    // Remote embedders (OpenAI auto-embedding) reject prefix search; it must be
                    // disabled explicitly or every hybrid query fails with a 400.
                    searchParameters.Prefix = false;
                }

                // Add filters if provided (e.g., "topics:=productivity")
                if (!string.IsNullOrEmpty(filters))
                {
                    searchParameters.FilterBy = filters;
                }

                var searchResult = await _typesenseClient.Search<MixlistDocument>(_mixlistCollectionName, searchParameters);
                
                _logger.LogDebug("Mixlist search for '{Query}' returned {Count} results.", query, searchResult.Found);
                
                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Typesense mixlists for query '{Query}'.", query);
                throw;
            }
        }

        /// <summary>
        /// Re-indexes all mixlists from PostgreSQL into Typesense.
        /// Useful for initial setup or full synchronization.
        /// </summary>
        public async Task<int> BulkReindexAllMixlistsAsync()
        {
            try
            {
                _logger.LogInformation("Starting bulk re-index of all mixlists...");

                // Fetch all mixlists with their media items and related data
                var mixlists = await _context.Mixlists
                    .Include(m => m.MediaItems)
                        .ThenInclude(mi => mi.Topics)
                    .Include(m => m.MediaItems)
                        .ThenInclude(mi => mi.Genres)
                    .AsNoTracking()
                    .ToListAsync();

                var documents = new List<MixlistDocument>();

                foreach (var mixlist in mixlists)
                {
                    var mediaItemTitles = mixlist.MediaItems.Select(mi => mi.Title).ToList();
                    var topics = mixlist.MediaItems
                        .SelectMany(mi => mi.Topics.Select(t => t.Name))
                        .Distinct()
                        .ToList();
                    var genres = mixlist.MediaItems
                        .SelectMany(mi => mi.Genres.Select(g => g.Name))
                        .Distinct()
                        .ToList();

                    var document = new MixlistDocument
                    {
                        Id = mixlist.Id.ToString(),
                        Name = mixlist.Name,
                        Description = mixlist.Description,
                        Thumbnail = mixlist.Thumbnail,
                        DateCreated = ((DateTimeOffset)mixlist.DateCreated).ToUnixTimeSeconds(),
                        MediaItemCount = mixlist.MediaItems.Count,
                        MediaItemTitles = mediaItemTitles,
                        Topics = topics,
                        Genres = genres
                    };

                    documents.Add(document);
                }

                // Ensure the collection exists without dropping it. Upsert-in-place keeps the live
                // index searchable throughout and lets Typesense skip re-embedding unchanged docs
                // (the embedding_source text is stable). Rows deleted in Postgres are reconciled
                // away below; use the explicit reset endpoint for a destructive full rebuild.
                await EnsureMixlistCollectionExistsAsync();

                var successCount = 0;
                if (documents.Count == 0)
                {
                    _logger.LogInformation("No mixlists found to index.");
                }
                else
                {
                    // Upsert so existing docs are updated in place; unchanged docs avoid a needless re-embed.
                    var importResults = await _typesenseClient.ImportDocuments<MixlistDocument>(
                        _mixlistCollectionName,
                        documents,
                        40, // Batch size
                        ImportType.Upsert
                    );

                    successCount = importResults.Count(r => r.Success);
                    var failureCount = importResults.Count(r => !r.Success);

                    _logger.LogInformation(
                        "Bulk re-index of mixlists complete. Success: {SuccessCount}, Failures: {FailureCount}",
                        successCount,
                        failureCount
                    );

                    if (failureCount > 0)
                    {
                        _logger.LogWarning("Some mixlist documents failed to index. Check Typesense logs for details.");
                    }
                }

                // Remove any indexed documents whose source rows no longer exist in Postgres so
                // deleted mixlists stop appearing as ghost search hits.
                await ReconcileDeletedDocumentsAsync(_mixlistCollectionName, documents.Select(d => d.Id));

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk re-index of mixlists.");
                throw;
            }
        }

        /// <summary>
        /// Re-indexes a single mixlist by ID, including aggregated topics/genres from its media items.
        /// </summary>
        public async Task<bool> ReindexMixlistByIdAsync(Guid id)
        {
            var mixlist = await _context.Mixlists
                .Include(m => m.MediaItems)
                    .ThenInclude(mi => mi.Topics)
                .Include(m => m.MediaItems)
                    .ThenInclude(mi => mi.Genres)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mixlist == null)
            {
                _logger.LogInformation("ReindexMixlistByIdAsync: mixlist {Id} not found.", id);
                return false;
            }

            var mediaItemTitles = mixlist.MediaItems.Select(mi => mi.Title).ToList();
            var topics = mixlist.MediaItems
                .SelectMany(mi => mi.Topics.Select(t => t.Name))
                .Distinct()
                .ToList();
            var genres = mixlist.MediaItems
                .SelectMany(mi => mi.Genres.Select(g => g.Name))
                .Distinct()
                .ToList();

            await IndexMixlistAsync(
                mixlist.Id,
                mixlist.Name,
                mixlist.Description,
                mixlist.Thumbnail,
                mixlist.DateCreated,
                mediaItemTitles,
                topics,
                genres);

            return true;
        }

        /// <summary>
        /// Deletes and recreates the media_items collection to completely clear all data.
        /// </summary>
        public async Task ResetMediaItemsCollectionAsync()
        {
            try
            {
                _logger.LogInformation("Resetting Typesense collection '{CollectionName}'...", _mediaCollectionName);

                // Delete the collection if it exists
                try
                {
                    await _typesenseClient.DeleteCollection(_mediaCollectionName);
                    _logger.LogInformation("Deleted existing collection '{CollectionName}'.", _mediaCollectionName);
                }
                catch (TypesenseApiNotFoundException)
                {
                    _logger.LogInformation("Collection '{CollectionName}' doesn't exist, skipping delete.", _mediaCollectionName);
                }

                // Recreate the collection with the schema
                await EnsureCollectionExistsAsync();
                
                _logger.LogInformation("Successfully reset collection '{CollectionName}'.", _mediaCollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting Typesense collection '{CollectionName}'.", _mediaCollectionName);
                throw;
            }
        }

        /// <summary>
        /// Deletes and recreates the mixlists collection to completely clear all data.
        /// </summary>
        public async Task ResetMixlistsCollectionAsync()
        {
            try
            {
                _logger.LogInformation("Resetting Typesense collection '{CollectionName}'...", _mixlistCollectionName);

                // Delete the collection if it exists
                try
                {
                    await _typesenseClient.DeleteCollection(_mixlistCollectionName);
                    _logger.LogInformation("Deleted existing collection '{CollectionName}'.", _mixlistCollectionName);
                }
                catch (TypesenseApiNotFoundException)
                {
                    _logger.LogInformation("Collection '{CollectionName}' doesn't exist, skipping delete.", _mixlistCollectionName);
                }

                // Recreate the collection with the schema
                await EnsureMixlistCollectionExistsAsync();

                _logger.LogInformation("Successfully reset collection '{CollectionName}'.", _mixlistCollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting Typesense collection '{CollectionName}'.", _mixlistCollectionName);
                throw;
            }
        }

        // ============================================
        // Obsidian Notes collection methods
        // ============================================

        /// <summary>
        /// Ensures the obsidian_notes collection exists with proper schema.
        /// </summary>
        public async Task EnsureNotesCollectionExistsAsync()
        {
            try
            {
                await _typesenseClient.RetrieveCollection(_notesCollectionName);
                _logger.LogInformation("Typesense collection '{CollectionName}' already exists.", _notesCollectionName);
            }
            catch (TypesenseApiNotFoundException)
            {
                _logger.LogInformation("Creating Typesense collection '{CollectionName}'...", _notesCollectionName);

                var fields = new List<Field>
                {
                    new Field("id", FieldType.String, false),
                    new Field("slug", FieldType.String, false),
                    new Field("title", FieldType.String, false) { Sort = true }, // Searchable, sortable for Title (A-Z)
                    new Field("content", FieldType.String, false, optional: true),
                    new Field("description", FieldType.String, false, optional: true),
                    new Field("vault_name", FieldType.String, true), // Facetable
                    new Field("source_url", FieldType.String, false, optional: true, index: false),
                    new Field("tags", FieldType.StringArray, true), // Facetable array
                    new Field("date_imported", FieldType.Int64, false),
                    new Field("note_date", FieldType.Int64, false, optional: true),
                    new Field("linked_media_count", FieldType.Int32, false)
                };

                AddEmbeddingFields(fields);

                var schema = new Schema(_notesCollectionName, fields)
                {
                    DefaultSortingField = "date_imported"
                };

                await _typesenseClient.CreateCollection(schema);
                _logger.LogInformation("Successfully created Typesense collection '{CollectionName}'.", _notesCollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring Typesense notes collection exists.");
                throw;
            }
        }

        /// <summary>
        /// Indexes or updates a note document in Typesense.
        /// </summary>
        public async Task IndexNoteAsync(
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
            int linkedMediaCount)
        {
            try
            {
                var document = new ObsidianNoteDocument
                {
                    Id = id.ToString(),
                    Slug = slug,
                    Title = title,
                    Content = content,
                    Description = description,
                    VaultName = vaultName,
                    SourceUrl = sourceUrl,
                    Tags = tags ?? new List<string>(),
                    DateImported = ((DateTimeOffset)dateImported).ToUnixTimeSeconds(),
                    NoteDate = noteDate.HasValue ? ((DateTimeOffset)noteDate.Value).ToUnixTimeSeconds() : null,
                    LinkedMediaCount = linkedMediaCount
                };

                await _typesenseClient.UpsertDocument<ObsidianNoteDocument>(_notesCollectionName, document);
                _logger.LogDebug("Successfully indexed note {Id} ({Title}) in Typesense.", id, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing note {Id} in Typesense.", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a note document from Typesense.
        /// </summary>
        public async Task DeleteNoteAsync(Guid id)
        {
            try
            {
                await _typesenseClient.DeleteDocument<ObsidianNoteDocument>(_notesCollectionName, id.ToString());
                _logger.LogDebug("Successfully deleted note {Id} from Typesense.", id);
            }
            catch (TypesenseApiNotFoundException)
            {
                _logger.LogWarning("Note {Id} not found in Typesense (may have already been deleted).", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {Id} from Typesense.", id);
                throw;
            }
        }

        /// <summary>
        /// Searches the obsidian_notes collection in Typesense.
        /// </summary>
        public async Task<object> SearchNotesAsync(string query, string? filters = null, int perPage = 20, int page = 1, string? sortBy = null)
        {
            try
            {
                // Default: relevance first, then recency. An explicit, allowlisted sortBy overrides it.
                var resolvedSort = ResolveSortBy(sortBy, "_text_match:desc,date_imported:desc", NotesSortableFields);

                var searchParameters = new SearchParameters(
                    query,
                    BuildQueryBy("title,content,description,tags")
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = resolvedSort
                };

                if (_autoEmbeddingEnabled)
                {
                    searchParameters.ExcludeFields = "embedding";
                    // Remote embedders (OpenAI auto-embedding) reject prefix search; it must be
                    // disabled explicitly or every hybrid query fails with a 400.
                    searchParameters.Prefix = false;
                }

                if (!string.IsNullOrEmpty(filters))
                {
                    searchParameters.FilterBy = filters;
                }

                var searchResult = await _typesenseClient.Search<ObsidianNoteDocument>(_notesCollectionName, searchParameters);
                _logger.LogDebug("Notes search for '{Query}' returned {Count} results.", query, searchResult.Found);
                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Typesense notes for query '{Query}'.", query);
                throw;
            }
        }

        /// <summary>
        /// Re-indexes all notes from PostgreSQL into Typesense.
        /// </summary>
        public async Task<int> BulkReindexAllNotesAsync()
        {
            try
            {
                _logger.LogInformation("Starting bulk re-index of all notes...");

                var notes = await _context.Notes
                    .Include(n => n.MediaItemNotes)
                    .AsNoTracking()
                    .ToListAsync();

                var documents = notes.Select(note => new ObsidianNoteDocument
                {
                    Id = note.Id.ToString(),
                    Slug = note.Slug,
                    Title = note.Title,
                    Content = note.Content,
                    Description = note.Description,
                    VaultName = note.VaultName,
                    SourceUrl = note.SourceUrl,
                    Tags = note.Tags ?? new List<string>(),
                    DateImported = ((DateTimeOffset)note.DateImported).ToUnixTimeSeconds(),
                    NoteDate = note.NoteDate.HasValue ? ((DateTimeOffset)note.NoteDate.Value).ToUnixTimeSeconds() : null,
                    LinkedMediaCount = note.MediaItemNotes.Count
                }).ToList();

                // Ensure the collection exists without dropping it. Upsert-in-place keeps the live
                // index searchable throughout and lets Typesense skip re-embedding unchanged docs
                // (the embedding_source text is stable). Rows deleted in Postgres are reconciled
                // away below; use the explicit reset endpoint for a destructive full rebuild.
                await EnsureNotesCollectionExistsAsync();

                var successCount = 0;
                if (documents.Count == 0)
                {
                    _logger.LogInformation("No notes found to index.");
                }
                else
                {
                    // Upsert so existing docs are updated in place; unchanged docs avoid a needless re-embed.
                    var importResults = await _typesenseClient.ImportDocuments<ObsidianNoteDocument>(
                        _notesCollectionName,
                        documents,
                        40,
                        ImportType.Upsert
                    );

                    successCount = importResults.Count(r => r.Success);
                    var failureCount = importResults.Count(r => !r.Success);

                    _logger.LogInformation(
                        "Bulk re-index of notes complete. Success: {SuccessCount}, Failures: {FailureCount}",
                        successCount,
                        failureCount
                    );
                }

                // Remove any indexed documents whose source rows no longer exist in Postgres so
                // deleted notes stop appearing as ghost search hits.
                await ReconcileDeletedDocumentsAsync(_notesCollectionName, documents.Select(d => d.Id));

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk re-index of notes.");
                throw;
            }
        }

        /// <summary>
        /// Re-indexes a single note by ID.
        /// </summary>
        public async Task<bool> ReindexNoteByIdAsync(Guid id)
        {
            var note = await _context.Notes
                .Include(n => n.MediaItemNotes)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                _logger.LogInformation("ReindexNoteByIdAsync: note {Id} not found.", id);
                return false;
            }

            await IndexNoteAsync(
                note.Id,
                note.Slug,
                note.Title,
                note.Content,
                note.Description,
                note.VaultName,
                note.SourceUrl,
                note.Tags ?? new List<string>(),
                note.DateImported,
                note.NoteDate,
                note.MediaItemNotes.Count);

            return true;
        }

        /// <summary>
        /// Deletes and recreates the obsidian_notes collection.
        /// </summary>
        public async Task ResetNotesCollectionAsync()
        {
            try
            {
                _logger.LogInformation("Resetting Typesense collection '{CollectionName}'...", _notesCollectionName);

                try
                {
                    await _typesenseClient.DeleteCollection(_notesCollectionName);
                    _logger.LogInformation("Deleted existing collection '{CollectionName}'.", _notesCollectionName);
                }
                catch (TypesenseApiNotFoundException)
                {
                    _logger.LogInformation("Collection '{CollectionName}' doesn't exist, skipping delete.", _notesCollectionName);
                }

                await EnsureNotesCollectionExistsAsync();
                _logger.LogInformation("Successfully reset collection '{CollectionName}'.", _notesCollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting Typesense collection '{CollectionName}'.", _notesCollectionName);
                throw;
            }
        }

        /// <summary>
        /// Performs a multi-search across media_items, mixlists, and obsidian_notes collections.
        /// Returns combined results from all three collections.
        /// </summary>
        public async Task<object> MultiSearchAsync(string query, string? filters = null, int perPage = 20, int page = 1)
        {
            try
            {
                // Run all three searches in parallel
                var mediaSearchTask = SearchAsync(query, filters, perPage, page);
                var mixlistSearchTask = SearchMixlistsAsync(query, filters, perPage, page);
                var notesSearchTask = SearchNotesAsync(query, filters, perPage, page);

                await Task.WhenAll(mediaSearchTask, mixlistSearchTask, notesSearchTask);

                var result = new
                {
                    media_items = mediaSearchTask.Result,
                    mixlists = mixlistSearchTask.Result,
                    notes = notesSearchTask.Result
                };

                _logger.LogDebug("Multi-search for '{Query}' completed.", query);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing multi-search for query '{Query}'.", query);
                throw;
            }
        }

        // ============================================
        // Hybrid/Semantic Search methods
        // When auto-embedding is enabled, Typesense generates the query vector from the query text
        // and blends keyword + vector matches via rank fusion. With no OpenAI key these fall back to
        // keyword-only search.
        // ============================================

        /// <summary>
        /// Searches the media_items collection. Runs hybrid (keyword + vector rank fusion) when
        /// auto-embedding is enabled, otherwise keyword-only.
        /// </summary>
        public async Task<object> HybridSearchMediaAsync(
            string query,
            string? filters = null,
            float alpha = 0.5f,
            int perPage = 20,
            int page = 1)
        {
            try
            {
                var searchParameters = new SearchParameters(
                    query,
                    BuildQueryBy("title,description,author,director,creator,publisher")
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = "_text_match:desc,date_added:desc"
                };

                if (_autoEmbeddingEnabled)
                {
                    searchParameters.ExcludeFields = "embedding";
                    // Remote embedders (OpenAI auto-embedding) reject prefix search; it must be
                    // disabled explicitly or every hybrid query fails with a 400.
                    searchParameters.Prefix = false;
                }

                if (!string.IsNullOrEmpty(filters))
                {
                    searchParameters.FilterBy = filters;
                }

                var searchResult = await _typesenseClient.Search<MediaItemDocument>(_mediaCollectionName, searchParameters);

                _logger.LogDebug("Media hybrid search for '{Query}' returned {Count} results (hybrid: {Hybrid}).",
                    query, searchResult.Found, _autoEmbeddingEnabled);

                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing media search for query '{Query}'.", query);
                throw;
            }
        }

        /// <summary>
        /// Searches the obsidian_notes collection. Runs hybrid (keyword + vector rank fusion) when
        /// auto-embedding is enabled, otherwise keyword-only.
        /// </summary>
        public async Task<object> HybridSearchNotesAsync(
            string query,
            string? filters = null,
            float alpha = 0.5f,
            int perPage = 20,
            int page = 1)
        {
            try
            {
                var searchParameters = new SearchParameters(
                    query,
                    BuildQueryBy("title,content,description,tags")
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = "_text_match:desc,date_imported:desc"
                };

                if (_autoEmbeddingEnabled)
                {
                    searchParameters.ExcludeFields = "embedding";
                    // Remote embedders (OpenAI auto-embedding) reject prefix search; it must be
                    // disabled explicitly or every hybrid query fails with a 400.
                    searchParameters.Prefix = false;
                }

                if (!string.IsNullOrEmpty(filters))
                {
                    searchParameters.FilterBy = filters;
                }

                var searchResult = await _typesenseClient.Search<ObsidianNoteDocument>(_notesCollectionName, searchParameters);

                _logger.LogDebug("Notes hybrid search for '{Query}' returned {Count} results (hybrid: {Hybrid}).",
                    query, searchResult.Found, _autoEmbeddingEnabled);

                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing notes search for query '{Query}'.", query);
                throw;
            }
        }

        /// <inheritdoc />
        public bool IsAutoEmbeddingEnabled => _autoEmbeddingEnabled;

        /// <summary>
        /// Field name of the auto-embedding vector on every collection. Vector queries target this field.
        /// </summary>
        private const string EmbeddingFieldName = "embedding";

        /// <summary>
        /// Builds a Typesense vector query against the <c>embedding</c> field. Pass <paramref name="id"/>
        /// for same-collection nearest-neighbour by stored vector (Typesense excludes the source), or
        /// <paramref name="vector"/> for a raw query vector. The distance threshold is passed via
        /// ExtraParams because the v8 client's strongly-typed property is dropped by ToQuery().
        /// </summary>
        private static VectorQuery BuildVectorQuery(float[]? vector, Guid? id, int limit, double? distanceThreshold)
        {
            Dictionary<string, string>? extraParams = distanceThreshold.HasValue
                ? new Dictionary<string, string>
                {
                    ["distance_threshold"] = distanceThreshold.Value.ToString(CultureInfo.InvariantCulture)
                }
                : null;

            return new VectorQuery(
                vector ?? Array.Empty<float>(),
                EmbeddingFieldName,
                id?.ToString(),
                limit,
                flatSearchCutoff: null,
                extraParams,
                distanceThreshold: null);
        }

        /// <summary>Converts a Typesense vector distance (0 = identical) into a 0-1 similarity score.</summary>
        private static double ToSimilarity(double? vectorDistance) =>
            vectorDistance.HasValue ? 1.0 - vectorDistance.Value : 0.0;

        private static List<MediaVectorHit> MapMediaHits(IEnumerable<Hit<MediaItemDocument>> hits) =>
            hits.Select(h => new MediaVectorHit
            {
                Id = Guid.TryParse(h.Document.Id, out var id) ? id : Guid.Empty,
                Title = h.Document.Title,
                MediaType = h.Document.MediaType,
                Description = h.Document.Description,
                Thumbnail = h.Document.Thumbnail,
                Status = h.Document.Status,
                Rating = h.Document.Rating,
                SimilarityScore = ToSimilarity(h.VectorDistance)
            }).ToList();

        private static List<NoteVectorHit> MapNoteHits(IEnumerable<Hit<ObsidianNoteDocument>> hits) =>
            hits.Select(h => new NoteVectorHit
            {
                Id = Guid.TryParse(h.Document.Id, out var id) ? id : Guid.Empty,
                Title = h.Document.Title,
                VaultName = h.Document.VaultName,
                Description = h.Document.Description,
                SourceUrl = h.Document.SourceUrl,
                Tags = h.Document.Tags,
                SimilarityScore = ToSimilarity(h.VectorDistance)
            }).ToList();

        /// <summary>Runs a vector query against the media collection via multi-search and maps the hits.</summary>
        private async Task<List<MediaVectorHit>> RunMediaVectorSearchAsync(VectorQuery vectorQuery, string? filters, int limit)
        {
            var parameters = new MultiSearchParameters(_mediaCollectionName, "*")
            {
                VectorQuery = vectorQuery,
                PerPage = limit,
                ExcludeFields = EmbeddingFieldName
            };
            if (!string.IsNullOrEmpty(filters))
                parameters.FilterBy = filters;

            var result = await _typesenseClient.MultiSearch<MediaItemDocument>(parameters);
            return MapMediaHits(result.Hits);
        }

        /// <summary>Runs a vector query against the notes collection via multi-search and maps the hits.</summary>
        private async Task<List<NoteVectorHit>> RunNoteVectorSearchAsync(VectorQuery vectorQuery, string? filters, int limit)
        {
            var parameters = new MultiSearchParameters(_notesCollectionName, "*")
            {
                VectorQuery = vectorQuery,
                PerPage = limit,
                ExcludeFields = EmbeddingFieldName
            };
            if (!string.IsNullOrEmpty(filters))
                parameters.FilterBy = filters;

            var result = await _typesenseClient.MultiSearch<ObsidianNoteDocument>(parameters);
            return MapNoteHits(result.Hits);
        }

        /// <inheritdoc />
        public async Task<List<MediaVectorHit>> FindSimilarMediaByIdAsync(
            Guid id, int limit = 10, string? filters = null, double? distanceThreshold = null)
        {
            if (!_autoEmbeddingEnabled)
            {
                _logger.LogWarning("FindSimilarMediaByIdAsync called while auto-embedding is disabled; returning no results.");
                return new List<MediaVectorHit>();
            }

            try
            {
                var vectorQuery = BuildVectorQuery(vector: null, id: id, limit, distanceThreshold);
                return await RunMediaVectorSearchAsync(vectorQuery, filters, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding media items similar to {Id} in Typesense.", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<NoteVectorHit>> FindSimilarNotesByIdAsync(
            Guid id, int limit = 10, string? filters = null, double? distanceThreshold = null)
        {
            if (!_autoEmbeddingEnabled)
            {
                _logger.LogWarning("FindSimilarNotesByIdAsync called while auto-embedding is disabled; returning no results.");
                return new List<NoteVectorHit>();
            }

            try
            {
                var vectorQuery = BuildVectorQuery(vector: null, id: id, limit, distanceThreshold);
                return await RunNoteVectorSearchAsync(vectorQuery, filters, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding notes similar to {Id} in Typesense.", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<MediaVectorHit>> VectorSearchMediaAsync(
            float[] embedding, string? filters = null, int limit = 10, double? distanceThreshold = null)
        {
            if (!_autoEmbeddingEnabled)
            {
                _logger.LogWarning("VectorSearchMediaAsync called while auto-embedding is disabled; returning no results.");
                return new List<MediaVectorHit>();
            }
            if (embedding == null || embedding.Length == 0)
                return new List<MediaVectorHit>();

            try
            {
                var vectorQuery = BuildVectorQuery(embedding, id: null, limit, distanceThreshold);
                return await RunMediaVectorSearchAsync(vectorQuery, filters, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing media vector search in Typesense.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<NoteVectorHit>> VectorSearchNotesAsync(
            float[] embedding, string? filters = null, int limit = 10, double? distanceThreshold = null)
        {
            if (!_autoEmbeddingEnabled)
            {
                _logger.LogWarning("VectorSearchNotesAsync called while auto-embedding is disabled; returning no results.");
                return new List<NoteVectorHit>();
            }
            if (embedding == null || embedding.Length == 0)
                return new List<NoteVectorHit>();

            try
            {
                var vectorQuery = BuildVectorQuery(embedding, id: null, limit, distanceThreshold);
                return await RunNoteVectorSearchAsync(vectorQuery, filters, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing notes vector search in Typesense.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<MediaVectorHit>> SemanticSearchMediaAsync(string query, string? filters = null, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<MediaVectorHit>();

            try
            {
                var searchParameters = new SearchParameters(
                    query,
                    BuildQueryBy("title,description,author,director,creator,publisher"))
                {
                    PerPage = limit,
                    SortBy = "_text_match:desc,date_added:desc"
                };

                if (_autoEmbeddingEnabled)
                {
                    searchParameters.ExcludeFields = EmbeddingFieldName;
                    // Remote embedders (OpenAI auto-embedding) reject prefix search.
                    searchParameters.Prefix = false;
                }

                if (!string.IsNullOrEmpty(filters))
                    searchParameters.FilterBy = filters;

                var result = await _typesenseClient.Search<MediaItemDocument>(_mediaCollectionName, searchParameters);
                return MapMediaHits(result.Hits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing semantic media search for '{Query}'.", query);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<float[]?> GetMediaEmbeddingAsync(Guid id)
        {
            var embeddings = await GetMediaEmbeddingsAsync(new[] { id });
            return embeddings.Count > 0 ? embeddings[0] : null;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<float[]>> GetMediaEmbeddingsAsync(IReadOnlyCollection<Guid> ids)
        {
            if (!_autoEmbeddingEnabled || ids == null || ids.Count == 0)
                return Array.Empty<float[]>();

            try
            {
                var searchParameters = new SearchParameters("*", "title")
                {
                    FilterBy = $"id:[{string.Join(",", ids)}]",
                    IncludeFields = $"id,{EmbeddingFieldName}",
                    PerPage = ids.Count
                };

                var result = await _typesenseClient.Search<EmbeddingDocument>(_mediaCollectionName, searchParameters);
                return result.Hits
                    .Where(h => h.Document.Embedding is { Length: > 0 })
                    .Select(h => h.Document.Embedding!)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading media embeddings from Typesense.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<float[]?> GetNoteEmbeddingAsync(Guid id)
        {
            if (!_autoEmbeddingEnabled)
                return null;

            try
            {
                var searchParameters = new SearchParameters("*", "title")
                {
                    FilterBy = $"id:={id}",
                    IncludeFields = $"id,{EmbeddingFieldName}",
                    PerPage = 1
                };

                var result = await _typesenseClient.Search<EmbeddingDocument>(_notesCollectionName, searchParameters);
                var embedding = result.Hits.FirstOrDefault()?.Document.Embedding;
                return embedding is { Length: > 0 } ? embedding : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading note embedding for {Id} from Typesense.", id);
                throw;
            }
        }

        /// <summary>
        /// Minimal projection used to read stored embedding vectors back out of Typesense via
        /// include_fields. The document models do not expose the vector (it is write-only / excluded).
        /// </summary>
        private sealed class EmbeddingDocument
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("embedding")]
            public float[]? Embedding { get; set; }
        }

        // ============================================
        // Highlights collection methods
        // ============================================

        /// <summary>
        /// Ensures the highlights collection exists with proper schema.
        /// Called once during application startup.
        /// </summary>
        public async Task EnsureHighlightsCollectionExistsAsync()
        {
            try
            {
                await _typesenseClient.RetrieveCollection(_highlightsCollectionName);
                _logger.LogInformation("Typesense collection '{CollectionName}' already exists.", _highlightsCollectionName);
            }
            catch (TypesenseApiNotFoundException)
            {
                _logger.LogInformation("Creating Typesense collection '{CollectionName}'...", _highlightsCollectionName);

                var fields = new List<Field>
                {
                    new Field("id", FieldType.String, false),
                    new Field("text", FieldType.String, false), // Main highlight content - searchable
                    new Field("note", FieldType.String, false, optional: true), // User annotation - searchable
                    new Field("title", FieldType.String, false, optional: true) { Sort = true }, // Source title - searchable, sortable for Title (A-Z)
                    new Field("author", FieldType.String, true, optional: true), // Facetable
                    new Field("category", FieldType.String, true, optional: true), // Facetable (books, articles, etc.)
                    new Field("tags", FieldType.StringArray, true), // Facetable array
                    new Field("source_url", FieldType.String, false, optional: true, index: false), // Not indexed
                    new Field("source_type", FieldType.String, true, optional: true), // Facetable (kindle, instapaper, etc.)
                    new Field("is_favorite", FieldType.Bool, true), // Facetable
                    new Field("highlighted_at", FieldType.Int64, false, optional: true), // Unix timestamp
                    new Field("created_at", FieldType.Int64, false), // Unix timestamp - default sort
                    new Field("article_id", FieldType.String, false, optional: true),
                    new Field("book_id", FieldType.String, false, optional: true),
                    new Field("linked_media_id", FieldType.String, false, optional: true),
                    new Field("linked_media_title", FieldType.String, false, optional: true),
                    new Field("linked_media_type", FieldType.String, true, optional: true), // Facetable (article, book, or null)
                    new Field("location", FieldType.Int32, false, optional: true),
                    new Field("image_url", FieldType.String, false, optional: true, index: false) // Not indexed
                };

                AddEmbeddingFields(fields);

                var schema = new Schema(_highlightsCollectionName, fields)
                {
                    DefaultSortingField = "created_at"
                };

                await _typesenseClient.CreateCollection(schema);
                _logger.LogInformation("Successfully created Typesense collection '{CollectionName}'.", _highlightsCollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring Typesense highlights collection exists.");
                throw;
            }
        }

        /// <summary>
        /// Indexes or updates a highlight document in Typesense.
        /// </summary>
        public async Task IndexHighlightAsync(
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
            string? imageUrl)
        {
            try
            {
                // Determine linked media type and ID
                string? linkedMediaId = null;
                string? linkedMediaType = null;
                if (articleId.HasValue)
                {
                    linkedMediaId = articleId.Value.ToString();
                    linkedMediaType = "article";
                }
                else if (bookId.HasValue)
                {
                    linkedMediaId = bookId.Value.ToString();
                    linkedMediaType = "book";
                }

                var document = new HighlightDocument
                {
                    Id = id.ToString(),
                    Text = text,
                    Note = note,
                    Title = title,
                    Author = author,
                    Category = category,
                    Tags = tags ?? new List<string>(),
                    SourceUrl = sourceUrl,
                    SourceType = sourceType,
                    IsFavorite = isFavorite,
                    HighlightedAt = highlightedAt.HasValue ? ((DateTimeOffset)highlightedAt.Value).ToUnixTimeSeconds() : null,
                    CreatedAt = ((DateTimeOffset)createdAt).ToUnixTimeSeconds(),
                    ArticleId = articleId?.ToString(),
                    BookId = bookId?.ToString(),
                    LinkedMediaId = linkedMediaId,
                    LinkedMediaTitle = linkedMediaTitle,
                    LinkedMediaType = linkedMediaType,
                    Location = location,
                    ImageUrl = imageUrl
                };

                await _typesenseClient.UpsertDocument<HighlightDocument>(_highlightsCollectionName, document);
                _logger.LogDebug("Successfully indexed highlight {Id} in Typesense.", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing highlight {Id} in Typesense.", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a highlight document from Typesense.
        /// </summary>
        public async Task DeleteHighlightAsync(Guid id)
        {
            try
            {
                await _typesenseClient.DeleteDocument<HighlightDocument>(_highlightsCollectionName, id.ToString());
                _logger.LogDebug("Successfully deleted highlight {Id} from Typesense.", id);
            }
            catch (TypesenseApiNotFoundException)
            {
                _logger.LogWarning("Highlight {Id} not found in Typesense (may have already been deleted).", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting highlight {Id} from Typesense.", id);
                throw;
            }
        }

        /// <summary>
        /// Searches the highlights collection in Typesense.
        /// </summary>
        public async Task<object> SearchHighlightsAsync(string query, string? filters = null, int perPage = 20, int page = 1, string? sortBy = null)
        {
            try
            {
                // Default: relevance first, then recency. An explicit, allowlisted sortBy overrides it.
                var resolvedSort = ResolveSortBy(sortBy, "_text_match:desc,created_at:desc", HighlightsSortableFields);

                // Many highlights share one source title; keep reading order within a source.
                if (resolvedSort.StartsWith("title:", StringComparison.Ordinal))
                {
                    resolvedSort += ",created_at:asc";
                }

                var searchParameters = new SearchParameters(
                    query,
                    BuildQueryBy("text,note,title,author,tags")
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = resolvedSort
                };

                if (_autoEmbeddingEnabled)
                {
                    searchParameters.ExcludeFields = "embedding";
                    // Remote embedders (OpenAI auto-embedding) reject prefix search; it must be
                    // disabled explicitly or every hybrid query fails with a 400.
                    searchParameters.Prefix = false;
                }

                if (!string.IsNullOrEmpty(filters))
                {
                    searchParameters.FilterBy = filters;
                }

                var searchResult = await _typesenseClient.Search<HighlightDocument>(_highlightsCollectionName, searchParameters);
                _logger.LogDebug("Highlights search for '{Query}' returned {Count} results.", query, searchResult.Found);
                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Typesense highlights for query '{Query}'.", query);
                throw;
            }
        }

        /// <summary>
        /// Re-indexes all highlights from PostgreSQL into Typesense.
        /// Includes linked media title for display purposes.
        /// </summary>
        public async Task<int> BulkReindexAllHighlightsAsync()
        {
            try
            {
                _logger.LogInformation("Starting bulk re-index of all highlights...");

                var highlights = await _context.Highlights
                    .Include(h => h.Article)
                    .Include(h => h.Book)
                    .AsNoTracking()
                    .ToListAsync();

                var documents = highlights.Select(highlight =>
                {
                    // Determine linked media
                    string? linkedMediaId = null;
                    string? linkedMediaTitle = null;
                    string? linkedMediaType = null;

                    if (highlight.ArticleId.HasValue && highlight.Article != null)
                    {
                        linkedMediaId = highlight.ArticleId.Value.ToString();
                        linkedMediaTitle = highlight.Article.Title;
                        linkedMediaType = "article";
                    }
                    else if (highlight.BookId.HasValue && highlight.Book != null)
                    {
                        linkedMediaId = highlight.BookId.Value.ToString();
                        linkedMediaTitle = highlight.Book.Title;
                        linkedMediaType = "book";
                    }

                    // Parse tags from comma-separated string
                    var tags = string.IsNullOrWhiteSpace(highlight.Tags)
                        ? new List<string>()
                        : highlight.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .ToList();

                    return new HighlightDocument
                    {
                        Id = highlight.Id.ToString(),
                        Text = highlight.Text,
                        Note = highlight.Note,
                        Title = highlight.Title,
                        Author = highlight.Author,
                        Category = highlight.Category,
                        Tags = tags,
                        SourceUrl = highlight.SourceUrl,
                        SourceType = highlight.SourceType,
                        IsFavorite = highlight.IsFavorite,
                        HighlightedAt = highlight.HighlightedAt.HasValue
                            ? ((DateTimeOffset)highlight.HighlightedAt.Value).ToUnixTimeSeconds()
                            : null,
                        CreatedAt = ((DateTimeOffset)highlight.CreatedAt).ToUnixTimeSeconds(),
                        ArticleId = highlight.ArticleId?.ToString(),
                        BookId = highlight.BookId?.ToString(),
                        LinkedMediaId = linkedMediaId,
                        LinkedMediaTitle = linkedMediaTitle,
                        LinkedMediaType = linkedMediaType,
                        Location = highlight.Location,
                        ImageUrl = highlight.ImageUrl
                    };
                }).ToList();

                // Ensure the collection exists without dropping it. Upsert-in-place keeps the live
                // index searchable throughout and lets Typesense skip re-embedding unchanged docs
                // (the embedding_source text is stable). Rows deleted in Postgres are reconciled
                // away below; use the explicit reset endpoint for a destructive full rebuild.
                await EnsureHighlightsCollectionExistsAsync();

                var successCount = 0;
                if (documents.Count == 0)
                {
                    _logger.LogInformation("No highlights found to index.");
                }
                else
                {
                    // Upsert so existing docs are updated in place; unchanged docs avoid a needless re-embed.
                    var importResults = await _typesenseClient.ImportDocuments<HighlightDocument>(
                        _highlightsCollectionName,
                        documents,
                        40,
                        ImportType.Upsert
                    );

                    successCount = importResults.Count(r => r.Success);
                    var failureCount = importResults.Count(r => !r.Success);

                    _logger.LogInformation(
                        "Bulk re-index of highlights complete. Success: {SuccessCount}, Failures: {FailureCount}",
                        successCount,
                        failureCount
                    );
                }

                // Remove any indexed documents whose source rows no longer exist in Postgres so
                // deleted highlights stop appearing as ghost search hits.
                await ReconcileDeletedDocumentsAsync(_highlightsCollectionName, documents.Select(d => d.Id));

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk re-index of highlights.");
                throw;
            }
        }

        /// <summary>
        /// Re-indexes a single highlight by ID, resolving the linked article/book title for display.
        /// </summary>
        public async Task<bool> ReindexHighlightByIdAsync(Guid id)
        {
            var highlight = await _context.Highlights
                .Include(h => h.Article)
                .Include(h => h.Book)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (highlight == null)
            {
                _logger.LogInformation("ReindexHighlightByIdAsync: highlight {Id} not found.", id);
                return false;
            }

            string? linkedMediaTitle = null;
            if (highlight.ArticleId.HasValue && highlight.Article != null)
            {
                linkedMediaTitle = highlight.Article.Title;
            }
            else if (highlight.BookId.HasValue && highlight.Book != null)
            {
                linkedMediaTitle = highlight.Book.Title;
            }

            var tags = string.IsNullOrWhiteSpace(highlight.Tags)
                ? new List<string>()
                : highlight.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

            await IndexHighlightAsync(
                highlight.Id,
                highlight.Text,
                highlight.Note,
                highlight.Title,
                highlight.Author,
                highlight.Category,
                tags,
                highlight.SourceUrl,
                highlight.SourceType,
                highlight.IsFavorite,
                highlight.HighlightedAt,
                highlight.CreatedAt,
                highlight.ArticleId,
                highlight.BookId,
                linkedMediaTitle,
                highlight.Location,
                highlight.ImageUrl);

            return true;
        }

        /// <summary>
        /// Deletes and recreates the highlights collection.
        /// </summary>
        public async Task ResetHighlightsCollectionAsync()
        {
            try
            {
                _logger.LogInformation("Resetting Typesense collection '{CollectionName}'...", _highlightsCollectionName);

                try
                {
                    await _typesenseClient.DeleteCollection(_highlightsCollectionName);
                    _logger.LogInformation("Deleted existing collection '{CollectionName}'.", _highlightsCollectionName);
                }
                catch (TypesenseApiNotFoundException)
                {
                    _logger.LogInformation("Collection '{CollectionName}' doesn't exist, skipping delete.", _highlightsCollectionName);
                }

                await EnsureHighlightsCollectionExistsAsync();
                _logger.LogInformation("Successfully reset collection '{CollectionName}'.", _highlightsCollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting Typesense collection '{CollectionName}'.", _highlightsCollectionName);
                throw;
            }
        }
    }
}
