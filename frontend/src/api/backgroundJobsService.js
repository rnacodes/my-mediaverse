import { apiClient } from './apiClient';

// ============================================
// Background Jobs API calls
// ============================================

// Book Description Enrichment
// ============================================

/**
 * Get the status of book description enrichment
 * Returns count of books needing enrichment
 */
export const getBookEnrichmentStatus = async () => {
    const response = await apiClient.get('/bookenrichment/status');
    return response.data;
};

/**
 * Run a single batch of book description enrichment
 * @param {Object} options - Optional parameters
 * @param {number} options.batchSize - Number of books to process (1-500, default: 50)
 * @param {number} options.delayBetweenCallsMs - Delay between API calls in ms (100-10000, default: 1000)
 */
export const runBookEnrichment = async (options = {}) => {
    const response = await apiClient.post('/bookenrichment/run', {
        batchSize: options.batchSize || 50,
        delayBetweenCallsMs: options.delayBetweenCallsMs || 1000
    });
    return response.data;
};

/**
 * Run book description enrichment for all books without descriptions
 * Processes multiple batches until maxBooks is reached
 * @param {Object} options - Optional parameters
 * @param {number} options.batchSize - Books per batch (1-200, default: 50)
 * @param {number} options.delayBetweenCallsMs - Delay between API calls in ms (default: 1000)
 * @param {number} options.maxBooks - Maximum total books to process (1-10000, default: 500)
 * @param {number} options.pauseBetweenBatchesSeconds - Pause between batches in seconds (default: 30)
 */
export const runBookEnrichmentAll = async (options = {}) => {
    const response = await apiClient.post('/bookenrichment/run-all', {
        batchSize: options.batchSize || 50,
        delayBetweenCallsMs: options.delayBetweenCallsMs || 1000,
        maxBooks: options.maxBooks || 500,
        pauseBetweenBatchesSeconds: options.pauseBetweenBatchesSeconds || 30
    });
    return response.data;
};

/**
 * Enrich a single book by its ID
 * Fetches description from Google Books using the book's ISBN
 * @param {string} bookId - The media ID of the book to enrich
 * @returns {Object} Result with success, bookTitle, description, errorMessage, etc.
 */
export const enrichBookById = async (bookId) => {
    const response = await apiClient.post(`/bookenrichment/${bookId}`);
    return response.data;
};

// Movie/TV TMDB Enrichment
// ============================================

/**
 * Get the status of Movie/TV TMDB enrichment
 * Returns count of movies and TV shows needing enrichment
 */
export const getMovieTvEnrichmentStatus = async () => {
    const response = await apiClient.get('/movietvenrichment/status');
    return response.data;
};

/**
 * Run a single batch of movie TMDB enrichment
 * @param {Object} options - Optional parameters
 * @param {number} options.batchSize - Number of movies to process (1-500, default: 50)
 * @param {number} options.delayBetweenCallsMs - Delay between API calls in ms (100-10000, default: 500)
 */
export const runMovieEnrichment = async (options = {}) => {
    const response = await apiClient.post('/movietvenrichment/run/movies', {
        batchSize: options.batchSize || 50,
        delayBetweenCallsMs: options.delayBetweenCallsMs || 500
    });
    return response.data;
};

/**
 * Run a single batch of TV show TMDB enrichment
 * @param {Object} options - Optional parameters
 * @param {number} options.batchSize - Number of TV shows to process (1-500, default: 50)
 * @param {number} options.delayBetweenCallsMs - Delay between API calls in ms (100-10000, default: 500)
 */
export const runTvShowEnrichment = async (options = {}) => {
    const response = await apiClient.post('/movietvenrichment/run/tvshows', {
        batchSize: options.batchSize || 50,
        delayBetweenCallsMs: options.delayBetweenCallsMs || 500
    });
    return response.data;
};

/**
 * Run Movie/TV enrichment for all items without TMDB data
 * @param {Object} options - Optional parameters
 * @param {number} options.batchSize - Items per batch (1-200, default: 50)
 * @param {number} options.delayBetweenCallsMs - Delay between API calls in ms (default: 500)
 * @param {number} options.maxMovies - Maximum movies to process (0-5000, default: 500)
 * @param {number} options.maxTvShows - Maximum TV shows to process (0-5000, default: 500)
 * @param {number} options.pauseBetweenBatchesSeconds - Pause between batches in seconds (default: 30)
 */
export const runMovieTvEnrichmentAll = async (options = {}) => {
    const response = await apiClient.post('/movietvenrichment/run-all', {
        batchSize: options.batchSize || 50,
        delayBetweenCallsMs: options.delayBetweenCallsMs || 500,
        maxMovies: options.maxMovies || 500,
        maxTvShows: options.maxTvShows || 500,
        pauseBetweenBatchesSeconds: options.pauseBetweenBatchesSeconds || 30
    });
    return response.data;
};

// Podcast Feed Enrichment
// ============================================

/**
 * Get the status of podcast feed enrichment
 * Returns count of podcasts needing enrichment
 */
export const getPodcastEnrichmentStatus = async () => {
    const response = await apiClient.get('/podcastenrichment/status');
    return response.data;
};

/**
 * Run a single batch of podcast feed enrichment
 * @param {Object} options - Optional parameters
 * @param {number} options.batchSize - Number of podcasts to process (1-100, default: 25)
 * @param {number} options.delayBetweenCallsMs - Delay between API calls in ms (500-30000, default: 1500)
 */
export const runPodcastEnrichment = async (options = {}) => {
    const response = await apiClient.post('/podcastenrichment/run', {
        batchSize: options.batchSize || 25,
        delayBetweenCallsMs: options.delayBetweenCallsMs || 1500
    });
    return response.data;
};

/**
 * Run podcast enrichment for every series that has not been filled from its feed
 * @param {Object} options - Optional parameters
 * @param {number} options.batchSize - Podcasts per batch (1-50, default: 25)
 * @param {number} options.delayBetweenCallsMs - Delay between API calls in ms (default: 1500)
 * @param {number} options.maxPodcasts - Maximum podcasts to process (1-500, default: 100)
 * @param {number} options.pauseBetweenBatchesSeconds - Pause between batches in seconds (default: 60)
 */
export const runPodcastEnrichmentAll = async (options = {}) => {
    const response = await apiClient.post('/podcastenrichment/run-all', {
        batchSize: options.batchSize || 25,
        delayBetweenCallsMs: options.delayBetweenCallsMs || 1500,
        maxPodcasts: options.maxPodcasts || 100,
        pauseBetweenBatchesSeconds: options.pauseBetweenBatchesSeconds || 60
    });
    return response.data;
};
