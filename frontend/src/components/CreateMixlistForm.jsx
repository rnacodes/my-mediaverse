import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { TextField, Button, Box, Typography, Chip, Autocomplete } from '@mui/material';
import { useCreateMixlist } from '../hooks/useMixlist';
import { useUploadThumbnail } from '../hooks/useUpload';
import { useTopicSearch, useGenreSearch } from '../hooks/useTopicGenre';

function CreateMixlistForm() {
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [thumbnail, setThumbnail] = useState('');
    const [thumbnailFile, setThumbnailFile] = useState(null);
    const [topics, setTopics] = useState([]);
    const [genres, setGenres] = useState([]);
    const [topicInput, setTopicInput] = useState('');
    const [genreInput, setGenreInput] = useState('');
    const navigate = useNavigate();
    const location = useLocation();

    const createMixlistMutation = useCreateMixlist();
    const uploadThumbnailMutation = useUploadThumbnail();
    const isSubmitting = createMixlistMutation.isPending;

    const topicSearchQuery = useTopicSearch(topicInput);
    const genreSearchQuery = useGenreSearch(genreInput);
    const topicSuggestions = topicSearchQuery.data ?? [];
    const genreSuggestions = genreSearchQuery.data ?? [];

    // Handle thumbnail file upload
    const handleThumbnailUpload = (event) => {
        const file = event.target.files[0];
        if (!file) return;

        setThumbnailFile(file);

        uploadThumbnailMutation.mutate(file, {
            onSuccess: (data) => {
                setThumbnail(data.url);
            },
            onError: (error) => {
                console.error('Error uploading thumbnail:', error);
                alert('Failed to upload thumbnail. Please try again.');
                setThumbnailFile(null);
            },
        });
    };

    const handleSubmit = (event) => {
        event.preventDefault();

        const mixlistData = {
            name: name.trim(),
            description: description.trim() || null,
            thumbnail: thumbnail || 'https://project-loopbreaker.atl1.cdn.digitaloceanspaces.com/thumbnails/mixlist-placeholder.png',
            topics: topics.length > 0 ? topics : [],
            genres: genres.length > 0 ? genres : []
        };

        createMixlistMutation.mutate(mixlistData, {
            onSuccess: (data) => {
                const returnTo = location.state?.returnTo;
                if (returnTo) {
                    navigate(returnTo);
                } else {
                    navigate(`/mixlist/${data.id}`);
                }
            },
            onError: (error) => {
                console.error('Failed to create mixlist:', error);
                alert(`Failed to create mixlist: ${error.response?.data?.error || error.message}`);
            },
        });
    };

    return (
        <Box sx={{ 
            minHeight: '100vh', 
            display: 'flex', 
            justifyContent: 'center', 
            alignItems: 'flex-start',
            py: 4,
            px: 2,
            // Global font size override for this form
            '& .MuiInputBase-input': {
                fontSize: '16px !important'
            },
            '& .MuiInputLabel-root': {
                fontSize: '16px !important'
            }
        }}>
            <Box 
                component="form" 
                onSubmit={handleSubmit} 
                sx={{ 
                    width: '100%',
                    maxWidth: '500px',
                    backgroundColor: 'background.paper',
                    borderRadius: '16px',
                    p: 4,
                    boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
                }}
            >
                <Typography variant="h4" component="h1" gutterBottom sx={{ 
                    textAlign: 'center', 
                    fontSize: '28px',
                    fontWeight: 'bold',
                    mb: 3
                }}>
                    Create New Mixlist
                </Typography>
                
                {/* Mixlist Name */}
                <Typography variant="h5" sx={{ 
                    fontSize: '20px', 
                    fontWeight: 'bold', 
                    mb: 1,
                    color: '#ffffff'
                }}>
                    Mixlist Name
                </Typography>
                <TextField
                    placeholder="Enter mixlist name..."
                    variant="outlined"
                    fullWidth
                    required
                    margin="normal"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    sx={{
                        mb: 3,
                        '& .MuiInputBase-input': {
                            fontSize: '16px'
                        },
                        '& .MuiInputBase-input::placeholder': {
                            color: '#ffffff',
                            opacity: 1
                        }
                    }}
                />

                {/* Description */}
                <Typography variant="h5" sx={{
                    fontSize: '20px',
                    fontWeight: 'bold',
                    mb: 1,
                    color: '#ffffff'
                }}>
                    Description
                </Typography>
                <TextField
                    placeholder="Enter a description for your mixlist..."
                    variant="outlined"
                    fullWidth
                    multiline
                    rows={3}
                    margin="normal"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    sx={{
                        mb: 3,
                        '& .MuiInputBase-input': {
                            fontSize: '16px'
                        },
                        '& .MuiInputBase-input::placeholder': {
                            color: '#ffffff',
                            opacity: 1
                        }
                    }}
                />

                {/* Thumbnail URL */}
                <TextField
                    label="Thumbnail URL"
                    placeholder="https://example.com/thumbnail.jpg"
                    variant="outlined"
                    fullWidth
                    margin="normal"
                    value={thumbnail}
                    onChange={(e) => setThumbnail(e.target.value)}
                    sx={{
                        mb: 2,
                        '& .MuiInputBase-input': {
                            fontSize: '14px'
                        },
                        '& .MuiInputBase-input::placeholder': {
                            color: '#ffffff',
                            opacity: 1
                        },
                        '& .MuiInputLabel-root': {
                            color: '#ffffff',
                            fontSize: '14px'
                        },
                        '& .MuiInputLabel-root.Mui-focused': {
                            color: '#ffffff'
                        }
                    }}
                />

                {/* Thumbnail Upload */}
                <Box sx={{ mb: 3 }}>
                    <Typography variant="body1" sx={{ 
                        mb: 2, 
                        fontSize: '16px',
                        fontWeight: 'bold',
                        color: '#ffffff'
                    }}>
                        Upload Thumbnail
                    </Typography>
                    <Button
                        variant="contained"
                        component="label"
                        sx={{
                            fontSize: '16px',
                            fontWeight: 'bold',
                            textTransform: 'none',
                            py: 1.5,
                            backgroundColor: '#9c27b0',
                            color: 'white',
                            '&:hover': {
                                backgroundColor: '#7b1fa2'
                            }
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
                            color: '#ffffff'
                        }}>
                            Selected: {thumbnailFile.name}
                        </Typography>
                    )}
                    {thumbnail && (
                        <Typography variant="body2" sx={{ 
                            mt: 1, 
                            fontSize: '14px',
                            color: '#22c55e'
                        }}>
                            ✓ Thumbnail uploaded successfully
                        </Typography>
                    )}
                </Box>

                {/* Topics */}
                <Typography variant="h5" sx={{
                    fontSize: '20px',
                    fontWeight: 'bold',
                    mb: 1,
                    color: '#ffffff'
                }}>
                    Topics
                </Typography>
                <Autocomplete
                    multiple
                    freeSolo
                    options={topicSuggestions.map((option) => option.name || option.Name)}
                    value={topics}
                    onChange={(event, newValue) => {
                        setTopics(newValue.map(t => t.toLowerCase()));
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
                    sx={{ mb: 3 }}
                />

                {/* Genres */}
                <Typography variant="h5" sx={{
                    fontSize: '20px',
                    fontWeight: 'bold',
                    mb: 1,
                    color: '#ffffff'
                }}>
                    Genres
                </Typography>
                <Autocomplete
                    multiple
                    freeSolo
                    options={genreSuggestions.map((option) => option.name || option.Name)}
                    value={genres}
                    onChange={(event, newValue) => {
                        setGenres(newValue.map(g => g.toLowerCase()));
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
                    sx={{ mb: 3 }}
                />

                {/* Info about thumbnail generation */}
                <Box sx={{ mb: 3, p: 2, backgroundColor: 'rgba(255,255,255,0.1)', borderRadius: '8px' }}>
                    <Typography variant="body2" sx={{
                        fontSize: '14px',
                        color: '#ffffff',
                        opacity: 0.8,
                        mb: 1
                    }}>
                        🎨 Upload a custom thumbnail or leave empty for a placeholder image.
                    </Typography>
                    <Typography variant="body2" sx={{
                        fontSize: '14px',
                        color: '#ffffff',
                        opacity: 0.8
                    }}>
                        📐 Ideal image size: 400x400 pixels (square format recommended).
                    </Typography>
                </Box>

                <Box sx={{ display: 'flex', gap: 2, mt: 4 }}>
                    <Button
                        type="button"
                        variant="contained"
                        onClick={() => navigate(-1)}
                        sx={{
                            flex: 1,
                            fontSize: '16px',
                            fontWeight: 'bold',
                            textTransform: 'none',
                            py: 1.5,
                            backgroundColor: '#9c27b0',
                            color: 'white',
                            '&:hover': {
                                backgroundColor: '#7b1fa2'
                            }
                        }}
                    >
                        Cancel
                    </Button>
                    <Button
                        type="submit"
                        variant="contained"
                        disabled={!name || isSubmitting}
                        sx={{
                            flex: 2,
                            fontSize: '16px',
                            fontWeight: 'bold',
                            textTransform: 'none',
                            py: 1.5,
                            backgroundColor: '#9c27b0',
                            color: 'white',
                            '&:hover': {
                                backgroundColor: '#7b1fa2'
                            }
                        }}
                    >
                        {isSubmitting ? 'Creating...' : 'Create Mixlist'}
                    </Button>
                </Box>
            </Box>
        </Box>
    );
}

export default CreateMixlistForm;
