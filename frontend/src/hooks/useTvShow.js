import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllTvShows,
  getTvShowById,
  getTvShowsByCreator,
  getTvShowsByYear,
  createTvShow,
  updateTvShow,
  deleteTvShow,
  importTvShowFromTmdb,
  searchTvShowsFromTmdb,
  getEpisodesByShowId,
  getTvShowEpisodeById,
  deleteTvShowEpisode,
} from '../api/tvShowService';
import { tvShowKeys, mediaKeys } from '../api/queryKeys';

export function useAllTvShows(options = {}) {
  return useQuery({
    queryKey: tvShowKeys.lists(),
    queryFn: async () => (await getAllTvShows()).data,
    ...options,
  });
}

export function useTvShow(id, options = {}) {
  return useQuery({
    queryKey: tvShowKeys.detail(id),
    queryFn: async () => (await getTvShowById(id)).data,
    enabled: !!id,
    ...options,
  });
}

export function useTvShowsByCreator(creator, options = {}) {
  return useQuery({
    queryKey: [...tvShowKeys.lists(), { creator }],
    queryFn: async () => (await getTvShowsByCreator(creator)).data,
    enabled: !!creator,
    ...options,
  });
}

export function useTvShowsByYear(year, options = {}) {
  return useQuery({
    queryKey: [...tvShowKeys.lists(), { year }],
    queryFn: async () => (await getTvShowsByYear(year)).data,
    enabled: !!year,
    ...options,
  });
}

export function useTvShowTmdbSearch(query, page = 1, options = {}) {
  return useQuery({
    queryKey: [...tvShowKeys.all, 'tmdbSearch', { query, page }],
    queryFn: () => searchTvShowsFromTmdb(query, page),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useTvShowEpisodes(showId, options = {}) {
  return useQuery({
    queryKey: [...tvShowKeys.detail(showId), 'episodes'],
    queryFn: async () => (await getEpisodesByShowId(showId)).data,
    enabled: !!showId,
    ...options,
  });
}

export function useTvShowEpisode(id, options = {}) {
  return useQuery({
    queryKey: [...tvShowKeys.all, 'episode', id],
    queryFn: async () => (await getTvShowEpisodeById(id)).data,
    enabled: !!id,
    ...options,
  });
}

export function useCreateTvShow() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (tvShowData) => createTvShow(tvShowData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdateTvShow() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, tvShowData }) => updateTvShow(id, tvShowData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tvShowKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeleteTvShow() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteTvShow(id).then((r) => r.data),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.removeQueries({ queryKey: tvShowKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useImportTvShowFromTmdb() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (tvShowId) => importTvShowFromTmdb(tvShowId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeleteTvShowEpisode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteTvShowEpisode(id).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tvShowKeys.all });
    },
  });
}
