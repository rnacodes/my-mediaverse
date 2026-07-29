import { apiClient } from './apiClient';

// ============================================
// Mixlist API calls
// ============================================

export const getAllMixlists = () => {
    return apiClient.get('/mixlist');
};

export const createMixlist = (mixlistData) => {
    return apiClient.post('/mixlist', mixlistData);
};

export const addMediaToMixlist = (mixlistId, mediaItemId) => {
    return apiClient.post(`/mixlist/${mixlistId}/items/${mediaItemId}`);
};

export const getMixlistById = (id) => {
    return apiClient.get(`/mixlist/${id}`);
};

export const updateMixlist = (id, mixlistData) => {
    return apiClient.put(`/mixlist/${id}`, mixlistData);
};

export const deleteMixlist = (id) => {
    return apiClient.delete(`/mixlist/${id}`);
};

export const removeMediaFromMixlist = (mixlistId, mediaItemId) => {
    return apiClient.delete(`/mixlist/${mixlistId}/items/${mediaItemId}`);
};

export const seedMixlists = () => {
    return apiClient.post('/dev/seed-mixlists');
};

export const importMixlists = (mixlists) => {
    return apiClient.post('/mixlist/import', mixlists);
};

// ============================================
// Mixlist-Note linking API calls
// ============================================

export const getNotesForMixlist = (mixlistId) => {
    return apiClient.get(`/mixlist/${mixlistId}/notes`);
};

export const linkNoteToMixlist = (mixlistId, noteId, linkDescription) => {
    return apiClient.post(`/mixlist/${mixlistId}/notes`, { noteId, linkDescription });
};

export const unlinkNoteFromMixlist = (mixlistId, noteId) => {
    return apiClient.delete(`/mixlist/${mixlistId}/notes/${noteId}`);
};
