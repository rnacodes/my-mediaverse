import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  validateReadwiseConnection,
  syncAll,
  fetchArticleContent,
} from '../api/readwiseService';
import { readwiseKeys, articleKeys, highlightKeys, mediaKeys } from '../api/queryKeys';

export function useValidateReadwiseConnection(options = {}) {
  return useQuery({
    queryKey: [...readwiseKeys.all, 'validate'],
    queryFn: async () => (await validateReadwiseConnection()).data,
    ...options,
  });
}

export function useReadwiseSyncAll() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (incremental = true) => syncAll(incremental),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: articleKeys.all });
      queryClient.invalidateQueries({ queryKey: highlightKeys.all });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useReadwiseFetchArticleContent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ batchSize = 50, recentOnly = false } = {}) => fetchArticleContent(batchSize, recentOnly),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: articleKeys.all });
    },
  });
}
