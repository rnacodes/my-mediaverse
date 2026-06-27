import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAiStatus,
  generateNoteDescription,
  generateNoteDescriptionsBatch,
  getPendingNoteDescriptions,
} from '../api/aiService';
import { aiKeys, noteKeys } from '../api/queryKeys';

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
