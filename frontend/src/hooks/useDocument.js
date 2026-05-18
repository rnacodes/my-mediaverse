import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllDocuments,
  getDocumentById,
  createDocument,
  updateDocument,
  deleteDocument,
  getDocumentsByType,
  getDocumentsByCorrespondent,
  getArchivedDocuments,
  searchDocuments,
  getDocumentsByDateRange,
  syncDocumentsFromPaperless,
  syncSingleDocumentFromPaperless,
  getPaperlessStatus,
} from '../api/documentService';
import { documentKeys, mediaKeys } from '../api/queryKeys';

export function useAllDocuments(options = {}) {
  return useQuery({
    queryKey: documentKeys.lists(),
    queryFn: () => getAllDocuments(),
    ...options,
  });
}

export function useDocument(id, options = {}) {
  return useQuery({
    queryKey: documentKeys.detail(id),
    queryFn: () => getDocumentById(id),
    enabled: !!id,
    ...options,
  });
}

export function useDocumentsByType(documentType, options = {}) {
  return useQuery({
    queryKey: [...documentKeys.lists(), { documentType }],
    queryFn: () => getDocumentsByType(documentType),
    enabled: !!documentType,
    ...options,
  });
}

export function useDocumentsByCorrespondent(correspondent, options = {}) {
  return useQuery({
    queryKey: [...documentKeys.lists(), { correspondent }],
    queryFn: () => getDocumentsByCorrespondent(correspondent),
    enabled: !!correspondent,
    ...options,
  });
}

export function useArchivedDocuments(options = {}) {
  return useQuery({
    queryKey: [...documentKeys.lists(), 'archived'],
    queryFn: () => getArchivedDocuments(),
    ...options,
  });
}

export function useDocumentSearch(query, options = {}) {
  return useQuery({
    queryKey: [...documentKeys.all, 'search', query],
    queryFn: () => searchDocuments(query),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useDocumentsByDateRange(startDate, endDate, options = {}) {
  return useQuery({
    queryKey: [...documentKeys.lists(), { startDate, endDate }],
    queryFn: () => getDocumentsByDateRange(startDate, endDate),
    enabled: !!startDate && !!endDate,
    ...options,
  });
}

export function usePaperlessStatus(options = {}) {
  return useQuery({
    queryKey: [...documentKeys.all, 'paperlessStatus'],
    queryFn: () => getPaperlessStatus(),
    ...options,
  });
}

export function useCreateDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentData) => createDocument(documentData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: documentKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdateDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, documentData }) => updateDocument(id, documentData),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: documentKeys.lists() });
      queryClient.invalidateQueries({ queryKey: documentKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeleteDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteDocument(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: documentKeys.lists() });
      queryClient.removeQueries({ queryKey: documentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useSyncDocumentsFromPaperless() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => syncDocumentsFromPaperless(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: documentKeys.all });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useSyncSingleDocumentFromPaperless() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (paperlessId) => syncSingleDocumentFromPaperless(paperlessId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: documentKeys.all });
    },
  });
}
