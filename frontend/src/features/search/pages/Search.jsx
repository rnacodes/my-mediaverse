import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Container, Box, Typography, Grid, Button, ButtonGroup, Collapse, CircularProgress, Paper, Alert, Toolbar, Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions, Snackbar, FormControl, InputLabel, Select, MenuItem } from '@mui/material';
import { SearchBarSection } from '../SearchBarSection';
import { SearchFilterSidebar } from '../SearchFilterSidebar';
import { Search as SearchIcon, Delete, CheckBox, CheckBoxOutlineBlank, PlaylistAdd } from '@mui/icons-material';
import { ResultHeader } from '../ResultHeader';
import SearchViewToggles from '../SearchViewToggles';
import { SearchResultCard } from '../SearchResultCard';
import { MediaListItem } from '../MediaListItem';
import { typesenseAdvancedSearch, typesenseAdvancedSearchMixlists, searchHighlights } from '@/api/typesenseService';
import { searchNotes } from '@/api/noteService';
import { useAllTopics, useAllGenres } from '@/hooks/useTopicGenre';
import { useAllMixlists, useAddMediaToMixlist, useLinkNoteToMixlist } from '@/hooks/useMixlist';
import { useBulkDeleteMedia } from '@/hooks/useMedia';
import { useBulkDeleteHighlights } from '@/hooks/useHighlight';
import { useBulkDeleteNotes } from '@/hooks/useNote';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';



const mediaTypeOptions = [
    { value: 'all', label: 'All Media Types' },
    { value: 'Article', label: 'Articles' },
    { value: 'Book', label: 'Books' },
    { value: 'Channel', label: 'Channels' },
    { value: 'Movie', label: 'Movies' },
    { value: 'Playlist', label: 'Playlists' },
    { value: 'Podcast', label: 'Podcasts' },
    { value: 'TVShow', label: 'TV Shows' },
    { value: 'Video', label: 'Videos' },
    { value: 'Website', label: 'Websites' }
];

// HELPER FUNCTIONS

// Transform Typesense note hits into the shared result shape
const transformNoteHits = (hits) => hits.map(hit => {
    const doc = hit.document;
    return {
        id: doc.id,
        title: doc.title,
        mediaType: 'Note',
        status: null,
        ratingType: null,
        topics: doc.tags || [],
        genres: [],
        author: doc.vault_name || 'Unknown Vault',
        dateAdded: doc.date_imported ? new Date(doc.date_imported * 1000).toISOString().split('T')[0] : null,
        notes: doc.description || '',
        thumbnail: null,
        isMixlist: false,
        isNote: true,
        isHighlight: false,
        sourceUrl: doc.source_url,
        vaultName: doc.vault_name,
        linkedMediaCount: doc.linked_media_count || 0
    };
});

// Transform Typesense media hits into the shared result shape
const transformMediaHits = (hits) => hits.map(hit => {
    const doc = hit.document;
    return {
        id: doc.id,
        title: doc.title,
        mediaType: doc.media_type,
        status: doc.status,
        ratingType: doc.rating?.toLowerCase() || null,
        topics: doc.topics || [],
        genres: doc.genres || [],
        dateAdded: new Date(doc.date_added * 1000).toISOString().split('T')[0],
        notes: doc.description || '',
        thumbnail: doc.thumbnail,
        isMixlist: false,
        isNote: false,
        isHighlight: false,
        author: doc.author || null,
        director: doc.director || null,
        creator: doc.creator || null,
        publisher: doc.publisher || null,
        channel: doc.channel_title || doc.channel || null,
        platform: doc.platform || null,
        goodreadsRating: doc.goodreads_rating || null,
        tmdbRating: doc.tmdb_rating || null,
        releaseYear: doc.release_year || null,
        runtimeMinutes: doc.runtime_minutes || null,
        lengthInSeconds: doc.length_in_seconds || null,
        durationInSeconds: doc.duration_in_seconds || null,
        seriesId: doc.series_id || null,
        podcastType: doc.podcast_type || null,
        publication: doc.publication || null,
        estimatedReadingTimeMinutes: doc.estimated_reading_time_minutes || null,
        wordCount: doc.word_count || null,
        isStarred: doc.is_starred || false
    };
});

// Transform Typesense highlight hits into the shared result shape
const transformHighlightHits = (hits) => hits.map(hit => {
    const doc = hit.document;
    return {
        id: doc.id,
        title: doc.title || 'Untitled Highlight',
        mediaType: 'Highlight',
        status: null,
        ratingType: null,
        topics: doc.tags || [],
        genres: [],
        author: doc.author || null,
        dateAdded: doc.created_at ? new Date(doc.created_at * 1000).toISOString().split('T')[0] : null,
        notes: doc.text || '', // The highlight text
        thumbnail: doc.image_url,
        isMixlist: false,
        isNote: false,
        isHighlight: true,
        highlightText: doc.text,
        highlightNote: doc.note,
        category: doc.category,
        linkedMediaId: doc.linked_media_id,
        linkedMediaTitle: doc.linked_media_title,
        linkedMediaType: doc.linked_media_type
    };
});

