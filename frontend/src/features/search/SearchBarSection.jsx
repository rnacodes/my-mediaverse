import React from 'react';
import { Box, Typography, TextField, InputAdornment, Chip, Paper, IconButton, ToggleButtonGroup, ToggleButton } from '@mui/material';
import { Search as SearchIcon, Clear } from '@mui/icons-material';

export const SearchBarSection = React.memo(({
    searchQuery,
    setSearchQuery,
    allTopics,
    selectedTopics,
    handleTopicToggle,
    searchMode,
    setSearchMode,
    setCurrentPage
}) => {
    return (
        <Paper 
            elevation={3}
            sx={{ 
                p: { xs: 2, sm: 3 }, 
                mb: 4,
                backgroundColor: 'background.paper',
                borderRadius: 3
            }}
        >
            <Box sx={{ mb: 2 }}>
                <ToggleButtonGroup
                    value={searchMode}
                    exclusive
                    onChange={(e, newMode) => {
                        if (newMode !== null) {
                            setSearchMode(newMode);
                            setCurrentPage(1); // Reset to first page when switching modes
                        }
                    }}
                    size="small"
                    sx={{
                        backgroundColor: 'background.paper',
                        '& .MuiToggleButton-root': {
                            px: 3,
                            py: 1,
                            textTransform: 'none',
                            fontWeight: 'bold'
                        }
                    }}
                >
                    <ToggleButton value="media">
                        Media Items
                    </ToggleButton>
                    <ToggleButton value="mixlists">
                        Mixlists
                    </ToggleButton>
                </ToggleButtonGroup>
            </Box>
            <TextField
                fullWidth
                variant="outlined"
                placeholder="Search by title, author, topic, or any keyword..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                InputProps={{
                    startAdornment: (
                        <InputAdornment position="start">
                            <SearchIcon sx={{ fontSize: 28, color: 'text.secondary' }} />
                        </InputAdornment>
                    ),
                    endAdornment: searchQuery && (
                        <InputAdornment position="end">
                            <IconButton onClick={() => setSearchQuery('')} size="small">
                                <Clear />
                            </IconButton>
                        </InputAdornment>
                    ),
                    sx: { 
                        fontSize: '1.1rem',
                        '& .MuiOutlinedInput-notchedOutline': {
                            borderColor: 'rgba(255, 255, 255, 0.23)'
                        }
                    }
                }}
            />
            
            {/* Quick Filters */}
            {allTopics.length > 0 && (
                <Box sx={{ mt: 2, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    <Typography variant="body2" color="text.secondary" sx={{ mr: 1, alignSelf: 'center' }}>
                        Quick filters:
                    </Typography>
                    {allTopics.slice(0, 4).map((topic) => (
                        <Chip
                            key={topic}
                            label={topic}
                            onClick={() => handleTopicToggle(topic)}
                            color={selectedTopics.includes(topic) ? 'primary' : 'default'}
                            sx={{ cursor: 'pointer' }}
                        />
                    ))}
                </Box>
            )}
        </Paper>
    );
});

SearchBarSection.displayName = 'SearchBarSection';
