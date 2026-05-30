import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    TextField, Button, Box, Typography,
    Select, MenuItem, InputLabel, FormControl,
    Card, CardContent, CircularProgress, Alert, Chip,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { Search, Download, VideoLibrary, ExpandMore, OpenInNew } from '@mui/icons-material';
import {
    searchYouTube, getYouTubePlaylistDetails,
    importYouTubeVideo, importYouTubePlaylistEntity, importYouTubeChannelEntity,
    importFromYouTubeUrl, checkYouTubeChannelExists
} from '../../api/youtubeService';
import WhiteOutlineButton from '@/shared/WhiteOutlineButton';
import SafeImage from './SafeImage';

function YouTubeImportSection({ expanded, onAccordionChange, onSnackbar }) {
    const navigate = useNavigate();

    const [youtubeImportMethod, setYoutubeImportMethod] = useState('search');
    const [youtubeSearchQuery, setYoutubeSearchQuery] = useState('');
    const [youtubeSearchType, setYoutubeSearchType] = useState('video');
    const [youtubeUrl, setYoutubeUrl] = useState('');
    const [youtubeSearchResults, setYoutubeSearchResults] = useState([]);
    const [youtubeIsLoading, setYoutubeIsLoading] = useState(false);
    const [youtubeError, setYoutubeError] = useState('');
    const [youtubeSuccess, setYoutubeSuccess] = useState('');
    const [displayedCount, setDisplayedCount] = useState(10);
    const [hasSearched, setHasSearched] = useState(false);

    const handleYoutubeSearch = async () => {
        if (!youtubeSearchQuery.trim()) {
            setYoutubeError('Please enter a search term');
            return;
        }

        setYoutubeIsLoading(true);
        setYoutubeError('');
        setYoutubeSuccess('');
        setYoutubeSearchResults([]);
        setDisplayedCount(10);
        setHasSearched(true);

        try {
            const data = await searchYouTube(youtubeSearchQuery, youtubeSearchType, 50);

            const transformedResults = data.items?.map(item => ({
                id: item.id?.videoId || item.id?.playlistId || item.id?.channelId,
                kind: item.id?.kind,
                title: item.snippet?.title || 'Unknown Title',
                description: item.snippet?.description || 'No description available',
                channelTitle: item.snippet?.channelTitle || 'Unknown Channel',
                thumbnail: getYoutubeThumbnailUrl(item.snippet?.thumbnails),
                publishedAt: item.snippet?.publishedAt,
                publishTime: item.snippet?.publishTime
            })) || [];

            // For playlists, fetch detailed info to get video count
            const resultsWithDetails = await Promise.all(
                transformedResults.map(async (item) => {
                    if (item.kind === 'youtube#playlist') {
                        try {
                            const playlistDetails = await getYouTubePlaylistDetails(item.id);
                            return {
                                ...item,
                                videoCount: playlistDetails.contentDetails?.itemCount || 0
                            };
                        } catch (error) {
                            console.warn(`Failed to fetch details for playlist ${item.id}:`, error);
                            return item;
                        }
                    }
                    return item;
                })
            );

            setYoutubeSearchResults(resultsWithDetails);
            setYoutubeIsLoading(false);

        } catch (err) {
            console.error('YouTube search error:', err);
            setYoutubeError('Failed to search YouTube. Please try again.');
            setYoutubeIsLoading(false);
        }
    };

    const handleLoadMore = () => {
        setDisplayedCount(prev => prev + 10);
    };

    const displayedResults = youtubeSearchResults.slice(0, displayedCount);

    const handleYoutubeImportFromUrl = async () => {
        if (!youtubeUrl.trim()) {
            onSnackbar({ open: true, message: 'Please enter a YouTube URL', severity: 'error' });
            return;
        }

        setYoutubeIsLoading(true);
        setYoutubeError('');
        setYoutubeSuccess('');

        try {
            let result;
            let navigateTo;

            if (youtubeSearchType === 'video') {
                const videoIdMatch = youtubeUrl.match(/(?:v=|\/v\/|youtu\.be\/|\/embed\/|\/watch\?v=)([^&\n?#]+)/);
                if (!videoIdMatch) {
                    throw new Error('Could not extract video ID from URL. Please check the URL format.');
                }
                const videoId = videoIdMatch[1];
                result = await importYouTubeVideo(videoId);
                navigateTo = `/media/${result.id || result.Id}`;

            } else if (youtubeSearchType === 'playlist') {
                const playlistIdMatch = youtubeUrl.match(/[?&]list=([^&\n?#]+)/);
                if (!playlistIdMatch) {
                    throw new Error('Could not extract playlist ID from URL. Please check the URL format.');
                }
                const playlistId = playlistIdMatch[1];
                result = await importYouTubePlaylistEntity(playlistId);
                navigateTo = `/youtube-playlist/${result.id || result.Id}`;

            } else if (youtubeSearchType === 'channel') {
                let channelId = null;

                const channelIdMatch = youtubeUrl.match(/\/channel\/([^/\n?#]+)/);
                if (channelIdMatch) {
                    channelId = channelIdMatch[1];
                } else {
                    const handleMatch = youtubeUrl.match(/\/@([^/\n?#]+)/);
                    const customMatch = youtubeUrl.match(/\/c\/([^/\n?#]+)/);
                    const userMatch = youtubeUrl.match(/\/user\/([^/\n?#]+)/);

                    if (handleMatch || customMatch || userMatch) {
                        result = await importFromYouTubeUrl(youtubeUrl);
                        navigateTo = `/youtube-channel/${result.id || result.Id}`;
                    } else {
                        throw new Error('Could not extract channel ID from URL. Please check the URL format.');
                    }
                }

                if (channelId && !result) {
                    const exists = await checkYouTubeChannelExists(channelId);
                    if (exists) {
                        onSnackbar({ open: true, message: 'This channel has already been imported. Redirecting to channel page...', severity: 'info' });
                        setYoutubeIsLoading(false);
                        setTimeout(() => {
                            navigate(`/youtube-channel/${channelId}`);
                        }, 1500);
                        return;
                    }
                    result = await importYouTubeChannelEntity(channelId);
                    navigateTo = `/youtube-channel/${result.id || result.Id}`;
                }
            } else {
                throw new Error('Please select a content type');
            }

            const contentTypeLabel = youtubeSearchType.charAt(0).toUpperCase() + youtubeSearchType.slice(1);
            onSnackbar({ open: true, message: `YouTube ${contentTypeLabel} imported successfully!`, severity: 'success' });
            setYoutubeIsLoading(false);
            setYoutubeUrl('');

            console.log('YouTube import successful:', result);

            if (navigateTo) {
                setTimeout(() => {
                    navigate(navigateTo);
                }, 1500);
            }

        } catch (err) {
            console.error('YouTube URL import error:', err);
            onSnackbar({ open: true, message: `Failed to import: ${err.message || 'Please check the URL and try again.'}`, severity: 'error' });
            setYoutubeIsLoading(false);
        }
    };

    const handleImportYoutubeItem = async (item) => {
        setYoutubeIsLoading(true);
        setYoutubeError('');
        setYoutubeSuccess('');

        try {
            let result;

            if (item.kind === 'youtube#video') {
                result = await importYouTubeVideo(item.id);

                onSnackbar({ open: true, message: `"${item.title}" imported successfully!`, severity: 'success' });
                setYoutubeIsLoading(false);

                const mediaId = result.id || result.Id;
                if (mediaId) {
                    setTimeout(() => {
                        navigate(`/media/${mediaId}`);
                    }, 1500);
                }
            } else if (item.kind === 'youtube#playlist') {
                result = await importYouTubePlaylistEntity(item.id);

                onSnackbar({ open: true, message: `Playlist "${item.title}" imported successfully!`, severity: 'success' });
                setYoutubeIsLoading(false);

                if (result.id) {
                    setTimeout(() => {
                        navigate(`/youtube-playlist/${result.id}`);
                    }, 1500);
                }
            } else if (item.kind === 'youtube#channel') {
                const exists = await checkYouTubeChannelExists(item.id);
                if (exists) {
                    onSnackbar({ open: true, message: 'This channel has already been imported. Redirecting to channel page...', severity: 'info' });
                    setYoutubeIsLoading(false);
                    setTimeout(() => {
                        navigate(`/youtube-channel/${item.id}`);
                    }, 1500);
                    return;
                }

                result = await importYouTubeChannelEntity(item.id);

                onSnackbar({ open: true, message: `Channel "${item.title}" imported successfully!`, severity: 'success' });
                setYoutubeIsLoading(false);

                if (result.id) {
                    setTimeout(() => {
                        navigate(`/youtube-channel/${result.id}`);
                    }, 1500);
                }
            } else {
                throw new Error('Unknown YouTube content type');
            }

        } catch (err) {
            console.error('YouTube import error:', err);
            onSnackbar({ open: true, message: `Failed to import: ${err.message}`, severity: 'error' });
            setYoutubeIsLoading(false);
        }
    };

    const getYoutubeThumbnailUrl = (thumbnails) => {
        if (!thumbnails) return '/placeholder-video.png';

        return thumbnails.high?.url ||
            thumbnails.medium?.url ||
            thumbnails.default?.url ||
            '/placeholder-video.png';
    };

    const getYoutubeItemType = (item) => {
        if (item.kind === 'youtube#video') return 'Video';
        if (item.kind === 'youtube#playlist') return 'Playlist';
        if (item.kind === 'youtube#channel') return 'Channel';
        return 'Unknown';
    };

    const formatYoutubeDate = (dateString) => {
        if (!dateString) return '';
        return new Date(dateString).toLocaleDateString();
    };

    return (
        <Accordion
            expanded={expanded === 'youtube'}
            onChange={onAccordionChange('youtube')}
            sx={{ mb: 2 }}
        >
            <AccordionSummary
                expandIcon={<ExpandMore />}
                aria-controls="youtube-content"
                id="youtube-header"
            >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                    <VideoLibrary />
                    <Typography variant="h6">
                        YouTube Videos
                    </Typography>
                    <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                            Powered by
                        </Typography>
                        <Button
                            variant="text"
                            size="small"
                            href="https://www.youtube.com"
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
                            YouTube
                        </Button>
                    </Box>
                </Box>
            </AccordionSummary>
            <AccordionDetails>
                <Box sx={{ padding: 2 }}>
                    <FormControl fullWidth margin="normal">
                        <InputLabel>Import Method</InputLabel>
                        <Select
                            value={youtubeImportMethod}
                            label="Import Method"
                            onChange={(e) => setYoutubeImportMethod(e.target.value)}
                        >
                            <MenuItem value="search">Search and Select</MenuItem>
                            <MenuItem value="url">By YouTube URL</MenuItem>
                        </Select>
                    </FormControl>

                    {youtubeImportMethod === 'search' && (
                        <Box>
                            <FormControl fullWidth margin="normal">
                                <InputLabel>Content Type</InputLabel>
                                <Select
                                    value={youtubeSearchType}
                                    label="Content Type"
                                    onChange={(e) => setYoutubeSearchType(e.target.value)}
                                >
                                    <MenuItem value="video">Videos</MenuItem>
                                    <MenuItem value="playlist">Playlists</MenuItem>
                                    <MenuItem value="channel">Channels</MenuItem>
                                </Select>
                            </FormControl>

                            <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                                <TextField
                                    label="Search YouTube"
                                    value={youtubeSearchQuery}
                                    onChange={(e) => setYoutubeSearchQuery(e.target.value)}
                                    variant="outlined"
                                    fullWidth
                                    onKeyPress={(e) => e.key === 'Enter' && handleYoutubeSearch()}
                                    InputLabelProps={{
                                        sx: { color: 'white' }
                                    }}
                                />
                                <Button
                                    variant="contained"
                                    onClick={handleYoutubeSearch}
                                    disabled={youtubeIsLoading}
                                    startIcon={<Search />}
                                >
                                    Search
                                </Button>
                            </Box>

                            {youtubeError && (
                                <Alert severity="error" sx={{ mt: 2, mb: 2 }}>
                                    {youtubeError}
                                </Alert>
                            )}

                            {youtubeSuccess && (
                                <Alert severity="success" sx={{ mt: 2, mb: 2 }}>
                                    {youtubeSuccess}
                                </Alert>
                            )}

                            {hasSearched && !youtubeIsLoading && youtubeSearchResults.length === 0 && !youtubeError && (
                                <Alert severity="info" sx={{ mt: 2 }}>
                                    No results found. Try a different search term.
                                </Alert>
                            )}

                            {displayedResults.length > 0 && (
                                <Box sx={{ mt: 2 }}>
                                    <Typography variant="h6" gutterBottom>
                                        Search Results ({youtubeSearchResults.length})
                                    </Typography>
                                    {displayedResults.map((item) => (
                                        <Card key={`yt-${item.id}`} sx={{ mb: 2 }}>
                                            <CardContent>
                                                <Box sx={{ display: 'flex', gap: 2 }}>
                                                    <Box
                                                        sx={{
                                                            width: 120,
                                                            height: 90,
                                                            borderRadius: 1,
                                                            overflow: 'hidden',
                                                            flexShrink: 0,
                                                            backgroundColor: 'rgba(255, 255, 255, 0.1)'
                                                        }}
                                                    >
                                                        <SafeImage
                                                            src={item.thumbnail}
                                                            alt={item.title}
                                                            style={{
                                                                width: '100%',
                                                                height: '100%',
                                                                objectFit: 'cover',
                                                                display: 'block'
                                                            }}
                                                        />
                                                    </Box>
                                                    <Box sx={{ flex: 1 }}>
                                                        <Typography
                                                            variant="h6"
                                                            gutterBottom
                                                            sx={{
                                                                fontSize: '1.1rem',
                                                                fontWeight: 600,
                                                                lineHeight: 1.2
                                                            }}
                                                        >
                                                            {item.title}
                                                        </Typography>
                                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1, flexWrap: 'wrap' }}>
                                                            <Chip
                                                                label={getYoutubeItemType(item)}
                                                                size="small"
                                                                color="primary"
                                                            />
                                                            {item.videoCount !== undefined && (
                                                                <Chip
                                                                    label={`${item.videoCount} video${item.videoCount !== 1 ? 's' : ''}`}
                                                                    size="small"
                                                                    sx={{ backgroundColor: 'rgba(255, 255, 255, 0.1)' }}
                                                                />
                                                            )}
                                                            <Typography variant="body2" color="text.secondary">
                                                                by {item.channelTitle}
                                                            </Typography>
                                                            {item.publishedAt && (
                                                                <Typography variant="body2" color="text.secondary">
                                                                    - {formatYoutubeDate(item.publishedAt)}
                                                                </Typography>
                                                            )}
                                                        </Box>
                                                        <Typography
                                                            variant="body2"
                                                            sx={{
                                                                mb: 2,
                                                                fontSize: '0.875rem',
                                                                lineHeight: 1.4,
                                                                display: '-webkit-box',
                                                                WebkitLineClamp: 2,
                                                                WebkitBoxOrient: 'vertical',
                                                                overflow: 'hidden'
                                                            }}
                                                        >
                                                            {item.description ?
                                                                (item.description.length > 150
                                                                    ? `${item.description.substring(0, 150)}...`
                                                                    : item.description
                                                                ) : 'No description available.'}
                                                        </Typography>
                                                        <Box sx={{ display: 'flex', gap: 1 }}>
                                                            <WhiteOutlineButton
                                                                size="small"
                                                                href={
                                                                    item.kind === 'youtube#video'
                                                                        ? `https://www.youtube.com/watch?v=${item.id}`
                                                                        : item.kind === 'youtube#playlist'
                                                                            ? `https://www.youtube.com/playlist?list=${item.id}`
                                                                            : `https://www.youtube.com/channel/${item.id}`
                                                                }
                                                                target="_blank"
                                                                rel="noopener noreferrer"
                                                                endIcon={<OpenInNew fontSize="small" />}
                                                            >
                                                                View Details
                                                            </WhiteOutlineButton>
                                                            <Button
                                                                variant="contained"
                                                                size="small"
                                                                onClick={() => handleImportYoutubeItem(item)}
                                                                disabled={youtubeIsLoading}
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
                                    {displayedResults.length < youtubeSearchResults.length && (
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2 }}>
                                            <Typography variant="body2" color="text.secondary">
                                                Showing {displayedResults.length} of {youtubeSearchResults.length} results
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

                    {youtubeImportMethod === 'url' && (
                        <Box>
                            <FormControl fullWidth margin="normal">
                                <InputLabel>Content Type</InputLabel>
                                <Select
                                    value={youtubeSearchType}
                                    label="Content Type"
                                    onChange={(e) => setYoutubeSearchType(e.target.value)}
                                >
                                    <MenuItem value="video">Video</MenuItem>
                                    <MenuItem value="playlist">Playlist</MenuItem>
                                    <MenuItem value="channel">Channel</MenuItem>
                                </Select>
                            </FormControl>

                            <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                                <TextField
                                    label="YouTube URL"
                                    value={youtubeUrl}
                                    onChange={(e) => setYoutubeUrl(e.target.value)}
                                    variant="outlined"
                                    fullWidth
                                    InputLabelProps={{
                                        sx: { color: 'white' }
                                    }}
                                    placeholder={
                                        youtubeSearchType === 'video'
                                            ? "https://www.youtube.com/watch?v=..."
                                            : youtubeSearchType === 'playlist'
                                                ? "https://www.youtube.com/playlist?list=..."
                                                : "https://www.youtube.com/channel/... or https://www.youtube.com/@..."
                                    }
                                    onKeyPress={(e) => e.key === 'Enter' && handleYoutubeImportFromUrl()}
                                />
                                <Button
                                    variant="contained"
                                    onClick={handleYoutubeImportFromUrl}
                                    disabled={youtubeIsLoading}
                                    startIcon={<Download />}
                                >
                                    Import
                                </Button>
                            </Box>

                            {youtubeError && (
                                <Alert severity="error" sx={{ mt: 2, mb: 2 }}>
                                    {youtubeError}
                                </Alert>
                            )}

                            {youtubeSuccess && (
                                <Alert severity="success" sx={{ mt: 2, mb: 2 }}>
                                    {youtubeSuccess}
                                </Alert>
                            )}
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                                Select the content type and paste the corresponding YouTube URL. The import will use the selected type.
                            </Typography>
                        </Box>
                    )}

                    {youtubeIsLoading && (
                        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
                            <CircularProgress />
                        </Box>
                    )}

                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default YouTubeImportSection;
