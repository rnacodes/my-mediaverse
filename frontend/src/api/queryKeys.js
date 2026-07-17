export const mediaKeys = {
  all: ['media'],
  lists: () => [...mediaKeys.all, 'list'],
  byType: (type) => [...mediaKeys.lists(), { type }],
  byTopic: (topicId) => [...mediaKeys.lists(), { topicId }],
  byGenre: (genreId) => [...mediaKeys.lists(), { genreId }],
  search: (query) => [...mediaKeys.all, 'search', query],
  detail: (id) => [...mediaKeys.all, 'detail', id],
};

export const mixlistKeys = {
  all: ['mixlist'],
  lists: () => [...mixlistKeys.all, 'list'],
  detail: (id) => [...mixlistKeys.all, 'detail', id],
};

export const podcastKeys = {
  all: ['podcast'],
  series: {
    all: ['podcast', 'series'],
    lists: () => [...podcastKeys.series.all, 'list'],
    detail: (id) => [...podcastKeys.series.all, 'detail', id],
    search: (query) => [...podcastKeys.series.all, 'search', query],
    subscribed: () => [...podcastKeys.series.all, 'subscribed'],
    episodes: (seriesId) => [...podcastKeys.series.all, 'episodes', seriesId],
  },
  episodes: {
    all: ['podcast', 'episode'],
    lists: () => [...podcastKeys.episodes.all, 'list'],
    detail: (id) => [...podcastKeys.episodes.all, 'detail', id],
  },
  search: (query) => [...podcastKeys.all, 'search', query],
};

export const bookKeys = {
  all: ['book'],
  lists: () => [...bookKeys.all, 'list'],
  detail: (id) => [...bookKeys.all, 'detail', id],
};

export const movieKeys = {
  all: ['movie'],
  lists: () => [...movieKeys.all, 'list'],
  detail: (id) => [...movieKeys.all, 'detail', id],
};

export const tvShowKeys = {
  all: ['tvShow'],
  lists: () => [...tvShowKeys.all, 'list'],
  detail: (id) => [...tvShowKeys.all, 'detail', id],
};

export const videoKeys = {
  all: ['video'],
  lists: () => [...videoKeys.all, 'list'],
  detail: (id) => [...videoKeys.all, 'detail', id],
};

export const articleKeys = {
  all: ['article'],
  lists: () => [...articleKeys.all, 'list'],
  detail: (id) => [...articleKeys.all, 'detail', id],
};

export const documentKeys = {
  all: ['document'],
  lists: () => [...documentKeys.all, 'list'],
  detail: (id) => [...documentKeys.all, 'detail', id],
};

export const noteKeys = {
  all: ['note'],
  lists: () => [...noteKeys.all, 'list'],
  detail: (id) => [...noteKeys.all, 'detail', id],
  search: (query) => [...noteKeys.all, 'search', query],
};

export const highlightKeys = {
  all: ['highlight'],
  lists: () => [...highlightKeys.all, 'list'],
  detail: (id) => [...highlightKeys.all, 'detail', id],
  search: (query) => [...highlightKeys.all, 'search', query],
};

export const websiteKeys = {
  all: ['website'],
  lists: () => [...websiteKeys.all, 'list'],
  detail: (id) => [...websiteKeys.all, 'detail', id],
};

export const youtubeKeys = {
  all: ['youtube'],
  channels: {
    all: ['youtube', 'channel'],
    lists: () => [...youtubeKeys.channels.all, 'list'],
    detail: (id) => [...youtubeKeys.channels.all, 'detail', id],
    byExternalId: (externalId) => [...youtubeKeys.channels.all, 'external', externalId],
    videos: (channelId) => [...youtubeKeys.channels.all, 'videos', channelId],
  },
  playlists: {
    all: ['youtube', 'playlist'],
    lists: () => [...youtubeKeys.playlists.all, 'list'],
    detail: (id, includeVideos = false) => [...youtubeKeys.playlists.all, 'detail', id, { includeVideos }],
    byExternalId: (externalId, includeVideos = false) => [...youtubeKeys.playlists.all, 'external', externalId, { includeVideos }],
    videos: (playlistId) => [...youtubeKeys.playlists.all, 'videos', playlistId],
  },
  videos: {
    all: ['youtube', 'video'],
    detail: (videoId) => [...youtubeKeys.videos.all, 'detail', videoId],
  },
  externalSearch: (query, type, channelId) => [...youtubeKeys.all, 'externalSearch', { query, type, channelId }],
};

export const topicKeys = {
  all: ['topic'],
  lists: () => [...topicKeys.all, 'list'],
  detail: (id) => [...topicKeys.all, 'detail', id],
  search: (query) => [...topicKeys.all, 'search', query],
};

export const genreKeys = {
  all: ['genre'],
  lists: () => [...genreKeys.all, 'list'],
  detail: (id) => [...genreKeys.all, 'detail', id],
  search: (query) => [...genreKeys.all, 'search', query],
};

export const typesenseKeys = {
  all: ['typesense'],
  search: (params) => [...typesenseKeys.all, 'search', params],
  mixlistSearch: (params) => [...typesenseKeys.all, 'mixlistSearch', params],
};

export const aiKeys = {
  all: ['ai'],
};

export const readwiseKeys = {
  all: ['readwise'],
};

export const traktKeys = {
  all: ['trakt'],
};

export const recommendationKeys = {
  all: ['recommendation'],
  lists: () => [...recommendationKeys.all, 'list'],
  byMedia: (mediaId) => [...recommendationKeys.all, 'byMedia', mediaId],
};

export const relatedMediaKeys = {
  all: ['relatedMedia'],
  byMedia: (mediaId) => [...relatedMediaKeys.all, 'byMedia', mediaId],
  saved: (mediaId) => [...relatedMediaKeys.all, 'saved', mediaId],
  byEmbedding: (mediaId) => [...relatedMediaKeys.all, 'byEmbedding', mediaId],
};

export const tmdbKeys = {
  all: ['tmdb'],
  movieSearch: (query, page, language) => [...tmdbKeys.all, 'search', 'movies', { query, page, language }],
  tvSearch: (query, page, language) => [...tmdbKeys.all, 'search', 'tv', { query, page, language }],
  multiSearch: (query, page, language) => [...tmdbKeys.all, 'search', 'multi', { query, page, language }],
  movieDetails: (id, language) => [...tmdbKeys.all, 'movie', id, { language }],
  tvDetails: (id, language) => [...tmdbKeys.all, 'tv', id, { language }],
  popularMovies: (page, language) => [...tmdbKeys.all, 'popular', 'movies', { page, language }],
  popularTv: (page, language) => [...tmdbKeys.all, 'popular', 'tv', { page, language }],
  movieGenres: (language) => [...tmdbKeys.all, 'genres', 'movies', { language }],
  tvGenres: (language) => [...tmdbKeys.all, 'genres', 'tv', { language }],
  imageUrl: (imagePath, size) => [...tmdbKeys.all, 'image', { imagePath, size }],
};

export const backgroundJobsKeys = {
  all: ['backgroundJobs'],
  bookEnrichmentStatus: () => [...backgroundJobsKeys.all, 'bookEnrichment', 'status'],
  movieTvEnrichmentStatus: () => [...backgroundJobsKeys.all, 'movieTvEnrichment', 'status'],
  podcastEnrichmentStatus: () => [...backgroundJobsKeys.all, 'podcastEnrichment', 'status'],
};
