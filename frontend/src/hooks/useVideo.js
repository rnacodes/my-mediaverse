import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getVideoById,
  createVideo,
  updateVideo,
  getPlaylistsForVideo,
} from '../api/videoService';
import { videoKeys, mediaKeys } from '../api/queryKeys';

export function useVideo(id, options = {}) {
  return useQuery({
    queryKey: videoKeys.detail(id),
    queryFn: async () => (await getVideoById(id)).data,
    enabled: !!id,
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
