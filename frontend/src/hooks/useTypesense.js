import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  typesenseReindex,
  reindexMediaItem,
  reindexMixlist,
  typesenseHealth,
  typesenseResetMediaItems,
  typesenseResetMixlists,
  typesenseSearch,
  typesenseAdvancedSearch,
  typesenseSearchMixlists,
  typesenseAdvancedSearchMixlists,
  reindexMixlists,
  reindexNotes,
  resetNotesCollection,
  searchHighlights,
  searchHighlightsAdvanced,
  reindexHighlights,
  resetHighlightsCollection,
} from '../api/typesenseService';
import { typesenseKeys, highlightKeys } from '../api/queryKeys';

// ----- Queries -----

export function useTypesenseHealth(options = {}) {
  return useQuery({
    queryKey: [...typesenseKeys.all, 'health'],
    queryFn: () => typesenseHealth(),
    ...options,
  });
}

export function useTypesenseSearch(query, mediaType = 'all', page = 1, perPage = 20, options = {}) {
  return useQuery({
    queryKey: typesenseKeys.search({ query, mediaType, page, perPage }),
    queryFn: () => typesenseSearch(query, mediaType, page, perPage),
    enabled: !!query,
    ...options,
  });
}

export function useTypesenseAdvancedSearch(searchOptions, options = {}) {
  return useQuery({
    queryKey: typesenseKeys.search({ advanced: true, ...searchOptions }),
    queryFn: () => typesenseAdvancedSearch(searchOptions),
    enabled: !!searchOptions,
    ...options,
  });
}

export function useTypesenseMixlistSearch(query, filter = null, page = 1, perPage = 20, options = {}) {
  return useQuery({
    queryKey: typesenseKeys.mixlistSearch({ query, filter, page, perPage }),
    queryFn: () => typesenseSearchMixlists(query, filter, page, perPage),
    enabled: !!query,
    ...options,
  });
}

export function useTypesenseAdvancedMixlistSearch(searchOptions, options = {}) {
  return useQuery({
    queryKey: typesenseKeys.mixlistSearch({ advanced: true, ...searchOptions }),
    queryFn: () => typesenseAdvancedSearchMixlists(searchOptions),
    enabled: !!searchOptions,
    ...options,
  });
}

export function useHighlightSearch(query = '*', filter = null, page = 1, perPage = 20, options = {}) {
  return useQuery({
    queryKey: [...highlightKeys.search(query), { filter, page, perPage }],
    queryFn: () => searchHighlights(query, filter, page, perPage),
    ...options,
  });
}

export function useHighlightSearchAdvanced(searchOptions, options = {}) {
  return useQuery({
    queryKey: [...highlightKeys.all, 'advancedSearch', searchOptions],
    queryFn: () => searchHighlightsAdvanced(searchOptions),
    enabled: !!searchOptions,
    ...options,
  });
}

// ----- Mutations -----

export function useTypesenseReindex() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => typesenseReindex(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: typesenseKeys.all });
    },
  });
}

export function useReindexMediaItem() {
  return useMutation({
    mutationFn: (id) => reindexMediaItem(id),
  });
}

export function useReindexMixlist() {
  return useMutation({
    mutationFn: (id) => reindexMixlist(id),
  });
}

export function useTypesenseResetMediaItems() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => typesenseResetMediaItems(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: typesenseKeys.all });
    },
  });
}

export function useTypesenseResetMixlists() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => typesenseResetMixlists(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: typesenseKeys.all });
    },
  });
}

export function useTypesenseReindexMixlists() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => reindexMixlists(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: typesenseKeys.all });
    },
  });
}

export function useTypesenseReindexNotes() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => reindexNotes(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: typesenseKeys.all });
    },
  });
}

export function useTypesenseResetNotesCollection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => resetNotesCollection(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: typesenseKeys.all });
    },
  });
}

export function useReindexHighlights() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => reindexHighlights(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: typesenseKeys.all });
    },
  });
}

export function useResetHighlightsCollection() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => resetHighlightsCollection(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: typesenseKeys.all });
    },
  });
}
