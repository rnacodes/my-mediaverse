import React from 'react';
import { Box, Typography, ToggleButtonGroup, ToggleButton, FormControl, InputLabel, Select, MenuItem, Chip } from '@mui/material';
import { ViewModule, ViewList } from '@mui/icons-material';
import SearchViewToggles from './SearchViewToggles';

const sortOptions = [
    { value: 'relevance', label: 'Most Relevant' },
    { value: 'dateAdded', label: 'Recently Added' },
    { value: 'title', label: 'Title (A–Z)' }
];

export const ResultHeader = React.memo(({
    totalResults,
    searchQuery,
    searchMode,
    viewMode,
    setViewMode,
    sortBy,
    setSortBy,
    showFilters,
    setShowFilters,
    showSearchBar,
    setShowSearchBar,
    selectedTopics,
    selectedGenres,
    selectedMediaTypes,
    handleTopicToggle,
    handleGenreToggle,
    handleMediaTypeToggle,
    mediaTypeOptions
}) => {
    return (
        <Box sx={{ 
            mb: 3, 
            display: 'flex', 
            flexDirection: { xs: 'column', sm: 'row' },
            justifyContent: 'space-between', 
            alignItems: { xs: 'flex-start', sm: 'center' },
            gap: 2
        }}>
            <Box>
                <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                    {totalResults} Results
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    {searchQuery && `Showing results for "${searchQuery}"`}
                    {!searchQuery && searchMode === 'media' && 'Showing all media items'}
                    {!searchQuery && searchMode === 'mixlists' && 'Showing all mixlists'}
                    {!searchQuery && searchMode === 'notes' && 'Showing all notes'}
                </Typography>
            </Box>

            <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
                {/* Sort */}
                <FormControl size="small" sx={{ minWidth: 180 }}>
                    <InputLabel>Sort by</InputLabel>
                    <Select
                        value={sortBy}
                        label="Sort by"
                        onChange={(e) => setSortBy(e.target.value)}
                    >
                        {sortOptions.map((option) => (
                            <MenuItem key={option.value} value={option.value}>
                                {option.label}
                            </MenuItem>
                        ))}
                    </Select>
                </FormControl>

                {/* View Mode Toggle */}
                <ToggleButtonGroup
                    value={viewMode}
                    exclusive
                    onChange={(e, newMode) => newMode && setViewMode(newMode)}
                    size="small"
                >
                    <ToggleButton value="card">
                        <ViewModule />
                    </ToggleButton>
                    <ToggleButton value="list">
                        <ViewList />
                    </ToggleButton>
                </ToggleButtonGroup>

                {/* Toggles live under the page intro on mobile instead */}
                <SearchViewToggles
                    showSearchBar={showSearchBar}
                    setShowSearchBar={setShowSearchBar}
                    showFilters={showFilters}
                    setShowFilters={setShowFilters}
                    sx={{ display: { xs: 'none', sm: 'flex' } }}
                />
            </Box>
            
            {/* Active Filters Display */}
            {(selectedTopics.length > 0 || selectedGenres.length > 0 || (selectedMediaTypes && !selectedMediaTypes.includes('all'))) && (
                <Box sx={{ mt: 2, display: 'flex', gap: 1, flexWrap: 'wrap', alignItems: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                        Active filters:
                    </Typography>
                    {selectedTopics.map((topic) => (
                        <Chip
                            key={`filter-topic-${topic}`}
                            label={topic}
                            size="small"
                            onDelete={() => handleTopicToggle(topic)}
                            color="primary"
                        />
                    ))}
                    {selectedGenres.map((genre) => (
                        <Chip
                            key={`filter-genre-${genre}`}
                            label={genre}
                            size="small"
                            onDelete={() => handleGenreToggle(genre)}
                            color="secondary"
                        />
                    ))}
                    {(selectedMediaTypes && !selectedMediaTypes.includes('all')) && selectedMediaTypes.map((type) => (
                        <Chip
                            key={`filter-type-${type}`}
                            label={mediaTypeOptions.find(o => o.value === type)?.label || type}
                            size="small"
                            onDelete={() => handleMediaTypeToggle(type)}
                        />
                    ))}
                </Box>
            )}
        </Box>
    );
});

ResultHeader.displayName = 'ResultHeader';
