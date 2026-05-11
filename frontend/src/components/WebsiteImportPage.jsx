import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    TextField, Button, Box, Typography, Container,
    Card, CardContent, CircularProgress, Alert,
    Chip, Grid, Paper, Autocomplete
} from '@mui/material';
import { Language, Download, Visibility, OpenInNew, RssFeed, ArrowBack } from '@mui/icons-material';
import { scrapeWebsitePreview, importWebsite } from '../api/websiteService';
import { searchTopics, searchGenres } from '../api/topicGenreService';

function WebsiteImportPage() {
    const [url, setUrl] = useState('');
    const [notes, setNotes] = useState('');
    const [topics, setTopics] = useState([]);
    const [genres, setGenres] = useState([]);
    const [topicSuggestions, setTopicSuggestions] = useState([]);
    const [genreSuggestions, setGenreSuggestions] = useState([]);
    const [titleOverride, setTitleOverride] = useState('');

    const [previewData, setPreviewData] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const [isImporting, setIsImporting] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');

    const navigate = useNavigate();

    const handleTopicSearch = async (inputValue) => {
        if (inputValue.length > 0) {
            try {
                const response = await searchTopics(inputValue);
                setTopicSuggestions(response.data);
            } catch (error) {
                console.error('Error searching topics:', error);
                setTopicSuggestions([]);
            }
        } else {
            setTopicSuggestions([]);
        }
    };

    const handleGenreSearch = async (inputValue) => {
        if (inputValue.length > 0) {
            try {
                const response = await searchGenres(inputValue);
                setGenreSuggestions(response.data);
            } catch (error) {
                console.error('Error searching genres:', error);
                setGenreSuggestions([]);
            }
        } else {
            setGenreSuggestions([]);
        }
    };

    const handlePreview = async () => {
        if (!url.trim()) {
            setError('Please enter a URL');
            return;
        }

        // Basic URL validation
        try {
            new URL(url);
        } catch {
            setError('Please enter a valid URL (e.g., https://example.com)');
            return;
        }

        setIsLoading(true);
        setError('');
        setPreviewData(null);

        try {
            const data = await scrapeWebsitePreview(url);
            setPreviewData(data);
            setIsLoading(false);
        } catch (err) {
            console.error('Preview error:', err);
            setError(err.response?.data?.error || 'Failed to scrape website. Please check the URL and try again.');
            setIsLoading(false);
        }
    };

    const handleImport = async () => {
        if (!url.trim()) {
            setError('Please enter a URL');
            return;
        }

        setIsImporting(true);
        setError('');
        setSuccess('');

        try {
            const websiteData = {
                url: url.trim(),
                notes: notes.trim() || null,
                topics: topics.length > 0 ? topics : null,
                genres: genres.length > 0 ? genres : null,
                titleOverride: titleOverride.trim() || null
            };

            const result = await importWebsite(websiteData);
            setSuccess(`Website "${result.title}" imported successfully! Redirecting...`);
            setIsImporting(false);

            // Redirect to the newly created media profile after a short delay
            setTimeout(() => {
                navigate(`/media/${result.id}`);
            }, 1500);
        } catch (err) {
            console.error('Import error:', err);
            setError(err.response?.data?.error || 'Failed to import website. Please try again.');
            setIsImporting(false);
        }
    };

    const handleKeyPress = (event) => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            if (previewData) {
                handleImport();
            } else {
                handlePreview();
            }
        }
    };

    return (
        <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
            <Box sx={{ mb: 4 }}>
                <Typography variant="h3" gutterBottom sx={{ color: '#ffffff', fontWeight: 700 }}>
                    <Language sx={{ fontSize: 40, verticalAlign: 'middle', mr: 2, color: '#90caf9' }} />
                    Import Website
                </Typography>
                <Typography variant="body1" sx={{ color: 'text.secondary' }}>
                    Save websites to your library. We'll automatically extract title, description, images, and RSS feeds.
                </Typography>
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError('')}>
                    {error}
                </Alert>
            )}

            {success && (
                <Alert severity="success" sx={{ mb: 3 }} onClose={() => setSuccess('')}>
                    {success}
                </Alert>
            )}

            <Paper sx={{ mb: 3, p: 3, backgroundColor: 'background.paper' }}>
                <Typography variant="h6" gutterBottom sx={{ color: 'text.primary', mb: 2 }}>
                    Website URL
                </Typography>

                <TextField
                    fullWidth
                    label="URL"
                    value={url}
                    onChange={(e) => setUrl(e.target.value)}
                    onKeyPress={handleKeyPress}
                    placeholder="https://example.com"
                    sx={{ mb: 2 }}
                    disabled={isLoading || isImporting}
                />

                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                    <Button
                        variant="outlined"
                        onClick={handlePreview}
                        disabled={isLoading || isImporting || !url.trim()}
                        startIcon={isLoading ? <CircularProgress size={20} color="inherit" /> : <Visibility />}
                        sx={{
                            borderColor: '#90caf9',
                            color: '#90caf9',
                            '&:hover': {
                                borderColor: '#64b5f6',
                                backgroundColor: 'rgba(144, 202, 249, 0.08)'
                            },
                            '&.Mui-disabled': {
                                borderColor: 'rgba(255, 255, 255, 0.3)',
                                color: 'rgba(255, 255, 255, 0.3)'
                            }
                        }}
                    >
                        {isLoading ? 'Scraping...' : 'Preview'}
                    </Button>

                    <Button
                        variant="contained"
                        onClick={handleImport}
                        disabled={isLoading || isImporting || !url.trim()}
                        startIcon={isImporting ? <CircularProgress size={20} color="inherit" /> : <Download />}
                        sx={{
                            backgroundColor: '#362759',
                            '&:hover': {
                                backgroundColor: '#1f1a35'
                            }
                        }}
                    >
                        {isImporting ? 'Importing...' : 'Import Directly'}
                    </Button>

                    {url.trim() && (
                        <Button
                            variant="text"
                            onClick={() => window.open(url, '_blank')}
                            startIcon={<OpenInNew />}
                            sx={{ ml: 'auto', color: 'text.secondary' }}
                        >
                            Visit Website
                        </Button>
                    )}
                </Box>
            </Paper>

            {previewData && (
                <Paper sx={{ mb: 3, p: 3, backgroundColor: 'background.paper' }}>
                    <Typography variant="h6" gutterBottom sx={{ color: 'text.primary', mb: 3 }}>
                        Preview
                    </Typography>

                    <Grid container spacing={3}>
                        {previewData.imageUrl && (
                            <Grid item xs={12} md={4}>
                                <img
                                    src={previewData.imageUrl}
                                    alt={previewData.title || 'Website preview'}
                                    style={{
                                        width: '100%',
                                        maxHeight: 300,
                                        objectFit: 'cover',
                                        borderRadius: 8
                                    }}
                                    onError={(e) => {
                                        e.target.style.display = 'none';
                                    }}
                                />
                            </Grid>
                        )}
                        <Grid item xs={12} md={previewData.imageUrl ? 8 : 12}>
                            <Box>
                                <Typography variant="h5" gutterBottom sx={{ color: 'text.primary' }}>
                                    {previewData.title || 'Untitled'}
                                </Typography>

                                {previewData.domain && (
                                    <Chip
                                        label={previewData.domain}
                                        size="small"
                                        icon={<Language />}
                                        sx={{ mb: 2 }}
                                    />
                                )}

                                {previewData.description && (
                                    <Typography variant="body1" sx={{ color: 'text.secondary', mb: 2 }}>
                                        {previewData.description}
                                    </Typography>
                                )}

                                {previewData.author && (
                                    <Typography variant="body2" sx={{ color: 'text.secondary', mb: 1 }}>
                                        <strong>Author:</strong> {previewData.author}
                                    </Typography>
                                )}

                                {previewData.publication && (
                                    <Typography variant="body2" sx={{ color: 'text.secondary', mb: 1 }}>
                                        <strong>Publication:</strong> {previewData.publication}
                                    </Typography>
                                )}

                                {previewData.rssFeedUrl && (
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2 }}>
                                        <RssFeed sx={{ fontSize: 20, color: '#ff6b35' }} />
                                        <Typography variant="body2" sx={{ color: '#4caf50' }}>
                                            RSS Feed Detected
                                        </Typography>
                                    </Box>
                                )}
                            </Box>
                        </Grid>
                    </Grid>
                </Paper>
            )}

            <Paper sx={{ p: 3, backgroundColor: 'background.paper' }}>
                <Typography variant="h6" gutterBottom sx={{ color: 'text.primary', mb: 2 }}>
                    Additional Information (Optional)
                </Typography>

                <TextField
                    fullWidth
                    label="Title Override"
                    value={titleOverride}
                    onChange={(e) => setTitleOverride(e.target.value)}
                    placeholder="Override the scraped title"
                    sx={{ mb: 2 }}
                    disabled={isLoading || isImporting}
                    helperText="Leave empty to use the scraped title"
                />

                <TextField
                    fullWidth
                    label="Notes"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    placeholder="Add your personal notes about this website"
                    multiline
                    rows={3}
                    sx={{ mb: 2 }}
                    disabled={isLoading || isImporting}
                />

                <Box sx={{ mb: 2 }}>
                    <Typography variant="body2" sx={{ mb: 1, fontWeight: 'bold' }}>
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
                        onInputChange={(event, newInputValue) => {
                            handleTopicSearch(newInputValue);
                        }}
                        disabled={isLoading || isImporting}
                        renderTags={(value, getTagProps) =>
                            value.map((option, index) => (
                                <Chip
                                    key={index}
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

                <Box sx={{ mb: 2 }}>
                    <Typography variant="body2" sx={{ mb: 1, fontWeight: 'bold' }}>
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
                        onInputChange={(event, newInputValue) => {
                            handleGenreSearch(newInputValue);
                        }}
                        disabled={isLoading || isImporting}
                        renderTags={(value, getTagProps) =>
                            value.map((option, index) => (
                                <Chip
                                    key={index}
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
            </Paper>

            {/* Footer buttons */}
            <Box sx={{ mt: 3, display: 'flex', gap: 2, justifyContent: 'space-between', flexWrap: 'wrap' }}>
                <Button
                    variant="outlined"
                    onClick={() => navigate('/import-media')}
                    startIcon={<ArrowBack />}
                    sx={{
                        borderColor: 'rgba(255, 255, 255, 0.5)',
                        color: 'text.primary',
                        '&:hover': {
                            borderColor: 'rgba(255, 255, 255, 0.8)',
                            backgroundColor: 'rgba(255, 255, 255, 0.05)'
                        }
                    }}
                >
                    Back to Import Options
                </Button>

                <Button
                    variant="contained"
                    onClick={handleImport}
                    disabled={isLoading || isImporting || !url.trim()}
                    startIcon={isImporting ? <CircularProgress size={20} color="inherit" /> : <Download />}
                    size="large"
                    sx={{
                        backgroundColor: '#7c4dff',
                        '&:hover': {
                            backgroundColor: '#651fff'
                        },
                        px: 4
                    }}
                >
                    {isImporting ? 'Importing...' : 'Import Website'}
                </Button>
            </Box>
        </Container>
    );
}

export default WebsiteImportPage;

