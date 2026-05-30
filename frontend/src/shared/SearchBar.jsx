import React, { useState, useRef, useEffect } from 'react';
import { Box, TextField, IconButton, InputAdornment, Popper, Paper, List, ListItem, ListItemText, ListItemIcon, Typography, CircularProgress, Chip } from '@mui/material';
import {
  Search,
  Clear,
  History,
  TrendingUp,
  Book,
  Movie,
  Tv,
  Article,
  LibraryMusic,
  Podcasts,
  SportsEsports,
  YouTube,
  Language,
  PlaylistPlay
} from '@mui/icons-material';
import { commonStyles, COLORS } from './DesignSystem';
import { searchAll } from '@/services/searchService';

const SearchBar = ({
  onSearch,
  placeholder = "Search your media library...",
  recentSearches = [],
  trendingSearches = [],
  showSuggestions = true,
  fullWidth = true,
  size = 'medium',
  variant = 'outlined',
  sx = {},
  ...props
}) => {
  const [query, setQuery] = useState('');
  const [showSuggestionsPanel, setShowSuggestionsPanel] = useState(false);
  const [focused, setFocused] = useState(false);
  const [searchResults, setSearchResults] = useState({ media: [], mixlists: [] });
  const [searching, setSearching] = useState(false);
  const anchorEl = useRef(null);
  const searchTimeoutRef = useRef(null);

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, []);

  // Media type icons for suggestions
  const mediaTypeIcons = {
    podcast: <Podcasts />,
    book: <Book />,
    movie: <Movie />,
    tv: <Tv />,
    article: <Article />,
    music: <LibraryMusic />,
    game: <SportsEsports />,
    video: <YouTube />,
    website: <Language />
  };

  const handleSearch = async (searchQuery = query) => {
    if (searchQuery.trim()) {
      setSearching(true);
      try {
        console.log('🔍 SearchBar: Searching for:', searchQuery.trim());
        const results = await searchAll(searchQuery.trim());
        console.log('🔍 SearchBar: Raw results:', results);
        console.log('🔍 SearchBar: Media count:', results.media?.length || 0);
        console.log('🔍 SearchBar: Mixlists count:', results.mixlists?.length || 0);
        
        setSearchResults(results);
        setShowSuggestionsPanel(true);
        onSearch?.(searchQuery.trim(), results);
      } catch (error) {
        console.error('❌ SearchBar: Search error:', error);
        console.error('❌ SearchBar: Error details:', error.response?.data || error.message);
        setSearchResults({ media: [], mixlists: [] });
      } finally {
        setSearching(false);
      }
    }
  };

  const handleQueryChange = (newQuery) => {
    setQuery(newQuery);
    
    // Clear previous timeout
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }
    
    // Debounced search after 300ms of no typing
    if (newQuery.trim()) {
      searchTimeoutRef.current = setTimeout(() => {
        handleSearch(newQuery);
      }, 300);
    } else {
      setSearchResults({ media: [], mixlists: [] });
      setShowSuggestionsPanel(false);
    }
  };

  const handleKeyPress = (event) => {
    if (event.key === 'Enter') {
      handleSearch();
    }
  };

  const handleClear = () => {
    setQuery('');
    setShowSuggestionsPanel(false);
    onSearch?.('');
  };



  const handleFocus = () => {
    setFocused(true);
    if (showSuggestions && (searchResults.media.length > 0 || searchResults.mixlists.length > 0 || recentSearches.length > 0 || trendingSearches.length > 0)) {
      setShowSuggestionsPanel(true);
    }
  };

  const handleBlur = () => {
    setFocused(false);
    // Delay hiding suggestions to allow for clicks
    setTimeout(() => setShowSuggestionsPanel(false), 200);
  };

  // Handle navigation to media or mixlist
  const handleSuggestionClick = (item) => {
    if (item.id || item.Id) {
      // Check if it's a mixlist (has mediaItems property) or media item
      if (item.mediaItems || item.MediaItems) {
        // It's a mixlist
        window.location.href = `/mixlist/${item.id || item.Id}`;
      } else if (item.mediaType === 'Podcast' && !item.seriesId && !item.SeriesId) {
        // It's a podcast series (no seriesId means it's not an episode)
        window.location.href = `/podcast-series/${item.id || item.Id}`;
      } else if (item.mediaType === 'Channel') {
        // It's a YouTube channel
        window.location.href = `/youtube-channel/${item.id || item.Id}`;
      } else {
        // It's a media item
        window.location.href = `/media/${item.id || item.Id}`;
      }
    }
    setShowSuggestionsPanel(false);
  };

  return (
    <Box sx={{ position: 'relative', width: fullWidth ? '100%' : 'auto', maxWidth: '700px', margin: 'auto', ...sx }}>
      <TextField
        ref={anchorEl}
        value={query}
        onChange={(e) => handleQueryChange(e.target.value)}
        onKeyPress={handleKeyPress}
        onFocus={handleFocus}
        onBlur={handleBlur}
        placeholder={placeholder}
        variant={variant}
        size={size}
        fullWidth={fullWidth}
        sx={{
          ...commonStyles.searchBar,
          '& .MuiOutlinedInput-root': {
            backgroundColor: COLORS.background.elevated,
            borderRadius: '30px',
            padding: '5px 15px',
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: COLORS.primary.light
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: COLORS.primary.main,
              borderWidth: '2px'
            }
          },
          '& .MuiInputBase-input': {
            fontSize: '1.2rem',
            padding: '12px 16px'
          }
        }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <IconButton
                onClick={() => handleSearch()}
                edge="start"
                sx={{ p: '10px', color: COLORS.text.secondary }}
              >
                {searching ? <CircularProgress size={20} /> : <Search sx={{ fontSize: 30 }} />}
              </IconButton>
            </InputAdornment>
          ),
          endAdornment: query && (
            <InputAdornment position="end">
              <IconButton
                onClick={handleClear}
                edge="end"
                sx={{ color: COLORS.text.hint }}
              >
                <Clear />
              </IconButton>
            </InputAdornment>
          )
        }}
        {...props}
      />

      {/* Suggestions Panel */}
      {showSuggestionsPanel && (focused || showSuggestionsPanel) && (
        <Popper
          open={showSuggestionsPanel}
          anchorEl={anchorEl.current}
          placement="bottom-start"
          sx={{
            zIndex: 1300,
            width: anchorEl.current?.offsetWidth || '100%',
            mt: 1
          }}
        >
          <Paper
            elevation={8}
            sx={{
              backgroundColor: COLORS.background.paper,
              borderRadius: '16px',
              maxHeight: '400px',
              overflow: 'auto',
              border: `1px solid ${COLORS.primary.main}20`
            }}
          >
            {/* Media Search Results */}
            {searchResults.media.length > 0 && (
              <Box>
                <Typography
                  variant="subtitle2"
                  sx={{
                    px: 2,
                    py: 1,
                    color: COLORS.text.secondary,
                    borderBottom: `1px solid ${COLORS.background.elevated}`
                  }}
                >
                  Media Items
                </Typography>
                <List dense>
                  {searchResults.media.slice(0, 5).map((item) => (
                    <ListItem
                      key={item.id || item.Id}
                      button
                      onClick={() => handleSuggestionClick(item)}
                      sx={{
                        '&:hover': {
                          backgroundColor: COLORS.background.elevated
                        }
                      }}
                    >
                      <ListItemIcon sx={{ color: COLORS.primary.dark }}>
                        {mediaTypeIcons[item.mediaType?.toLowerCase() || item.MediaType?.toLowerCase()] || <Search />}
                      </ListItemIcon>
                      <ListItemText
                        primary={item.title || item.Title}
                        secondary={item.mediaType || item.MediaType}
                        primaryTypographyProps={{
                          variant: 'body2',
                          color: COLORS.text.primary
                        }}
                        secondaryTypographyProps={{
                          variant: 'caption',
                          color: COLORS.text.secondary
                        }}
                      />
                    </ListItem>
                  ))}
                </List>
              </Box>
            )}

            {/* Mixlist Search Results */}
            {searchResults.mixlists.length > 0 && (
              <Box>
                <Typography
                  variant="subtitle2"
                  sx={{
                    px: 2,
                    py: 1,
                    color: COLORS.text.secondary,
                    borderBottom: `1px solid ${COLORS.background.elevated}`
                  }}
                >
                  Mixlists
                </Typography>
                <List dense>
                  {searchResults.mixlists.slice(0, 3).map((mixlist) => (
                    <ListItem
                      key={mixlist.id || mixlist.Id}
                      button
                      onClick={() => handleSuggestionClick(mixlist)}
                      sx={{
                        '&:hover': {
                          backgroundColor: COLORS.background.elevated
                        }
                      }}
                    >
                      <ListItemIcon sx={{ color: COLORS.primary.dark }}>
                        <PlaylistPlay />
                      </ListItemIcon>
                      <ListItemText
                        primary={mixlist.name || mixlist.Name}
                        secondary={`${(mixlist.mediaItems || mixlist.MediaItems || []).length} items`}
                        primaryTypographyProps={{
                          variant: 'body2',
                          color: COLORS.text.primary
                        }}
                        secondaryTypographyProps={{
                          variant: 'caption',
                          color: COLORS.text.secondary
                        }}
                      />
                    </ListItem>
                  ))}
                </List>
              </Box>
            )}

            {/* Recent Searches */}
            {recentSearches.length > 0 && (
              <Box>
                <Typography
                  variant="subtitle2"
                  sx={{
                    px: 2,
                    py: 1,
                    color: COLORS.text.secondary,
                    borderBottom: `1px solid ${COLORS.background.elevated}`
                  }}
                >
                  Recent Searches
                </Typography>
                <List dense>
                  {recentSearches.slice(0, 3).map((search) => (
                    <ListItem
                      key={`recent-${search}`}
                      button
                      onClick={() => handleSuggestionClick(search)}
                      sx={{
                        '&:hover': {
                          backgroundColor: COLORS.background.elevated
                        }
                      }}
                    >
                      <ListItemIcon sx={{ color: COLORS.text.hint }}>
                        <History />
                      </ListItemIcon>
                      <ListItemText
                        primary={search}
                        primaryTypographyProps={{
                          variant: 'body2',
                          color: COLORS.text.primary
                        }}
                      />
                    </ListItem>
                  ))}
                </List>
              </Box>
            )}

            {/* Trending Searches */}
            {trendingSearches.length > 0 && (
              <Box>
                <Typography
                  variant="subtitle2"
                  sx={{
                    px: 2,
                    py: 1,
                    color: COLORS.text.secondary,
                    borderBottom: `1px solid ${COLORS.background.elevated}`
                  }}
                >
                  Trending
                </Typography>
                <Box sx={{ p: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                  {trendingSearches.slice(0, 6).map((trend) => (
                    <Chip
                      key={`trend-${trend}`}
                      label={trend}
                      size="small"
                      icon={<TrendingUp />}
                      onClick={() => handleSuggestionClick(trend)}
                      sx={{
                        backgroundColor: COLORS.primary.main,
                        color: COLORS.primary.contrastText,
                        '&:hover': {
                          backgroundColor: COLORS.primary.dark
                        }
                      }}
                    />
                  ))}
                </Box>
              </Box>
            )}

            {/* No results message */}
            {searchResults.media.length === 0 && searchResults.mixlists.length === 0 && recentSearches.length === 0 && trendingSearches.length === 0 && (
              <Box sx={{ p: 2, textAlign: 'center' }}>
                <Typography variant="body2" color={COLORS.text.hint}>
                  {query ? `No results found for "${query}"` : 'No suggestions available'}
                </Typography>
                {query && (
                  <Typography variant="caption" color={COLORS.text.hint} sx={{ display: 'block', mt: 1 }}>
                    Try different keywords or check your spelling
                  </Typography>
                )}
                {/* Debug info - remove in production */}
                {import.meta.env.DEV && query && (
                  <Box sx={{ mt: 2, p: 1, backgroundColor: COLORS.background.elevated, borderRadius: 1 }}>
                    <Typography variant="caption" color={COLORS.text.hint}>
                      Debug: Searched for &quot;{query}&quot; in titles, descriptions, genres, topics, and media types
                    </Typography>
                  </Box>
                )}
              </Box>
            )}
          </Paper>
        </Popper>
      )}
    </Box>
  );
};

export default SearchBar;
