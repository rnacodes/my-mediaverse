import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  searchYouTube,
  getYouTubeVideoDetails,
  getYouTubeVideos,
  getYouTubePlaylistDetails,
  getYouTubePlaylistItems,
  getAllYouTubePlaylistItems,
  getYouTubeChannelDetails,
  getYouTubeChannelByUsername,
  getYouTubeChannelUploads,
  importYouTubeVideo,
  getAllYouTubeChannels,
  getYouTubeChannelById,
  getYouTubeChannelByExternalId,
  getYouTubeChannelVideos,
  createYouTubeChannel,
  updateYouTubeChannel,
  deleteYouTubeChannel,
  importYouTubeChannelEntity,
  syncYouTubeChannelMetadata,
  checkYouTubeChannelExists,
  importFromYouTubeUrl,
  getAllYouTubePlaylists,
  getYouTubePlaylistById,
  getYouTubePlaylistByExternalId,
  getYouTubePlaylistVideos,
  importYouTubePlaylistEntity,
  syncYouTubePlaylist,
  addVideoToYouTubePlaylist,
  removeVideoFromYouTubePlaylist,
  deleteYouTubePlaylist,
} from '../api/youtubeService';
import { youtubeKeys, mediaKeys, videoKeys } from '../api/queryKeys';

// ----- External (YouTube Data API) queries -----

export function useYouTubeExternalSearch(query, type = 'video', maxResults = 25, pageToken = null, channelId = null, options = {}) {
  return useQuery({
    queryKey: youtubeKeys.externalSearch(query, type, channelId),
    queryFn: () => searchYouTube(query, type, maxResults, pageToken, channelId),
    enabled: !!query && query.length > 0,
    ...options,
  });
}

export function useYouTubeVideoDetails(videoId, options = {}) {
  return useQuery({
    queryKey: youtubeKeys.videos.detail(videoId),
    queryFn: () => getYouTubeVideoDetails(videoId),
    enabled: !!videoId,
    ...options,
  });
}

export function useYouTubeVideosBatch(videoIds, options = {}) {
  return useQuery({
    queryKey: [...youtubeKeys.videos.all, 'batch', videoIds],
    queryFn: () => getYouTubeVideos(videoIds),
    enabled: !!videoIds && videoIds.length > 0,
    ...options,
  });
}

export function useYouTubePlaylistDetails(playlistId, options = {}) {
  return useQuery({
    queryKey: [...youtubeKeys.playlists.all, 'externalDetail', playlistId],
    queryFn: () => getYouTubePlaylistDetails(playlistId),
    enabled: !!playlistId,
    ...options,
  });
}

export function useYouTubePlaylistItems(playlistId, maxResults = 50, pageToken = null, options = {}) {
  return useQuery({
    queryKey: [...youtubeKeys.playlists.all, 'externalItems', playlistId, { maxResults, pageToken }],
    queryFn: () => getYouTubePlaylistItems(playlistId, maxResults, pageToken),
    enabled: !!playlistId,
    ...options,
  });
}

export function useAllYouTubePlaylistItems(playlistId, options = {}) {
  return useQuery({
    queryKey: [...youtubeKeys.playlists.all, 'externalAllItems', playlistId],
    queryFn: () => getAllYouTubePlaylistItems(playlistId),
    enabled: !!playlistId,
    ...options,
  });
}

export function useYouTubeChannelDetails(channelId, options = {}) {
  return useQuery({
    queryKey: [...youtubeKeys.channels.all, 'externalDetail', channelId],
    queryFn: () => getYouTubeChannelDetails(channelId),
    enabled: !!channelId,
    ...options,
  });
}

export function useYouTubeChannelByUsername(username, options = {}) {
  return useQuery({
    queryKey: [...youtubeKeys.channels.all, 'externalByUsername', username],
    queryFn: () => getYouTubeChannelByUsername(username),
    enabled: !!username,
    ...options,
  });
}

export function useYouTubeChannelUploads(channelId, maxResults = 25, pageToken = null, options = {}) {
  return useQuery({
    queryKey: [...youtubeKeys.channels.all, 'externalUploads', channelId, { maxResults, pageToken }],
    queryFn: () => getYouTubeChannelUploads(channelId, maxResults, pageToken),
    enabled: !!channelId,
    ...options,
  });
}

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

export function useYouTubeChannelByExternalId(externalId, options = {}) {
  return useQuery({
    queryKey: youtubeKeys.channels.byExternalId(externalId),
    queryFn: () => getYouTubeChannelByExternalId(externalId),
    enabled: !!externalId,
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

export function useCheckYouTubeChannelExists(externalId, options = {}) {
  return useQuery({
    queryKey: [...youtubeKeys.channels.all, 'exists', externalId],
    queryFn: () => checkYouTubeChannelExists(externalId),
    enabled: !!externalId,
    ...options,
  });
}

export function useAllYouTubePlaylists(options = {}) {
  return useQuery({
    queryKey: youtubeKeys.playlists.lists(),
    queryFn: () => getAllYouTubePlaylists(),
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

export function useYouTubePlaylistByExternalId(externalId, includeVideos = false, options = {}) {
  return useQuery({
    queryKey: youtubeKeys.playlists.byExternalId(externalId, includeVideos),
    queryFn: () => getYouTubePlaylistByExternalId(externalId, includeVideos),
    enabled: !!externalId,
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

export function useImportYouTubeVideo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (videoId) => importYouTubeVideo(videoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: videoKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useCreateYouTubeChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (channelData) => createYouTubeChannel(channelData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.channels.lists() });
    },
  });
}

export function useUpdateYouTubeChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, channelData }) => updateYouTubeChannel(id, channelData),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.channels.lists() });
      queryClient.invalidateQueries({ queryKey: youtubeKeys.channels.detail(variables.id) });
    },
  });
}

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

export function useImportYouTubeChannelEntity() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (channelId) => importYouTubeChannelEntity(channelId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.channels.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
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

export function useImportFromYouTubeUrl() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (url) => importFromYouTubeUrl(url),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.all });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useImportYouTubePlaylistEntity() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (playlistExternalId) => importYouTubePlaylistEntity(playlistExternalId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: youtubeKeys.playlists.lists() });
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

export function useRemoveVideoFromYouTubePlaylist() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ playlistId, videoId }) => removeVideoFromYouTubePlaylist(playlistId, videoId),
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
