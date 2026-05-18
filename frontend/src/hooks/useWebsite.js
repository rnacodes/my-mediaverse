import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  scrapeWebsitePreview,
  importWebsite,
  getAllWebsites,
  getWebsiteById,
  getWebsitesByDomain,
  getWebsitesWithRss,
  createWebsite,
  updateWebsite,
  deleteWebsite,
  getWebsiteRssFeedItems,
} from '../api/websiteService';
import { websiteKeys, mediaKeys } from '../api/queryKeys';

export function useAllWebsites(options = {}) {
  return useQuery({
    queryKey: websiteKeys.lists(),
    queryFn: () => getAllWebsites(),
    ...options,
  });
}

export function useWebsite(id, options = {}) {
  return useQuery({
    queryKey: websiteKeys.detail(id),
    queryFn: () => getWebsiteById(id),
    enabled: !!id,
    ...options,
  });
}

export function useWebsitesByDomain(domain, options = {}) {
  return useQuery({
    queryKey: [...websiteKeys.lists(), { domain }],
    queryFn: () => getWebsitesByDomain(domain),
    enabled: !!domain,
    ...options,
  });
}

export function useWebsitesWithRss(options = {}) {
  return useQuery({
    queryKey: [...websiteKeys.lists(), 'withRss'],
    queryFn: () => getWebsitesWithRss(),
    ...options,
  });
}

export function useWebsiteRssFeedItems(id, maxItems = 3, options = {}) {
  return useQuery({
    queryKey: [...websiteKeys.detail(id), 'rss', { maxItems }],
    queryFn: () => getWebsiteRssFeedItems(id, maxItems),
    enabled: !!id,
    ...options,
  });
}

export function useScrapeWebsitePreview() {
  return useMutation({
    mutationFn: (url) => scrapeWebsitePreview(url),
  });
}

export function useImportWebsite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (websiteData) => importWebsite(websiteData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: websiteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useCreateWebsite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (websiteData) => createWebsite(websiteData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: websiteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdateWebsite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, websiteData }) => updateWebsite(id, websiteData),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: websiteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: websiteKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeleteWebsite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteWebsite(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: websiteKeys.lists() });
      queryClient.removeQueries({ queryKey: websiteKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}
