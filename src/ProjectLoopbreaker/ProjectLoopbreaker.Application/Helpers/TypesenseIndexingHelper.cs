using ProjectLoopbreaker.Domain.Entities;
using ProjectLoopbreaker.Shared.Interfaces;

namespace ProjectLoopbreaker.Application.Helpers
{
    /// <summary>
    /// Helper class for indexing media items in Typesense.
    /// Provides static methods that can be called from any service after CRUD operations.
    /// </summary>
    public static class TypesenseIndexingHelper
    {
        /// <summary>
        /// Controls whether real-time indexing is enabled for individual CRUD operations.
        /// When disabled, media items will not be automatically indexed/deleted in Typesense.
        /// Bulk reindex operations are not affected by this flag.
        /// Defaults to true and resets to true on application restart.
        /// </summary>
        public static bool IsRealTimeIndexingEnabled { get; set; } = true;

        /// <summary>
        /// Indexes a media item in Typesense after it has been created or updated.
        /// Handles extracting the appropriate fields based on media type.
        /// </summary>
        /// <param name="mediaItem">The base media item with navigation properties loaded</param>
        /// <param name="typeSenseService">The Typesense service instance</param>
        /// <param name="additionalFields">Optional dictionary for media-specific fields</param>
        public static async Task IndexMediaItemAsync(
            BaseMediaItem mediaItem,
            ITypeSenseService? typeSenseService,
            Dictionary<string, object>? additionalFields = null)
        {
            // If real-time indexing is disabled or Typesense service is not available, skip
            if (!IsRealTimeIndexingEnabled || typeSenseService == null)
                return;

            try
            {
                var topics = mediaItem.Topics?.Select(t => t.Name).ToList() ?? new List<string>();
                var genres = mediaItem.Genres?.Select(g => g.Name).ToList() ?? new List<string>();

                await typeSenseService.IndexMediaItemAsync(
                    id: mediaItem.Id,
                    title: mediaItem.Title,
                    mediaType: mediaItem.MediaType.ToString(),
                    description: mediaItem.Description,
                    topics: topics,
                    genres: genres,
                    dateAdded: mediaItem.DateAdded,
                    status: mediaItem.Status.ToString(),
                    rating: mediaItem.Rating?.ToString(),
                    thumbnail: mediaItem.Thumbnail,
                    additionalFields: additionalFields
                );
            }
            catch (Exception)
            {
                // Log error but don't throw - we don't want Typesense failures to break CRUD operations
                // The error will be logged by the TypesenseService itself
            }
        }

        /// <summary>
        /// Deletes a media item from Typesense after it has been deleted from PostgreSQL.
        /// </summary>
        /// <param name="id">The ID of the media item to delete</param>
        /// <param name="typeSenseService">The Typesense service instance</param>
        public static async Task DeleteMediaItemAsync(
            Guid id,
            ITypeSenseService? typeSenseService)
        {
            // If real-time indexing is disabled or Typesense service is not available, skip
            if (!IsRealTimeIndexingEnabled || typeSenseService == null)
                return;

            try
            {
                await typeSenseService.DeleteMediaItemAsync(id);
            }
            catch (Exception)
            {
                // Log error but don't throw - we don't want Typesense failures to break delete operations
            }
        }

        /// <summary>
        /// Helper to extract additional fields for Articles.
        /// </summary>
        public static Dictionary<string, object>? GetArticleFields(Article article)
        {
            var fields = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(article.Author))
                fields["author"] = article.Author;
            
            return fields.Any() ? fields : null;
        }

        /// <summary>
        /// Helper to extract additional fields for Books.
        /// </summary>
        public static Dictionary<string, object>? GetBookFields(Book book)
        {
            var fields = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(book.Author))
                fields["author"] = book.Author;
            
            return fields.Any() ? fields : null;
        }

        /// <summary>
        /// Helper to extract additional fields for Movies.
        /// </summary>
        public static Dictionary<string, object>? GetMovieFields(Movie movie)
        {
            var fields = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(movie.Director))
                fields["director"] = movie.Director;
            
            if (movie.ReleaseYear.HasValue)
                fields["release_year"] = movie.ReleaseYear.Value;
            
            return fields.Any() ? fields : null;
        }

        /// <summary>
        /// Helper to extract additional fields for TV Shows.
        /// </summary>
        public static Dictionary<string, object>? GetTvShowFields(TvShow tvShow)
        {
            var fields = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(tvShow.Creator))
                fields["creator"] = tvShow.Creator;
            
            if (tvShow.FirstAirYear.HasValue)
                fields["release_year"] = tvShow.FirstAirYear.Value;
            
            return fields.Any() ? fields : null;
        }

        /// <summary>
        /// Helper to extract additional fields for Podcast Series.
        /// </summary>
        public static Dictionary<string, object>? GetPodcastSeriesFields(PodcastSeries podcast)
        {
            var fields = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(podcast.Publisher))
                fields["publisher"] = podcast.Publisher;
            
            return fields.Any() ? fields : null;
        }

        /// <summary>
        /// Helper to extract additional fields for Videos.
        /// </summary>
        public static Dictionary<string, object>? GetVideoFields(Video video)
        {
            var fields = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(video.Platform))
                fields["platform"] = video.Platform;
            
            return fields.Any() ? fields : null;
        }

        /// <summary>
        /// Helper to extract additional fields for Websites.
        /// </summary>
        public static Dictionary<string, object>? GetWebsiteFields(Website website)
        {
            var fields = new Dictionary<string, object>();
            
            if (!string.IsNullOrEmpty(website.Author))
                fields["author"] = website.Author;
            
            return fields.Any() ? fields : null;
        }
    }
}
