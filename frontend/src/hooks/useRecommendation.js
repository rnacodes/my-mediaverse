import { useQuery } from '@tanstack/react-query';
import {
  getRecommendationStatus,
  getSimilarMedia,
  getSimilarNotes,
  searchByVibe,
  getForYouRecommendations,
  getMediaForNote,
} from '../api/recommendationService';
import { recommendationKeys } from '../api/queryKeys';

export function useRecommendationStatus(options = {}) {
  return useQuery({
    queryKey: [...recommendationKeys.all, 'status'],
    queryFn: () => getRecommendationStatus(),
    ...options,
  });
}

export function useSimilarMedia(id, limit = 10, mediaType = null, options = {}) {
  return useQuery({
    queryKey: [...recommendationKeys.all, 'similar', 'media', id, { limit, mediaType }],
    queryFn: () => getSimilarMedia(id, limit, mediaType),
    enabled: !!id,
    ...options,
  });
}

export function useSimilarNotes(id, limit = 10, vault = null, options = {}) {
  return useQuery({
    queryKey: [...recommendationKeys.all, 'similar', 'note', id, { limit, vault }],
    queryFn: () => getSimilarNotes(id, limit, vault),
    enabled: !!id,
    ...options,
  });
}

export function useVibeSearch(query, mediaType = null, limit = 20, options = {}) {
  return useQuery({
    queryKey: [...recommendationKeys.all, 'vibe', { query, mediaType, limit }],
    queryFn: () => searchByVibe(query, mediaType, limit),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useForYouRecommendations(limit = 20, mediaType = null, options = {}) {
  return useQuery({
    queryKey: [...recommendationKeys.all, 'forYou', { limit, mediaType }],
    queryFn: () => getForYouRecommendations(limit, mediaType),
    ...options,
  });
}

export function useMediaForNoteByEmbedding(noteId, limit = 10, mediaType = null, options = {}) {
  return useQuery({
    queryKey: [...recommendationKeys.all, 'mediaForNote', noteId, { limit, mediaType }],
    queryFn: () => getMediaForNote(noteId, limit, mediaType),
    enabled: !!noteId,
    ...options,
  });
}

