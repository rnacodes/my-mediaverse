import { searchMediaViaTypesense, searchMixlistsViaTypesense } from '../api/typesenseService';

// Quick-search used by the homepage/nav SearchBar dropdown. Backed by Typesense
// (typo-tolerant, ranked) so it matches the full Search page rather than the old
// Postgres substring search.
export const searchAll = async (query) => {
    if (!query || !query.trim()) {
        return { media: [], mixlists: [] };
    }

    try {
        const [media, mixlists] = await Promise.all([
            searchMediaViaTypesense(query.trim()),
            searchMixlistsViaTypesense(query.trim()),
        ]);

        return { media, mixlists };
    } catch (error) {
        console.error('searchService.searchAll failed:', error.response?.data || error.message);
        return { media: [], mixlists: [] };
    }
};
