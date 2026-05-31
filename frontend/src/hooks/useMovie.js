import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllMovies,
  getMovieById,
  getMoviesByDirector,
  getMoviesByYear,
  createMovie,
  updateMovie,
  deleteMovie,
  importMovieFromTmdb,
  searchMoviesFromTmdb,
} from '../api/movieService';
import { movieKeys, mediaKeys } from '../api/queryKeys';

export function useAllMovies(options = {}) {
  return useQuery({
    queryKey: movieKeys.lists(),
    queryFn: async () => (await getAllMovies()).data,
    ...options,
  });
}

export function useMovie(id, options = {}) {
  return useQuery({
    queryKey: movieKeys.detail(id),
    queryFn: async () => (await getMovieById(id)).data,
    enabled: !!id,
    ...options,
  });
}

export function useMoviesByDirector(director, options = {}) {
  return useQuery({
    queryKey: [...movieKeys.lists(), { director }],
    queryFn: async () => (await getMoviesByDirector(director)).data,
    enabled: !!director,
    ...options,
  });
}

export function useMoviesByYear(year, options = {}) {
  return useQuery({
    queryKey: [...movieKeys.lists(), { year }],
    queryFn: async () => (await getMoviesByYear(year)).data,
    enabled: !!year,
    ...options,
  });
}

export function useMovieTmdbSearch(query, page = 1, options = {}) {
  return useQuery({
    queryKey: [...movieKeys.all, 'tmdbSearch', { query, page }],
    queryFn: () => searchMoviesFromTmdb(query, page),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useCreateMovie() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (movieData) => createMovie(movieData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdateMovie() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, movieData }) => updateMovie(id, movieData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: movieKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeleteMovie() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteMovie(id).then((r) => r.data),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.removeQueries({ queryKey: movieKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useImportMovieFromTmdb() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (movieId) => importMovieFromTmdb(movieId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}
