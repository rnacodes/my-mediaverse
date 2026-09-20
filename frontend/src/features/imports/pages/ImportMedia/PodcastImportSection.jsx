import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    TextField, Button, Box, Typography, Tabs, Tab,
    Card, CardContent, CircularProgress, Alert, Chip,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { Search, Download, Podcasts, ExpandMore, OpenInNew, Visibility } from '@mui/icons-material';
import { usePodcastDirectorySearch, useImportPodcastSeriesFromFeed } from '@/hooks/usePodcast';
import WhiteOutlineButton from '@/shared/WhiteOutlineButton';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';
import { DEMO_IMPORT_BLOCKED } from '@/features/demo/demoMessages';
import { getPlaceholderImage } from '@/utils/mediaImageUtils';
import AttributionBadge from '@/shared/AttributionBadge';
import SafeImage from './SafeImage';
import { parseFeedInput } from './parseFeedInput';

const PAGE_SIZE = 10;

const errorText = (err, fallback) => err?.response?.data?.error || fallback;

const resultKey = (podcast) =>
    podcast.applePodcastsId || podcast.feedUrl || podcast.podcastIndexId || podcast.title;

function PodcastImportSection({ expanded, onAccordionChange, onSnackbar }) {
    const navigate = useNavigate();
    const writeBlocked = useDemoWriteBlocked();

    const [activeTab, setActiveTab] = useState('search');
    const [searchInput, setSearchInput] = useState('');
    const [submittedTerm, setSubmittedTerm] = useState('');
    const [feedInput, setFeedInput] = useState('');
    const [inputError, setInputError] = useState('');
    const [displayedCount, setDisplayedCount] = useState(PAGE_SIZE);

    const directorySearch = usePodcastDirectorySearch(submittedTerm);
    const importSeries = useImportPodcastSeriesFromFeed();

    const results = directorySearch.data || [];
    const displayedResults = results.slice(0, displayedCount);
    const isSearching = directorySearch.isFetching;
    const isImporting = importSeries.isPending;

    const handleTabChange = (_event, tab) => {
        setActiveTab(tab);
        setInputError('');
        importSeries.reset();
    };

    const handleSearch = () => {
        const term = searchInput.trim();
        if (!term) {
            setInputError('Please enter a search term');
            return;
        }

        setInputError('');
        setDisplayedCount(PAGE_SIZE);
        importSeries.reset();

        // Same term again: the query key is unchanged, so ask for a fresh read.
        if (term === submittedTerm) {
            directorySearch.refetch();
        } else {
            setSubmittedTerm(term);
        }
    };

    const importFromFeed = (payload, title) => {
        setInputError('');
        importSeries.mutate(payload, {
            onSuccess: ({ status, data }) => {
                const name = data?.series?.title || title || 'Podcast';
                const created = status === 201;

                onSnackbar?.({
                    open: true,
                    message: data?.warningMessage
                        || (created ? `"${name}" imported successfully!` : `"${name}" is already in your library.`),
                    severity: data?.warningMessage ? 'warning' : created ? 'success' : 'info',
                });
                setFeedInput('');

                const seriesId = data?.series?.id;
                setTimeout(() => {
                    navigate(seriesId ? `/podcast-series/${seriesId}` : '/all-media');
                }, 1500);
            },
        });
    };

    const handleImportFromInput = () => {
        // Enter in the text box reaches here without going through the guarded button.
        if (writeBlocked) return;

        const payload = parseFeedInput(feedInput);
        if (!payload) {
            setInputError('Enter a feed URL, an Apple Podcasts link, or an Apple Podcasts id');
            return;
        }
        importFromFeed(payload);
    };

    const handleImportResult = (podcast) => {
        importFromFeed(
            { feedUrl: podcast.feedUrl || undefined, applePodcastsId: podcast.applePodcastsId || undefined },
            podcast.title,
        );
    };

    const errorMessage = inputError
        || (importSeries.isError && errorText(importSeries.error, 'Failed to import podcast. Please try again.'))
        || (activeTab === 'search' && directorySearch.isError
            && errorText(directorySearch.error, 'Failed to search podcasts. Please try again.'));

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
                </Box>
            </AccordionSummary>
            <AccordionDetails>
                <Box sx={{ padding: 2 }}>
                    <Tabs
                        value={activeTab}
                        onChange={handleTabChange}
                        textColor="inherit"
                        sx={{ mb: 2 }}
                    >
                        <Tab value="search" label="Search Apple Podcasts" />
                        <Tab value="feed" label="Paste a feed URL" />
                    </Tabs>

                    {activeTab === 'search' && (
                        <Box>
                            <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                                <TextField
                                    label="Search Podcasts"
                                    value={searchInput}
                                    onChange={(e) => setSearchInput(e.target.value)}
                                    variant="outlined"
                                    fullWidth
                                    onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                                    InputLabelProps={{
                                        sx: { color: 'white' }
                                    }}
                                />
                                <Button
                                    variant="contained"
                                    onClick={handleSearch}
                                    disabled={isSearching}
                                    startIcon={<Search />}
                                >
                                    Search
                                </Button>
                            </Box>

                            {directorySearch.isSuccess && !isSearching && results.length === 0 && (
                                <Alert severity="info" sx={{ mt: 2 }}>
                                    No results found. Try a different search term.
                                </Alert>
                            )}

                            {displayedResults.length > 0 && (
                                <Box sx={{ mt: 2 }}>
                                    <Typography variant="h6">
                                        Search Results ({results.length})
                                    </Typography>
                                    <AttributionBadge provider="apple" sx={{ mb: 2 }} />
                                    {displayedResults.map((podcast) => (
                                        <Card key={resultKey(podcast)} sx={{ mb: 2 }}>
                                            <CardContent>
                                                <Box sx={{ display: 'flex', gap: 2 }}>
                                                    <SafeImage
                                                        src={podcast.artworkUrl}
                                                        fallbackSrc={getPlaceholderImage('Podcast')}
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
                                                            {podcast.publisher || 'Unknown Publisher'}
                                                        </Typography>
                                                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 1 }}>
                                                            {podcast.episodeCount != null && (
                                                                <Chip label={`${podcast.episodeCount} episodes`} size="small" />
                                                            )}
                                                            {(podcast.genres || []).slice(0, 3).map((genre) => (
                                                                <Chip key={genre} label={genre} size="small" variant="outlined" />
                                                            ))}
                                                            {podcast.existingSeriesId && (
                                                                <Chip label="In your library" size="small" color="success" />
                                                            )}
                                                        </Box>
                                                        {podcast.source === 'podcastindex' && (
                                                            <AttributionBadge provider="podcastindex" sx={{ mb: 1 }} />
                                                        )}
                                                        <Box sx={{ display: 'flex', gap: 1 }}>
                                                            {podcast.storeUrl && (
                                                                <WhiteOutlineButton
                                                                    size="small"
                                                                    href={podcast.storeUrl}
                                                                    target="_blank"
                                                                    rel="noopener noreferrer"
                                                                    endIcon={<OpenInNew fontSize="small" />}
                                                                >
                                                                    View on Apple Podcasts
                                                                </WhiteOutlineButton>
                                                            )}
                                                            {podcast.existingSeriesId ? (
                                                                <Button
                                                                    variant="contained"
                                                                    size="small"
                                                                    onClick={() => navigate(`/podcast-series/${podcast.existingSeriesId}`)}
                                                                    startIcon={<Visibility />}
                                                                >
                                                                    View in library
                                                                </Button>
                                                            ) : (
                                                                <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                                                                    <Button
                                                                        variant="contained"
                                                                        size="small"
                                                                        onClick={() => handleImportResult(podcast)}
                                                                        disabled={isImporting}
                                                                        startIcon={<Download />}
                                                                    >
                                                                        Import
                                                                    </Button>
                                                                </DemoWriteGuard>
                                                            )}
                                                        </Box>
                                                    </Box>
                                                </Box>
                                            </CardContent>
                                        </Card>
                                    ))}
                                    {displayedResults.length < results.length && (
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2 }}>
                                            <Typography variant="body2" color="text.secondary">
                                                Showing {displayedResults.length} of {results.length} results
                                            </Typography>
                                            <Button
                                                variant="contained"
                                                size="small"
                                                onClick={() => setDisplayedCount(prev => prev + PAGE_SIZE)}
                                            >
                                                Load {PAGE_SIZE} More
                                            </Button>
                                        </Box>
                                    )}
                                </Box>
                            )}
                        </Box>
                    )}

                    {activeTab === 'feed' && (
                        <Box>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                                Paste the show&apos;s RSS feed URL, an Apple Podcasts link, or an Apple Podcasts id.
                            </Typography>
                            <Box sx={{ display: 'flex', gap: 2 }}>
                                <TextField
                                    label="Feed URL or Apple Podcasts link"
                                    value={feedInput}
                                    onChange={(e) => setFeedInput(e.target.value)}
                                    variant="outlined"
                                    fullWidth
                                    onKeyDown={(e) => e.key === 'Enter' && handleImportFromInput()}
                                    InputLabelProps={{
                                        sx: { color: 'white' }
                                    }}
                                />
                                <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                                    <Button
                                        variant="contained"
                                        onClick={handleImportFromInput}
                                        disabled={isImporting}
                                        startIcon={<Download />}
                                    >
                                        Import
                                    </Button>
                                </DemoWriteGuard>
                            </Box>
                        </Box>
                    )}

                    {(isSearching || isImporting) && (
                        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
                            <CircularProgress />
                        </Box>
                    )}

                    {errorMessage && (
                        <Alert severity="error" sx={{ mt: 2 }}>
                            {errorMessage}
                        </Alert>
                    )}
                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default PodcastImportSection;
