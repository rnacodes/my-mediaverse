using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Infrastructure.Models;
using MyMediaVerse.Shared.Interfaces;
using Typesense;
using Typesense.Setup;

namespace MyMediaVerse.Infrastructure.Services.Search
{
    /// <summary>
    /// Service for managing Typesense search indexing and querying.
    /// Handles CRUD synchronization between PostgreSQL and Typesense.
    /// </summary>
    public class TypeSenseService : ITypeSenseService
    {
        private readonly ITypesenseClient _typesenseClient;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TypeSenseService> _logger;
        private readonly string _mediaCollectionName;
        private readonly string _mixlistCollectionName;
        private readonly string _notesCollectionName;
        private readonly string _highlightsCollectionName;

        public TypeSenseService(
            ITypesenseClient typesenseClient,
            IApplicationDbContext context,
            ILogger<TypeSenseService> logger,
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
                "TypeSense collections configured with prefix '{Prefix}' (source: {Source}): {MediaCollection}, {MixlistCollection}, {NotesCollection}, {HighlightsCollection}",
                collectionPrefix, prefixSource, _mediaCollectionName, _mixlistCollectionName, _notesCollectionName, _highlightsCollectionName);
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

                var schema = new Schema(_mediaCollectionName, new List<Field>
                {
                    new Field("id", FieldType.String, false), // Not facet, primary key
                    new Field("title", FieldType.String, false), // Searchable
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
                    // Note: Vector embeddings are stored in PostgreSQL with pgvector for similarity search
                })
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

