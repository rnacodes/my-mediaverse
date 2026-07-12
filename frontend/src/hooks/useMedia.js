import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getMediaById,
  addMedia,
  updateMedia,
  deleteMedia,
  bulkDeleteMedia,
  updateMediaTopicsGenres,
} from '../api/mediaService';
import { searchMediaViaTypesense } from '../api/typesenseService';
import { mediaKeys, relatedMediaKeys, recommendationKeys } from '../api/queryKeys';

// Hook convention: queryFn unwraps axios `response.data` so consumers receive
// the payload directly via `data` from useQuery. All hooks in this layer follow
// the same shape.


export function useMediaSearch(query, options = {}) {
  return useQuery({
    queryKey: mediaKeys.search(query),
    queryFn: async () => searchMediaViaTypesense(query),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useMediaItem(id, options = {}) {
  return useQuery({
    queryKey: mediaKeys.detail(id),
    queryFn: async () => (await getMediaById(id)).data,
    enabled: !!id,
    ...options,
  });
}

export function useAddMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (mediaData) => addMedia(mediaData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdateMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, mediaData }) => updateMedia(id, mediaData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.detail(variables.id) });
    },
  });
}

export function useDeleteMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteMedia(id).then((r) => r.data),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
      queryClient.removeQueries({ queryKey: mediaKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: relatedMediaKeys.byMedia(id) });
      queryClient.invalidateQueries({ queryKey: recommendationKeys.byMedia(id) });
    },
  });
}

export function useBulkDeleteMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ids) => bulkDeleteMedia(ids).then((r) => r.data),
    onSuccess: (_data, ids) => {
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
      ids.forEach((id) => {
        queryClient.removeQueries({ queryKey: mediaKeys.detail(id) });
      });
    },
  });
}

export function useUpdateMediaTopicsGenres() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ mediaId, topics, genres }) =>
      updateMediaTopicsGenres(mediaId, topics, genres).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.detail(variables.mediaId) });
    },
  });
}
