import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    Box, Typography, Button, Card, CardContent, CardMedia,
    Chip, IconButton, Collapse, Divider, Paper, Link,
    Dialog, DialogTitle, DialogContent, DialogActions,
    TextField, List, ListItem, ListItemText, Snackbar, Alert, InputAdornment,
    Checkbox, ListItemIcon
} from '@mui/material';
import {
    ArrowBack, ExpandMore, ExpandLess, OpenInNew, Edit,
    Search, Upload, FileDownload, AddCircle, Add, Delete, Sync
} from '@mui/icons-material';
import CircularProgress from '@mui/material/CircularProgress';
import { getMixlistById, removeMediaFromMixlist, addMediaToMixlist } from '../api/mixlistService';
import { searchMedia } from '../api/mediaService';
import { reindexMixlist } from '../api/typesenseService';
import SimpleMediaCarousel from './shared/SimpleMediaCarousel';
import MixlistRelatedNotesSection from './MixlistRelatedNotesSection';
import { formatMediaType, formatStatus } from '../utils/formatters';

function MixlistProfilePage() {
    const { id } = useParams();
    const navigate = useNavigate();
    const [mixlist, setMixlist] = useState(null);
    const [loading, setLoading] = useState(true);
    const [mediaListExpanded, setMediaListExpanded] = useState(false);
    const [selectedMedia, setSelectedMedia] = useState(null);
    const [addMediaDialogOpen, setAddMediaDialogOpen] = useState(false);
    const [searchResults, setSearchResults] = useState([]);
    const [searchQuery, setSearchQuery] = useState('');
    const [searching, setSearching] = useState(false);
    const [selectedMediaIds, setSelectedMediaIds] = useState([]);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [filterQuery, setFilterQuery] = useState('');
    const [reindexing, setReindexing] = useState(false);

    const handleReindex = async () => {
        setReindexing(true);
        try {
            await reindexMixlist(id);
            setSnackbar({ open: true, message: 'Mixlist re-indexed in search.', severity: 'success' });
        } catch (error) {
            if (error.response?.status !== 403) {
                console.error('Error re-indexing mixlist:', error);
                setSnackbar({ open: true, message: 'Failed to re-index mixlist.', severity: 'error' });
            }
        } finally {
            setReindexing(false);
        }
    };

    useEffect(() => {
        const loadMixlist = async () => {
            try {
                const mixlistResponse = await getMixlistById(id);
                setMixlist(mixlistResponse.data);
            } catch (error) {
                console.error('Error loading mixlist:', error);
                setSnackbar({ open: true, message: 'Failed to load mixlist', severity: 'error' });
            } finally {
                setLoading(false);
            }
        };
        
        if (id) {
            loadMixlist();
        } else {
            setLoading(false);
        }
    }, [id]);

    // Media search functionality
    const handleSearchMedia = async (query) => {
        if (!query.trim()) {
            setSearchResults([]);
            return;
        }

        setSearching(true);
        try {
            const response = await searchMedia(query);
            console.log('Search response:', response);
            // Handle both response.data and direct response array
            const results = Array.isArray(response.data) ? response.data : 
                           Array.isArray(response) ? response : [];
            console.log('Processed results:', results);
            setSearchResults(results);
        } catch (error) {
            console.error('Error searching media:', error);
            console.error('Error details:', error.response?.data);
            setSnackbar({ 
                open: true, 
                message: `Failed to search media: ${error.response?.data?.error || error.message}`, 
                severity: 'error' 
            });
        } finally {
            setSearching(false);
        }
    };

    const handleToggleMediaSelection = (mediaItemId) => {
        setSelectedMediaIds(prev => {
            if (prev.includes(mediaItemId)) {
                return prev.filter(id => id !== mediaItemId);
            } else {
                return [...prev, mediaItemId];
            }
        });
    };

    const handleAddSelectedMedia = async () => {
        if (selectedMediaIds.length === 0) {
            setSnackbar({ open: true, message: 'Please select at least one media item', severity: 'warning' });
            return;
        }

        try {
            console.log('=== Starting bulk add media ===');
            console.log('Mixlist ID:', id);
            console.log('Selected media IDs:', selectedMediaIds);
            console.log('Number of items to add:', selectedMediaIds.length);
            
            // Add each selected media item sequentially to better track errors
            let successCount = 0;
            let failedItems = [];
            
            for (const mediaItemId of selectedMediaIds) {
                try {
                    console.log(`Adding media item ${mediaItemId} to mixlist ${id}...`);
                    const result = await addMediaToMixlist(id, mediaItemId);
                    console.log(`Successfully added media item ${mediaItemId}:`, result);
                    successCount++;
                } catch (itemError) {
                    console.error(`Failed to add media item ${mediaItemId}:`, itemError);
                    console.error('Error response:', itemError.response?.data);
                    console.error('Error status:', itemError.response?.status);
                    failedItems.push({ id: mediaItemId, error: itemError.response?.data?.error || itemError.message });
                }
            }
            
            console.log(`=== Bulk add complete: ${successCount} succeeded, ${failedItems.length} failed ===`);
            
            // Reload the mixlist to update the display
            console.log('Reloading mixlist...');
            const response = await getMixlistById(id);
            console.log('Reloaded mixlist:', response);
            setMixlist(response.data);
            
            // Close dialog and clear search/selection
            setAddMediaDialogOpen(false);
            setSearchQuery('');
            setSearchResults([]);
            setSelectedMediaIds([]);
            
            if (failedItems.length === 0) {
                const itemText = successCount === 1 ? 'item' : 'items';
                setSnackbar({ 
                    open: true, 
                    message: `Successfully added ${successCount} media ${itemText}!`, 
                    severity: 'success' 
                });
            } else {
                setSnackbar({ 
                    open: true, 
                    message: `Added ${successCount} items, but ${failedItems.length} failed. Check console for details.`, 
                    severity: 'warning' 
                });
            }
        } catch (error) {
            console.error('=== Unexpected error in bulk add ===');
            console.error('Error:', error);
            console.error('Error details:', error.response?.data);
            setSnackbar({ 
                open: true, 
                message: `Failed to add media items: ${error.response?.data?.error || error.message}`, 
                severity: 'error' 
            });
        }
    };

    const handleRemoveMedia = async (mediaItemId) => {
        try {
            await removeMediaFromMixlist(id, mediaItemId);
            
            // Reload the mixlist to update the display
            const response = await getMixlistById(id);
            setMixlist(response.data);
            
            // Clear selected media if it was removed
            if (selectedMedia && selectedMedia.id === mediaItemId) {
                setSelectedMedia(null);
            }
            
            setSnackbar({ open: true, message: 'Media removed successfully!', severity: 'success' });
        } catch (error) {
            console.error('Error removing media from mixlist:', error);
            setSnackbar({ open: true, message: 'Failed to remove media', severity: 'error' });
        }
    };

    const handleMediaCarouselClick = (media) => {
        // The media parameter now contains the full media item data directly
        setSelectedMedia(media);
    };

    const getMediaTypeColor = (mediaType) => {
        const colors = {
            'Podcast': '#9C27B0', 'Book': '#2196F3', 'Movie': '#FF5722',
            'Article': '#4CAF50', 'Video': '#FF9800', 'Music': '#E91E63',
            'VideoGame': '#673AB7', 'TVShow': '#795548', 'Website': '#607D8B',
            'Document': '#3F51B5', 'Other': '#9E9E9E'
        };
        return colors[mediaType] || colors['Other'];
    };

    const getStatusColor = (status) => {
        const colors = {
            'Completed': '#4CAF50', 'ActivelyExploring': '#FF9800',
            'Uncharted': '#9E9E9E', 'Abandoned': '#F44336'
        };
        return colors[status] || colors['Uncharted'];
    };

    if (loading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '50vh' }}>
                <Typography variant="h6">Loading mixlist...</Typography>
            </Box>
        );
    }

    if (!mixlist) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '50vh' }}>
                <Typography variant="h6">Mixlist not found</Typography>
            </Box>
        );
    }

    const currentMediaItems = mixlist.MediaItems || mixlist.mediaItems || [];
    const hasMediaItems = currentMediaItems.length > 0;
    const filteredMediaItems = filterQuery.trim()
        ? currentMediaItems.filter(item =>
            (item.title || item.Title || '').toLowerCase().includes(filterQuery.toLowerCase())
          )
        : currentMediaItems;

    return (
        <Box sx={{ minHeight: '100vh' }}>
            {/* Header */}
            <Box sx={{ p: 3, pb: 0 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 4 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                        <IconButton onClick={() => navigate('/mixlists')} sx={{ mr: 2 }}>
                            <ArrowBack />
                        </IconButton>
                        <Typography variant="h3" component="h1" sx={{ 
                            fontSize: '32px', fontWeight: 'bold'
                        }}>
                            {mixlist.Name || mixlist.name}
                        </Typography>
                    </Box>

                    {/* Action Buttons */}
                    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                        <Button
                            onClick={handleReindex}
                            startIcon={reindexing ? <CircularProgress size={16} /> : <Sync />}
                            variant="outlined"
                            size="medium"
                            disabled={reindexing}
                        >
                            {reindexing ? 'Reindexing...' : 'Reindex'}
                        </Button>
                        <Button
                            onClick={() => navigate(`/mixlist/${id}/edit`)}
                            startIcon={<Edit />}
                            variant="contained"
                            color="primary"
                            size="large"
                        >
                            Edit Mixlist
                        </Button>
                    </Box>
                </Box>

                                {/* Action Buttons */}
                {/* <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
                    <Button
                        onClick={() => window.open(`/api/mixlist/${id}/export`, '_blank')}
                        startIcon={<FileDownload />}
                        variant="contained"
                        color="primary"
                        size="large"
                    >
                        Export Mixlist
                    </Button>
                </Box> */}
            </Box>

            {/* Main Content Container */}
            <Box sx={{ 
                display: 'flex', 
                justifyContent: 'center', 
                alignItems: 'flex-start',
                px: 2
            }}>
                <Box sx={{ 
                    width: '100%',
                    maxWidth: '700px' // Slightly larger than AddMediaForm's 600px
                }}>
                    {/* Mixlist Information Card */}
                    <Card sx={{ mb: 4, overflow: 'hidden' }}>
                        {/* Thumbnail */}
                        {(mixlist.Thumbnail || mixlist.thumbnail) && (
                            <CardMedia
                                component="img"
                                sx={{ width: '100%', height: 250, objectFit: 'cover' }}
                                image={mixlist.Thumbnail || mixlist.thumbnail}
                                alt={mixlist.Name || mixlist.name}
                            />
                        )}
                        
                        <CardContent sx={{ p: 3 }}>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                                <Typography variant="h4" component="h2" sx={{ fontWeight: 'bold' }}>
                                    {mixlist.Name || mixlist.name}
                                </Typography>
                                <Chip 
                                    label={`${currentMediaItems.length} items`}
                                    sx={{ 
                                        backgroundColor: 'primary.main', 
                                        color: 'white',
                                        fontSize: '1rem',
                                        fontWeight: 'bold'
                                    }}
                                />
                            </Box>

                            {/* Description */}
                            {(mixlist.Description || mixlist.description) && (
                                <Typography variant="body1" sx={{ mb: 3, lineHeight: 1.6 }}>
                                    {mixlist.Description || mixlist.description}
                                </Typography>
                            )}

                            {/* Date Created */}
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                                Created on: {new Date(mixlist.DateCreated || mixlist.dateCreated).toLocaleDateString()}
                            </Typography>

                            {/* Topics */}
                            {(mixlist.Topics || mixlist.topics || []).length > 0 && (
                                <Box sx={{ mb: 2 }}>
                                    <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 1 }}>
                                        Topics
                                    </Typography>
                                    <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                                        {(mixlist.Topics || mixlist.topics).map((topic, idx) => (
                                            <Chip
                                                key={idx}
                                                label={topic}
                                                size="small"
                                                sx={{
                                                    backgroundColor: 'primary.main',
                                                    color: 'white',
                                                    fontSize: '0.75rem'
                                                }}
                                            />
                                        ))}
                                    </Box>
                                </Box>
                            )}

                            {/* Genres */}
                            {(mixlist.Genres || mixlist.genres || []).length > 0 && (
                                <Box sx={{ mb: 3 }}>
                                    <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 1 }}>
                                        Genres
                                    </Typography>
                                    <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                                        {(mixlist.Genres || mixlist.genres).map((genre, idx) => (
                                            <Chip
                                                key={idx}
                                                label={genre}
                                                size="small"
                                                sx={{
                                                    backgroundColor: '#4b6aa2',
                                                    color: 'white',
                                                    fontSize: '0.75rem'
                                                }}
                                            />
                                        ))}
                                    </Box>
                                </Box>
                            )}

                            {/* Search Filter */}
                            {hasMediaItems && (
                                <TextField
                                    fullWidth
                                    placeholder="Search media in this mixlist..."
                                    value={filterQuery}
                                    onChange={(e) => setFilterQuery(e.target.value)}
                                    variant="outlined"
                                    size="small"
                                    InputProps={{
                                        startAdornment: (
                                            <InputAdornment position="start">
                                                <Search sx={{ color: 'rgba(255, 255, 255, 0.5)' }} />
                                            </InputAdornment>
                                        ),
                                    }}
                                    sx={{ mb: 3 }}
                                />
                            )}

                            {/* Collapsible Media List */}
                            <Box>
                                <Button
                                    onClick={() => setMediaListExpanded(!mediaListExpanded)}
                                    startIcon={mediaListExpanded ? <ExpandLess /> : <ExpandMore />}
                                    variant="contained"
                                    color="primary"
                                    sx={{ 
                                        fontSize: '1.1rem',
                                        fontWeight: 'bold',
                                        textTransform: 'none',
                                        mb: 2
                                    }}
                                >
                                    Media Items ({filterQuery ? `${filteredMediaItems.length} of ${currentMediaItems.length}` : currentMediaItems.length})
                                </Button>

                                <Collapse in={mediaListExpanded}>
                                    <Box sx={{ pl: 2 }}>
                                        {filteredMediaItems.length > 0 ? (
                                            filteredMediaItems.map((mediaItem, index) => (
                                                <Paper key={mediaItem.id || mediaItem.Id} sx={{ p: 2, mb: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                                                        <Typography variant="body2" color="text.secondary">
                                                            {index + 1}.
                                                        </Typography>
                                                        <Link
                                                            component="button"
                                                            variant="body1"
                                                            onClick={() => {
                                                                const itemType = mediaItem.mediaType || mediaItem.MediaType;
                                                                const itemId = mediaItem.id || mediaItem.Id;
                                                                if (itemType === 'Podcast' && !mediaItem.seriesId && !mediaItem.SeriesId) {
                                                                    navigate(`/podcast-series/${itemId}`);
                                                                } else if (itemType === 'Channel') {
                                                                    navigate(`/youtube-channel/${itemId}`);
                                                                } else {
                                                                    navigate(`/media/${itemId}`);
                                                                }
                                                            }}
                                                            sx={{ 
                                                                textDecoration: 'none',
                                                                fontWeight: 'medium',
                                                                color: 'white',
                                                                '&:hover': { textDecoration: 'underline' }
                                                            }}
                                                        >
                                                            {mediaItem.title || mediaItem.Title}
                                                        </Link>
                                                        <Chip
                                                            label={formatMediaType(mediaItem.mediaType || mediaItem.MediaType)}
                                                            size="small"
                                                            sx={{
                                                                backgroundColor: getMediaTypeColor(mediaItem.mediaType || mediaItem.MediaType),
                                                                color: 'white',
                                                                fontSize: '0.75rem'
                                                            }}
                                                        />
                                                    </Box>
                                                    <IconButton
                                                        onClick={() => handleRemoveMedia(mediaItem.id || mediaItem.Id)}
                                                        size="small"
                                                        color="error"
                                                    >
                                                        <Delete />
                                                    </IconButton>
                                                </Paper>
                                            ))
                                        ) : (
                                            <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
                                                {filterQuery ? `No media items matching "${filterQuery}".` : 'No media items in this mixlist yet.'}
                                            </Typography>
                                        )}
                                    </Box>
                                </Collapse>
                            </Box>
                        </CardContent>
                    </Card>

                    {/* Simple Media Carousel */}
                    {filteredMediaItems.length > 0 && (
                        <Box sx={{ mb: 4 }}>
                            <SimpleMediaCarousel
                                mediaItems={filteredMediaItems}
                                title="Browse Media"
                                subtitle="Click on any item to view details below"
                                onMediaClick={handleMediaCarouselClick}
                                cardWidth={250}
                                cardHeight={350}
                                showCardContent={false}
                            />
                        </Box>
                    )}

                    {/* Selected Media Profile */}
                    {selectedMedia && (
                        <Box sx={{ mb: 4 }}>
                            <Box sx={{ 
                                display: 'flex', 
                                justifyContent: 'center', 
                                alignItems: 'flex-start',
                                px: 2
                            }}>
                                <Box sx={{ 
                                    width: '100%',
                                    maxWidth: '300px' // Made slimmer for better proportions
                                }}>
                                    <Card sx={{ overflow: 'hidden' }}>
                                        {(selectedMedia.thumbnail || selectedMedia.Thumbnail) && (
                                            <CardMedia
                                                component="img"
                                                sx={{ width: '100%', height: 225, objectFit: 'cover' }} // 300 * 0.75 = 225 for 4:3 ratio
                                                image={selectedMedia.thumbnail || selectedMedia.Thumbnail}
                                                alt={selectedMedia.title || selectedMedia.Title}
                                            />
                                        )}
                                        <CardContent sx={{ p: 2.5 }}>
                                            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, mb: 2 }}>
                                                <Typography variant="h5" component="h3" sx={{ fontWeight: 'bold', fontSize: '1.2rem' }}>
                                                    {selectedMedia.title || selectedMedia.Title}
                                                </Typography>
                                                <Button
                                                    variant="contained"
                                                    color="primary"
                                                    size="small"
                                                    startIcon={<OpenInNew />}
                                                    onClick={() => {
                                                        const itemType = selectedMedia.mediaType || selectedMedia.MediaType;
                                                        const itemId = selectedMedia.id || selectedMedia.Id;
                                                        if (itemType === 'Podcast' && !selectedMedia.seriesId && !selectedMedia.SeriesId) {
                                                            navigate(`/podcast-series/${itemId}`);
                                                        } else if (itemType === 'Channel') {
                                                            navigate(`/youtube-channel/${itemId}`);
                                                        } else {
                                                            navigate(`/media/${itemId}`);
                                                        }
                                                    }}
                                                    sx={{ alignSelf: 'flex-start' }}
                                                >
                                                    View Full Profile
                                                </Button>
                                            </Box>
                                            
                                            {/* Description beneath title */}
                                            <Typography variant="body2" sx={{
                                                mb: 2,
                                                lineHeight: 1.4,
                                                color: 'text.secondary',
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                display: '-webkit-box',
                                                WebkitLineClamp: 4,
                                                WebkitBoxOrient: 'vertical'
                                            }}>
                                                {selectedMedia.description || selectedMedia.Description || selectedMedia.desc || selectedMedia.Desc || 'No description available'}
                                            </Typography>
                                            
                                            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 2 }}>
                                                <Chip
                                                    label={formatMediaType(selectedMedia.mediaType || selectedMedia.MediaType)}
                                                    size="small"
                                                    sx={{
                                                        backgroundColor: getMediaTypeColor(selectedMedia.mediaType || selectedMedia.MediaType),
                                                        color: 'white',
                                                        fontWeight: 'bold',
                                                        fontSize: '0.7rem'
                                                    }}
                                                />
                                                {(selectedMedia.status || selectedMedia.Status) && (
                                                    <Chip
                                                        label={formatStatus(selectedMedia.status || selectedMedia.Status)}
                                                        size="small"
                                                        sx={{
                                                            backgroundColor: getStatusColor(selectedMedia.status || selectedMedia.Status),
                                                            color: 'white',
                                                            fontWeight: 'bold',
                                                            fontSize: '0.7rem'
                                                        }}
                                                    />
                                                )}
                                            </Box>

                                            {selectedMedia.notes && (
                                                <Typography variant="body2" color="text.secondary" sx={{ lineHeight: 1.4, fontSize: '0.8rem' }}>
                                                    <strong>Notes:</strong> {selectedMedia.notes}
                                                </Typography>
                                            )}

                                            {selectedMedia.link && (
                                                <Box sx={{ mt: 1.5 }}>
                                                    <Link 
                                                        href={selectedMedia.link.startsWith('http') ? selectedMedia.link : `https://${selectedMedia.link}`}
                                                        target="_blank" 
                                                        rel="noopener"
                                                        sx={{ 
                                                            display: 'inline-flex', 
                                                            alignItems: 'center', 
                                                            gap: 0.5,
                                                            color: 'primary.main',
                                                            textDecoration: 'none',
                                                            fontSize: '0.8rem',
                                                            '&:hover': { textDecoration: 'underline' }
                                                        }}
                                                    >
                                                        View Source <OpenInNew fontSize="small" />
                                                    </Link>
                                                </Box>
                                            )}
                                        </CardContent>
                                    </Card>
                                </Box>
                            </Box>
                        </Box>
                    )}

                    {/* Related Notes Section */}
                    <MixlistRelatedNotesSection
                        mixlistId={mixlist?.id}
                        mixlistName={mixlist?.name}
                        setSnackbar={setSnackbar}
                    />

                    {/* Action Buttons Section */}
                    <Box sx={{ textAlign: 'center', mt: 4, pb: 4 }}>
                        <Typography variant="h5" sx={{ mb: 3, fontWeight: 'bold' }}>
                            Add Content to This Mixlist
                        </Typography>
                        <Box sx={{ display: 'flex', justifyContent: 'center', gap: 2, flexWrap: 'wrap' }}>
                            <Button 
                                variant="contained" 
                                size="large"
                                startIcon={<AddCircle />}
                                onClick={() => setAddMediaDialogOpen(true)}
                                sx={{ fontSize: '16px', px: 4, py: 1.5 }}
                            >
                                Add Existing Media
                            </Button>
                            <Button 
                                variant="contained" 
                                size="large"
                                startIcon={<Upload />}
                                onClick={() => navigate('/upload-media')}
                                sx={{ fontSize: '16px', px: 4, py: 1.5 }}
                            >
                                Upload Media
                            </Button>
                            <Button 
                                variant="contained" 
                                size="large"
                                startIcon={<FileDownload />}
                                onClick={() => navigate('/import-media')}
                                sx={{ fontSize: '16px', px: 4, py: 1.5 }}
                            >
                                Import via API
                            </Button>
                        </Box>
                    </Box>
                </Box>
            </Box>

            {/* Add Media Dialog */}
            <Dialog open={addMediaDialogOpen} onClose={() => setAddMediaDialogOpen(false)} maxWidth="md" fullWidth>
                <DialogTitle>Add Media to Mixlist</DialogTitle>
                <DialogContent>
                    <TextField
                        fullWidth
                        label="Search media by name"
                        variant="outlined"
                        value={searchQuery}
                        onChange={(e) => {
                            setSearchQuery(e.target.value);
                            handleSearchMedia(e.target.value);
                        }}
                        sx={{ mb: 2, mt: 1 }}
                        placeholder="Type to search for media..."
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <Search />
                                </InputAdornment>
                            )
                        }}
                    />

                    {searching && (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
                            <Typography variant="body2">Searching...</Typography>
                        </Box>
                    )}

                    {!searching && searchQuery && searchResults.length === 0 && (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
                            <Typography variant="body2" color="text.secondary">
                                No media found matching &quot;{searchQuery}&quot;
                            </Typography>
                        </Box>
                    )}

                    {!searching && searchResults.length > 0 && (
                        <List sx={{ maxHeight: 400, overflow: 'auto' }}>
                            {searchResults
                                .filter(media => !currentMediaItems.some(current => 
                                    (current.id || current.Id) === (media.id || media.Id)
                                ))
                                .map((media) => {
                                    const mediaId = media.id || media.Id;
                                    const isSelected = selectedMediaIds.includes(mediaId);
                                    
                                    return (
                                        <ListItem 
                                            key={mediaId} 
                                            button
                                            onClick={() => handleToggleMediaSelection(mediaId)}
                                            sx={{ 
                                                border: 1, 
                                                borderColor: isSelected ? 'primary.main' : 'divider', 
                                                mb: 1, 
                                                borderRadius: 1,
                                                backgroundColor: isSelected ? 'action.selected' : 'transparent'
                                            }}
                                        >
                                            <ListItemIcon>
                                                <Checkbox
                                                    edge="start"
                                                    checked={isSelected}
                                                    tabIndex={-1}
                                                    disableRipple
                                                />
                                            </ListItemIcon>
                                            <ListItemText 
                                                primary={media.title || media.Title}
                                                secondary={
                                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.5 }}>
                                                        <Chip
                                                            label={formatMediaType(media.mediaType || media.MediaType)}
                                                            size="small"
                                                            sx={{
                                                                backgroundColor: getMediaTypeColor(media.mediaType || media.MediaType),
                                                                color: 'white',
                                                                fontSize: '0.7rem'
                                                            }}
                                                        />
                                                        {media.description && (
                                                            <Typography variant="body2" color="text.secondary">
                                                                {media.description.length > 100 
                                                                    ? `${media.description.substring(0, 100)}...` 
                                                                    : media.description
                                                                }
                                                            </Typography>
                                                        )}
                                                    </Box>
                                                }
                                            />
                                        </ListItem>
                                    );
                                })}
                        </List>
                    )}

                    {!searchQuery && (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                            <Typography variant="body2" color="text.secondary">
                                Start typing to search for media to add
                            </Typography>
                        </Box>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button 
                        variant="outlined"
                        onClick={() => {
                            setAddMediaDialogOpen(false);
                            setSelectedMediaIds([]);
                        }}
                    >
                        Cancel
                    </Button>
                    {selectedMediaIds.length > 0 && (
                        <Button 
                            variant="contained" 
                            color="primary"
                            startIcon={<Add />}
                            onClick={handleAddSelectedMedia}
                        >
                            Add Selected ({selectedMediaIds.length})
                        </Button>
                    )}
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

export default MixlistProfilePage;
