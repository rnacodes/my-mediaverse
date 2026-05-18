import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAllVideos,
  getVideoById,
  getVideosByChannel,
  getVideoSeries,
  createVideo,
  updateVideo,
  deleteVideo,
  getPlaylistsForVideo,
} from '../api/videoService';
import { videoKeys, mediaKeys } from '../api/queryKeys';

export function useAllVideos(options = {}) {
  return useQuery({
    queryKey: videoKeys.lists(),
    queryFn: async () => (await getAllVideos()).data,
    ...options,
  });
}

export function useVideo(id, options = {}) {
  return useQuery({
    queryKey: videoKeys.detail(id),
    queryFn: async () => (await getVideoById(id)).data,
    enabled: !!id,
    ...options,
  });
}

export function useVideosByChannel(channelName, options = {}) {
  return useQuery({
    queryKey: [...videoKeys.lists(), { channelName }],
    queryFn: async () => (await getVideosByChannel(channelName)).data,
    enabled: !!channelName,
    ...options,
  });
}

export function useVideoSeries(options = {}) {
  return useQuery({
    queryKey: [...videoKeys.all, 'series'],
    queryFn: async () => (await getVideoSeries()).data,
    ...options,
  });
}

export function usePlaylistsForVideo(videoId, options = {}) {
  return useQuery({
    queryKey: [...videoKeys.detail(videoId), 'playlists'],
    queryFn: () => getPlaylistsForVideo(videoId),
    enabled: !!videoId,
    ...options,
  });
}

export function useCreateVideo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (videoData) => createVideo(videoData).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: videoKeys.lists() });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useUpdateVideo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, videoData }) => updateVideo(id, videoData).then((r) => r.data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: videoKeys.lists() });
      queryClient.invalidateQueries({ queryKey: videoKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}

export function useDeleteVideo() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id) => deleteVideo(id).then((r) => r.data),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: videoKeys.lists() });
      queryClient.removeQueries({ queryKey: videoKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    },
  });
}
