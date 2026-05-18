import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  uploadCsv,
  uploadThumbnail,
  uploadThumbnailFromUrl,
  uploadGoodreadsCsv,
} from '../api/uploadService';
import { mediaKeys, bookKeys } from '../api/queryKeys';

export function useUploadCsv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ file, mediaType }) => uploadCsv(file, mediaType).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUploadThumbnail() {
  return useMutation({
    mutationFn: (file) => uploadThumbnail(file).then((r) => r.data),
  });
}

export function useUploadThumbnailFromUrl() {
  return useMutation({
    mutationFn: (url) => uploadThumbnailFromUrl(url).then((r) => r.data),
  });
}

export function useUploadGoodreadsCsv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ file, updateExisting = true, chunkIndex = null, totalChunks = null }) =>
      uploadGoodreadsCsv(file, updateExisting, chunkIndex, totalChunks).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: bookKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}
