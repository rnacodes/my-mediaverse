import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getRelatedMedia,
  saveRelatedMedia,
  removeRelatedMedia,
  saveRelatedMediaBatch,
} from '../api/relatedMediaService';
import { relatedMediaKeys } from '../api/queryKeys';

export function useRelatedMedia(mediaItemId, includeBidirectional = true, options = {}) {
  return useQuery({
    queryKey: [...relatedMediaKeys.byMedia(mediaItemId), { includeBidirectional }],
    queryFn: () => getRelatedMedia(mediaItemId, includeBidirectional),
    enabled: !!mediaItemId,
    ...options,
  });
}

export function useSaveRelatedMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ sourceMediaItemId, relatedMediaItemId, source = 'ManuallyAdded', similarityScore = null, note = null }) =>
      saveRelatedMedia(sourceMediaItemId, relatedMediaItemId, source, similarityScore, note),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: relatedMediaKeys.byMedia(variables.sourceMediaItemId) });
      queryClient.invalidateQueries({ queryKey: relatedMediaKeys.byMedia(variables.relatedMediaItemId) });
    },
  });
}

export function useRemoveRelatedMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ sourceMediaItemId, relatedMediaItemId }) =>
      removeRelatedMedia(sourceMediaItemId, relatedMediaItemId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: relatedMediaKeys.byMedia(variables.sourceMediaItemId) });
      queryClient.invalidateQueries({ queryKey: relatedMediaKeys.byMedia(variables.relatedMediaItemId) });
    },
  });
}

export function useSaveRelatedMediaBatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ sourceMediaItemId, relatedItems }) =>
      saveRelatedMediaBatch(sourceMediaItemId, relatedItems),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: relatedMediaKeys.byMedia(variables.sourceMediaItemId) });
    },
  });
}
