import { apiClient } from './apiClient';

// ============================================
// YouTube API calls
// ============================================

export const searchYouTube = async (query, type = 'video', maxResults = 25, pageToken = null, channelId = null) => {
    try {
        const params = new URLSearchParams({
            query,
            type,
            maxResults: maxResults.toString()
        });

        if (pageToken) params.append('pageToken', pageToken);
        if (channelId) params.append('channelId', channelId);

        const response = await apiClient.get(`/youtube/search?${params}`);
        return response.data;
    } catch (error) {
        console.error('Error searching YouTube:', error);
        throw error;
    }
};

export const getYouTubeVideoDetails = async (videoId) => {
    try {
        const response = await apiClient.get(`/youtube/videos/${videoId}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube video details:', error);
        throw error;
    }
};

export const getYouTubeVideos = async (videoIds) => {
    try {
        const idsString = Array.isArray(videoIds) ? videoIds.join(',') : videoIds;
        const response = await apiClient.get(`/youtube/videos?videoIds=${encodeURIComponent(idsString)}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube videos:', error);
        throw error;
    }
};

export const getYouTubePlaylistDetails = async (playlistId) => {
    try {
        const response = await apiClient.get(`/youtube/playlists/${playlistId}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube playlist details:', error);
        throw error;
    }
};

export const getYouTubePlaylistItems = async (playlistId, maxResults = 50, pageToken = null) => {
    try {
        const params = new URLSearchParams({
            maxResults: maxResults.toString()
        });

        if (pageToken) params.append('pageToken', pageToken);

        const response = await apiClient.get(`/youtube/playlists/${playlistId}/items?${params}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube playlist items:', error);
        throw error;
    }
};

export const getAllYouTubePlaylistItems = async (playlistId) => {
    try {
        const response = await apiClient.get(`/youtube/playlists/${playlistId}/all-items`);
        return response.data;
    } catch (error) {
        console.error('Error getting all YouTube playlist items:', error);
        throw error;
    }
};

export const getYouTubeChannelDetails = async (channelId) => {
    try {
        const response = await apiClient.get(`/youtube/channels/${channelId}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube channel details:', error);
        throw error;
    }
};

export const getYouTubeChannelByUsername = async (username) => {
    try {
        const response = await apiClient.get(`/youtube/channels/by-username/${username}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube channel by username:', error);
        throw error;
    }
};

export const getYouTubeChannelUploads = async (channelId, maxResults = 25, pageToken = null) => {
    try {
        const params = new URLSearchParams({
            maxResults: maxResults.toString()
        });

        if (pageToken) params.append('pageToken', pageToken);

        const response = await apiClient.get(`/youtube/channels/${channelId}/uploads?${params}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube channel uploads:', error);
        throw error;
    }
};

export const importYouTubeVideo = async (videoId) => {
    try {
        const response = await apiClient.post(`/youtube/import/video/${videoId}`);
        return response.data;
    } catch (error) {
        console.error('Error importing YouTube video:', error);
        throw error;
    }
};

// ============================================
// YouTube Channel Management API calls
// ============================================

export const getAllYouTubeChannels = async () => {
    try {
        const response = await apiClient.get('/youtubechannel');
        return response.data;
    } catch (error) {
        console.error('Error getting all YouTube channels:', error);
        throw error;
    }
};

export const getYouTubeChannelById = async (id) => {
    try {
        const response = await apiClient.get(`/youtubechannel/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube channel by ID:', error);
        throw error;
    }
};

export const getYouTubeChannelByExternalId = async (externalId) => {
    try {
        const response = await apiClient.get(`/youtubechannel/by-external/${externalId}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube channel by external ID:', error);
        throw error;
    }
};

export const getYouTubeChannelVideos = async (channelId) => {
    try {
        const response = await apiClient.get(`/youtubechannel/${channelId}/videos`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube channel videos:', error);
        throw error;
    }
};

export const createYouTubeChannel = async (channelData) => {
    try {
        const response = await apiClient.post('/youtubechannel', channelData);
        return response.data;
    } catch (error) {
        console.error('Error creating YouTube channel:', error);
        throw error;
    }
};

export const updateYouTubeChannel = async (id, channelData) => {
    try {
        const response = await apiClient.put(`/youtubechannel/${id}`, channelData);
        return response.data;
    } catch (error) {
        console.error('Error updating YouTube channel:', error);
        throw error;
    }
};

export const deleteYouTubeChannel = async (id) => {
    try {
        await apiClient.delete(`/youtubechannel/${id}`);
    } catch (error) {
        console.error('Error deleting YouTube channel:', error);
        throw error;
    }
};

export const importYouTubeChannelEntity = async (channelId) => {
    try {
        const response = await apiClient.post(`/youtubechannel/import/${channelId}`);
        return response.data;
    } catch (error) {
        console.error('Error importing YouTube channel entity:', error);
        throw error;
    }
};

export const syncYouTubeChannelMetadata = async (id) => {
    try {
        const response = await apiClient.post(`/youtubechannel/${id}/sync`);
        return response.data;
    } catch (error) {
        console.error('Error syncing YouTube channel metadata:', error);
        throw error;
    }
};

export const checkYouTubeChannelExists = async (externalId) => {
    try {
        const response = await apiClient.get(`/youtubechannel/exists/${externalId}`);
        return response.data.exists;
    } catch (error) {
        console.error('Error checking if YouTube channel exists:', error);
        throw error;
    }
};

export const importFromYouTubeUrl = async (url) => {
    try {
        const response = await apiClient.post('/youtube/import/url', { url });
        return response.data;
    } catch (error) {
        console.error('Error importing from YouTube URL:', error);
        throw error;
    }
};

// ============================================
// YouTube Playlist Management API calls
// ============================================

export const getAllYouTubePlaylists = async () => {
    try {
        const response = await apiClient.get('/youtubeplaylist');
        return response.data;
    } catch (error) {
        console.error('Error getting all YouTube playlists:', error);
        throw error;
    }
};

export const getYouTubePlaylistById = async (id, includeVideos = false) => {
    try {
        const params = new URLSearchParams({
            includeVideos: includeVideos.toString()
        });
        const response = await apiClient.get(`/youtubeplaylist/${id}?${params}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube playlist by ID:', error);
        throw error;
    }
};

export const getYouTubePlaylistByExternalId = async (externalId, includeVideos = false) => {
    try {
        const params = new URLSearchParams({
            includeVideos: includeVideos.toString()
        });
        const response = await apiClient.get(`/youtubeplaylist/by-external/${externalId}?${params}`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube playlist by external ID:', error);
        throw error;
    }
};

export const getYouTubePlaylistVideos = async (id) => {
    try {
        const response = await apiClient.get(`/youtubeplaylist/${id}/videos`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube playlist videos:', error);
        throw error;
    }
};

export const importYouTubePlaylistEntity = async (playlistExternalId) => {
    try {
        const response = await apiClient.post(`/youtubeplaylist/import/${playlistExternalId}`);
        return response.data;
    } catch (error) {
        console.error('Error importing YouTube playlist:', error);
        throw error;
    }
};

export const syncYouTubePlaylist = async (id) => {
    try {
        const response = await apiClient.post(`/youtubeplaylist/${id}/sync`);
        return response.data;
    } catch (error) {
        console.error('Error syncing YouTube playlist:', error);
        throw error;
    }
};

export const addVideoToYouTubePlaylist = async (playlistId, videoId, position = null) => {
    try {
        const params = position !== null ? `?position=${position}` : '';
        const response = await apiClient.post(`/youtubeplaylist/${playlistId}/videos/${videoId}${params}`);
        return response.data;
    } catch (error) {
        console.error('Error adding video to YouTube playlist:', error);
        throw error;
    }
};

export const removeVideoFromYouTubePlaylist = async (playlistId, videoId) => {
    try {
        const response = await apiClient.delete(`/youtubeplaylist/${playlistId}/videos/${videoId}`);
        return response.data;
    } catch (error) {
        console.error('Error removing video from YouTube playlist:', error);
        throw error;
    }
};

export const deleteYouTubePlaylist = async (id) => {
    try {
        const response = await apiClient.delete(`/youtubeplaylist/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error deleting YouTube playlist:', error);
        throw error;
    }
};
