import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    TextField, Button, Box, Typography,
    Select, MenuItem, InputLabel, FormControl,
    Card, CardContent, CircularProgress, Alert, Chip,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { Search, Download, MovieFilter, ExpandMore, OpenInNew } from '@mui/icons-material';
import { searchMovies, searchTvShows, searchMulti, getMovieDetails, getTvShowDetails } from '@/api/tmdbService';
import { importMovieFromTmdb } from '@/api/movieService';
import { importTvShowFromTmdb } from '@/api/tvShowService';
import WhiteOutlineButton from '@/shared/WhiteOutlineButton';
import { getPlaceholderImage } from '@/utils/mediaImageUtils';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_IMPORT_BLOCKED } from '@/features/demo/demoMessages';

function TmdbImportSection({ expanded, onAccordionChange }) {
    const navigate = useNavigate();

    const [tmdbSearchQuery, setTmdbSearchQuery] = useState('');
    const [tmdbSearchType, setTmdbSearchType] = useState('multi');
    const [tmdbSearchResults, setTmdbSearchResults] = useState([]);
    const [tmdbIsLoading, setTmdbIsLoading] = useState(false);
    const [tmdbError, setTmdbError] = useState('');
    const [tmdbSuccess, setTmdbSuccess] = useState('');
    const [selectedTmdbItem, setSelectedTmdbItem] = useState(null);
    const [showTmdbDetails, setShowTmdbDetails] = useState(false);
    const [displayedCount, setDisplayedCount] = useState(10);
    const [hasSearched, setHasSearched] = useState(false);

    const handleTmdbSearch = async () => {
        if (!tmdbSearchQuery.trim()) {
            setTmdbError('Please enter a search term');
            return;
        }

        setTmdbIsLoading(true);
        setTmdbError('');
        setTmdbSearchResults([]);
        setDisplayedCount(10);
        setHasSearched(true);

        try {
            let results;
            switch (tmdbSearchType) {
                case 'movies':
                    results = await searchMovies(tmdbSearchQuery);
                    break;
                case 'tv':
                    results = await searchTvShows(tmdbSearchQuery);
                    break;
                case 'multi':
                default:
                    results = await searchMulti(tmdbSearchQuery);
                    break;
            }

            setTmdbSearchResults(results.results || []);
            setTmdbIsLoading(false);

        } catch (err) {
            console.error('TMDB search error:', err);
            setTmdbError('Failed to search. Please try again.');
            setTmdbIsLoading(false);
        }
    };

    const handleLoadMore = () => {
        setDisplayedCount(prev => prev + 10);
    };

    const displayedResults = tmdbSearchResults.slice(0, displayedCount);

    const handleTmdbItemClick = async (item) => {
        setTmdbIsLoading(true);
        try {
            let details;
            if (item.media_type === 'movie' || tmdbSearchType === 'movies') {
                details = await getMovieDetails(item.id);
            } else if (item.media_type === 'tv' || tmdbSearchType === 'tv') {
                details = await getTvShowDetails(item.id);
            } else {
                if (item.media_type === 'movie') {
                    details = await getMovieDetails(item.id);
                } else if (item.media_type === 'tv') {
                    details = await getTvShowDetails(item.id);
                }
            }
            setSelectedTmdbItem(details);
            setShowTmdbDetails(true);
        } catch (err) {
            setTmdbError('Failed to load details. Please try again.');
            console.error('TMDB details error:', err);
        } finally {
            setTmdbIsLoading(false);
        }
    };

    const handleTmdbImport = async (item) => {
        setTmdbIsLoading(true);
        setTmdbError('');

        try {
            let result;

            if (item.media_type === 'movie' || tmdbSearchType === 'movies') {
                result = await importMovieFromTmdb(item.id);
            } else if (item.media_type === 'tv' || tmdbSearchType === 'tv') {
                result = await importTvShowFromTmdb(item.id);
            } else {
                throw new Error('Unknown media type');
            }

            setTmdbSuccess(`"${item.title || item.name}" imported successfully!`);
            setTmdbIsLoading(false);

            console.log('TMDB import successful:', result);

            const mediaId = result.id || result.Id;
            if (mediaId) {
                setTimeout(() => {
                    navigate(`/media/${mediaId}`);
                }, 1500);
            } else {
                setTimeout(() => {
                    navigate('/all-media');
                }, 1500);
            }

        } catch (err) {
            console.error('TMDB import error:', err);
            setTmdbError(`Failed to import: ${err.message}`);
            setTmdbIsLoading(false);
        }
    };

    const getTmdbPlaceholder = (item) => {
        const isTv = item?.media_type === 'tv' || tmdbSearchType === 'tv';
        return getPlaceholderImage(isTv ? 'TVShow' : 'Movie');
    };

    const getTmdbImageUrl = (item) => {
        if (item.poster_path) {
            return `https://image.tmdb.org/t/p/w500${item.poster_path}`;
        }
        return getTmdbPlaceholder(item);
    };

    const getTmdbItemTitle = (item) => {
        return item.title || item.name || 'Unknown Title';
    };

    const getTmdbItemYear = (item) => {
        const date = item.release_date || item.first_air_date;
        return date ? new Date(date).getFullYear() : '';
    };

    const getTmdbItemType = (item) => {
        if (item.media_type) {
            return item.media_type === 'movie' ? 'Movie' : 'TV Show';
        }
        return tmdbSearchType === 'movies' ? 'Movie' : 'TV Show';
    };

    const formatDate = (dateString) => {
        if (!dateString) return '';
        return new Date(dateString).toLocaleDateString();
    };

    return (
        <>
            <Accordion
                expanded={expanded === 'tmdb'}
                onChange={onAccordionChange('tmdb')}
                sx={{ mb: 2 }}
            >
                <AccordionSummary
                    expandIcon={<ExpandMore />}
                    aria-controls="tmdb-content"
                    id="tmdb-header"
                >
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                        <MovieFilter />
                        <Typography variant="h6">
                            Movies & TV Shows
                        </Typography>
                        <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Typography variant="body2" color="text.secondary">
                                Powered by
                            </Typography>
                            <Button
                                variant="text"
                                size="small"
                                href="https://www.themoviedb.org"
                                target="_blank"
                                rel="noopener noreferrer"
                                endIcon={<OpenInNew fontSize="small" />}
                                sx={{
                                    minWidth: 'auto',
                                    textTransform: 'none',
                                    color: '#ffffff',
                                    '&:hover': { backgroundColor: 'transparent', textDecoration: 'underline' }
                                }}
                                onClick={(e) => e.stopPropagation()}
                            >
                                TMDB
                            </Button>
                        </Box>
                    </Box>
                </AccordionSummary>
                <AccordionDetails>
                    <Box sx={{ padding: 2 }}>
                        <FormControl fullWidth margin="normal">
                            <InputLabel>Search Type</InputLabel>
                            <Select
                                value={tmdbSearchType}
                                label="Search Type"
                                onChange={(e) => setTmdbSearchType(e.target.value)}
                            >
                                <MenuItem value="multi">All (Movies & TV Shows)</MenuItem>
                                <MenuItem value="movies">Movies Only</MenuItem>
                                <MenuItem value="tv">TV Shows Only</MenuItem>
                            </Select>
                        </FormControl>

                        <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                            <TextField
                                label="Search Movies & TV Shows"
                                value={tmdbSearchQuery}
                                onChange={(e) => setTmdbSearchQuery(e.target.value)}
                                variant="outlined"
                                fullWidth
                                onKeyPress={(e) => e.key === 'Enter' && handleTmdbSearch()}
                                InputLabelProps={{
                                    sx: { color: 'white' }
                                }}
                            />
                            <Button
                                variant="contained"
                                onClick={handleTmdbSearch}
                                disabled={tmdbIsLoading}
                                startIcon={<Search />}
                            >
                                Search
                            </Button>
                        </Box>

                        {/* TMDB Credit */}
                        <Box sx={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 1,
                            mb: 2,
                            justifyContent: 'center',
                            py: 1
                        }}>
                            <img
                                src="/tmdb-primary-short-logo.svg"
                                alt="TMDB"
                                style={{
                                    height: '20px',
                                    width: 'auto'
                                }}
                            />
                            <Typography
                                variant="caption"
                                sx={{
                                    color: 'text.secondary',
                                    fontSize: '0.75rem'
                                }}
                            >
                                This product uses the TMDB API but is not endorsed or certified by TMDB
                            </Typography>
                        </Box>

                        {hasSearched && !tmdbIsLoading && tmdbSearchResults.length === 0 && !tmdbError && (
                            <Alert severity="info" sx={{ mt: 2 }}>
                                No results found. Try a different search term.
                            </Alert>
                        )}

                        {displayedResults.length > 0 && (
                            <Box sx={{ mt: 2 }}>
                                <Typography variant="h6" gutterBottom>
                                    Search Results ({tmdbSearchResults.length})
                                </Typography>
                                {displayedResults.map((item) => (
                                    <Card key={`tmdb-${item.id}`} sx={{ mb: 2 }}>
                                        <CardContent>
                                            <Box sx={{ display: 'flex', gap: 2 }}>
                                                <Box
                                                    sx={{
                                                        width: 80,
                                                        height: 120,
                                                        borderRadius: 1,
                                                        overflow: 'hidden',
                                                        flexShrink: 0,
                                                        backgroundColor: 'rgba(255, 255, 255, 0.1)'
                                                    }}
                                                >
                                                    <img
                                                        src={getTmdbImageUrl(item)}
                                                        alt=""
                                                        style={{
                                                            width: '100%',
                                                            height: '100%',
                                                            objectFit: 'cover',
                                                            display: 'block',
                                                            fontSize: '12px'
                                                        }}
                                                        onError={(e) => {
                                                            e.target.onerror = null;
                                                            e.target.src = getTmdbPlaceholder(item);
                                                        }}
                                                    />
                                                </Box>
                                                <Box sx={{ flex: 1 }}>
                                                    <Typography
                                                        variant="h6"
                                                        gutterBottom
                                                        sx={{
                                                            fontSize: '1.25rem',
                                                            fontWeight: 600,
                                                            minHeight: '1.5rem',
                                                            lineHeight: 1.2
                                                        }}
                                                    >
                                                        {getTmdbItemTitle(item)}
                                                    </Typography>
                                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                                                        <Chip
                                                            label={getTmdbItemType(item)}
                                                            size="small"
                                                            color="primary"
                                                        />
                                                        {getTmdbItemYear(item) && (
                                                            <Typography variant="body2" color="text.secondary">
                                                                ({getTmdbItemYear(item)})
                                                            </Typography>
                                                        )}
                                                        {item.vote_average && (
                                                            <Typography variant="body2" color="text.secondary">
                                                                {item.vote_average.toFixed(1)}
                                                            </Typography>
                                                        )}
                                                    </Box>
                                                    <Typography
                                                        variant="body2"
                                                        sx={{
                                                            mb: 2,
                                                            fontSize: '0.875rem',
                                                            lineHeight: 1.4,
                                                            minHeight: '2.8rem',
                                                            display: '-webkit-box',
                                                            WebkitLineClamp: 3,
                                                            WebkitBoxOrient: 'vertical',
                                                            overflow: 'hidden'
                                                        }}
                                                    >
                                                        {item.overview ?
                                                            (item.overview.length > 200
                                                                ? `${item.overview.substring(0, 200)}...`
                                                                : item.overview
                                                            ) : 'No description available.'}
                                                    </Typography>
                                                    <Box sx={{ display: 'flex', gap: 1 }}>
                                                        <WhiteOutlineButton
                                                            size="small"
                                                            onClick={() => handleTmdbItemClick(item)}
                                                            disabled={tmdbIsLoading}
                                                        >
                                                            View Details
                                                        </WhiteOutlineButton>
                                                        <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                                                            <Button
                                                                variant="contained"
                                                                size="small"
                                                                onClick={() => handleTmdbImport(item)}
                                                                disabled={tmdbIsLoading}
                                                                startIcon={<Download />}
                                                            >
                                                                Import
                                                            </Button>
                                                        </DemoWriteGuard>
                                                    </Box>
                                                </Box>
                                            </Box>
                                        </CardContent>
                                    </Card>
                                ))}
                                {displayedResults.length < tmdbSearchResults.length && (
                                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2 }}>
                                        <Typography variant="body2" color="text.secondary">
                                            Showing {displayedResults.length} of {tmdbSearchResults.length} results
                                        </Typography>
                                        <Button
                                            variant="contained"
                                            size="small"
                                            onClick={handleLoadMore}
                                        >
                                            Load 10 More
                                        </Button>
                                    </Box>
                                )}
                            </Box>
                        )}

                        {tmdbIsLoading && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
                                <CircularProgress />
                            </Box>
                        )}

                        {tmdbError && (
                            <Alert severity="error" sx={{ mt: 2 }}>
                                {tmdbError}
                            </Alert>
                        )}

                        {tmdbSuccess && (
                            <Alert severity="success" sx={{ mt: 2 }}>
                                {tmdbSuccess}
                            </Alert>
                        )}
                    </Box>
                </AccordionDetails>
            </Accordion>

            {/* TMDB Details Modal */}
            {showTmdbDetails && selectedTmdbItem && (
                <Box
                    sx={{
                        position: 'fixed',
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                        backgroundColor: 'rgba(0, 0, 0, 0.5)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        zIndex: 1300,
                        p: 2
                    }}
                    onClick={() => setShowTmdbDetails(false)}
                >
                    <Box
                        sx={{
                            backgroundColor: '#474350',
                            borderRadius: 2,
                            maxWidth: '800px',
                            width: '100%',
                            maxHeight: '90vh',
                            overflow: 'auto',
                            p: 3
                        }}
                        onClick={(e) => e.stopPropagation()}
                    >
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 3 }}>
                            <Typography variant="h4" sx={{ flex: 1, mr: 2, color: '#ffffff' }}>
                                {selectedTmdbItem.title || selectedTmdbItem.name}
                            </Typography>
                            <WhiteOutlineButton
                                onClick={() => setShowTmdbDetails(false)}
                                sx={{ minWidth: 'auto', p: 1 }}
                            >
                                x
                            </WhiteOutlineButton>
                        </Box>

                        <Box sx={{ display: 'flex', gap: 3, mb: 3 }}>
                            <img
                                src={getTmdbImageUrl(selectedTmdbItem)}
                                alt={selectedTmdbItem.title || selectedTmdbItem.name}
                                style={{
                                    width: 200,
                                    height: 300,
                                    objectFit: 'cover',
                                    borderRadius: 8
                                }}
                                onError={(e) => {
                                    e.target.onerror = null;
                                    e.target.src = getTmdbPlaceholder(selectedTmdbItem);
                                }}
                            />
                            <Box sx={{ flex: 1 }}>
                                <Typography variant="h6" gutterBottom sx={{ color: '#ffffff' }}>
                                    Overview
                                </Typography>
                                <Typography variant="body1" sx={{ mb: 3, color: '#ffffff' }}>
                                    {selectedTmdbItem.overview || 'No description available.'}
                                </Typography>

                                <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2, mb: 3 }}>
                                    <Box>
                                        <Typography variant="subtitle2" sx={{ color: '#fcfafa' }}>
                                            Release Date
                                        </Typography>
                                        <Typography variant="body2" sx={{ color: '#ffffff' }}>
                                            {formatDate(selectedTmdbItem.release_date || selectedTmdbItem.first_air_date)}
                                        </Typography>
                                    </Box>
                                    <Box>
                                        <Typography variant="subtitle2" sx={{ color: '#fcfafa' }}>
                                            Rating
                                        </Typography>
                                        <Typography variant="body2" sx={{ color: '#ffffff' }}>
                                            {selectedTmdbItem.vote_average ? `${selectedTmdbItem.vote_average.toFixed(1)}` : 'N/A'}
                                        </Typography>
                                    </Box>
                                    {selectedTmdbItem.runtime && (
                                        <Box>
                                            <Typography variant="subtitle2" sx={{ color: '#fcfafa' }}>
                                                Runtime
                                            </Typography>
                                            <Typography variant="body2" sx={{ color: '#ffffff' }}>
                                                {selectedTmdbItem.runtime} minutes
                                            </Typography>
                                        </Box>
                                    )}
                                    {selectedTmdbItem.number_of_seasons && (
                                        <Box>
                                            <Typography variant="subtitle2" sx={{ color: '#fcfafa' }}>
                                                Seasons
                                            </Typography>
                                            <Typography variant="body2" sx={{ color: '#ffffff' }}>
                                                {selectedTmdbItem.number_of_seasons}
                                            </Typography>
                                        </Box>
                                    )}
                                </Box>

                                {selectedTmdbItem.genres && selectedTmdbItem.genres.length > 0 && (
                                    <Box sx={{ mb: 3 }}>
                                        <Typography variant="subtitle2" sx={{ color: '#fcfafa' }} gutterBottom>
                                            Genres
                                        </Typography>
                                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                                            {selectedTmdbItem.genres.map((genre) => (
                                                <Chip
                                                    key={`genre-${genre.id || genre.name}`}
                                                    label={genre.name}
                                                    size="small"
                                                    variant="outlined"
                                                />
                                            ))}
                                        </Box>
                                    </Box>
                                )}

                                <Box sx={{ display: 'flex', gap: 2 }}>
                                    <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                                        <Button
                                            variant="contained"
                                            onClick={() => {
                                                handleTmdbImport(selectedTmdbItem);
                                                setShowTmdbDetails(false);
                                            }}
                                            disabled={tmdbIsLoading}
                                            startIcon={<Download />}
                                        >
                                            Import to Library
                                        </Button>
                                    </DemoWriteGuard>
                                    <WhiteOutlineButton
                                        onClick={() => setShowTmdbDetails(false)}
                                    >
                                        Close
                                    </WhiteOutlineButton>
                                </Box>
                            </Box>
                        </Box>
                    </Box>
                </Box>
            )}
        </>
    );
}

export default TmdbImportSection;