        /// <summary>
        /// Searches the media_items collection in Typesense.
        /// </summary>
        public async Task<object> SearchAsync(string query, string? filters = null, int perPage = 20, int page = 1)
        {
            try
            {
                // Create search parameters with query and queryBy fields
                var searchParameters = new SearchParameters(
                    query,
                    // Search across these fields
                    "title,description,author,director,creator,publisher"
                )
                {
                    PerPage = perPage,
                    Page = page,
                    // Sort by relevance first, then by recency
                    SortBy = "_text_match:desc,date_added:desc"
                };

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

                // Reset collection to remove any orphaned/stale documents
                _logger.LogInformation("Resetting collection '{CollectionName}' before re-indexing to remove stale data...", _mediaCollectionName);
                await ResetMediaItemsCollectionAsync();

                if (documents.Count == 0)
                {
                    _logger.LogInformation("No media items found to index.");
                    return 0;
                }

                // Import documents in batch (collection is fresh, so use Create)
                var importResults = await _typesenseClient.ImportDocuments<MediaItemDocument>(
                    _mediaCollectionName,
                    documents,
                    40, // Batch size
                    ImportType.Create
                );

                var successCount = importResults.Count(r => r.Success);
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

                var schema = new Schema(_mixlistCollectionName, new List<Field>
                {
                    new Field("id", FieldType.String, false), // Primary key
                    new Field("name", FieldType.String, false), // Searchable
                    new Field("description", FieldType.String, false, optional: true), // Searchable, optional
                    new Field("thumbnail", FieldType.String, false, optional: true, index: false), // Not searchable
                    new Field("date_created", FieldType.Int64, false), // Sortable timestamp
                    new Field("media_item_count", FieldType.Int32, false), // Sortable/facetable
                    new Field("media_item_titles", FieldType.StringArray, false, optional: true), // Searchable array
                    new Field("topics", FieldType.StringArray, true, optional: true), // Facetable array
                    new Field("genres", FieldType.StringArray, true, optional: true) // Facetable array
                })
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
        public async Task<object> SearchMixlistsAsync(string query, string? filters = null, int perPage = 20, int page = 1)
        {
            try
            {
                // Create search parameters with query and queryBy fields
                var searchParameters = new SearchParameters(
                    query,
                    // Search across these fields
                    "name,description,media_item_titles"
                )
                {
                    PerPage = perPage,
                    Page = page,
                    // Sort by relevance first, then by recency
                    SortBy = "_text_match:desc,date_created:desc"
                };

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

                // Reset collection to remove any orphaned/stale documents
                _logger.LogInformation("Resetting collection '{CollectionName}' before re-indexing to remove stale data...", _mixlistCollectionName);
                await ResetMixlistsCollectionAsync();

                if (documents.Count == 0)
                {
                    _logger.LogInformation("No mixlists found to index.");
                    return 0;
                }

                // Import documents in batch (collection is fresh, so use Create)
                var importResults = await _typesenseClient.ImportDocuments<MixlistDocument>(
                    _mixlistCollectionName,
                    documents,
                    40, // Batch size
                    ImportType.Create
                );

                var successCount = importResults.Count(r => r.Success);
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

                var schema = new Schema(_notesCollectionName, new List<Field>
                {
                    new Field("id", FieldType.String, false),
                    new Field("slug", FieldType.String, false),
                    new Field("title", FieldType.String, false),
                    new Field("content", FieldType.String, false, optional: true),
                    new Field("description", FieldType.String, false, optional: true),
                    new Field("vault_name", FieldType.String, true), // Facetable
                    new Field("source_url", FieldType.String, false, optional: true, index: false),
                    new Field("tags", FieldType.StringArray, true), // Facetable array
                    new Field("date_imported", FieldType.Int64, false),
                    new Field("note_date", FieldType.Int64, false, optional: true),
                    new Field("linked_media_count", FieldType.Int32, false)
                    // Note: Vector embeddings are stored in PostgreSQL with pgvector for similarity search
                })
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
        public async Task<object> SearchNotesAsync(string query, string? filters = null, int perPage = 20, int page = 1)
        {
            try
            {
                var searchParameters = new SearchParameters(
                    query,
                    "title,content,description,tags"
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = "_text_match:desc,date_imported:desc"
                };

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

                // Reset collection to remove any orphaned/stale documents
                _logger.LogInformation("Resetting collection '{CollectionName}' before re-indexing to remove stale data...", _notesCollectionName);
                await ResetNotesCollectionAsync();

                if (documents.Count == 0)
                {
                    _logger.LogInformation("No notes found to index.");
                    return 0;
                }

                // Import documents in batch (collection is fresh, so use Create)
                var importResults = await _typesenseClient.ImportDocuments<ObsidianNoteDocument>(
                    _notesCollectionName,
                    documents,
                    40,
                    ImportType.Create
                );

                var successCount = importResults.Count(r => r.Success);
                var failureCount = importResults.Count(r => !r.Success);

                _logger.LogInformation(
                    "Bulk re-index of notes complete. Success: {SuccessCount}, Failures: {FailureCount}",
                    successCount,
                    failureCount
                );

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
        // Note: Vector similarity search is handled by PostgreSQL with pgvector
        // These methods provide Typesense keyword search with optional embedding-based
        // result boosting that will be implemented via the RecommendationService
        // ============================================

        /// <summary>
        /// Performs a keyword search across the media_items collection.
        /// For semantic/vector search, use the RecommendationService which queries PostgreSQL with pgvector.
        /// The queryEmbedding parameter is reserved for future hybrid search integration.
        /// </summary>
        public async Task<object> HybridSearchMediaAsync(
            string query,
            float[]? queryEmbedding = null,
            string? filters = null,
            float alpha = 0.5f,
            int perPage = 20,
            int page = 1)
        {
            try
            {
                // Currently using keyword-only search via Typesense
                // Vector similarity search is handled separately via PostgreSQL + pgvector
                var searchParameters = new SearchParameters(
                    query,
                    "title,description,author,director,creator,publisher"
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = "_text_match:desc,date_added:desc"
                };

                if (!string.IsNullOrEmpty(filters))
                {
                    searchParameters.FilterBy = filters;
                }

                var searchResult = await _typesenseClient.Search<MediaItemDocument>(_mediaCollectionName, searchParameters);

                _logger.LogDebug("Media search for '{Query}' returned {Count} results (embedding provided: {HasEmbedding}).",
                    query, searchResult.Found, queryEmbedding != null);

                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing media search for query '{Query}'.", query);
                throw;
            }
        }

        /// <summary>
        /// Performs a keyword search across the obsidian_notes collection.
        /// For semantic/vector search, use the RecommendationService which queries PostgreSQL with pgvector.
        /// </summary>
        public async Task<object> HybridSearchNotesAsync(
            string query,
            float[]? queryEmbedding = null,
            string? filters = null,
            float alpha = 0.5f,
            int perPage = 20,
            int page = 1)
        {
            try
            {
                var searchParameters = new SearchParameters(
                    query,
                    "title,content,description,tags"
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = "_text_match:desc,date_imported:desc"
                };

                if (!string.IsNullOrEmpty(filters))
                {
                    searchParameters.FilterBy = filters;
                }

                var searchResult = await _typesenseClient.Search<ObsidianNoteDocument>(_notesCollectionName, searchParameters);

                _logger.LogDebug("Notes search for '{Query}' returned {Count} results (embedding provided: {HasEmbedding}).",
                    query, searchResult.Found, queryEmbedding != null);

                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing notes search for query '{Query}'.", query);
                throw;
            }
        }

        /// <summary>
        /// Vector similarity search for media items.
        /// Note: This is a placeholder. Actual vector search is handled by PostgreSQL + pgvector
        /// via the RecommendationService for better performance and consistency.
        /// </summary>
        public async Task<object> VectorSearchMediaAsync(
            float[] embedding,
            string? filters = null,
            Guid? excludeId = null,
            int limit = 10)
        {
            // Vector search should be done via PostgreSQL + pgvector through RecommendationService
            // This method returns empty results as a fallback
            _logger.LogWarning("VectorSearchMediaAsync called but vector search is handled by PostgreSQL. Use RecommendationService instead.");

            return await Task.FromResult(new
            {
                found = 0,
                hits = Array.Empty<object>(),
                message = "Vector search is handled by PostgreSQL + pgvector. Use RecommendationService for similarity queries."
            });
        }

        /// <summary>
        /// Vector similarity search for notes.
        /// Note: This is a placeholder. Actual vector search is handled by PostgreSQL + pgvector
        /// via the RecommendationService.
        /// </summary>
        public async Task<object> VectorSearchNotesAsync(
            float[] embedding,
            string? filters = null,
            Guid? excludeId = null,
            int limit = 10)
        {
            _logger.LogWarning("VectorSearchNotesAsync called but vector search is handled by PostgreSQL. Use RecommendationService instead.");

            return await Task.FromResult(new
            {
                found = 0,
                hits = Array.Empty<object>(),
                message = "Vector search is handled by PostgreSQL + pgvector. Use RecommendationService for similarity queries."
            });
        }

        /// <summary>
        /// Updates the embedding for a media item.
        /// Note: Embeddings are stored in PostgreSQL, not Typesense.
        /// This method is a no-op placeholder for interface compatibility.
        /// </summary>
        public Task UpdateMediaItemEmbeddingAsync(Guid id, float[] embedding)
        {
            // Embeddings are stored in PostgreSQL with pgvector, not in Typesense
            _logger.LogDebug("UpdateMediaItemEmbeddingAsync called for {Id} - embeddings are stored in PostgreSQL, not Typesense.", id);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the embedding for a note.
        /// Note: Embeddings are stored in PostgreSQL, not Typesense.
        /// This method is a no-op placeholder for interface compatibility.
        /// </summary>
        public Task UpdateNoteEmbeddingAsync(Guid id, float[] embedding)
        {
            // Embeddings are stored in PostgreSQL with pgvector, not in Typesense
            _logger.LogDebug("UpdateNoteEmbeddingAsync called for {Id} - embeddings are stored in PostgreSQL, not Typesense.", id);
            return Task.CompletedTask;
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

                var schema = new Schema(_highlightsCollectionName, new List<Field>
                {
                    new Field("id", FieldType.String, false),
                    new Field("text", FieldType.String, false), // Main highlight content - searchable
                    new Field("note", FieldType.String, false, optional: true), // User annotation - searchable
                    new Field("title", FieldType.String, false, optional: true), // Source title - searchable
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
                })
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
        public async Task<object> SearchHighlightsAsync(string query, string? filters = null, int perPage = 20, int page = 1)
        {
            try
            {
                var searchParameters = new SearchParameters(
                    query,
                    "text,note,title,author,tags"
                )
                {
                    PerPage = perPage,
                    Page = page,
                    SortBy = "_text_match:desc,created_at:desc"
                };

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

                // Reset collection to remove any orphaned/stale documents
                _logger.LogInformation("Resetting collection '{CollectionName}' before re-indexing to remove stale data...", _highlightsCollectionName);
                await ResetHighlightsCollectionAsync();

                if (documents.Count == 0)
                {
                    _logger.LogInformation("No highlights found to index.");
                    return 0;
                }

                // Import documents in batch (collection is fresh, so use Create)
                var importResults = await _typesenseClient.ImportDocuments<HighlightDocument>(
                    _highlightsCollectionName,
                    documents,
                    40,
                    ImportType.Create
                );

                var successCount = importResults.Count(r => r.Success);
                var failureCount = importResults.Count(r => !r.Success);

                _logger.LogInformation(
                    "Bulk re-index of highlights complete. Success: {SuccessCount}, Failures: {FailureCount}",
                    successCount,
                    failureCount
                );

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
