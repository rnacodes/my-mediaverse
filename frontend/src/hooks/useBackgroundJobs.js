import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getBookEnrichmentStatus,
  runBookEnrichment,
  runBookEnrichmentAll,
  enrichBookById,
  getMovieTvEnrichmentStatus,
  runMovieEnrichment,
  runTvShowEnrichment,
  runMovieTvEnrichmentAll,
  getPodcastEnrichmentStatus,
  runPodcastEnrichment,
  runPodcastEnrichmentAll,
} from '../api/backgroundJobsService';
import { backgroundJobsKeys, bookKeys, movieKeys, tvShowKeys, podcastKeys, mediaKeys } from '../api/queryKeys';

// ----- Status queries -----

export function useBookEnrichmentStatus(options = {}) {
  return useQuery({
    queryKey: backgroundJobsKeys.bookEnrichmentStatus(),
    queryFn: () => getBookEnrichmentStatus(),
    ...options,
  });
}

export function useMovieTvEnrichmentStatus(options = {}) {
  return useQuery({
    queryKey: backgroundJobsKeys.movieTvEnrichmentStatus(),
    queryFn: () => getMovieTvEnrichmentStatus(),
    ...options,
  });
}

export function usePodcastEnrichmentStatus(options = {}) {
  return useQuery({
    queryKey: backgroundJobsKeys.podcastEnrichmentStatus(),
    queryFn: () => getPodcastEnrichmentStatus(),
    ...options,
  });
}

// ----- Book enrichment mutations -----

export function useRunBookEnrichment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobOptions) => runBookEnrichment(jobOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: backgroundJobsKeys.bookEnrichmentStatus() });
      queryClient.invalidateQueries({ queryKey: bookKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useRunBookEnrichmentAll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobOptions) => runBookEnrichmentAll(jobOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: backgroundJobsKeys.bookEnrichmentStatus() });
      queryClient.invalidateQueries({ queryKey: bookKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useEnrichBookById() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (bookId) => enrichBookById(bookId),
    onSuccess: (_data, bookId) => {
      queryClient.invalidateQueries({ queryKey: backgroundJobsKeys.bookEnrichmentStatus() });
      queryClient.invalidateQueries({ queryKey: bookKeys.detail(bookId) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.detail(bookId) });
    },
  });
}

// ----- Movie/TV enrichment mutations -----

export function useRunMovieEnrichment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobOptions) => runMovieEnrichment(jobOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: backgroundJobsKeys.movieTvEnrichmentStatus() });
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useRunTvShowEnrichment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobOptions) => runTvShowEnrichment(jobOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: backgroundJobsKeys.movieTvEnrichmentStatus() });
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useRunMovieTvEnrichmentAll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobOptions) => runMovieTvEnrichmentAll(jobOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: backgroundJobsKeys.movieTvEnrichmentStatus() });
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

// ----- Podcast enrichment mutations -----

export function useRunPodcastEnrichment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobOptions) => runPodcastEnrichment(jobOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: backgroundJobsKeys.podcastEnrichmentStatus() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.all });
    },
  });
}

export function useRunPodcastEnrichmentAll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobOptions) => runPodcastEnrichmentAll(jobOptions),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: backgroundJobsKeys.podcastEnrichmentStatus() });
      queryClient.invalidateQueries({ queryKey: podcastKeys.all });
    },
  });
}
