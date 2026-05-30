import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    Container, Typography, TextField, Button, Box,
    Card, CardContent, Snackbar, Alert, CircularProgress,
    Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions,
    Chip, Autocomplete
} from '@mui/material';
import { Save, Cancel, ArrowBack, Delete } from '@mui/icons-material';
import { useMixlist, useUpdateMixlist, useDeleteMixlist } from '@/hooks/useMixlist';
import { useUploadThumbnail } from '@/hooks/useUpload';
import { useTopicSearch, useGenreSearch } from '@/hooks/useTopicGenre';

function EditMixlistForm() {
    const { id } = useParams();
    const navigate = useNavigate();
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [thumbnailFile, setThumbnailFile] = useState(null);
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

    const [topicInput, setTopicInput] = useState('');
    const [genreInput, setGenreInput] = useState('');

    const [formData, setFormData] = useState({
        name: '',
        description: '',
        thumbnail: '',
        topics: [],
        genres: []
    });

    const mixlistQuery = useMixlist(id);
    const loading = mixlistQuery.isLoading;

    // Seed form state once the mixlist loads.
    useEffect(() => {
        const mixlist = mixlistQuery.data;
        if (!mixlist) return;
        setFormData({
            name: mixlist.Name || mixlist.name || '',
            description: mixlist.Description || mixlist.description || '',
            thumbnail: mixlist.Thumbnail || mixlist.thumbnail || '',
            topics: mixlist.Topics || mixlist.topics || [],
            genres: mixlist.Genres || mixlist.genres || []
        });
    }, [mixlistQuery.data]);

    useEffect(() => {
        if (mixlistQuery.error) {
            console.error('Failed to fetch mixlist:', mixlistQuery.error);
            setSnackbar({ open: true, message: 'Failed to load mixlist', severity: 'error' });
        }
    }, [mixlistQuery.error]);

    const updateMutation = useUpdateMixlist();
    const deleteMutation = useDeleteMixlist();
    const uploadThumbnailMutation = useUploadThumbnail();
    const saving = updateMutation.isPending;

    const topicSearchQuery = useTopicSearch(topicInput);
    const genreSearchQuery = useGenreSearch(genreInput);
    const topicSuggestions = topicSearchQuery.data ?? [];
    const genreSuggestions = genreSearchQuery.data ?? [];

    const handleInputChange = (field, value) => {
        setFormData(prev => ({
            ...prev,
            [field]: value
        }));
    };

    const handleThumbnailUpload = (event) => {
        const file = event.target.files[0];
        if (!file) return;

        setThumbnailFile(file);
        uploadThumbnailMutation.mutate(file, {
            onSuccess: (data) => {
                handleInputChange('thumbnail', data.url);
                setSnackbar({
                    open: true,
                    message: 'Thumbnail uploaded successfully!',
                    severity: 'success'
                });
            },
            onError: (error) => {
                console.error('Error uploading thumbnail:', error);
                setSnackbar({
                    open: true,
                    message: 'Failed to upload thumbnail. Please try again.',
                    severity: 'error'
                });
                setThumbnailFile(null);
            },
        });
    };

    const handleSubmit = (e) => {
        e.preventDefault();

        updateMutation.mutate(
            { id, mixlistData: formData },
            {
                onSuccess: () => {
                    setSnackbar({
                        open: true,
                        message: 'Mixlist updated successfully!',
                        severity: 'success'
                    });
                    setTimeout(() => navigate(`/mixlist/${id}`), 1500);
                },
                onError: (error) => {
                    console.error('Failed to update mixlist:', error);
                    setSnackbar({
                        open: true,
                        message: error.response?.data?.message || 'Failed to update mixlist',
                        severity: 'error'
                    });
                },
            }
        );
    };

    const handleCancel = () => {
        navigate(`/mixlist/${id}`);
    };

    const handleDelete = () => {
        deleteMutation.mutate(id, {
            onSuccess: () => {
                setSnackbar({
                    open: true,
                    message: 'Mixlist deleted successfully!',
                    severity: 'success'
                });
                setTimeout(() => navigate('/mixlists'), 1500);
            },
            onError: (error) => {
                console.error('Failed to delete mixlist:', error);
                setSnackbar({
                    open: true,
                    message: error.response?.data?.error || 'Failed to delete mixlist',
                    severity: 'error'
                });
            },
            onSettled: () => setDeleteDialogOpen(false),
        });
    };

    if (loading) {
        return (
            <Container maxWidth="md">
                <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '50vh' }}>
                    <CircularProgress />
                </Box>
            </Container>
        );
    }

    return (
        <Container maxWidth="md">
            <Box sx={{ mt: 4 }}>
                {/* Header */}
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 4 }}>
                <Button
                        onClick={handleCancel}
                        startIcon={<ArrowBack />}
                        variant="outlined"
                        sx={{ 
                            mr: { xs: 0, sm: 2 },
                            minHeight: '44px',
                            color: 'white',
                            borderColor: 'white',
                            fontSize: { xs: '0.875rem', sm: '1rem' },
                            '&:hover': {
                                borderColor: 'white',
                                backgroundColor: 'rgba(255, 255, 255, 0.08)'
                            }
                        }}
                    >
                        Back
                    </Button>
                    <Typography variant="h4" component="h1" sx={{ fontWeight: 'bold' }}>
                        Edit Mixlist
                    </Typography>
                </Box>

                <Card>
                    <CardContent sx={{ p: 4 }}>
                        <form onSubmit={handleSubmit}>
                            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                                {/* Name */}
                                <TextField
                                    fullWidth
                                    label="Mixlist Name *"
                                    value={formData.name}
                                    onChange={(e) => handleInputChange('name', e.target.value)}
                                    required
                                    placeholder="Enter a name for your mixlist"
                                    InputLabelProps={{ sx: { color: '#fcfafa' } }}
                                />

                                {/* Description */}
                                <TextField
                                    fullWidth
                                    label="Description"
                                    multiline
                                    rows={4}
                                    value={formData.description}
                                    onChange={(e) => handleInputChange('description', e.target.value)}
                                    placeholder="Describe what this mixlist is about..."
                                    InputLabelProps={{ sx: { color: '#fcfafa' } }}
                                />

                                {/* Thumbnail URL */}
                                <TextField
                                    fullWidth
                                    label="Thumbnail URL"
                                    value={formData.thumbnail}
                                    onChange={(e) => handleInputChange('thumbnail', e.target.value)}
                                    placeholder="https://example.com/image.jpg"
                                    helperText="Optional: URL to an image for this mixlist"
                                    InputLabelProps={{ sx: { color: '#fcfafa' } }}
                                    FormHelperTextProps={{ sx: { color: '#fcfafa' } }}
                                />

                                {/* Thumbnail Upload */}
                                <Box sx={{ mt: 2 }}>
                                    <Typography variant="body1" sx={{ 
                                        mb: 2, 
                                        fontSize: '16px',
                                        fontWeight: 'bold'
                                    }}>
                                        Upload New Thumbnail
                                    </Typography>
                                    <Button
                                        variant="contained"
                                        color="primary"
                                        component="label"
                                        sx={{ 
                                            fontSize: '16px',
                                            fontWeight: 'bold',
                                            textTransform: 'none',
                                            py: 1.5,
                                            px: 3,
                                            borderRadius: '8px'
                                        }}
                                    >
                                        Choose File
                                        <input
                                            type="file"
                                            accept="image/*"
                                            hidden
                                            onChange={handleThumbnailUpload}
                                        />
                                    </Button>
                                    {thumbnailFile && (
                                        <Typography variant="body2" sx={{ 
                                            mt: 1, 
                                            fontSize: '14px',
                                            color: 'text.secondary'
                                        }}>
                                            Selected: {thumbnailFile.name}
                                        </Typography>
                                    )}
                                </Box>

                                {/* Topics */}
                                <Box>
                                    <Typography variant="body1" sx={{ mb: 1, fontWeight: 'bold' }}>
                                        Topics
                                    </Typography>
                                    <Autocomplete
                                        multiple
                                        freeSolo
                                        options={topicSuggestions.map((option) => option.name || option.Name)}
                                        value={formData.topics}
                                        onChange={(event, newValue) => {
                                            handleInputChange('topics', newValue.map(t => t.toLowerCase()));
                                        }}
                                        onInputChange={(event, newInputValue) => setTopicInput(newInputValue)}
                                        renderTags={(value, getTagProps) =>
                                            value.map((option, index) => (
                                                <Chip
                                                    key={`topic-${option}`}
                                                    label={option}
                                                    size="small"
                                                    sx={{
                                                        backgroundColor: 'primary.main',
                                                        color: 'white',
                                                        fontSize: '0.75rem'
                                                    }}
                                                    {...getTagProps({ index })}
                                                />
                                            ))
                                        }
                                        renderInput={(params) => (
                                            <TextField
                                                {...params}
                                                placeholder="Type to search or add topics..."
                                                variant="outlined"
                                            />
                                        )}
                                    />
                                </Box>

                                {/* Genres */}
                                <Box>
                                    <Typography variant="body1" sx={{ mb: 1, fontWeight: 'bold' }}>
                                        Genres
                                    </Typography>
                                    <Autocomplete
                                        multiple
                                        freeSolo
                                        options={genreSuggestions.map((option) => option.name || option.Name)}
                                        value={formData.genres}
                                        onChange={(event, newValue) => {
                                            handleInputChange('genres', newValue.map(g => g.toLowerCase()));
                                        }}
                                        onInputChange={(event, newInputValue) => setGenreInput(newInputValue)}
                                        renderTags={(value, getTagProps) =>
                                            value.map((option, index) => (
                                                <Chip
                                                    key={`genre-${option}`}
                                                    label={option}
                                                    size="small"
                                                    sx={{
                                                        backgroundColor: '#4b6aa2',
                                                        color: 'white',
                                                        fontSize: '0.75rem'
                                                    }}
                                                    {...getTagProps({ index })}
                                                />
                                            ))
                                        }
                                        renderInput={(params) => (
                                            <TextField
                                                {...params}
                                                placeholder="Type to search or add genres..."
                                                variant="outlined"
                                            />
                                        )}
                                    />
                                </Box>

                                {/* Action Buttons */}
                                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'space-between', mt: 4 }}>
                                    <Button
                                        variant="contained"
                                        color="primary"
                                        startIcon={<Delete />}
                                        onClick={() => setDeleteDialogOpen(true)}
                                        disabled={saving}
                                        size="large"
                                    >
                                        Delete Mixlist
                                    </Button>
                                    <Box sx={{ display: 'flex', gap: 2 }}>
                                        <Button
                                            variant="contained"
                                            color="primary"
                                            startIcon={<Cancel />}
                                            onClick={handleCancel}
                                            disabled={saving}
                                            size="large"
                                            sx={{
                                                color: 'primary',
                                                fontSize: '16px',
                                                fontWeight: 'bold',
                                                textTransform: 'none',
                                                py: 1.5
                                            }}
                                        >
                                            Cancel
                                        </Button>
                                        <Button
                                            type="submit"
                                            variant="contained"
                                            color="primary"
                                            startIcon={<Save />}
                                            disabled={saving}
                                            size="large"
                                            sx={{
                                                fontSize: '16px',
                                                fontWeight: 'bold',
                                                textTransform: 'none',
                                                py: 1.5
                                            }}
                                        >
                                            {saving ? 'Saving...' : 'Save Changes'}
                                        </Button>
                                    </Box>
                                </Box>
                            </Box>
                        </form>
                    </CardContent>
                </Card>
            </Box>

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

            {/* Delete Confirmation Dialog */}
            <Dialog
                open={deleteDialogOpen}
                onClose={() => setDeleteDialogOpen(false)}
            >
                <DialogTitle>Confirm Delete</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        Are you sure you want to delete the mixlist &quot;{formData.name}&quot;? 
                        This will remove the mixlist but will NOT delete the media items in it.
                        This action cannot be undone.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setDeleteDialogOpen(false)}>
                        Cancel
                    </Button>
                    <Button onClick={handleDelete} color="error" variant="contained">
                        Delete
                    </Button>
                </DialogActions>
            </Dialog>
        </Container>
    );
}

export default EditMixlistForm;
