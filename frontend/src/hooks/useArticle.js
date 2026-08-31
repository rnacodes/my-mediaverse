import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllArticles,
  getArticleById,
  createArticle,
  updateArticle,
  scrapeArticlePreview,
  findDuplicateArticles,
  deduplicateArticles,
  fetchArticleContent,
  bulkFetchArticleContents,
  syncDocumentsFromReader,
} from '../api/articleService';
import { articleKeys, mediaKeys } from '../api/queryKeys';

export function useAllArticles(options = {}) {
  return useQuery({
    queryKey: articleKeys.lists(),
    queryFn: () => getAllArticles(),
    ...options,
  });
}

export function useArticle(id, options = {}) {
  return useQuery({
    queryKey: articleKeys.detail(id),
    queryFn: () => getArticleById(id),
    enabled: !!id,
    ...options,
  });
}

export function useDuplicateArticles(options = {}) {
  return useQuery({
    queryKey: [...articleKeys.all, 'duplicates'],
    queryFn: async () => (await findDuplicateArticles()).data,
    ...options,
  });
}

export function useCreateArticle() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (articleData) => createArticle(articleData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: articleKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdateArticle() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, articleData }) => updateArticle(id, articleData),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: articleKeys.lists() });
      queryClient.invalidateQueries({ queryKey: articleKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useScrapeArticlePreview() {
  return useMutation({
    mutationFn: (url) => scrapeArticlePreview(url),
  });
}

export function useDeduplicateArticles() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => deduplicateArticles(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: articleKeys.all });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useFetchArticleContent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (articleId) => fetchArticleContent(articleId),
    onSuccess: (_data, articleId) => {
      queryClient.invalidateQueries({ queryKey: articleKeys.detail(articleId) });
    },
  });
}

export function useBulkFetchArticleContents() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (batchSize) => bulkFetchArticleContents(batchSize),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: articleKeys.all });
    },
  });
}

export function useSyncDocumentsFromReader() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (location) => syncDocumentsFromReader(location),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: articleKeys.all });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}
