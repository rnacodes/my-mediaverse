import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Container, Box, Typography, Grid, Button, CircularProgress,
    Alert, ToggleButton, ToggleButtonGroup, TextField,
    InputAdornment, Chip, Select, MenuItem, FormControl,
    InputLabel, Paper, Divider, IconButton, Tooltip,
    List, ListItem, ListItemText
} from '@mui/material';
import { ArrowBack, CloudDownload, ViewModule, ViewList, Search, FilterList, Star, CheckCircle, Inbox, Sort, Refresh, OpenInNew } from '@mui/icons-material';
import { getAllArticles } from '../api/articleService';
import ArticleCard from './shared/ArticleCard';
import { formatStatus } from '../utils/formatters';

function ArticlesPage() {
    const navigate = useNavigate();
    const [articles, setArticles] = useState([]);
    const [filteredArticles, setFilteredArticles] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [viewMode, setViewMode] = useState('grid');
    const [searchQuery, setSearchQuery] = useState('');
    const [filterStatus, setFilterStatus] = useState('all');
    const [filterFolder, setFilterFolder] = useState('all');
    const [sortBy, setSortBy] = useState('dateAdded');

    useEffect(() => {
        fetchArticles();
    }, []);

    useEffect(() => {
        applyFiltersAndSort();
    }, [articles, searchQuery, filterStatus, filterFolder, sortBy]);

    const fetchArticles = async () => {
        setLoading(true);
        setError('');
        try {
            const data = await getAllArticles();
            setArticles(data);
        } catch (err) {
            setError(err.message || 'Failed to load articles');
        } finally {
            setLoading(false);
        }
    };

    const applyFiltersAndSort = () => {
        let filtered = [...articles];

        // Apply search filter
        if (searchQuery) {
            const query = searchQuery.toLowerCase();
            filtered = filtered.filter(article =>
                article.title?.toLowerCase().includes(query) ||
                article.author?.toLowerCase().includes(query) ||
                article.publication?.toLowerCase().includes(query) ||
                article.description?.toLowerCase().includes(query)
            );
        }

        // Apply status filter
        if (filterStatus !== 'all') {
            filtered = filtered.filter(article => {
                switch (filterStatus) {
                    case 'unread':
                        return article.status === 'Uncharted' || (article.status === 'ActivelyExploring' && article.readingProgress < 1);
                    case 'reading':
                        return article.status === 'ActivelyExploring' && article.readingProgress > 0 && article.readingProgress < 1;
                    case 'completed':
                        return article.status === 'Completed' || article.isReadingCompleted;
                    default:
                        return true;
                }
            });
        }

        // Apply folder filter (archived/starred)
        if (filterFolder !== 'all') {
            filtered = filtered.filter(article => {
                switch (filterFolder) {
                    case 'starred':
                        return article.isStarred;
                    case 'archived':
                        return article.isArchived;
                    case 'unarchived':
                        return !article.isArchived;
                    default:
                        return true;
                }
            });
        }

        // Apply sorting
        filtered.sort((a, b) => {
            switch (sortBy) {
                case 'dateAdded':
                    return new Date(b.dateAdded) - new Date(a.dateAdded);
                case 'dateAddedAsc':
                    return new Date(a.dateAdded) - new Date(b.dateAdded);
                case 'title':
                    return (a.title || '').localeCompare(b.title || '');
                case 'author':
                    return (a.author || '').localeCompare(b.author || '');
                case 'readingTime':
                    return b.estimatedReadingTimeMinutes - a.estimatedReadingTimeMinutes;
                case 'progress':
                    return b.readingProgress - a.readingProgress;
                default:
                    return 0;
            }
        });

        setFilteredArticles(filtered);
    };

    const getStatsChips = () => {
        const total = articles.length;
        const unread = articles.filter(a => a.status === 'Uncharted' || (a.readingProgress === 0 && !a.isReadingCompleted)).length;
        const reading = articles.filter(a => a.readingProgress > 0 && a.readingProgress < 1).length;
        const completed = articles.filter(a => a.isReadingCompleted || a.status === 'Completed').length;
        const starred = articles.filter(a => a.isStarred).length;

        return { total, unread, reading, completed, starred };
    };

    const stats = getStatsChips();

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
                <Box sx={{ 
                    width: '100%',
                    maxWidth: '600px',
                    backgroundColor: 'background.paper',
                    borderRadius: '16px',
                    p: 4,
                    boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
                    textAlign: 'center'
                }}>
                    <CircularProgress size={60} sx={{ mb: 2 }} />
                    <Typography variant="h6">Loading articles...</Typography>
                </Box>
            </Box>
        );
    }

    return (
        <Box
            sx={{
                minHeight: '100vh',
                py: 4,
                px: 2
            }}
        >
            <Container maxWidth="xl">
                <Box sx={{ mb: 3, display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
                    <Button
                        sx={{ color: 'white' }}
                        startIcon={<ArrowBack />}
                        onClick={() => navigate('/')}
                    >
                        Back to Home
                    </Button>

                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <Tooltip title="Refresh">
                            <IconButton
                                onClick={fetchArticles}
                            >
                                <Refresh />
                            </IconButton>
                        </Tooltip>
                        
                        <Button
                            variant="contained"
                            startIcon={<CloudDownload />}
                            onClick={() => navigate('/readwise-sync')}
                        >
                            Sync from Readwise
                        </Button>
                    </Box>
                </Box>

                <Paper
                    elevation={8}
                    sx={{
                        p: 4,
                        borderRadius: '16px',
                        backgroundColor: 'background.paper',
                        boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
                    }}
                >
                    <Typography variant="h4" gutterBottom fontWeight="bold">
                        My Articles
                    </Typography>

                    {/* Stats Chips */}
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 3 }}>
                        <Chip label={`Total: ${stats.total}`} color="default" />
                        <Chip label={`Unread: ${stats.unread}`} icon={<Inbox />} color="primary" />
                        <Chip label={`Reading: ${stats.reading}`} color="info" />
                        <Chip label={`Completed: ${stats.completed}`} icon={<CheckCircle />} color="success" />
                        {stats.starred > 0 && (
                            <Chip label={`Starred: ${stats.starred}`} icon={<Star />} color="warning" />
                        )}
                    </Box>

                    <Divider sx={{ mb: 3 }} />

                    {error && (
                        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError('')}>
                            {error}
                        </Alert>
                    )}

                    {/* Filters and Search */}
                    <Grid container spacing={2} sx={{ mb: 3 }}>
                        <Grid item xs={12} md={4}>
                            <TextField
                                fullWidth
                                placeholder="Search articles..."
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                InputProps={{
                                    startAdornment: (
                                        <InputAdornment position="start">
                                            <Search />
                                        </InputAdornment>
                                    )
                                }}
                            />
                        </Grid>

                        <Grid item xs={6} md={2}>
                            <FormControl fullWidth>
                                <InputLabel>Status</InputLabel>
                                <Select
                                    value={filterStatus}
                                    onChange={(e) => setFilterStatus(e.target.value)}
                                    label="Status"
                                    startAdornment={<FilterList sx={{ ml: 1, mr: -0.5 }} />}
                                >
                                    <MenuItem value="all">All Status</MenuItem>
                                    <MenuItem value="unread">Unread</MenuItem>
                                    <MenuItem value="reading">Reading</MenuItem>
                                    <MenuItem value="completed">Completed</MenuItem>
                                </Select>
                            </FormControl>
                        </Grid>

                        <Grid item xs={6} md={2}>
                            <FormControl fullWidth>
                                <InputLabel>Folder</InputLabel>
                                <Select
                                    value={filterFolder}
                                    onChange={(e) => setFilterFolder(e.target.value)}
                                    label="Folder"
                                >
                                    <MenuItem value="all">All Folders</MenuItem>
                                    <MenuItem value="starred">Starred</MenuItem>
                                    <MenuItem value="archived">Archived</MenuItem>
                                    <MenuItem value="unarchived">Unarchived</MenuItem>
                                </Select>
                            </FormControl>
                        </Grid>

                        <Grid item xs={6} md={2}>
                            <FormControl fullWidth>
                                <InputLabel>Sort By</InputLabel>
                                <Select
                                    value={sortBy}
                                    onChange={(e) => setSortBy(e.target.value)}
                                    label="Sort By"
                                    startAdornment={<Sort sx={{ ml: 1, mr: -0.5 }} />}
                                >
                                    <MenuItem value="dateAdded">Newest First</MenuItem>
                                    <MenuItem value="dateAddedAsc">Oldest First</MenuItem>
                                    <MenuItem value="title">Title (A-Z)</MenuItem>
                                    <MenuItem value="author">Author (A-Z)</MenuItem>
                                    <MenuItem value="readingTime">Reading Time</MenuItem>
                                    <MenuItem value="progress">Progress</MenuItem>
                                </Select>
                            </FormControl>
                        </Grid>

                        <Grid item xs={6} md={2}>
                            <ToggleButtonGroup
                                value={viewMode}
                                exclusive
                                onChange={(e, newMode) => newMode && setViewMode(newMode)}
                                fullWidth
                                sx={{ height: '56px' }}
                            >
                                <ToggleButton value="grid">
                                    <ViewModule />
                                </ToggleButton>
                                <ToggleButton value="list">
                                    <ViewList />
                                </ToggleButton>
                            </ToggleButtonGroup>
                        </Grid>
                    </Grid>

                    {/* Articles Display */}
                    {filteredArticles.length === 0 ? (
                        <Box sx={{ textAlign: 'center', py: 8 }}>
                            <Typography variant="h6" color="text.secondary" gutterBottom>
                                No articles found
                            </Typography>
                            <Typography variant="body2" color="text.secondary" paragraph>
                                {articles.length === 0
                                    ? "Sync articles from Readwise to get started"
                                    : "Try adjusting your filters or search query"}
                            </Typography>
                            {articles.length === 0 && (
                                <Button
                                    variant="contained"
                                    startIcon={<CloudDownload />}
                                    onClick={() => navigate('/readwise-sync')}
                                    sx={{ mt: 2 }}
                                >
                                    Sync from Readwise
                                </Button>
                            )}
                        </Box>
                    ) : (
                        <>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                                Showing {filteredArticles.length} of {articles.length} articles
                            </Typography>
                            
                            {viewMode === 'grid' ? (
                                <Grid container spacing={3}>
                                    {filteredArticles.map(article => (
                                        <Grid
                                            item
                                            xs={12}
                                            sm={6}
                                            md={4}
                                            lg={3}
                                            key={article.id}
                                        >
                                            <ArticleCard article={article} />
                                        </Grid>
                                    ))}
                                </Grid>
                            ) : (
                                <List sx={{ mt: 2 }}>
                                    {filteredArticles.map((article, index) => (
                                        <React.Fragment key={article.id}>
                                            <ListItem
                                                sx={{
                                                    cursor: 'pointer',
                                                    '&:hover': {
                                                        backgroundColor: 'action.hover'
                                                    },
                                                    py: 2,
                                                    px: 2,
                                                    borderRadius: 1
                                                }}
                                                onClick={() => navigate(`/media/${article.id}`)}
                                            >
                                                <ListItemText
                                                    primary={
                                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5, flexWrap: 'wrap' }}>
                                                            <Typography variant="h6" component="div" sx={{ fontWeight: 600 }}>
                                                                {article.title}
                                                            </Typography>
                                                            {article.isStarred && (
                                                                <Star sx={{ color: 'warning.main', fontSize: 20 }} />
                                                            )}
                                                            {article.author && (
                                                                <Typography variant="body2" color="text.secondary">
                                                                    by {article.author}
                                                                </Typography>
                                                            )}
                                                        </Box>
                                                    }
                                                    secondary={
                                                        <Box>
                                                            {article.description && (
                                                                <Typography
                                                                    variant="body2"
                                                                    sx={{
                                                                        mb: 1,
                                                                        overflow: 'hidden',
                                                                        textOverflow: 'ellipsis',
                                                                        display: '-webkit-box',
                                                                        WebkitLineClamp: 2,
                                                                        WebkitBoxOrient: 'vertical'
                                                                    }}
                                                                >
                                                                    {article.description}
                                                                </Typography>
                                                            )}
                                                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 1 }}>
                                                                <Chip
                                                                    label={formatStatus(article.status)}
                                                                    size="small"
                                                                    color={article.status === 'Completed' ? 'success' : article.status === 'ActivelyExploring' ? 'primary' : 'default'}
                                                                />
                                                                {article.publication && (
                                                                    <Chip
                                                                        label={article.publication}
                                                                        size="small"
                                                                        variant="outlined"
                                                                    />
                                                                )}
                                                                {article.estimatedReadingTimeMinutes > 0 && (
                                                                    <Chip
                                                                        label={`${article.estimatedReadingTimeMinutes} min read`}
                                                                        size="small"
                                                                        variant="outlined"
                                                                    />
                                                                )}
                                                                {article.readingProgress > 0 && (
                                                                    <Chip
                                                                        label={`${Math.round(article.readingProgress * 100)}% complete`}
                                                                        size="small"
                                                                        color="info"
                                                                        variant="outlined"
                                                                    />
                                                                )}
                                                            </Box>
                                                        </Box>
                                                    }
                                                />
                                                {(article.effectiveUrl || article.originalUrl || article.link) && (
                                                    <IconButton
                                                        edge="end"
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            const url = article.effectiveUrl || article.originalUrl || article.link;
                                                            if (url) {
                                                                window.open(url, '_blank', 'noopener,noreferrer');
                                                            }
                                                        }}
                                                        sx={{ ml: 2 }}
                                                    >
                                                        <OpenInNew />
                                                    </IconButton>
                                                )}
                                            </ListItem>
                                            {index < filteredArticles.length - 1 && <Divider />}
                                        </React.Fragment>
                                    ))}
                                </List>
                            )}
                        </>
                    )}
                </Paper>
            </Container>
        </Box>
    );
}

export default ArticlesPage;

