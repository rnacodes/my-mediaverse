import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllYouTubeChannels,
  getYouTubeChannelById,
  getYouTubeChannelVideos,
  deleteYouTubeChannel,
  syncYouTubeChannelMetadata,
  importLatestYouTubeChannelUploads,
  getYouTubePlaylistById,
  getYouTubePlaylistVideos,
  syncYouTubePlaylist,
  addVideoToYouTubePlaylist,
  deleteYouTubePlaylist,
} from '../api/youtubeService';
import { youtubeKeys, mediaKeys } from '../api/queryKeys';

// ----- External (YouTube Data API) queries -----

// ----- Managed channel/playlist queries -----

export function useAllYouTubeChannels(options = {}) {
  return useQuery({
    queryKey: youtubeKeys.channels.lists(),
    queryFn: () => getAllYouTubeChannels(),
    ...options,
  });
}

export function useYouTubeChannel(id, options = {}) {
  return useQuery({
    queryKey: youtubeKeys.channels.detail(id),
    queryFn: () => getYouTubeChannelById(id),
    enabled: !!id,
    ...options,
  });
}

export function useYouTubeChannelVideos(channelId, options = {}) {
  return useQuery({
    queryKey: youtubeKeys.channels.videos(channelId),
    queryFn: () => getYouTubeChannelVideos(channelId),
    enabled: !!channelId,
    ...options,
  });
}

export function useYouTubePlaylist(id, includeVideos = false, options = {}) {
  return useQuery({
    queryKey: youtubeKeys.playlists.detail(id, includeVideos),
    queryFn: () => getYouTubePlaylistById(id, includeVideos),
    enabled: !!id,
    ...options,
  });
}

export function useYouTubePlaylistVideos(id, options = {}) {
  return useQuery({
    queryKey: youtubeKeys.playlists.videos(id),
    queryFn: () => getYouTubePlaylistVideos(id),
    enabled: !!id,
    ...options,
  });
}

// ----- Mutations -----

export function useDeleteYouTubeChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteYouTubeChannel(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.channels.lists() });
      queryClient.removeQueries({ queryKey: youtubeKeys.channels.detail(id) });
    },
  });
}

export function useSyncYouTubeChannelMetadata() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => syncYouTubeChannelMetadata(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.channels.detail(id) });
    },
  });
}

export function useImportLatestYouTubeChannelUploads() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, count }) => importLatestYouTubeChannelUploads(id, count),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.channels.detail(id) });
      queryClient.invalidateQueries({ queryKey: youtubeKeys.channels.videos(id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useSyncYouTubePlaylist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => syncYouTubePlaylist(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.playlists.detail(id) });
      queryClient.invalidateQueries({ queryKey: youtubeKeys.playlists.videos(id) });
    },
  });
}

export function useAddVideoToYouTubePlaylist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ playlistId, videoId, position }) =>
      addVideoToYouTubePlaylist(playlistId, videoId, position),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.playlists.videos(variables.playlistId) });
      queryClient.invalidateQueries({ queryKey: youtubeKeys.playlists.detail(variables.playlistId) });
    },
  });
}

export function useDeleteYouTubePlaylist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteYouTubePlaylist(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.playlists.lists() });
      queryClient.removeQueries({ queryKey: youtubeKeys.playlists.detail(id) });
    },
  });
}
