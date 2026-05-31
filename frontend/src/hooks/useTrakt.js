import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getTraktStatus,
  startDeviceAuth,
  pollDeviceToken,
  disconnectTrakt,
  syncWatched,
  syncWatchlist,
  syncRatings,
  syncAll,
} from '../api/traktService';
import { traktKeys, movieKeys, tvShowKeys, mediaKeys } from '../api/queryKeys';

export function useTraktStatus(options = {}) {
  return useQuery({
    queryKey: [...traktKeys.all, 'status'],
    queryFn: async () => (await getTraktStatus()).data,
    ...options,
  });
}

export function useStartTraktDeviceAuth() {
  return useMutation({
    mutationFn: () => startDeviceAuth(),
  });
}

export function usePollTraktDeviceToken() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (deviceCode) => pollDeviceToken(deviceCode),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...traktKeys.all, 'status'] });
    },
  });
}

export function useDisconnectTrakt() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => disconnectTrakt(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...traktKeys.all, 'status'] });
    },
  });
}

export function useTraktSyncWatched() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => syncWatched(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useTraktSyncWatchlist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => syncWatchlist(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useTraktSyncRatings() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => syncRatings(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useTraktSyncAll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => syncAll(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: movieKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tvShowKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}
