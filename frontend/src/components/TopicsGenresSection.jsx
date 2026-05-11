import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Box, Typography, Chip, Button, Card, CardContent, Dialog,
    DialogTitle, DialogContent, DialogActions, TextField, InputAdornment,
    List, ListItem, ListItemText, ListItemIcon, IconButton, CircularProgress, Checkbox
} from '@mui/material';
import { useMediaQuery, useTheme } from '@mui/material';
import {
    Topic as TopicIcon, Category as GenreIcon, Add as AddIcon,
    Search, Close, Remove as RemoveIcon
} from '@mui/icons-material';
import { getAllTopics, getAllGenres, createTopic, createGenre } from '../api/topicGenreService';
import { updateMediaTopicsGenres } from '../api/mediaService';

function TopicsGenresSection({ mediaItem, setSnackbar, onUpdate }) {
    const navigate = useNavigate();

    // State for dialogs
    const [addTopicDialog, setAddTopicDialog] = useState(false);
    const [addGenreDialog, setAddGenreDialog] = useState(false);
    const [topicSearchQuery, setTopicSearchQuery] = useState('');
    const [genreSearchQuery, setGenreSearchQuery] = useState('');
    const [selectedTopicIds, setSelectedTopicIds] = useState([]);
    const [selectedGenreIds, setSelectedGenreIds] = useState([]);

    // State for inline create
    const [newTopicName, setNewTopicName] = useState('');
    const [newGenreName, setNewGenreName] = useState('');
    const [creating, setCreating] = useState(false);

    // State for available topics/genres
    const [availableTopics, setAvailableTopics] = useState([]);
    const [availableGenres, setAvailableGenres] = useState([]);
    const [loadingTopics, setLoadingTopics] = useState(false);
    const [loadingGenres, setLoadingGenres] = useState(false);
    const [saving, setSaving] = useState(false);

    // Extract current topics and genres from mediaItem
    const currentTopics = (mediaItem?.topics || mediaItem?.topicNames || []).map(t =>
        typeof t === 'string' ? t : (t?.name || t?.Name || '')
    ).filter(Boolean);

    const currentGenres = (mediaItem?.genres || mediaItem?.genreNames || []).map(g =>
        typeof g === 'string' ? g : (g?.name || g?.Name || '')
    ).filter(Boolean);

    // Fetch available topics when dialog opens
    const fetchTopics = useCallback(async () => {
        setLoadingTopics(true);
        try {
            const response = await getAllTopics();
            setAvailableTopics(response.data || []);
        } catch (error) {
            console.error('Error fetching topics:', error);
        } finally {
            setLoadingTopics(false);
        }
    }, []);

    // Fetch available genres when dialog opens
    const fetchGenres = useCallback(async () => {
        setLoadingGenres(true);
        try {
            const response = await getAllGenres();
            setAvailableGenres(response.data || []);
        } catch (error) {
            console.error('Error fetching genres:', error);
        } finally {
            setLoadingGenres(false);
        }
    }, []);

    // Open dialogs
    const handleOpenTopicDialog = () => {
        setAddTopicDialog(true);
        fetchTopics();
    };

    const handleOpenGenreDialog = () => {
        setAddGenreDialog(true);
        fetchGenres();
    };

    // Close dialogs
    const handleCloseTopicDialog = () => {
        setAddTopicDialog(false);
        setSelectedTopicIds([]);
        setTopicSearchQuery('');
    };

    const handleCloseGenreDialog = () => {
        setAddGenreDialog(false);
        setSelectedGenreIds([]);
        setGenreSearchQuery('');
    };

    // Toggle topic selection
    const handleToggleTopicId = (topicId) => {
        setSelectedTopicIds(prev =>
            prev.includes(topicId)
                ? prev.filter(id => id !== topicId)
                : [...prev, topicId]
        );
    };

    // Toggle genre selection
    const handleToggleGenreId = (genreId) => {
        setSelectedGenreIds(prev =>
            prev.includes(genreId)
                ? prev.filter(id => id !== genreId)
                : [...prev, genreId]
        );
    };

    // Add selected topics to media item
    const handleAddTopics = async () => {
        if (selectedTopicIds.length === 0) {
            setSnackbar?.({ open: true, message: 'Please select at least one topic', severity: 'warning' });
            return;
        }

        setSaving(true);
        try {
            const selectedNames = selectedTopicIds.map(id => {
                const topic = availableTopics.find(t => (t.id || t.Id) === id);
                return topic?.name || topic?.Name;
            }).filter(Boolean);
            const newTopics = [...currentTopics, ...selectedNames];

            await updateMediaTopicsGenres(mediaItem.id, newTopics, currentGenres);
            const count = selectedNames.length;
            setSnackbar?.({ open: true, message: `Added ${count} topic${count > 1 ? 's' : ''}`, severity: 'success' });
            handleCloseTopicDialog();
            onUpdate?.();
        } catch (error) {
            console.error('Error adding topics:', error);
            setSnackbar?.({ open: true, message: 'Failed to add topics', severity: 'error' });
        } finally {
            setSaving(false);
        }
    };

    // Add selected genres to media item
    const handleAddGenres = async () => {
        if (selectedGenreIds.length === 0) {
            setSnackbar?.({ open: true, message: 'Please select at least one genre', severity: 'warning' });
            return;
        }

        setSaving(true);
        try {
            const selectedNames = selectedGenreIds.map(id => {
                const genre = availableGenres.find(g => (g.id || g.Id) === id);
                return genre?.name || genre?.Name;
            }).filter(Boolean);
            const newGenres = [...currentGenres, ...selectedNames];

            await updateMediaTopicsGenres(mediaItem.id, currentTopics, newGenres);
            const count = selectedNames.length;
            setSnackbar?.({ open: true, message: `Added ${count} genre${count > 1 ? 's' : ''}`, severity: 'success' });
            handleCloseGenreDialog();
            onUpdate?.();
        } catch (error) {
            console.error('Error adding genres:', error);
            setSnackbar?.({ open: true, message: 'Failed to add genres', severity: 'error' });
        } finally {
            setSaving(false);
        }
    };

    // Remove topic from media item
    const handleRemoveTopic = async (topicToRemove) => {
        setSaving(true);
        try {
            const newTopics = currentTopics.filter(t => t !== topicToRemove);
            await updateMediaTopicsGenres(mediaItem.id, newTopics, currentGenres);
            setSnackbar?.({ open: true, message: `Removed topic "${topicToRemove}"`, severity: 'success' });
            onUpdate?.();
        } catch (error) {
            console.error('Error removing topic:', error);
            setSnackbar?.({ open: true, message: 'Failed to remove topic', severity: 'error' });
        } finally {
            setSaving(false);
        }
    };

    // Remove genre from media item
    const handleRemoveGenre = async (genreToRemove) => {
        setSaving(true);
        try {
            const newGenres = currentGenres.filter(g => g !== genreToRemove);
            await updateMediaTopicsGenres(mediaItem.id, currentTopics, newGenres);
            setSnackbar?.({ open: true, message: `Removed genre "${genreToRemove}"`, severity: 'success' });
            onUpdate?.();
        } catch (error) {
            console.error('Error removing genre:', error);
            setSnackbar?.({ open: true, message: 'Failed to remove genre', severity: 'error' });
        } finally {
            setSaving(false);
        }
    };

    // Create new topic and add to media
    const handleCreateAndAddTopic = async () => {
        const name = newTopicName.trim().toLowerCase();
        if (!name) return;

        setCreating(true);
        try {
            const response = await createTopic({ name });
            const createdTopic = response.data;
            const topicName = createdTopic?.name || createdTopic?.Name || name;
            const newTopics = [...currentTopics, topicName];

            await updateMediaTopicsGenres(mediaItem.id, newTopics, currentGenres);
            setSnackbar?.({ open: true, message: `Created and added topic "${topicName}"`, severity: 'success' });
            setNewTopicName('');
            handleCloseTopicDialog();
            onUpdate?.();
        } catch (error) {
            console.error('Error creating topic:', error);
            const msg = error.response?.data?.error || error.response?.data?.message || 'Failed to create topic';
            setSnackbar?.({ open: true, message: msg, severity: 'error' });
        } finally {
            setCreating(false);
        }
    };

    // Create new genre and add to media
    const handleCreateAndAddGenre = async () => {
        const name = newGenreName.trim().toLowerCase();
        if (!name) return;

        setCreating(true);
        try {
            const response = await createGenre({ name });
            const createdGenre = response.data;
            const genreName = createdGenre?.name || createdGenre?.Name || name;
            const newGenres = [...currentGenres, genreName];

            await updateMediaTopicsGenres(mediaItem.id, currentTopics, newGenres);
            setSnackbar?.({ open: true, message: `Created and added genre "${genreName}"`, severity: 'success' });
            setNewGenreName('');
            handleCloseGenreDialog();
            onUpdate?.();
        } catch (error) {
            console.error('Error creating genre:', error);
            const msg = error.response?.data?.error || error.response?.data?.message || 'Failed to create genre';
            setSnackbar?.({ open: true, message: msg, severity: 'error' });
        } finally {
            setCreating(false);
        }
    };

    // Navigate to search with filter
    const handleTopicClick = (topic) => {
        navigate(`/search?topics=${encodeURIComponent(topic)}`);
    };

    const handleGenreClick = (genre) => {
        navigate(`/search?genres=${encodeURIComponent(genre)}`);
    };

    // Filter available topics/genres (exclude already assigned ones)
    const filteredAvailableTopics = availableTopics
        .filter(topic => {
            const topicName = (topic.name || topic.Name || '').toLowerCase();
            return !currentTopics.some(ct => ct.toLowerCase() === topicName);
        })
        .filter(topic =>
            (topic.name || topic.Name || '').toLowerCase().includes(topicSearchQuery.toLowerCase())
        );

    const filteredAvailableGenres = availableGenres
        .filter(genre => {
            const genreName = (genre.name || genre.Name || '').toLowerCase();
            return !currentGenres.some(cg => cg.toLowerCase() === genreName);
        })
        .filter(genre =>
            (genre.name || genre.Name || '').toLowerCase().includes(genreSearchQuery.toLowerCase())
        );

    const muiTheme = useTheme();
    const isMobile = useMediaQuery(muiTheme.breakpoints.down('sm'));

    return (
        <Card sx={{ mt: 3, overflow: 'hidden', borderRadius: 2 }}>
            <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
                {/* Header with buttons */}
                <Box sx={{
                    display: 'flex',
                    flexDirection: { xs: 'column', sm: 'row' },
                    justifyContent: 'space-between',
                    alignItems: { xs: 'flex-start', sm: 'center' },
                    gap: { xs: 2, sm: 0 },
                    mb: 3
                }}>
                    <Typography
                        variant="h5"
                        sx={{
                            fontWeight: 'bold',
                            fontSize: { xs: '1.25rem', sm: '1.5rem' }
                        }}
                    >
                        Topics & Genres
                    </Typography>
                    <Box sx={{
                        display: 'flex',
                        flexDirection: { xs: 'column', sm: 'row' },
                        gap: 1,
                        width: { xs: '100%', sm: 'auto' }
                    }}>
                        <Button
                            variant="outlined"
                            size="small"
                            startIcon={<TopicIcon sx={{ color: 'white' }} />}
                            onClick={handleOpenTopicDialog}
                            fullWidth={isMobile}
                            disabled={saving}
                            sx={{
                                borderColor: 'white',
                                color: 'white',
                                '&:hover': {
                                    borderColor: 'white',
                                    backgroundColor: 'rgba(255,255,255,0.1)'
                                }
                            }}
                        >
                            Add Topic
                        </Button>
                        <Button
                            variant="outlined"
                            size="small"
                            startIcon={<GenreIcon sx={{ color: 'white' }} />}
                            onClick={handleOpenGenreDialog}
                            fullWidth={isMobile}
                            disabled={saving}
                            sx={{
                                borderColor: 'white',
                                color: 'white',
                                '&:hover': {
                                    borderColor: 'white',
                                    backgroundColor: 'rgba(255,255,255,0.1)'
                                }
                            }}
                        >
                            Add Genre
                        </Button>
                    </Box>
                </Box>

                {/* Topics Section */}
                <Box sx={{ mb: 3 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                        <TopicIcon sx={{ fontSize: 24, color: 'white' }} />
                        <Typography variant="h6" sx={{ fontWeight: 'bold', fontSize: '1.1rem' }}>
                            Topics
                        </Typography>
                    </Box>
                    {currentTopics.length > 0 ? (
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                            {currentTopics.map((topic, index) => (
                                <Chip
                                    key={`topic-${index}`}
                                    label={topic}
                                    onClick={() => handleTopicClick(topic)}
                                    onDelete={() => handleRemoveTopic(topic)}
                                    deleteIcon={<Close sx={{ fontSize: 18, color: 'white !important' }} />}
                                    disabled={saving}
                                    sx={{
                                        cursor: 'pointer',
                                        backgroundColor: 'primary.main',
                                        color: 'white',
                                        fontWeight: 'bold',
                                        fontSize: '0.95rem',
                                        height: '36px',
                                        '& .MuiChip-label': {
                                            px: 1.5
                                        },
                                        '&:hover': {
                                            backgroundColor: 'primary.dark'
                                        }
                                    }}
                                />
                            ))}
                        </Box>
                    ) : (
                        <Typography variant="body1" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                            No topics assigned. Click &quot;Add Topic&quot; to add one.
                        </Typography>
                    )}
                </Box>

                {/* Genres Section */}
                <Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                        <GenreIcon sx={{ fontSize: 24, color: 'white' }} />
                        <Typography variant="h6" sx={{ fontWeight: 'bold', fontSize: '1.1rem' }}>
                            Genres
                        </Typography>
                    </Box>
                    {currentGenres.length > 0 ? (
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                            {currentGenres.map((genre, index) => (
                                <Chip
                                    key={`genre-${index}`}
                                    label={genre}
                                    onClick={() => handleGenreClick(genre)}
                                    onDelete={() => handleRemoveGenre(genre)}
                                    deleteIcon={<Close sx={{ fontSize: 18, color: 'white !important' }} />}
                                    disabled={saving}
                                    sx={{
                                        cursor: 'pointer',
                                        backgroundColor: '#4b6aa2',
                                        color: 'white',
                                        fontWeight: 'bold',
                                        fontSize: '0.95rem',
                                        height: '36px',
                                        '& .MuiChip-label': {
                                            px: 1.5
                                        },
                                        '&:hover': {
                                            backgroundColor: '#3d5a8a'
                                        }
                                    }}
                                />
                            ))}
                        </Box>
                    ) : (
                        <Typography variant="body1" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                            No genres assigned. Click &quot;Add Genre&quot; to add one.
                        </Typography>
                    )}
                </Box>
            </CardContent>

            {/* Add Topic Dialog */}
            <Dialog
                open={addTopicDialog}
                onClose={handleCloseTopicDialog}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="h6">Add Topics</Typography>
                        <IconButton
                            onClick={handleCloseTopicDialog}
                            size="small"
                            sx={{
                                color: 'rgba(255, 255, 255, 0.7)',
                                '&:hover': {
                                    color: 'white',
                                    backgroundColor: 'rgba(255, 255, 255, 0.1)'
                                }
                            }}
                        >
                            <Close fontSize="small" />
                        </IconButton>
                    </Box>
                </DialogTitle>
                <DialogContent>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        Select topics to add to &quot;{mediaItem?.title}&quot;:
                    </Typography>

                    {/* Search Bar */}
                    <Box sx={{ mb: 2 }}>
                        <TextField
                            fullWidth
                            placeholder="Search topics..."
                            value={topicSearchQuery}
                            onChange={(e) => setTopicSearchQuery(e.target.value)}
                            variant="outlined"
                            size="small"
                            InputProps={{
                                startAdornment: (
                                    <InputAdornment position="start">
                                        <Search sx={{ color: 'rgba(255, 255, 255, 0.5)' }} />
                                    </InputAdornment>
                                ),
                            }}
                            sx={{
                                '& .MuiOutlinedInput-root': {
                                    color: 'white',
                                    '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
                                    '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.5)' },
                                    '&.Mui-focused fieldset': { borderColor: 'rgba(255, 255, 255, 0.7)' },
                                },
                                '& .MuiInputBase-input::placeholder': {
                                    color: 'rgba(255, 255, 255, 0.5)',
                                    opacity: 1,
                                },
                            }}
                        />
                    </Box>

                    {/* Topic List */}
                    {loadingTopics ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                            <CircularProgress size={30} />
                        </Box>
                    ) : (
                        <List sx={{ maxHeight: '300px', overflowY: 'auto' }}>
                            {filteredAvailableTopics.length > 0 ? (
                                filteredAvailableTopics.map((topic) => {
                                    const topicId = topic.id || topic.Id;
                                    const isSelected = selectedTopicIds.includes(topicId);
                                    return (
                                        <ListItem
                                            key={topicId}
                                            onClick={() => handleToggleTopicId(topicId)}
                                            sx={{
                                                borderRadius: 1,
                                                mb: 1,
                                                cursor: 'pointer',
                                                backgroundColor: isSelected
                                                    ? 'rgba(25, 118, 210, 0.3)'
                                                    : 'transparent',
                                                border: isSelected
                                                    ? '2px solid rgba(25, 118, 210, 0.8)'
                                                    : '1px solid rgba(255, 255, 255, 0.1)',
                                                '&:hover': {
                                                    backgroundColor: isSelected
                                                        ? 'rgba(25, 118, 210, 0.4)'
                                                        : 'rgba(255, 255, 255, 0.05)'
                                                }
                                            }}
                                        >
                                            <ListItemIcon sx={{ minWidth: 36 }}>
                                                <Checkbox
                                                    edge="start"
                                                    checked={isSelected}
                                                    tabIndex={-1}
                                                    disableRipple
                                                    sx={{ color: 'rgba(255, 255, 255, 0.5)', '&.Mui-checked': { color: '#1976d2' } }}
                                                />
                                            </ListItemIcon>
                                            <ListItemText primary={topic.name || topic.Name} />
                                        </ListItem>
                                    );
                                })
                            ) : (
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                                    {topicSearchQuery
                                        ? 'No topics match your search.'
                                        : 'No available topics to add. Create new topics from the Topics & Genres page.'}
                                </Typography>
                            )}
                        </List>
                    )}

                    {/* Inline Create */}
                    <Box sx={{ mt: 2, pt: 2, borderTop: '1px solid rgba(255,255,255,0.1)' }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                            Or create a new topic:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1 }}>
                            <TextField
                                fullWidth
                                placeholder="New topic name..."
                                value={newTopicName}
                                onChange={(e) => setNewTopicName(e.target.value)}
                                onKeyDown={(e) => e.key === 'Enter' && handleCreateAndAddTopic()}
                                variant="outlined"
                                size="small"
                                sx={{
                                    '& .MuiOutlinedInput-root': {
                                        color: 'white',
                                        '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
                                        '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.5)' },
                                        '&.Mui-focused fieldset': { borderColor: 'rgba(255, 255, 255, 0.7)' },
                                    },
                                    '& .MuiInputBase-input::placeholder': {
                                        color: 'rgba(255, 255, 255, 0.5)',
                                        opacity: 1,
                                    },
                                }}
                            />
                            <Button
                                variant="contained"
                                color="primary"
                                size="small"
                                onClick={handleCreateAndAddTopic}
                                disabled={!newTopicName.trim() || creating}
                                sx={{ color: '#fcfafa', whiteSpace: 'nowrap' }}
                            >
                                {creating ? 'Creating...' : 'Create & Add'}
                            </Button>
                        </Box>
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseTopicDialog} sx={{ color: 'white' }}>
                        Cancel
                    </Button>
                    <Button
                        onClick={handleAddTopics}
                        sx={{ color: 'white' }}
                        disabled={selectedTopicIds.length === 0 || saving}
                    >
                        {saving ? 'Adding...' : `Add${selectedTopicIds.length > 0 ? ` (${selectedTopicIds.length})` : ''}`}
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Add Genre Dialog */}
            <Dialog
                open={addGenreDialog}
                onClose={handleCloseGenreDialog}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="h6">Add Genres</Typography>
                        <IconButton
                            onClick={handleCloseGenreDialog}
                            size="small"
                            sx={{
                                color: 'rgba(255, 255, 255, 0.7)',
                                '&:hover': {
                                    color: 'white',
                                    backgroundColor: 'rgba(255, 255, 255, 0.1)'
                                }
                            }}
                        >
                            <Close fontSize="small" />
                        </IconButton>
                    </Box>
                </DialogTitle>
                <DialogContent>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        Select genres to add to &quot;{mediaItem?.title}&quot;:
                    </Typography>

                    {/* Search Bar */}
                    <Box sx={{ mb: 2 }}>
                        <TextField
                            fullWidth
                            placeholder="Search genres..."
                            value={genreSearchQuery}
                            onChange={(e) => setGenreSearchQuery(e.target.value)}
                            variant="outlined"
                            size="small"
                            InputProps={{
                                startAdornment: (
                                    <InputAdornment position="start">
                                        <Search sx={{ color: 'rgba(255, 255, 255, 0.5)' }} />
                                    </InputAdornment>
                                ),
                            }}
                            sx={{
                                '& .MuiOutlinedInput-root': {
                                    color: 'white',
                                    '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
                                    '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.5)' },
                                    '&.Mui-focused fieldset': { borderColor: 'rgba(255, 255, 255, 0.7)' },
                                },
                                '& .MuiInputBase-input::placeholder': {
                                    color: 'rgba(255, 255, 255, 0.5)',
                                    opacity: 1,
                                },
                            }}
                        />
                    </Box>

                    {/* Genre List */}
                    {loadingGenres ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                            <CircularProgress size={30} />
                        </Box>
                    ) : (
                        <List sx={{ maxHeight: '300px', overflowY: 'auto' }}>
                            {filteredAvailableGenres.length > 0 ? (
                                filteredAvailableGenres.map((genre) => {
                                    const genreId = genre.id || genre.Id;
                                    const isSelected = selectedGenreIds.includes(genreId);
                                    return (
                                        <ListItem
                                            key={genreId}
                                            onClick={() => handleToggleGenreId(genreId)}
                                            sx={{
                                                borderRadius: 1,
                                                mb: 1,
                                                cursor: 'pointer',
                                                backgroundColor: isSelected
                                                    ? 'rgba(75, 106, 162, 0.3)'
                                                    : 'transparent',
                                                border: isSelected
                                                    ? '2px solid rgba(75, 106, 162, 0.8)'
                                                    : '1px solid rgba(255, 255, 255, 0.1)',
                                                '&:hover': {
                                                    backgroundColor: isSelected
                                                        ? 'rgba(75, 106, 162, 0.4)'
                                                        : 'rgba(255, 255, 255, 0.05)'
                                                }
                                            }}
                                        >
                                            <ListItemIcon sx={{ minWidth: 36 }}>
                                                <Checkbox
                                                    edge="start"
                                                    checked={isSelected}
                                                    tabIndex={-1}
                                                    disableRipple
                                                    sx={{ color: 'rgba(255, 255, 255, 0.5)', '&.Mui-checked': { color: '#4b6aa2' } }}
                                                />
                                            </ListItemIcon>
                                            <ListItemText primary={genre.name || genre.Name} />
                                        </ListItem>
                                    );
                                })
                            ) : (
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                                    {genreSearchQuery
                                        ? 'No genres match your search.'
                                        : 'No available genres to add. Create new genres from the Topics & Genres page.'}
                                </Typography>
                            )}
                        </List>
                    )}

                    {/* Inline Create */}
                    <Box sx={{ mt: 2, pt: 2, borderTop: '1px solid rgba(255,255,255,0.1)' }}>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                            Or create a new genre:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1 }}>
                            <TextField
                                fullWidth
                                placeholder="New genre name..."
                                value={newGenreName}
                                onChange={(e) => setNewGenreName(e.target.value)}
                                onKeyDown={(e) => e.key === 'Enter' && handleCreateAndAddGenre()}
                                variant="outlined"
                                size="small"
                                sx={{
                                    '& .MuiOutlinedInput-root': {
                                        color: 'white',
                                        '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
                                        '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.5)' },
                                        '&.Mui-focused fieldset': { borderColor: 'rgba(255, 255, 255, 0.7)' },
                                    },
                                    '& .MuiInputBase-input::placeholder': {
                                        color: 'rgba(255, 255, 255, 0.5)',
                                        opacity: 1,
                                    },
                                }}
                            />
                            <Button
                                variant="contained"
                                color="primary"
                                size="small"
                                onClick={handleCreateAndAddGenre}
                                disabled={!newGenreName.trim() || creating}
                                sx={{ color: '#fcfafa', whiteSpace: 'nowrap' }}
                            >
                                {creating ? 'Creating...' : 'Create & Add'}
                            </Button>
                        </Box>
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseGenreDialog} sx={{ color: 'white' }}>
                        Cancel
                    </Button>
                    <Button
                        onClick={handleAddGenres}
                        sx={{ color: 'white' }}
                        disabled={selectedGenreIds.length === 0 || saving}
                    >
                        {saving ? 'Adding...' : `Add${selectedGenreIds.length > 0 ? ` (${selectedGenreIds.length})` : ''}`}
                    </Button>
                </DialogActions>
            </Dialog>
        </Card>
    );
}

export default React.memo(TopicsGenresSection);
