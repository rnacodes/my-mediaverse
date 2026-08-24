import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllNotes,
  getNoteById,
  getNoteBySlug,
  createNote,
  updateNote,
  deleteNote,
  bulkDeleteNotes,
  linkNoteToMedia,
  unlinkNoteFromMedia,
  getMediaForNote,
  getNotesForMedia,
  syncVault,
  syncAllVaults,
  getSyncStatus,
  searchNotes,
  searchNotesByVault,
  multiSearch,
  reindexNotes,
  resetNotesCollection,
} from '../api/noteService';
import { noteKeys, mediaKeys } from '../api/queryKeys';

export function useAllNotes(vault = null, options = {}) {
  return useQuery({
    queryKey: [...noteKeys.lists(), { vault }],
    queryFn: () => getAllNotes(vault),
    ...options,
  });
}

export function useNote(id, options = {}) {
  return useQuery({
    queryKey: noteKeys.detail(id),
    queryFn: () => getNoteById(id),
    enabled: !!id,
    ...options,
  });
}

export function useNoteBySlug(vault, slug, options = {}) {
  return useQuery({
    queryKey: [...noteKeys.all, 'slug', vault, slug],
    queryFn: () => getNoteBySlug(vault, slug),
    enabled: !!vault && !!slug,
    ...options,
  });
}

export function useMediaForNote(noteId, options = {}) {
  return useQuery({
    queryKey: [...noteKeys.detail(noteId), 'media'],
    queryFn: () => getMediaForNote(noteId),
    enabled: !!noteId,
    ...options,
  });
}

export function useNotesForMedia(mediaItemId, options = {}) {
  return useQuery({
    queryKey: [...noteKeys.all, 'forMedia', mediaItemId],
    queryFn: () => getNotesForMedia(mediaItemId),
    enabled: !!mediaItemId,
    ...options,
  });
}

export function useNoteSearch(query, filter = null, page = 1, perPage = 20, options = {}) {
  return useQuery({
    queryKey: [...noteKeys.search(query), { filter, page, perPage }],
    queryFn: () => searchNotes(query, filter, page, perPage),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useNotesByVaultSearch(vault, query, page = 1, perPage = 20, options = {}) {
  return useQuery({
    queryKey: [...noteKeys.all, 'searchByVault', vault, query, { page, perPage }],
    queryFn: () => searchNotesByVault(vault, query, page, perPage),
    enabled: !!vault && !!query,
    ...options,
  });
}

export function useMultiSearch(query, filter = null, page = 1, perPage = 20, options = {}) {
  return useQuery({
    queryKey: ['multiSearch', query, { filter, page, perPage }],
    queryFn: () => multiSearch(query, filter, page, perPage),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useNoteSyncStatus(options = {}) {
  return useQuery({
    queryKey: [...noteKeys.all, 'syncStatus'],
    queryFn: () => getSyncStatus(),
    ...options,
  });
}

export function useCreateNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (noteData) => createNote(noteData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: noteKeys.lists() });
    },
  });
}

export function useUpdateNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, noteData }) => updateNote(id, noteData),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: noteKeys.lists() });
      queryClient.invalidateQueries({ queryKey: noteKeys.detail(variables.id) });
    },
  });
}

export function useDeleteNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteNote(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: noteKeys.lists() });
      queryClient.removeQueries({ queryKey: noteKeys.detail(id) });
    },
  });
}

export function useBulkDeleteNotes() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ids) => bulkDeleteNotes(ids),
    onSuccess: (_data, ids) => {
      queryClient.invalidateQueries({ queryKey: noteKeys.lists() });
      ids.forEach((id) => {
        queryClient.removeQueries({ queryKey: noteKeys.detail(id) });
      });
    },
  });
}

export function useLinkNoteToMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ noteId, mediaItemId, linkDescription }) =>
      linkNoteToMedia(noteId, mediaItemId, linkDescription),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: [...noteKeys.detail(variables.noteId), 'media'] });
      queryClient.invalidateQueries({ queryKey: [...noteKeys.all, 'forMedia', variables.mediaItemId] });
      queryClient.invalidateQueries({ queryKey: mediaKeys.detail(variables.mediaItemId) });
    },
  });
}

export function useUnlinkNoteFromMedia() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ noteId, mediaItemId }) => unlinkNoteFromMedia(noteId, mediaItemId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: [...noteKeys.detail(variables.noteId), 'media'] });
      queryClient.invalidateQueries({ queryKey: [...noteKeys.all, 'forMedia', variables.mediaItemId] });
      queryClient.invalidateQueries({ queryKey: mediaKeys.detail(variables.mediaItemId) });
    },
  });
}

export function useSyncVault() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ vault, url, authToken }) => syncVault(vault, url, authToken),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: noteKeys.all });
    },
  });
}

export function useSyncAllVaults() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => syncAllVaults(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: noteKeys.all });
    },
  });
}

export function useReindexNotes() {
  return useMutation({
    mutationFn: () => reindexNotes(),
  });
}

export function useResetNotesCollection() {
  return useMutation({
    mutationFn: () => resetNotesCollection(),
  });
}
