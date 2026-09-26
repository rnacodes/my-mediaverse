import { apiClient } from './apiClient';

// ============================================
// Movie API calls
// ============================================

export const getAllMovies = () => {
    return apiClient.get('/movie');
};

export const getMovieById = (id) => {
    return apiClient.get(`/movie/${id}`);
};

export const getMoviesByDirector = (director) => {
    return apiClient.get(`/movie/by-director/${encodeURIComponent(director)}`);
};

export const getMoviesByYear = (year) => {
    return apiClient.get(`/movie/by-year/${year}`);
};

export const createMovie = (movieData) => {
    return apiClient.post('/movie', movieData);
};

export const updateMovie = (id, movieData) => {
    return apiClient.put(`/movie/${id}`, movieData);
};

export const deleteMovie = (id) => {
    return apiClient.delete(`/movie/${id}`);
};

// The API answers 201 when the movie was just created and 200 when a movie with
// the same TMDB id was already in the library (the existing row is returned
// untouched). Callers need both the item and which of the two happened.
export const importMovieFromTmdb = async (movieId) => {
    try {
        const response = await apiClient.post(`/movie/from-tmdb/${movieId}`);
        return { item: response.data, created: response.status === 201 };
    } catch (error) {
        console.error('Error importing movie from TMDB:', error);
        throw error;
    }
};

export const searchMoviesFromTmdb = async (query, page = 1) => {
    try {
        const response = await apiClient.get(`/movie/search-tmdb?query=${encodeURIComponent(query)}&page=${page}`);
        return response.data;
    } catch (error) {
        console.error('Error searching movies from TMDB:', error);
        throw error;
    }
};
