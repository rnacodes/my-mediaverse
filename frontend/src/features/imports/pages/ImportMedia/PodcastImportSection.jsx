import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    TextField, Button, Box, Typography,
    Select, MenuItem, InputLabel, FormControl,
    Card, CardContent, CircularProgress, Alert, Chip,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { Search, Download, Podcasts, ExpandMore, OpenInNew } from '@mui/icons-material';
import { searchPodcasts, importPodcastSeriesFromApi, importPodcastSeriesByName } from '@/api/podcastService';
import WhiteOutlineButton from '@/shared/WhiteOutlineButton';
import { getPlaceholderImage } from '@/utils/mediaImageUtils';

function PodcastImportSection({ expanded, onAccordionChange, onSnackbar }) {
    const navigate = useNavigate();

    const [podcastImportMethod, setPodcastImportMethod] = useState('search');
    const [podcastSearchQuery, setPodcastSearchQuery] = useState('');
    const [podcastId, setPodcastId] = useState('');
    const [podcastName, setPodcastName] = useState('');
    const [podcastSearchResults, setPodcastSearchResults] = useState([]);
    const [podcastIsLoading, setPodcastIsLoading] = useState(false);
    const [podcastError, setPodcastError] = useState('');
    const [displayedCount, setDisplayedCount] = useState(10);
    const [hasSearched, setHasSearched] = useState(false);

    const handlePodcastSearch = async () => {
        if (!podcastSearchQuery.trim()) {
            setPodcastError('Please enter a search term');
            return;
        }

        setPodcastIsLoading(true);
        setPodcastError('');
        setPodcastSearchResults([]);
        setDisplayedCount(10);
        setHasSearched(true);

        try {
            const data = await searchPodcasts(podcastSearchQuery);

            const transformedResults = data.results?.map(podcast => ({
                id: podcast.id,
                title: podcast.title_original || podcast.title_highlighted || 'Unknown Title',
                publisher: podcast.publisher_original || podcast.publisher_highlighted || 'Unknown Publisher',
                description: podcast.description_original || podcast.description_highlighted || 'No description available',
                image: podcast.image || getPlaceholderImage('Podcast'),
                total_episodes: podcast.total_episodes || 0,
                listennotes_url: podcast.listennotes_url || null
            })) || [];

            setPodcastSearchResults(transformedResults);
            setPodcastIsLoading(false);

        } catch (err) {
            console.error('Search error:', err);
            setPodcastError('Failed to search podcasts. Please try again.');
            setPodcastIsLoading(false);
        }
    };

    const handlePodcastImportById = async () => {
        if (!podcastId.trim()) {
            setPodcastError('Please enter a podcast ID');
            return;
        }

        setPodcastIsLoading(true);
        setPodcastError('');

        try {
            const result = await importPodcastSeriesFromApi(podcastId);
            const responseData = result.data || result;

            onSnackbar?.({ open: true, message: 'Podcast series imported successfully!', severity: 'success' });
            setPodcastIsLoading(false);
            setPodcastId('');

            console.log('Podcast imported successfully:', responseData);

            const mediaId = responseData.id || responseData.Id;
            setTimeout(() => {
                navigate(mediaId ? `/podcast-series/${mediaId}` : '/all-media');
            }, 1500);

        } catch (err) {
            console.error('Import by ID error:', err);
            setPodcastError('Failed to import podcast. Please check the ID and try again.');
            setPodcastIsLoading(false);
        }
    };

    const handlePodcastImportByName = async () => {
        if (!podcastName.trim()) {
            setPodcastError('Please enter a podcast name');
            return;
        }

        setPodcastIsLoading(true);
        setPodcastError('');

        try {
            const result = await importPodcastSeriesByName(podcastName);
            const responseData = result.data || result;

            onSnackbar?.({ open: true, message: 'Podcast series imported successfully!', severity: 'success' });
            setPodcastIsLoading(false);
            setPodcastName('');

            console.log('Podcast imported successfully:', responseData);

            const mediaId = responseData.id || responseData.Id;
            setTimeout(() => {
                navigate(mediaId ? `/podcast-series/${mediaId}` : '/all-media');
            }, 1500);

        } catch (err) {
            console.error('Import by name error:', err);
            setPodcastError('Failed to import podcast. Please check the name and try again.');
            setPodcastIsLoading(false);
        }
    };

    const handleImportPodcast = async (podcast) => {
        setPodcastIsLoading(true);
        setPodcastError('');

        try {
            const result = await importPodcastSeriesFromApi(podcast.id);
            const responseData = result.data || result;

            onSnackbar?.({ open: true, message: `"${podcast.title}" imported successfully!`, severity: 'success' });
            setPodcastIsLoading(false);

            console.log('Podcast imported successfully:', responseData);

            const mediaId = responseData.id || responseData.Id;
            setTimeout(() => {
                navigate(mediaId ? `/podcast-series/${mediaId}` : '/all-media');
            }, 1500);

        } catch (err) {
            console.error('Import podcast error:', err);
            setPodcastError('Failed to import podcast. Please try again.');
            setPodcastIsLoading(false);
        }
    };

    const handleLoadMore = () => {
        setDisplayedCount(prev => prev + 10);
    };

    const displayedResults = podcastSearchResults.slice(0, displayedCount);

    return (
        <Accordion
            expanded={expanded === 'podcasts'}
            onChange={onAccordionChange('podcasts')}
            sx={{ mb: 2 }}
        >
            <AccordionSummary
                expandIcon={<ExpandMore />}
                aria-controls="podcasts-content"
                id="podcasts-header"
            >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                    <Podcasts />
                    <Typography variant="h6">
                        Podcasts
                    </Typography>
                    <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                            Powered by
                        </Typography>
                        <Button
                            variant="text"
                            size="small"
                            href="https://www.listennotes.com"
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
                            ListenNotes
                        </Button>
                    </Box>
                </Box>
            </AccordionSummary>
            <AccordionDetails>
                <Box sx={{ padding: 2 }}>
                    <FormControl fullWidth margin="normal">
                        <InputLabel>Import Method</InputLabel>
                        <Select
                            value={podcastImportMethod}
                            label="Import Method"
                            onChange={(e) => setPodcastImportMethod(e.target.value)}
                        >
                            <MenuItem value="search">Search and Select</MenuItem>
                            <MenuItem value="id">By Podcast ID</MenuItem>
                            <MenuItem value="name">By Podcast Name</MenuItem>
                        </Select>
                    </FormControl>

                    {podcastImportMethod === 'search' && (
                        <Box>
                            <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                                <TextField
                                    label="Search Podcasts"
                                    value={podcastSearchQuery}
                                    onChange={(e) => setPodcastSearchQuery(e.target.value)}
                                    variant="outlined"
                                    fullWidth
                                    onKeyPress={(e) => e.key === 'Enter' && handlePodcastSearch()}
                                    InputLabelProps={{
                                        sx: { color: 'white' }
                                    }}
                                />
                                <Button
                                    variant="contained"
                                    onClick={handlePodcastSearch}
                                    disabled={podcastIsLoading}
                                    startIcon={<Search />}
                                >
                                    Search
                                </Button>
                            </Box>

                            {hasSearched && !podcastIsLoading && podcastSearchResults.length === 0 && !podcastError && (
                                <Alert severity="info" sx={{ mt: 2 }}>
                                    No results found. Try a different search term.
                                </Alert>
                            )}

                            {displayedResults.length > 0 && (
                                <Box sx={{ mt: 2 }}>
                                    <Typography variant="h6" gutterBottom>
                                        Search Results ({podcastSearchResults.length})
                                    </Typography>
                                    {displayedResults.map((podcast) => (
                                        <Card key={podcast.id} sx={{ mb: 2 }}>
                                            <CardContent>
                                                <Box sx={{ display: 'flex', gap: 2 }}>
                                                    <img
                                                        src={podcast.image}
                                                        alt={podcast.title}
                                                        style={{
                                                            width: 80,
                                                            height: 80,
                                                            objectFit: 'cover',
                                                            borderRadius: 4
                                                        }}
                                                    />
                                                    <Box sx={{ flex: 1 }}>
                                                        <Typography variant="h6" gutterBottom>
                                                            {podcast.title}
                                                        </Typography>
                                                        <Typography variant="body2" color="text.secondary" gutterBottom>
                                                            {podcast.publisher}
                                                        </Typography>
                                                        <Typography variant="body2" sx={{ mb: 1 }}>
                                                            {podcast.description.length > 200
                                                                ? `${podcast.description.substring(0, 200)}...`
                                                                : podcast.description}
                                                        </Typography>
                                                        <Chip
                                                            label={`${podcast.total_episodes} episodes`}
                                                            size="small"
                                                            sx={{ mb: 1 }}
                                                        />
                                                        <Box sx={{ display: 'flex', gap: 1 }}>
                                                            <WhiteOutlineButton
                                                                size="small"
                                                                href={podcast.listennotes_url || `https://www.listennotes.com/podcasts/${podcast.id}/`}
                                                                target="_blank"
                                                                rel="noopener noreferrer"
                                                                endIcon={<OpenInNew fontSize="small" />}
                                                            >
                                                                View Details
                                                            </WhiteOutlineButton>
                                                            <Button
                                                                variant="contained"
                                                                size="small"
                                                                onClick={() => handleImportPodcast(podcast)}
                                                                disabled={podcastIsLoading}
                                                                startIcon={<Download />}
                                                            >
                                                                Import
                                                            </Button>
                                                        </Box>
                                                    </Box>
                                                </Box>
                                            </CardContent>
                                        </Card>
                                    ))}
                                    {displayedResults.length < podcastSearchResults.length && (
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2 }}>
                                            <Typography variant="body2" color="text.secondary">
                                                Showing {displayedResults.length} of {podcastSearchResults.length} results
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
                        </Box>
                    )}

                    {podcastImportMethod === 'id' && (
                        <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
                            <TextField
                                label="Podcast ID"
                                value={podcastId}
                                onChange={(e) => setPodcastId(e.target.value)}
                                variant="outlined"
                                fullWidth
                                onKeyPress={(e) => e.key === 'Enter' && handlePodcastImportById()}
                                InputLabelProps={{
                                    sx: { color: 'white' }
                                }}
                            />
                            <Button
                                variant="contained"
                                onClick={handlePodcastImportById}
                                disabled={podcastIsLoading}
                                startIcon={<Download />}
                            >
                                Import
                            </Button>
                        </Box>
                    )}

                    {podcastImportMethod === 'name' && (
                        <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
                            <TextField
                                label="Podcast Name"
                                value={podcastName}
                                onChange={(e) => setPodcastName(e.target.value)}
                                variant="outlined"
                                fullWidth
                                onKeyPress={(e) => e.key === 'Enter' && handlePodcastImportByName()}
                                InputLabelProps={{
                                    sx: { color: 'white' }
                                }}
                            />
                            <Button
                                variant="contained"
                                onClick={handlePodcastImportByName}
                                disabled={podcastIsLoading}
                                startIcon={<Download />}
                            >
                                Import
                            </Button>
                        </Box>
                    )}

                    {podcastIsLoading && (
                        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
                            <CircularProgress />
                        </Box>
                    )}

                    {podcastError && (
                        <Alert severity="error" sx={{ mt: 2 }}>
                            {podcastError}
                        </Alert>
                    )}
                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default PodcastImportSection;
