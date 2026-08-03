import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllMixlists,
  getMixlistById,
  createMixlist,
  updateMixlist,
  deleteMixlist,
  addMediaToMixlist,
  removeMediaFromMixlist,
  getNotesForMixlist,
  linkNoteToMixlist,
  unlinkNoteFromMixlist,
} from '../api/mixlistService';
import { mixlistKeys, noteKeys } from '../api/queryKeys';

export function useAllMixlists(options = {}) {
  return useQuery({
    queryKey: mixlistKeys.lists(),
    queryFn: async () => (await getAllMixlists()).data,
    ...options,
  });
}

export function useMixlist(id, options = {}) {
  return useQuery({
    queryKey: mixlistKeys.detail(id),
    queryFn: async () => (await getMixlistById(id)).data,
    enabled: !!id,
    ...options,
  });
}

export function useNotesForMixlist(mixlistId, options = {}) {
  return useQuery({
    queryKey: [...mixlistKeys.detail(mixlistId), 'notes'],
    queryFn: async () => (await getNotesForMixlist(mixlistId)).data,
    enabled: !!mixlistId,
    ...options,
  });
}

export function useCreateMixlist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (mixlistData) => createMixlist(mixlistData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: mixlistKeys.lists() });
    },
  });
}

export function useUpdateMixlist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, mixlistData }) => updateMixlist(id, mixlistData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: mixlistKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mixlistKeys.detail(variables.id) });
    },
  });
}

export function useDeleteMixlist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteMixlist(id).then((r) => r.data),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: mixlistKeys.lists() });
      queryClient.removeQueries({ queryKey: mixlistKeys.detail(id) });
    },
  });
}

export function useAddMediaToMixlist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ mixlistId, mediaItemId }) =>
      addMediaToMixlist(mixlistId, mediaItemId).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: mixlistKeys.detail(variables.mixlistId) });
      queryClient.invalidateQueries({ queryKey: mixlistKeys.lists() });
    },
  });
}

export function useRemoveMediaFromMixlist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ mixlistId, mediaItemId }) =>
      removeMediaFromMixlist(mixlistId, mediaItemId).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: mixlistKeys.detail(variables.mixlistId) });
      queryClient.invalidateQueries({ queryKey: mixlistKeys.lists() });
    },
  });
}

export function useLinkNoteToMixlist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ mixlistId, noteId, linkDescription }) =>
      linkNoteToMixlist(mixlistId, noteId, linkDescription).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: [...mixlistKeys.detail(variables.mixlistId), 'notes'] });
      queryClient.invalidateQueries({ queryKey: noteKeys.detail(variables.noteId) });
    },
  });
}

export function useUnlinkNoteFromMixlist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ mixlistId, noteId }) =>
      unlinkNoteFromMixlist(mixlistId, noteId).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: [...mixlistKeys.detail(variables.mixlistId), 'notes'] });
      queryClient.invalidateQueries({ queryKey: noteKeys.detail(variables.noteId) });
    },
  });
}
