import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Container, Box, Typography, Grid, Button, CircularProgress,
    Alert, TextField, InputAdornment, Chip, Select, MenuItem,
    FormControl, InputLabel, Card, CardContent, CardActions,
    CardMedia, IconButton, Tooltip
} from '@mui/material';
import {
    ArrowBack, Add, Search, OpenInNew, RssFeed,
    Language, Sort, Refresh, Delete
} from '@mui/icons-material';
import { useAllWebsites, useWebsitesWithRss, useDeleteWebsite } from '../hooks/useWebsite';

function WebsitesPage() {
    const navigate = useNavigate();
    const [searchQuery, setSearchQuery] = useState('');
    const [filterRss, setFilterRss] = useState('all');
    const [sortBy, setSortBy] = useState('dateAdded');
    const [overrideError, setOverrideError] = useState('');
    const [dismissedError, setDismissedError] = useState(false);

    const showRssOnly = filterRss === 'rss-only';
    const allQuery = useAllWebsites({ enabled: !showRssOnly });
    const rssQuery = useWebsitesWithRss({ enabled: showRssOnly });
    const activeQuery = showRssOnly ? rssQuery : allQuery;
    const websites = useMemo(() => activeQuery.data ?? [], [activeQuery.data]);
    const loading = activeQuery.isLoading;
    const queryError = !dismissedError && activeQuery.error
        ? (activeQuery.error.message || 'Failed to load websites')
        : '';
    const error = overrideError || queryError;

    const deleteMutation = useDeleteWebsite();

    const fetchWebsites = () => {
        setDismissedError(false);
        setOverrideError('');
        activeQuery.refetch();
    };

    const filteredWebsites = useMemo(() => {
        let filtered = [...websites];

        if (searchQuery) {
            const query = searchQuery.toLowerCase();
            filtered = filtered.filter(website =>
                website.title?.toLowerCase().includes(query) ||
                website.domain?.toLowerCase().includes(query) ||
                website.author?.toLowerCase().includes(query) ||
                website.publication?.toLowerCase().includes(query) ||
                website.description?.toLowerCase().includes(query)
            );
        }

        filtered.sort((a, b) => {
            switch (sortBy) {
                case 'dateAdded':
                    return new Date(b.dateAdded) - new Date(a.dateAdded);
                case 'dateAddedAsc':
                    return new Date(a.dateAdded) - new Date(b.dateAdded);
                case 'title':
                    return (a.title || '').localeCompare(b.title || '');
                case 'domain':
                    return (a.domain || '').localeCompare(b.domain || '');
                default:
                    return 0;
            }
        });

        return filtered;
    }, [websites, searchQuery, sortBy]);

    const handleDelete = (id, title) => {
        if (window.confirm(`Are you sure you want to delete "${title}"?`)) {
            deleteMutation.mutate(id, {
                onError: (err) => setOverrideError(`Failed to delete website: ${err.message}`),
            });
        }
    };

    const getStats = () => {
        const total = websites.length;
        const withRss = websites.filter(w => w.rssFeedUrl).length;
        const domains = new Set(websites.map(w => w.domain)).size;

        return { total, withRss, domains };
    };

    const stats = getStats();

    if (loading) {
        return (
            <Box
                sx={{
                    minHeight: '100vh',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    py: 4,
                    px: 2
                }}
            >
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Container maxWidth="xl" sx={{ py: 4, minHeight: '100vh' }}>
            {/* Header */}
            <Box sx={{ mb: 4 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                        <IconButton onClick={() => navigate('/')} sx={{ color: '#fcfafa' }}>
                            <ArrowBack />
                        </IconButton>
                        <Typography variant="h3" sx={{ color: '#fcfafa', fontWeight: 700 }}>
                            <Language sx={{ fontSize: 40, verticalAlign: 'middle', mr: 2 }} />
                            Websites
                        </Typography>
                    </Box>
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <Tooltip title="Refresh">
                            <IconButton onClick={fetchWebsites} sx={{ color: '#fcfafa' }}>
                                <Refresh />
                            </IconButton>
                        </Tooltip>
                        <Button
                            variant="contained"
                            startIcon={<Add />}
                            onClick={() => navigate('/import-website')}
                        >
                            Import Website
                        </Button>
                    </Box>
                </Box>

                {/* Stats */}
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mb: 3 }}>
                    <Chip label={`${stats.total} Total`} color="primary" />
                    <Chip label={`${stats.withRss} With RSS`} icon={<RssFeed />} />
                    <Chip label={`${stats.domains} Domains`} icon={<Language />} />
                </Box>

                {/* Search and Filters */}
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mb: 3 }}>
                    <TextField
                        placeholder="Search websites..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        sx={{ flexGrow: 1, minWidth: 300 }}
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <Search />
                                </InputAdornment>
                            ),
                            sx: { color: '#fcfafa' }
                        }}
                    />

                    <FormControl sx={{ minWidth: 150 }}>
                        <InputLabel sx={{ color: '#fcfafa' }}>Filter</InputLabel>
                        <Select
                            value={filterRss}
                            onChange={(e) => setFilterRss(e.target.value)}
                            label="Filter"
                            sx={{ color: '#fcfafa' }}
                        >
                            <MenuItem value="all">All Websites</MenuItem>
                            <MenuItem value="rss-only">With RSS Only</MenuItem>
                        </Select>
                    </FormControl>

                    <FormControl sx={{ minWidth: 180 }}>
                        <InputLabel sx={{ color: '#fcfafa' }}>Sort By</InputLabel>
                        <Select
                            value={sortBy}
                            onChange={(e) => setSortBy(e.target.value)}
                            label="Sort By"
                            startAdornment={<Sort />}
                            sx={{ color: '#fcfafa' }}
                        >
                            <MenuItem value="dateAdded">Newest First</MenuItem>
                            <MenuItem value="dateAddedAsc">Oldest First</MenuItem>
                            <MenuItem value="title">Title A-Z</MenuItem>
                            <MenuItem value="domain">Domain A-Z</MenuItem>
                        </Select>
                    </FormControl>
                </Box>
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 3 }} onClose={() => { setOverrideError(''); setDismissedError(true); }}>
                    {error}
                </Alert>
            )}

            {/* Websites Grid */}
            {filteredWebsites.length === 0 ? (
                <Box
                    sx={{
                        textAlign: 'center',
                        py: 8,
                        backgroundColor: 'rgba(252, 250, 250, 0.05)',
                        borderRadius: 2
                    }}
                >
                    <Language sx={{ fontSize: 80, color: '#fcfafa', opacity: 0.3, mb: 2 }} />
                    <Typography variant="h5" sx={{ color: '#fcfafa', mb: 2 }}>
                        {searchQuery ? 'No websites found' : 'No websites yet'}
                    </Typography>
                    <Typography variant="body1" sx={{ color: '#fcfafa', opacity: 0.7, mb: 3 }}>
                        {searchQuery 
                            ? 'Try adjusting your search or filters'
                            : 'Start by importing your first website'}
                    </Typography>
                    {!searchQuery && (
                        <Button
                            variant="contained"
                            startIcon={<Add />}
                            onClick={() => navigate('/import-website')}
                        >
                            Import Website
                        </Button>
                    )}
                </Box>
            ) : (
                <Grid container spacing={3}>
                    {filteredWebsites.map((website) => (
                        <Grid item xs={12} sm={6} md={4} lg={3} key={website.id}>
                            <Card
                                sx={{
                                    height: '100%',
                                    display: 'flex',
                                    flexDirection: 'column',
                                    backgroundColor: '#2d1b4e',
                                    border: '1px solid #3d2b5e',
                                    transition: 'transform 0.2s, box-shadow 0.2s',
                                    '&:hover': {
                                        transform: 'translateY(-4px)',
                                        boxShadow: '0 8px 24px rgba(252, 250, 250, 0.1)'
                                    }
                                }}
                            >
                                {website.thumbnail && (
                                    <CardMedia
                                        component="img"
                                        height="160"
                                        image={website.thumbnail}
                                        alt={website.title}
                                        sx={{ 
                                          width: '100%',
                                          maxHeight: 160,
                                          objectFit: 'cover' 
                                        }}
                                        onError={(e) => {
                                            e.target.style.display = 'none';
                                        }}
                                    />
                                )}
                                <CardContent sx={{ flexGrow: 1 }}>
                                    <Typography
                                        variant="h6"
                                        sx={{
                                            color: '#fcfafa',
                                            mb: 1,
                                            fontWeight: 600,
                                            overflow: 'hidden',
                                            textOverflow: 'ellipsis',
                                            display: '-webkit-box',
                                            WebkitLineClamp: 2,
                                            WebkitBoxOrient: 'vertical'
                                        }}
                                    >
                                        {website.title}
                                    </Typography>

                                    {website.domain && (
                                        <Chip
                                            label={website.domain}
                                            size="small"
                                            icon={<Language />}
                                            sx={{ mb: 1 }}
                                        />
                                    )}

                                    {website.rssFeedUrl && (
                                        <Chip
                                            label="RSS"
                                            size="small"
                                            icon={<RssFeed />}
                                            color="warning"
                                            sx={{ mb: 1, ml: 1 }}
                                        />
                                    )}

                                    {website.description && (
                                        <Typography
                                            variant="body2"
                                            sx={{
                                                color: '#fcfafa',
                                                opacity: 0.7,
                                                mt: 1,
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                display: '-webkit-box',
                                                WebkitLineClamp: 3,
                                                WebkitBoxOrient: 'vertical'
                                            }}
                                        >
                                            {website.description}
                                        </Typography>
                                    )}

                                    {(website.author || website.publication) && (
                                        <Typography
                                            variant="caption"
                                            sx={{ color: '#fcfafa', opacity: 0.5, display: 'block', mt: 1 }}
                                        >
                                            {website.author && `By ${website.author}`}
                                            {website.author && website.publication && ' • '}
                                            {website.publication}
                                        </Typography>
                                    )}

                                    {website.topics && website.topics.length > 0 && (
                                        <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                                            {website.topics.slice(0, 3).map((topic) => (
                                                <Chip
                                                    key={`topic-${topic}`}
                                                    label={topic}
                                                    size="small"
                                                    variant="outlined"
                                                    sx={{ fontSize: '0.7rem' }}
                                                />
                                            ))}
                                        </Box>
                                    )}
                                </CardContent>
                                <CardActions sx={{ justifyContent: 'space-between', px: 2, pb: 2 }}>
                                    <Button
                                        size="small"
                                        onClick={() => navigate(`/media/${website.id}`)}
                                    >
                                        Details
                                    </Button>
                                    <Box>
                                        {website.link && (
                                            <Tooltip title="Visit Website">
                                                <IconButton
                                                    size="small"
                                                    onClick={() => window.open(website.link, '_blank')}
                                                    sx={{ color: '#fcfafa' }}
                                                >
                                                    <OpenInNew fontSize="small" />
                                                </IconButton>
                                            </Tooltip>
                                        )}
                                        <Tooltip title="Delete">
                                            <IconButton
                                                size="small"
                                                onClick={() => handleDelete(website.id, website.title)}
                                                sx={{ color: '#fcfafa' }}
                                            >
                                                <Delete fontSize="small" />
                                            </IconButton>
                                        </Tooltip>
                                    </Box>
                                </CardActions>
                            </Card>
                        </Grid>
                    ))}
                </Grid>
            )}

            {/* Results count */}
            {filteredWebsites.length > 0 && (
                <Box sx={{ mt: 4, textAlign: 'center' }}>
                    <Typography variant="body2" sx={{ color: '#fcfafa', opacity: 0.7 }}>
                        Showing {filteredWebsites.length} of {websites.length} websites
                    </Typography>
                </Box>
            )}
        </Container>
    );
}

export default WebsitesPage;

