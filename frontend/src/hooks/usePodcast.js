import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  searchPodcasts,
  getPodcastFromApi,
  importPodcastFromApi,
  getAllPodcastSeries,
  getPodcastSeriesById,
  searchPodcastSeries,
  createPodcastSeries,
  updatePodcastSeries,
  deletePodcastSeries,
  subscribeToPodcastSeries,
  unsubscribeFromPodcastSeries,
  getSubscribedPodcastSeries,
  syncPodcastSeriesEpisodes,
  importPodcastSeriesFromApi,
  importPodcastSeriesByName,
  importPodcastEpisodeFromApi,
  getEpisodesBySeriesId,
  getPodcastEpisodeById,
  getAllPodcastEpisodes,
  createPodcastEpisode,
  updatePodcastEpisode,
  deletePodcastEpisode,
} from '../api/podcastService';
import { podcastKeys, mediaKeys } from '../api/queryKeys';

// ----- External (ListenNotes) queries -----

export function usePodcastExternalSearch(query, options = {}) {
  return useQuery({
    queryKey: [...podcastKeys.all, 'externalSearch', query],
    queryFn: () => searchPodcasts(query),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function usePodcastFromApi(id, options = {}) {
  return useQuery({
    queryKey: [...podcastKeys.all, 'externalDetail', id],
    queryFn: () => getPodcastFromApi(id),
    enabled: !!id,
    ...options,
  });
}

// ----- Series queries -----

export function useAllPodcastSeries(options = {}) {
  return useQuery({
    queryKey: podcastKeys.series.lists(),
    queryFn: async () => (await getAllPodcastSeries()).data,
    ...options,
  });
}

export function usePodcastSeries(id, options = {}) {
  return useQuery({
    queryKey: podcastKeys.series.detail(id),
    queryFn: async () => (await getPodcastSeriesById(id)).data,
    enabled: !!id,
    ...options,
  });
}

export function usePodcastSeriesSearch(query, options = {}) {
  return useQuery({
    queryKey: podcastKeys.series.search(query),
    queryFn: async () => (await searchPodcastSeries(query)).data,
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useSubscribedPodcastSeries(options = {}) {
  return useQuery({
    queryKey: podcastKeys.series.subscribed(),
    queryFn: async () => (await getSubscribedPodcastSeries()).data,
    ...options,
  });
}

export function useEpisodesBySeriesId(seriesId, options = {}) {
  return useQuery({
    queryKey: podcastKeys.series.episodes(seriesId),
    queryFn: async () => (await getEpisodesBySeriesId(seriesId)).data,
    enabled: !!seriesId,
    ...options,
  });
}

// ----- Episode queries -----

export function useAllPodcastEpisodes(options = {}) {
  return useQuery({
    queryKey: podcastKeys.episodes.lists(),
    queryFn: async () => (await getAllPodcastEpisodes()).data,
    ...options,
  });
}

export function usePodcastEpisode(id, options = {}) {
  return useQuery({
    queryKey: podcastKeys.episodes.detail(id),
    queryFn: async () => (await getPodcastEpisodeById(id)).data,
    enabled: !!id,
    ...options,
  });
}

// ----- Series mutations -----

export function useCreatePodcastSeries() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (seriesData) => createPodcastSeries(seriesData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdatePodcastSeries() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, seriesData }) => updatePodcastSeries(id, seriesData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.lists() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeletePodcastSeries() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deletePodcastSeries(id).then((r) => r.data),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.lists() });
      queryClient.removeQueries({ queryKey: podcastKeys.series.detail(id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useSubscribeToPodcastSeries() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (seriesId) => subscribeToPodcastSeries(seriesId).then((r) => r.data),
    onSuccess: (_data, seriesId) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.subscribed() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.detail(seriesId) });
    },
  });
}

export function useUnsubscribeFromPodcastSeries() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (seriesId) => unsubscribeFromPodcastSeries(seriesId).then((r) => r.data),
    onSuccess: (_data, seriesId) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.subscribed() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.detail(seriesId) });
    },
  });
}

export function useSyncPodcastSeriesEpisodes() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (seriesId) => syncPodcastSeriesEpisodes(seriesId).then((r) => r.data),
    onSuccess: (_data, seriesId) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.episodes(seriesId) });
      queryClient.invalidateQueries({ queryKey: podcastKeys.episodes.lists() });
    },
  });
}

export function useImportPodcastSeriesFromApi() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (podcastId) => importPodcastSeriesFromApi(podcastId).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useImportPodcastSeriesByName() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (podcastName) => importPodcastSeriesByName(podcastName).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useImportPodcastFromApi() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (podcastData) => importPodcastFromApi(podcastData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.all });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

// ----- Episode mutations -----

export function useCreatePodcastEpisode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (episodeData) => createPodcastEpisode(episodeData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.episodes.lists() });
    },
  });
}

export function useUpdatePodcastEpisode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, episodeData }) => updatePodcastEpisode(id, episodeData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.episodes.lists() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.episodes.detail(variables.id) });
      if (variables.seriesId) {
        queryClient.invalidateQueries({ queryKey: podcastKeys.series.episodes(variables.seriesId) });
      }
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeletePodcastEpisode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deletePodcastEpisode(id).then((r) => r.data),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.episodes.lists() });
      queryClient.removeQueries({ queryKey: podcastKeys.episodes.detail(id) });
    },
  });
}

export function useImportPodcastEpisodeFromApi() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ episodeId, seriesId }) => importPodcastEpisodeFromApi(episodeId, seriesId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.episodes.lists() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.episodes(variables.seriesId) });
    },
  });
}
