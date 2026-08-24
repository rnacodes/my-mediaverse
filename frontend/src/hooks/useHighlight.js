import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllHighlights,
  getHighlightById,
  getHighlightsByArticle,
  getHighlightsByBook,
  getHighlightsByTag,
  getUnlinkedHighlights,
  bulkCreateHighlights,
  createHighlight,
  updateHighlight,
  setHighlightLink,
  deleteHighlight,
  bulkDeleteHighlights,
  linkHighlightsToMedia,
  exportHighlightToReadwise,
  cleanHighlightText,
} from '../api/highlightService';
import { highlightKeys } from '../api/queryKeys';

export function useAllHighlights(options = {}) {
  return useQuery({
    queryKey: highlightKeys.lists(),
    queryFn: () => getAllHighlights(),
    ...options,
  });
}

export function useHighlight(id, options = {}) {
  return useQuery({
    queryKey: highlightKeys.detail(id),
    queryFn: () => getHighlightById(id),
    enabled: !!id,
    ...options,
  });
}

export function useHighlightsByArticle(articleId, options = {}) {
  return useQuery({
    queryKey: [...highlightKeys.lists(), { articleId }],
    queryFn: () => getHighlightsByArticle(articleId),
    enabled: !!articleId,
    ...options,
  });
}

export function useHighlightsByBook(bookId, options = {}) {
  return useQuery({
    queryKey: [...highlightKeys.lists(), { bookId }],
    queryFn: () => getHighlightsByBook(bookId),
    enabled: !!bookId,
    ...options,
  });
}

export function useHighlightsByTag(tag, options = {}) {
  return useQuery({
    queryKey: [...highlightKeys.lists(), { tag }],
    queryFn: () => getHighlightsByTag(tag),
    enabled: !!tag,
    ...options,
  });
}

export function useUnlinkedHighlights(options = {}) {
  return useQuery({
    queryKey: [...highlightKeys.lists(), 'unlinked'],
    queryFn: () => getUnlinkedHighlights(),
    ...options,
  });
}

export function useBulkCreateHighlights() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (highlights) => bulkCreateHighlights(highlights),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: highlightKeys.all });
    },
  });
}

export function useCreateHighlight() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (highlightData) => createHighlight(highlightData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: highlightKeys.lists() });
    },
  });
}

export function useUpdateHighlight() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, highlightData }) => updateHighlight(id, highlightData),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: highlightKeys.lists() });
      queryClient.invalidateQueries({ queryKey: highlightKeys.detail(variables.id) });
    },
  });
}

export function useSetHighlightLink() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, articleId = null, bookId = null }) => setHighlightLink(id, { articleId, bookId }),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: highlightKeys.lists() });
      queryClient.invalidateQueries({ queryKey: highlightKeys.detail(variables.id) });
    },
  });
}

export function useDeleteHighlight() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteHighlight(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: highlightKeys.lists() });
      queryClient.removeQueries({ queryKey: highlightKeys.detail(id) });
    },
  });
}

export function useBulkDeleteHighlights() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ids) => bulkDeleteHighlights(ids),
    onSuccess: (_data, ids) => {
      queryClient.invalidateQueries({ queryKey: highlightKeys.lists() });
      ids.forEach((id) => {
        queryClient.removeQueries({ queryKey: highlightKeys.detail(id) });
      });
    },
  });
}

export function useLinkHighlightsToMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => linkHighlightsToMedia(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: highlightKeys.all });
    },
  });
}

export function useExportHighlightToReadwise() {
  return useMutation({
    mutationFn: (id) => exportHighlightToReadwise(id),
  });
}

export function useCleanHighlightText() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => cleanHighlightText(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: highlightKeys.all });
    },
  });
}
