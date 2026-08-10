import React from 'react';
import { Box, Button } from '@mui/material';
import { FilterList, Search as SearchIcon } from '@mui/icons-material';

// The "Hide/Show Search Bar" and "Hide/Show Filters" pair. Rendered twice with
// complementary breakpoints: under the page intro on mobile, and inline with the sort
// and view controls from `sm` up. `sx` carries the breakpoint that decides which copy
// is visible.
export const SearchViewToggles = React.memo(({
    showSearchBar,
    setShowSearchBar,
    showFilters,
    setShowFilters,
    sx = {}
}) => (
    <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap', ...sx }}>
        <Button
            variant="outlined"
            size="small"
            onClick={() => setShowSearchBar(!showSearchBar)}
            startIcon={<SearchIcon />}
            sx={{ borderColor: '#fcfafa', color: '#fcfafa' }}
        >
            {showSearchBar ? 'Hide' : 'Show'} Search Bar
        </Button>

        <Button
            variant="outlined"
            size="small"
            onClick={() => setShowFilters(!showFilters)}
            startIcon={<FilterList />}
            sx={{ borderColor: '#fcfafa', color: '#fcfafa' }}
        >
            {showFilters ? 'Hide' : 'Show'} Filters
        </Button>
    </Box>
));

SearchViewToggles.displayName = 'SearchViewToggles';

export default SearchViewToggles;