// MAIN COMPONENT
export default function Search({ defaultMediaTypes = [] }) {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const [searchQuery, setSearchQuery] = useState('');
    const [viewMode, setViewMode] = useState('card');
    const [sortBy, setSortBy] = useState('relevance');
    const [searchMode, setSearchMode] = useState('media'); // 'media', 'mixlists', 'notes', or 'highlights'
    // Selection maps each id to its kind ('media' | 'highlight' | 'note' | 'mixlist') so
    // bulk actions can route ids to the right endpoints, even across result pages.
    const [selectedItems, setSelectedItems] = useState(new Map());
    const [selectedMediaTypes, setSelectedMediaTypes] = useState(defaultMediaTypes); // Empty = show "please select" message
    const [selectedTopics, setSelectedTopics] = useState([]);
    const [selectedGenres, setSelectedGenres] = useState([]);
    const [selectedStatuses, setSelectedStatuses] = useState([]);
    const [selectedRatings, setSelectedRatings] = useState([]);
    const [showFilters, setShowFilters] = useState(true);
    const [showSearchBar, setShowSearchBar] = useState(true);
    const [topicSearchQuery, setTopicSearchQuery] = useState('');
    const [genreSearchQuery, setGenreSearchQuery] = useState('');
    const [showAllTopics, setShowAllTopics] = useState(false);
    const [showAllGenres, setShowAllGenres] = useState(false);
    const [urlParamsLoaded, setUrlParamsLoaded] = useState(false);
    // Mixlists/notes/highlights modes start at a search prompt; browsing the whole
    // collection is an opt-in ("View All Mixlists", nav links with ?browseAll=true).
    const [browseAll, setBrowseAll] = useState(false);

    const debouncedSearchQuery = useDebouncedValue(searchQuery);

    // Bulk selection handlers
    const getItemKind = (item) => {
        if (item.isHighlight) return 'highlight';
        if (item.isNote) return 'note';
        if (item.isMixlist) return 'mixlist';
        return 'media';
    };

    const handleToggleSelect = (itemId) => {
        setSelectedItems(prev => {
            const next = new Map(prev);
            if (next.has(itemId)) {
                next.delete(itemId);
            } else {
                // The item is on the current page when it's toggled, so its kind can be
                // captured here and survives paging away.
                const item = searchResults.find(r => r.id === itemId);
                next.set(itemId, item ? getItemKind(item) : 'media');
            }
            return next;
        });
    };

    const handleSelectAll = () => {
        setSelectedItems(new Map(searchResults.map(item => [item.id, getItemKind(item)])));
    };

    const handleDeselectAll = () => {
        setSelectedItems(new Map());
    };

    const selectedIdsOfKind = (kind) =>
        Array.from(selectedItems).filter(([, k]) => k === kind).map(([id]) => id);

    const selectedHighlightCount = selectedIdsOfKind('highlight').length;

    // "3 media items, 5 highlights, 1 note" — for the dialogs and snackbars.
    const describeSelection = () => {
        const parts = [];
        const mediaCount = selectedIdsOfKind('media').length;
        const noteCount = selectedIdsOfKind('note').length;
        if (mediaCount > 0) parts.push(`${mediaCount} media item${mediaCount !== 1 ? 's' : ''}`);
        if (selectedHighlightCount > 0) parts.push(`${selectedHighlightCount} highlight${selectedHighlightCount !== 1 ? 's' : ''}`);
        if (noteCount > 0) parts.push(`${noteCount} note${noteCount !== 1 ? 's' : ''}`);
        return parts.length > 0 ? parts.join(', ') : `${selectedItems.size} item${selectedItems.size !== 1 ? 's' : ''}`;
    };

    // Mutations
    const bulkDeleteMutation = useBulkDeleteMedia();
    const bulkDeleteHighlightsMutation = useBulkDeleteHighlights();
    const bulkDeleteNotesMutation = useBulkDeleteNotes();
    const addMediaToMixlistMutation = useAddMediaToMixlist();
    const linkNoteToMixlistMutation = useLinkNoteToMixlist();

    // Bulk delete handler — media, highlights, and notes live in different tables behind
    // different endpoints, so the selection fans out to one call per kind.
    const handleBulkDelete = async () => {
        const kinds = [
            { label: 'media', ids: selectedIdsOfKind('media'), run: (ids) => bulkDeleteMutation.mutateAsync(ids) },
            { label: 'highlights', ids: selectedIdsOfKind('highlight'), run: (ids) => bulkDeleteHighlightsMutation.mutateAsync(ids) },
            { label: 'notes', ids: selectedIdsOfKind('note'), run: (ids) => bulkDeleteNotesMutation.mutateAsync(ids) },
        ].filter(k => k.ids.length > 0);

        const outcomes = await Promise.allSettled(kinds.map(k => k.run(k.ids)));

        const deletedParts = [];
        const failedParts = [];
        outcomes.forEach((outcome, i) => {
            const { label, ids } = kinds[i];
            if (outcome.status === 'fulfilled') {
                deletedParts.push(`${ids.length} ${label}`);
            } else {
                console.error(`Failed to delete ${label}:`, outcome.reason);
                failedParts.push(label);
            }
        });

        if (failedParts.length === 0) {
            setSnackbar({
                open: true,
                message: `Successfully deleted ${deletedParts.join(', ')}!`,
                severity: 'success'
            });
        } else {
            setSnackbar({
                open: true,
                message: `${deletedParts.length > 0 ? `Deleted ${deletedParts.join(', ')}, but ` : ''}deleting ${failedParts.join(' and ')} failed`,
                severity: 'error'
            });
        }

        // Refresh the search results either way; keep only failed kinds selected.
        performSearch();
        const failedKinds = new Set(failedParts.map(label => label === 'media' ? 'media' : label.slice(0, -1)));
        setSelectedItems(prev => new Map(Array.from(prev).filter(([, kind]) => failedKinds.has(kind))));
        setDeleteDialogOpen(false);
    };

    // Add to mixlist handler. Mixlists hold media items and notes; highlights have no
    // mixlist relationship, so the toolbar disables this action when any are selected.
    const handleAddToMixlist = async () => {
        if (!selectedMixlistForAdd) return;

        const mediaIds = selectedIdsOfKind('media');
        const noteIds = selectedIdsOfKind('note');
        try {
            for (const mediaId of mediaIds) {
                await addMediaToMixlistMutation.mutateAsync({
                    mixlistId: selectedMixlistForAdd,
                    mediaItemId: mediaId,
                });
            }
            for (const noteId of noteIds) {
                await linkNoteToMixlistMutation.mutateAsync({
                    mixlistId: selectedMixlistForAdd,
                    noteId,
                });
            }

            setSnackbar({
                open: true,
                message: `Successfully added ${describeSelection()} to mixlist!`,
                severity: 'success'
            });

            setSelectedItems(new Map());
            setSelectedMixlistForAdd('');
        } catch (error) {
            console.error('Failed to add items to mixlist:', error);
            setSnackbar({
                open: true,
                message: error.response?.data?.error || 'Failed to add items to mixlist',
                severity: 'error'
            });
        } finally {
            setAddToMixlistDialogOpen(false);
        }
    };

    // Open add to mixlist dialog — mixlists are auto-fetched by useAllMixlists when dialog opens.
    const openAddToMixlistDialog = () => {
        setAddToMixlistDialogOpen(true);
    };

    // Data state
    const [searchResults, setSearchResults] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [totalResults, setTotalResults] = useState(0);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const perPage = 20;

    // Bulk actions state
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [addToMixlistDialogOpen, setAddToMixlistDialogOpen] = useState(false);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [selectedMixlistForAdd, setSelectedMixlistForAdd] = useState('');

    // Filter/dialog data loaded via TanStack Query.
    const topicsQuery = useAllTopics();
    const genresQuery = useAllGenres();
    const allTopics = (topicsQuery.data ?? []).map((t) => t.name);
    const allGenres = (genresQuery.data ?? []).map((g) => g.name);

    const mixlistsQuery = useAllMixlists({ enabled: addToMixlistDialogOpen });
    const availableMixlists = mixlistsQuery.data ?? [];

    const deleting = bulkDeleteMutation.isPending
        || bulkDeleteHighlightsMutation.isPending
        || bulkDeleteNotesMutation.isPending;
    const addingToMixlist = addMediaToMixlistMutation.isPending
        || linkNoteToMixlistMutation.isPending;

    // Sync URL parameters into state — The URL is the source of truth 
    useEffect(() => {
        const query = searchParams.get('q');
        const mediaTypeParam = searchParams.get('mediaType');
        const topics = searchParams.get('topics');
        const genres = searchParams.get('genres');
        const status = searchParams.get('status');
        const mode = searchParams.get('searchMode');

        let mediaTypes = mediaTypeParam ? mediaTypeParam.split(',').map(t => t.trim()) : [];
        let resolvedMode = ['mixlists', 'notes', 'highlights'].includes(mode) ? mode : 'media';
        let resolvedBrowseAll = searchParams.get('browseAll') === 'true';

        // Legacy deep links: ?mediaType=Note / ?mediaType=Highlight resolve to browse-all.
        if (resolvedMode === 'media' && mediaTypes.length === 1 && mediaTypes[0] === 'Note') {
            resolvedMode = 'notes';
            resolvedBrowseAll = true;
            mediaTypes = [];
        } else if (resolvedMode === 'media' && mediaTypes.length === 1 && mediaTypes[0] === 'Highlight') {
            resolvedMode = 'highlights';
            resolvedBrowseAll = true;
            mediaTypes = [];
        } else {
            mediaTypes = mediaTypes.filter(t => t !== 'Note' && t !== 'Highlight');
        }

        setSearchQuery(query || '');
        setSelectedMediaTypes(mediaTypes.length > 0 ? mediaTypes : defaultMediaTypes);
        setSelectedTopics(topics ? topics.split(',').map(t => t.trim()) : []);
        setSelectedGenres(genres ? genres.split(',').map(g => g.trim()) : []);
        setSelectedStatuses(status ? status.split(',').map(s => s.trim()).filter(s => s && s !== 'all') : []);
        setSearchMode(resolvedMode);
        setBrowseAll(resolvedBrowseAll);

        setUrlParamsLoaded(true);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [searchParams]);

    const searchCriteriaKey = JSON.stringify([
        debouncedSearchQuery,
        searchMode,
        selectedMediaTypes,
        selectedTopics,
        selectedGenres,
        selectedStatuses,
        selectedRatings,
        sortBy,
        browseAll,
    ]);
    const lastSearchCriteriaKey = useRef(searchCriteriaKey);

    useEffect(() => {
        if (!urlParamsLoaded) return;

        if (lastSearchCriteriaKey.current !== searchCriteriaKey && currentPage !== 1) {
            lastSearchCriteriaKey.current = searchCriteriaKey;
            setCurrentPage(1);
            return;
        }

        lastSearchCriteriaKey.current = searchCriteriaKey;
        performSearch();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [searchCriteriaKey, currentPage, urlParamsLoaded]);

    // Check if we have any selection criteria for media search
    // The /all-media route opts in to browsing the whole library, so it needs no filters.
    // Everywhere else 'all' is only a no-selection sentinel and must not count as a chosen
    // filter, or arriving with ?mediaType=all and no query lists the entire library.
    const browseAllMode = defaultMediaTypes.includes('all');

    const hasMediaFilters = searchMode === 'media' && (
        browseAllMode ||
        debouncedSearchQuery.trim() !== '' ||
        selectedMediaTypes.some(type => type !== 'all') ||
        selectedTopics.length > 0 ||
        selectedGenres.length > 0 ||
        selectedStatuses.length > 0 ||
        selectedRatings.length > 0
    );

    const hasMixlistFilters = searchMode === 'mixlists' && (
        browseAll ||
        debouncedSearchQuery.trim() !== '' ||
        selectedTopics.length > 0 ||
        selectedGenres.length > 0
    );

    const hasNotesFilters = searchMode === 'notes' && (
        browseAll ||
        debouncedSearchQuery.trim() !== '' ||
        selectedTopics.length > 0
    );

    const hasHighlightsFilters = searchMode === 'highlights' && (
        browseAll ||
        debouncedSearchQuery.trim() !== '' ||
        selectedTopics.length > 0
    );

    const performSearch = async () => {
        // Every mode requires a query or at least one filter before searching;
        // otherwise switching modes would dump the entire collection.
        if ((searchMode === 'media' && !hasMediaFilters) ||
            (searchMode === 'mixlists' && !hasMixlistFilters) ||
            (searchMode === 'notes' && !hasNotesFilters) ||
            (searchMode === 'highlights' && !hasHighlightsFilters)) {
            setSearchResults([]);
            setTotalResults(0);
            setTotalPages(1);
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null);

        try {
            let response;

            if (searchMode === 'mixlists') {
                // Search mixlists
                const searchOptions = {
                    query: debouncedSearchQuery || '*',
                    topics: selectedTopics,
                    genres: selectedGenres,
                    page: currentPage,
                    perPage: perPage,
                    sortBy: sortBy
                };

                response = await typesenseAdvancedSearchMixlists(searchOptions);

                // Transform Typesense response for mixlists
                const hits = response.hits || [];
                const transformedResults = hits.map(hit => {
                    const doc = hit.document;
                    return {
                        id: doc.id,
                        title: doc.name,
                        mediaType: 'Mixlist',
                        status: null,
                        ratingType: null,
                        topics: doc.topics || [],
                        genres: doc.genres || [],
                        author: `${doc.media_item_count} items`,
                        duration: new Date(doc.date_created * 1000).toLocaleDateString(),
                        dateAdded: new Date(doc.date_created * 1000).toISOString().split('T')[0],
                        notes: doc.description || '',
                        thumbnail: doc.thumbnail,
                        isMixlist: true
                    };
                });

                setSearchResults(transformedResults);
                setTotalResults(response.found || 0);
                setTotalPages(Math.ceil((response.found || 0) / perPage));
            } else if (searchMode === 'notes') {
                // Search notes only
                const noteFilter = selectedTopics.length > 0 ? `tags:=[${selectedTopics.map(t => `"${t}"`).join(',')}]` : null;
                response = await searchNotes(debouncedSearchQuery || '*', noteFilter, currentPage, perPage, sortBy);
                const transformedResults = transformNoteHits(response.hits || []);
                setSearchResults(transformedResults);
                setTotalResults(response.found || 0);
                setTotalPages(Math.ceil((response.found || 0) / perPage));
            } else if (searchMode === 'highlights') {
                // Search highlights only
                const highlightFilter = selectedTopics.length > 0 ? `tags:=[${selectedTopics.map(t => `"${t}"`).join(',')}]` : null;
                response = await searchHighlights(debouncedSearchQuery || '*', highlightFilter, currentPage, perPage, sortBy);
                const transformedResults = transformHighlightHits(response.hits || []);
                setSearchResults(transformedResults);
                setTotalResults(response.found || 0);
                setTotalPages(Math.ceil((response.found || 0) / perPage));
            } else {
                // Search media items
                const mediaTypesFiltered = selectedMediaTypes.filter(type => type !== 'all');
                const searchOptions = {
                    query: debouncedSearchQuery || '*',
                    mediaTypes: selectedMediaTypes.includes('all') ? [] : mediaTypesFiltered,
                    topics: selectedTopics,
                    genres: selectedGenres,
                    status: selectedStatuses,
                    ratings: selectedRatings,
                    page: currentPage,
                    perPage: perPage,
                    sortBy: sortBy
                };

                response = await typesenseAdvancedSearch(searchOptions);
                const transformedResults = transformMediaHits(response.hits || []);
                setSearchResults(transformedResults);
                setTotalResults(response.found || 0);
                setTotalPages(Math.ceil((response.found || 0) / perPage));
            }
        } catch (err) {
            console.error('Search error:', err);
            setError('Failed to perform search. Please try again.');
            setSearchResults([]);
        } finally {
            setLoading(false);
        }
    };

    const handleSearchModeChange = (newMode) => {
        setSearchMode(newMode);
        setCurrentPage(1);
        setBrowseAll(false);
        setSelectedItems(new Map());
    };

    const handleViewAllMixlists = () => {
        setSearchQuery('');
        setSelectedTopics([]);
        setSelectedGenres([]);
        setBrowseAll(true);
        setCurrentPage(1);
    };

    const handleMediaTypeToggle = (value) => {
        if (value === 'all') {
            setSelectedMediaTypes(['all']);
        } else {
            const newSelection = selectedMediaTypes.includes('all')
                ? [value]
                : selectedMediaTypes.includes(value)
                    ? selectedMediaTypes.filter(t => t !== value)
                    : [...selectedMediaTypes.filter(t => t !== 'all'), value];

            // Allow empty selection - this will show the "please select" message
            setSelectedMediaTypes(newSelection);
        }
    };

    const handleTopicToggle = (topic) => {
        setSelectedTopics(prev =>
            prev.includes(topic) ? prev.filter(t => t !== topic) : [...prev, topic]
        );
    };

    const handleGenreToggle = (genre) => {
        setSelectedGenres(prev =>
            prev.includes(genre) ? prev.filter(g => g !== genre) : [...prev, genre]
        );
    };

    const handleClearFilters = () => {
        setSelectedMediaTypes([]);
        setSelectedTopics([]);
        setSelectedGenres([]);
        setSelectedStatuses([]);
        setSelectedRatings([]);
        setSearchQuery('');
        setTopicSearchQuery('');
        setGenreSearchQuery('');
        setShowAllTopics(false);
        setShowAllGenres(false);
        setCurrentPage(1);
        setBrowseAll(false);
        setSelectedItems(new Map());
    };

    return (
        <Box sx={{ backgroundColor: 'background.default', minHeight: '100vh' }}>
            <Container maxWidth="xl" sx={{ py: 4 }}>
                {/* Header */}
                <Box sx={{ mb: 4 }}>
                    <Typography 
                        variant="h3" 
                        sx={{ 
                            fontWeight: 'bold',
                            mb: 1,
                            fontSize: { xs: '2rem', sm: '2.5rem', md: '3rem' }
                        }}
                    >
                        Search MediaVerse
                    </Typography>
                    <Typography variant="body1" color="text.secondary">
                        {searchMode === 'media'
                            ? 'Search across all your media with powerful filters and instant results'
                            : searchMode === 'mixlists'
                                ? 'Search and discover curated mixlists by name, topics, or genres'
                                : searchMode === 'notes'
                                    ? 'Search your Obsidian notes by title, content, or tags'
                                    : 'Search your highlights by text, note, source, or tags'}
                    </Typography>
                    <SearchViewToggles
                        showSearchBar={showSearchBar}
                        setShowSearchBar={setShowSearchBar}
                        showFilters={showFilters}
                        setShowFilters={setShowFilters}
                        sx={{ display: { xs: 'flex', sm: 'none' }, mt: 2 }}
                    />
                </Box>

                {/* Search Bar */}
                <Collapse in={showSearchBar}>
                    <SearchBarSection
                        searchQuery={searchQuery}
                        setSearchQuery={setSearchQuery}
                        allTopics={allTopics}
                        selectedTopics={selectedTopics}
                        handleTopicToggle={handleTopicToggle}
                        searchMode={searchMode}
                        onSearchModeChange={handleSearchModeChange}
                        onCreateMixlist={() => navigate('/create-mixlist', { state: { returnTo: '/search?searchMode=mixlists' } })}
                        onViewAllMixlists={handleViewAllMixlists}
                    />
                </Collapse>

                <Grid container spacing={3}>
                    {showFilters && (
                        <SearchFilterSidebar
                            key="filter-sidebar"
                            searchMode={searchMode}
                            selectedMediaTypes={selectedMediaTypes}
                            setSelectedMediaTypes={setSelectedMediaTypes}
                            selectedTopics={selectedTopics}
                            setSelectedTopics={setSelectedTopics}
                            selectedGenres={selectedGenres}
                            setSelectedGenres={setSelectedGenres}
                            selectedStatuses={selectedStatuses}
                            setSelectedStatuses={setSelectedStatuses}
                            selectedRatings={selectedRatings}
                            setSelectedRatings={setSelectedRatings}
                            handleClearFilters={handleClearFilters}
                            topicSearchQuery={topicSearchQuery}
                            setTopicSearchQuery={setTopicSearchQuery}
                            genreSearchQuery={genreSearchQuery}
                            setGenreSearchQuery={setGenreSearchQuery}
                            showAllTopics={showAllTopics}
                            setShowAllTopics={setShowAllTopics}
                            showAllGenres={showAllGenres}
                            setShowAllGenres={setShowAllGenres}
                            allTopics={allTopics}
                            allGenres={allGenres}
                            mediaTypeOptions={mediaTypeOptions}
                        />
                    )}

                    <Grid item xs={12} md={showFilters ? 9 : 12}>
                        {/* Results Header */}
                        <ResultHeader
                            totalResults={totalResults}
                            searchQuery={debouncedSearchQuery}
                            searchMode={searchMode}
                            viewMode={viewMode}
                            setViewMode={setViewMode}
                            sortBy={sortBy}
                            setSortBy={setSortBy}
                            showFilters={showFilters}
                            setShowFilters={setShowFilters}
                            showSearchBar={showSearchBar}
                            setShowSearchBar={setShowSearchBar}
                            selectedTopics={selectedTopics}
                            selectedGenres={selectedGenres}
                            selectedMediaTypes={selectedMediaTypes}
                            handleTopicToggle={handleTopicToggle}
                            handleGenreToggle={handleGenreToggle}
                            handleMediaTypeToggle={handleMediaTypeToggle}
                            mediaTypeOptions={mediaTypeOptions}
                        />

                        {/* Bulk Actions Toolbar — every mode except mixlists */}
                        {searchResults.length > 0 && searchMode !== 'mixlists' && (
                            <Toolbar
                                sx={{
                                    mb: 2,
                                    bgcolor: 'background.paper',
                                    borderRadius: 1,
                                    px: { xs: 1, sm: 2 },
                                    py: { xs: 1, sm: 1 },
                                    display: 'flex',
                                    flexDirection: { xs: 'column', sm: 'row' },
                                    gap: { xs: 1, sm: 2 },
                                    justifyContent: 'space-between',
                                    alignItems: { xs: 'stretch', sm: 'center' }
                                }}
                            >
                                <Box sx={{
                                    display: 'flex',
                                    flexDirection: { xs: 'column', sm: 'row' },
                                    gap: 1,
                                    width: { xs: '100%', sm: 'auto' }
                                }}>
                                    <Button
                                        variant="outlined"
                                        size="small"
                                        onClick={handleSelectAll}
                                        startIcon={<CheckBox />}
                                        sx={{
                                            color: 'white',
                                            borderColor: 'white',
                                            minHeight: '44px',
                                            fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                            '&:hover': {
                                                borderColor: 'white',
                                                backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                            }
                                        }}
                                    >
                                        Select All
                                    </Button>
                                    <Button
                                        variant="outlined"
                                        size="small"
                                        onClick={handleDeselectAll}
                                        startIcon={<CheckBoxOutlineBlank />}
                                        disabled={selectedItems.size === 0}
                                        sx={{
                                            color: 'white',
                                            borderColor: 'white',
                                            minHeight: '44px',
                                            fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                            '&:hover': {
                                                borderColor: 'white',
                                                backgroundColor: 'rgba(255, 255, 255, 0.08)'
                                            },
                                            '&.Mui-disabled': {
                                                borderColor: 'rgba(255, 255, 255, 0.3)',
                                                color: 'rgba(255, 255, 255, 0.3)'
                                            }
                                        }}
                                    >
                                        Deselect All
                                    </Button>
                                    {selectedItems.size > 0 && (
                                        <Typography variant="body2" sx={{ alignSelf: 'center', color: 'text.secondary' }}>
                                            {selectedItems.size} selected
                                        </Typography>
                                    )}
                                </Box>
                                <Box sx={{
                                    display: 'flex',
                                    flexDirection: { xs: 'column', sm: 'row' },
                                    gap: 1,
                                    width: { xs: '100%', sm: 'auto' }
                                }}>
                                    {/* Highlights have no mixlist relationship, so the action is hidden in that mode */}
                                    {searchMode !== 'highlights' && (
                                        <Button
                                            variant="contained"
                                            color="primary"
                                            size="small"
                                            onClick={openAddToMixlistDialog}
                                            startIcon={<PlaylistAdd />}
                                            disabled={selectedItems.size === 0}
                                            sx={{
                                                minHeight: '44px',
                                                fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                                width: { xs: '100%', sm: 'auto' }
                                            }}
                                        >
                                            Add to Mixlist
                                        </Button>
                                    )}
                                    <Button
                                        variant="contained"
                                        color="error"
                                        size="small"
                                        onClick={() => setDeleteDialogOpen(true)}
                                        startIcon={<Delete />}
                                        disabled={selectedItems.size === 0}
                                        sx={{
                                            minHeight: '44px',
                                            fontSize: { xs: '0.8rem', sm: '0.875rem' },
                                            width: { xs: '100%', sm: 'auto' }
                                        }}
                                    >
                                        Delete ({selectedItems.size})
                                    </Button>
                                </Box>
                            </Toolbar>
                        )}

                        {/* Results Display */}
                        {loading ? (
                            <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
                                <CircularProgress />
                            </Box>
                        ) : error ? (
                            <Alert severity="error" sx={{ mb: 3 }}>
                                {error}
                            </Alert>
                        ) : searchMode === 'media' && !hasMediaFilters ? (
                            <Paper sx={{ p: 8, textAlign: 'center' }}>
                                <SearchIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                                <Typography variant="h6" color="text.secondary">
                                    Select filters to search
                                </Typography>
                                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                                    Use the search bar or select media types, topics, genres, or other filters to find your media
                                </Typography>
                            </Paper>
                        ) : searchMode === 'mixlists' && !hasMixlistFilters ? (
                            <Paper sx={{ p: 8, textAlign: 'center' }}>
                                <SearchIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                                <Typography variant="h6" color="text.secondary">
                                    Search your mixlists
                                </Typography>
                                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                                    Enter a name in the search bar or select topics or genres to find mixlists
                                </Typography>
                                <Button
                                    variant="outlined"
                                    onClick={handleViewAllMixlists}
                                    sx={{ mt: 3, minHeight: '44px' }}
                                >
                                    View All Mixlists
                                </Button>
                            </Paper>
                        ) : searchMode === 'notes' && !hasNotesFilters ? (
                            <Paper sx={{ p: 8, textAlign: 'center' }}>
                                <SearchIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                                <Typography variant="h6" color="text.secondary">
                                    Search your notes
                                </Typography>
                                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                                    Enter a search term or select topics to find notes from your vaults
                                </Typography>
                            </Paper>
                        ) : searchMode === 'highlights' && !hasHighlightsFilters ? (
                            <Paper sx={{ p: 8, textAlign: 'center' }}>
                                <SearchIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
                                <Typography variant="h6" color="text.secondary">
                                    Search your highlights
                                </Typography>
                                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                                    Enter a search term or select topics to find highlights from your sources
                                </Typography>
                            </Paper>
                        ) : searchResults.length === 0 ? (
                            <Paper sx={{ p: 8, textAlign: 'center' }}>
                                <Typography variant="h6" color="text.secondary">
                                    No results found
                                </Typography>
                                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                                    Try adjusting your filters or search query
                                </Typography>
                            </Paper>
                        ) : viewMode === 'card' ? (
                            <Grid container spacing={3}>
                                {searchResults.map((item) => (
                                    <Grid item xs={12} sm={6} lg={4} key={item.id}>
                                        <SearchResultCard
                                            item={item}
                                            isSelected={selectedItems.has(item.id)}
                                            onToggleSelect={handleToggleSelect}
                                            showCheckbox={searchMode !== 'mixlists'}
                                        />
                                    </Grid>
                                ))}
                            </Grid>
                        ) : (
                            <Box>
                                {searchResults.map((item) => (
                                    <MediaListItem
                                        key={item.id}
                                        item={item}
                                        isSelected={selectedItems.has(item.id)}
                                        onToggleSelect={handleToggleSelect}
                                        showCheckbox={searchMode !== 'mixlists'}
                                    />
                                ))}
                            </Box>
                        )}

                        {/* Pagination */}
                        {!loading && searchResults.length > 0 && totalPages > 1 && (
                            <Box sx={{ mt: 4, display: 'flex', justifyContent: 'center' }}>
                                <ButtonGroup variant="outlined" sx={{ '& .MuiButton-outlined': { color: 'white', borderColor: 'white' } }}>
                                    <Button 
                                        disabled={currentPage === 1}
                                        onClick={() => setCurrentPage(prev => prev - 1)}
                                    >
                                        Previous
                                    </Button>
                                    {[...Array(Math.min(5, totalPages))].map((_, index) => {
                                        const pageNum = index + 1;
                                        return (
                                            <Button
                                                key={pageNum}
                                                variant={currentPage === pageNum ? 'contained' : 'outlined'}
                                                onClick={() => setCurrentPage(pageNum)}
                                            >
                                                {pageNum}
                                            </Button>
                                        );
                                    })}
                                    {totalPages > 5 && <Button disabled>...</Button>}
                                    <Button 
                                        disabled={currentPage === totalPages}
                                        onClick={() => setCurrentPage(prev => prev + 1)}
                                    >
                                        Next
                                    </Button>
                                </ButtonGroup>
                            </Box>
                        )}
                    </Grid>
                </Grid>
            </Container>

            {/* Delete Confirmation Dialog */}
            <Dialog
                open={deleteDialogOpen}
                onClose={() => !deleting && setDeleteDialogOpen(false)}
            >
                <DialogTitle>Confirm Bulk Delete</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        Are you sure you want to delete {describeSelection()}?
                        This action cannot be undone.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteDialogOpen(false)} disabled={deleting}>
                        Cancel
                    </Button>
                    <Button
                        onClick={handleBulkDelete}
                        color="error"
                        variant="contained"
                        disabled={deleting}
                    >
                        {deleting ? 'Deleting...' : 'Delete'}
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Add to Mixlist Dialog */}
            <Dialog
                open={addToMixlistDialogOpen}
                onClose={() => !addingToMixlist && setAddToMixlistDialogOpen(false)}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>Add to Mixlist</DialogTitle>
                <DialogContent>
                    <DialogContentText sx={{ mb: 2 }}>
                        Select a mixlist to add {describeSelection()} to:
                    </DialogContentText>
                    <FormControl fullWidth>
                        <InputLabel>Select Mixlist</InputLabel>
                        <Select
                            value={selectedMixlistForAdd}
                            label="Select Mixlist"
                            onChange={(e) => setSelectedMixlistForAdd(e.target.value)}
                        >
                            {availableMixlists.map((mixlist) => (
                                <MenuItem key={mixlist.id} value={mixlist.id}>
                                    {mixlist.name}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>
                </DialogContent>
                <DialogActions>
                    <Button
                        onClick={() => setAddToMixlistDialogOpen(false)}
                        color="primary"
                        variant="contained"
                        disabled={addingToMixlist}
                    >
                        Cancel
                    </Button>
                    <Button
                        onClick={handleAddToMixlist}
                        color="primary"
                        variant="contained"
                        disabled={addingToMixlist || !selectedMixlistForAdd}
                    >
                        {addingToMixlist ? 'Adding...' : 'Add to Mixlist'}
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Snackbar for feedback */}
            <Snackbar
                open={snackbar.open}
                autoHideDuration={6000}
                onClose={() => setSnackbar({ ...snackbar, open: false })}
            >
                <Alert
                    onClose={() => setSnackbar({ ...snackbar, open: false })}
                    severity={snackbar.severity}
                >
                    {snackbar.message}
                </Alert>
            </Snackbar>
        </Box>
    );
}

