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

// The import endpoints answer 201 when they created the item and 200 when the
// library already held it. Callers get that as `alreadyInLibrary` beside the body.
const withImportStatus = (response) => ({
    ...response.data,
    alreadyInLibrary: response.status === 200
});

export const importYouTubeVideo = async (videoId) => {
    try {
        const response = await apiClient.post(`/youtube/import/video/${videoId}`);
        return withImportStatus(response);
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

export const getYouTubeChannelVideos = async (channelId) => {
    try {
        const response = await apiClient.get(`/youtubechannel/${channelId}/videos`);
        return response.data;
    } catch (error) {
        console.error('Error getting YouTube channel videos:', error);
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
        return withImportStatus(response);
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

export const importLatestYouTubeChannelUploads = async (id, count = null) => {
    try {
        const query = count ? `?count=${count}` : '';
        const response = await apiClient.post(`/youtubechannel/${id}/import-latest${query}`);
        return response.data;
    } catch (error) {
        console.error('Error importing latest YouTube channel uploads:', error);
        throw error;
    }
};
export const importFromYouTubeUrl = async (url) => {
    try {
        const response = await apiClient.post('/youtube/import/url', { url });
        return withImportStatus(response);
    } catch (error) {
        console.error('Error importing from YouTube URL:', error);
        throw error;
    }
};

// ============================================
// YouTube Playlist Management API calls
// ============================================

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
        return withImportStatus(response);
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

export const deleteYouTubePlaylist = async (id) => {
    try {
        const response = await apiClient.delete(`/youtubeplaylist/${id}`);
        return response.data;
    } catch (error) {
        console.error('Error deleting YouTube playlist:', error);
        throw error;
    }
};
