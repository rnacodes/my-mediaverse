import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  cleanupYouTubeData,
  cleanupPodcasts,
  cleanupBooks,
  cleanupMovies,
  cleanupTvShows,
  cleanupArticles,
  cleanupHighlights,
  cleanupMixlists,
  cleanupAllTopics,
  cleanupAllGenres,
  cleanupOrphanedTopics,
  cleanupOrphanedGenres,
  cleanupWebsites,
  cleanupChannels,
  cleanupPlaylists,
  cleanupNotes,
  cleanupDocuments,
  cleanupVideos,
  cleanupAllMedia,
} from '../api/devService';
import {
  mediaKeys,
  mixlistKeys,
  podcastKeys,
  bookKeys,
  movieKeys,
  tvShowKeys,
  videoKeys,
  articleKeys,
  documentKeys,
  noteKeys,
  highlightKeys,
  websiteKeys,
  youtubeKeys,
  topicKeys,
  genreKeys,
} from '../api/queryKeys';

// All cleanup endpoints invalidate the matching resource family. Components
// that show enrichment stats can re-trigger their own queries via these.

function makeCleanupMutation(fn, invalidateKeys = []) {
  return function useCleanup() {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: () => fn(),
      onSuccess: () => {
        invalidateKeys.forEach((key) => queryClient.invalidateQueries({ queryKey: key }));
      },
    });
  };
}

export const useCleanupYouTubeData = makeCleanupMutation(cleanupYouTubeData, [
  youtubeKeys.all,
  mediaKeys.lists(),
]);
export const useCleanupPodcasts = makeCleanupMutation(cleanupPodcasts, [podcastKeys.all, mediaKeys.lists()]);
export const useCleanupBooks = makeCleanupMutation(cleanupBooks, [bookKeys.all, mediaKeys.lists()]);
export const useCleanupMovies = makeCleanupMutation(cleanupMovies, [movieKeys.all, mediaKeys.lists()]);
export const useCleanupTvShows = makeCleanupMutation(cleanupTvShows, [tvShowKeys.all, mediaKeys.lists()]);
export const useCleanupArticles = makeCleanupMutation(cleanupArticles, [articleKeys.all, mediaKeys.lists()]);
export const useCleanupHighlights = makeCleanupMutation(cleanupHighlights, [highlightKeys.all]);
export const useCleanupMixlists = makeCleanupMutation(cleanupMixlists, [mixlistKeys.all]);
export const useCleanupAllTopics = makeCleanupMutation(cleanupAllTopics, [topicKeys.all]);
export const useCleanupAllGenres = makeCleanupMutation(cleanupAllGenres, [genreKeys.all]);
export const useCleanupOrphanedTopics = makeCleanupMutation(cleanupOrphanedTopics, [topicKeys.all]);
export const useCleanupOrphanedGenres = makeCleanupMutation(cleanupOrphanedGenres, [genreKeys.all]);
export const useCleanupWebsites = makeCleanupMutation(cleanupWebsites, [websiteKeys.all, mediaKeys.lists()]);
export const useCleanupChannels = makeCleanupMutation(cleanupChannels, [youtubeKeys.channels.all, mediaKeys.lists()]);
export const useCleanupPlaylists = makeCleanupMutation(cleanupPlaylists, [youtubeKeys.playlists.all, mediaKeys.lists()]);
export const useCleanupNotes = makeCleanupMutation(cleanupNotes, [noteKeys.all]);
export const useCleanupDocuments = makeCleanupMutation(cleanupDocuments, [documentKeys.all, mediaKeys.lists()]);
export const useCleanupVideos = makeCleanupMutation(cleanupVideos, [videoKeys.all, mediaKeys.lists()]);
export const useCleanupAllMedia = makeCleanupMutation(cleanupAllMedia, [mediaKeys.all]);
