import { useQuery, useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  searchPodcastDirectory,
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
  syncAllPodcastSeries,
  enrichPodcastSeries,
  importPodcastSeriesFromFeed,
  getPodcastFeedEpisodes,
  importPodcastEpisodeFromFeed,
  getEpisodesBySeriesId,
  getPodcastEpisodeById,
  getAllPodcastEpisodes,
  createPodcastEpisode,
  updatePodcastEpisode,
  deletePodcastEpisode,
} from '../api/podcastService';
import { podcastKeys, mediaKeys } from '../api/queryKeys';

// ----- Directory queries -----

export function usePodcastDirectorySearch(term, options = {}) {
  return useQuery({
    queryKey: podcastKeys.directory(term),
    queryFn: async () => (await searchPodcastDirectory(term)).data,
    enabled: !!term && term.length > 0,
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

// Pages through the series' feed (not the library). Each page is an offset into
// the feed; there is another page while the offset has not reached feedItemCount.
export function usePodcastFeedEpisodes(seriesId, { limit = 20 } = {}, options = {}) {
  return useInfiniteQuery({
    queryKey: podcastKeys.series.feedEpisodes(seriesId, limit),
    queryFn: async ({ pageParam }) => (await getPodcastFeedEpisodes(seriesId, { offset: pageParam, limit })).data,
    initialPageParam: 0,
    getNextPageParam: (lastPage) => {
      const next = lastPage.offset + lastPage.items.length;
      return lastPage.items.length > 0 && next < lastPage.feedItemCount ? next : undefined;
    },
    enabled: !!seriesId,
    ...options,
  });
}

// Makes the server re-read the feed (it caches each feed for a few minutes),
// then drops the pages already loaded so the browser starts again from the top.
export function useRefreshPodcastFeedEpisodes() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (seriesId) =>
      getPodcastFeedEpisodes(seriesId, { offset: 0, limit: 1, refresh: true }).then((r) => r.data),
    onSuccess: (_data, seriesId) =>
      queryClient.resetQueries({ queryKey: podcastKeys.series.feedEpisodesAll(seriesId) }),
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

export function useSyncAllPodcastSeries() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => syncAllPodcastSeries().then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.all });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useEnrichPodcastSeries() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ seriesId, force = false }) => enrichPodcastSeries(seriesId, force).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.lists() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.detail(variables.seriesId) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

// Resolves to { status, data } because the status is the only thing that tells
// a new import (201) from a series that was already in the library (200).
export function useImportPodcastSeriesFromFeed() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ feedUrl, applePodcastsId }) =>
      importPodcastSeriesFromFeed({ feedUrl, applePodcastsId }).then((r) => ({ status: r.status, data: r.data })),
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

export function useImportPodcastEpisodeFromFeed() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ seriesId, guid, audioUrl }) =>
      importPodcastEpisodeFromFeed({ seriesId, guid, audioUrl }).then((r) => ({ status: r.status, data: r.data })),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: podcastKeys.episodes.lists() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.episodes(variables.seriesId) });
      queryClient.invalidateQueries({ queryKey: podcastKeys.series.feedEpisodesAll(variables.seriesId) });
    },
  });
}
