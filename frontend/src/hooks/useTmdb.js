import { useQuery } from '@tanstack/react-query';
import {
  searchMovies,
  searchTvShows,
  searchMulti,
  getMovieDetails,
  getTvShowDetails,
  getPopularMovies,
  getPopularTvShows,
  getMovieGenres,
  getTvGenres,
  getTmdbImageUrl,
} from '../api/tmdbService';
import { tmdbKeys } from '../api/queryKeys';

export function useTmdbMovieSearch(query, page = 1, language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.movieSearch(query, page, language),
    queryFn: () => searchMovies(query, page, language),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useTmdbTvShowSearch(query, page = 1, language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.tvSearch(query, page, language),
    queryFn: () => searchTvShows(query, page, language),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useTmdbMultiSearch(query, page = 1, language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.multiSearch(query, page, language),
    queryFn: () => searchMulti(query, page, language),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useTmdbMovieDetails(movieId, language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.movieDetails(movieId, language),
    queryFn: () => getMovieDetails(movieId, language),
    enabled: !!movieId,
    ...options,
  });
}

export function useTmdbTvShowDetails(tvShowId, language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.tvDetails(tvShowId, language),
    queryFn: () => getTvShowDetails(tvShowId, language),
    enabled: !!tvShowId,
    ...options,
  });
}

export function useTmdbPopularMovies(page = 1, language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.popularMovies(page, language),
    queryFn: () => getPopularMovies(page, language),
    ...options,
  });
}

export function useTmdbPopularTvShows(page = 1, language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.popularTv(page, language),
    queryFn: () => getPopularTvShows(page, language),
    ...options,
  });
}

export function useTmdbMovieGenres(language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.movieGenres(language),
    queryFn: () => getMovieGenres(language),
    ...options,
  });
}

export function useTmdbTvGenres(language = 'en-US', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.tvGenres(language),
    queryFn: () => getTvGenres(language),
    ...options,
  });
}

export function useTmdbImageUrl(imagePath, size = 'w500', options = {}) {
  return useQuery({
    queryKey: tmdbKeys.imageUrl(imagePath, size),
    queryFn: () => getTmdbImageUrl(imagePath, size),
    enabled: !!imagePath,
    ...options,
  });
}
