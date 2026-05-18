import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAiStatus,
  generateNoteDescription,
  generateNoteDescriptionsBatch,
  getPendingNoteDescriptions,
  generateMediaEmbedding,
  generateMediaEmbeddingsBatch,
  getPendingMediaEmbeddings,
  generateNoteEmbedding,
  generateNoteEmbeddingsBatch,
  getPendingNoteEmbeddings,
} from '../api/aiService';
import { aiKeys, noteKeys, mediaKeys } from '../api/queryKeys';

// ----- Queries -----

export function useAiStatus(options = {}) {
  return useQuery({
    queryKey: [...aiKeys.all, 'status'],
    queryFn: () => getAiStatus(),
    ...options,
  });
}

export function usePendingNoteDescriptions(options = {}) {
  return useQuery({
    queryKey: [...aiKeys.all, 'pending', 'noteDescriptions'],
    queryFn: () => getPendingNoteDescriptions(),
    ...options,
  });
}

export function usePendingMediaEmbeddings(options = {}) {
  return useQuery({
    queryKey: [...aiKeys.all, 'pending', 'mediaEmbeddings'],
    queryFn: () => getPendingMediaEmbeddings(),
    ...options,
  });
}

export function usePendingNoteEmbeddings(options = {}) {
  return useQuery({
    queryKey: [...aiKeys.all, 'pending', 'noteEmbeddings'],
    queryFn: () => getPendingNoteEmbeddings(),
    ...options,
  });
}

// ----- Mutations -----

export function useGenerateNoteDescription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => generateNoteDescription(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: noteKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: [...aiKeys.all, 'pending', 'noteDescriptions'] });
    },
  });
}

export function useGenerateNoteDescriptionsBatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (batchSize) => generateNoteDescriptionsBatch(batchSize),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: noteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: [...aiKeys.all, 'pending', 'noteDescriptions'] });
    },
  });
}

export function useGenerateMediaEmbedding() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => generateMediaEmbedding(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: mediaKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: [...aiKeys.all, 'pending', 'mediaEmbeddings'] });
    },
  });
}

export function useGenerateMediaEmbeddingsBatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (batchSize) => generateMediaEmbeddingsBatch(batchSize),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...aiKeys.all, 'pending', 'mediaEmbeddings'] });
    },
  });
}

export function useGenerateNoteEmbedding() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => generateNoteEmbedding(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: noteKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: [...aiKeys.all, 'pending', 'noteEmbeddings'] });
    },
  });
}

export function useGenerateNoteEmbeddingsBatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (batchSize) => generateNoteEmbeddingsBatch(batchSize),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [...aiKeys.all, 'pending', 'noteEmbeddings'] });
    },
  });
}
