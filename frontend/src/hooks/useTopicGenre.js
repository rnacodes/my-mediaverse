import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllTopics,
  searchTopics,
  createTopic,
  deleteTopic,
  updateTopic,
  importTopicsFromJson,
  importTopicsFromCsv,
  getAllGenres,
  searchGenres,
  createGenre,
  deleteGenre,
  updateGenre,
  importGenresFromJson,
  importGenresFromCsv,
} from '../api/topicGenreService';
import { topicKeys, genreKeys } from '../api/queryKeys';

// ----- Topic queries -----

export function useAllTopics(options = {}) {
  return useQuery({
    queryKey: topicKeys.lists(),
    queryFn: async () => (await getAllTopics()).data,
    ...options,
  });
}

export function useTopicSearch(query, options = {}) {
  return useQuery({
    queryKey: topicKeys.search(query),
    queryFn: async () => (await searchTopics(query)).data,
    enabled: !!query && query.length > 0,
    ...options,
  });
}

// ----- Genre queries -----

export function useAllGenres(options = {}) {
  return useQuery({
    queryKey: genreKeys.lists(),
    queryFn: async () => (await getAllGenres()).data,
    ...options,
  });
}

export function useGenreSearch(query, options = {}) {
  return useQuery({
    queryKey: genreKeys.search(query),
    queryFn: async () => (await searchGenres(query)).data,
    enabled: !!query && query.length > 0,
    ...options,
  });
}

// ----- Topic mutations -----

export function useCreateTopic() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (topicData) => createTopic(topicData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: topicKeys.lists() });
    },
  });
}

export function useUpdateTopic() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ topicId, topicData }) => updateTopic(topicId, topicData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: topicKeys.lists() });
      queryClient.invalidateQueries({ queryKey: topicKeys.detail(variables.topicId) });
    },
  });
}

export function useDeleteTopic() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (topicId) => deleteTopic(topicId).then((r) => r.data),
    onSuccess: (_data, topicId) => {
      queryClient.invalidateQueries({ queryKey: topicKeys.lists() });
      queryClient.removeQueries({ queryKey: topicKeys.detail(topicId) });
    },
  });
}

export function useImportTopicsFromJson() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (topics) => importTopicsFromJson(topics).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: topicKeys.lists() });
    },
  });
}

export function useImportTopicsFromCsv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file) => importTopicsFromCsv(file).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: topicKeys.lists() });
    },
  });
}

// ----- Genre mutations -----

export function useCreateGenre() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (genreData) => createGenre(genreData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: genreKeys.lists() });
    },
  });
}

export function useUpdateGenre() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ genreId, genreData }) => updateGenre(genreId, genreData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: genreKeys.lists() });
      queryClient.invalidateQueries({ queryKey: genreKeys.detail(variables.genreId) });
    },
  });
}

export function useDeleteGenre() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (genreId) => deleteGenre(genreId).then((r) => r.data),
    onSuccess: (_data, genreId) => {
      queryClient.invalidateQueries({ queryKey: genreKeys.lists() });
      queryClient.removeQueries({ queryKey: genreKeys.detail(genreId) });
    },
  });
}

export function useImportGenresFromJson() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (genres) => importGenresFromJson(genres).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: genreKeys.lists() });
    },
  });
}

export function useImportGenresFromCsv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file) => importGenresFromCsv(file).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: genreKeys.lists() });
    },
  });
}
