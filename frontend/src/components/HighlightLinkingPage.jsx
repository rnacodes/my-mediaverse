//Update purple outline buttons to white

import React, { useState, useMemo } from 'react';
import { Box, Container, Typography, Paper, TextField, Button, Checkbox, Chip, CircularProgress, Alert, Snackbar, Dialog, DialogTitle, DialogContent, DialogActions, InputAdornment, Accordion, AccordionSummary, AccordionDetails, Divider } from '@mui/material';
import { Search as SearchIcon, ExpandMore as ExpandMoreIcon, Link as LinkIcon, MenuBook as BookIcon, Article as ArticleIcon } from '@mui/icons-material';
import { useUnlinkedHighlights, useUpdateHighlight } from '../hooks/useHighlight';
import { useAllBooks } from '../hooks/useBook';
import { useAllArticles } from '../hooks/useArticle';

export default function HighlightLinkingPage() {
    const unlinkedHighlightsQuery = useUnlinkedHighlights();
    const booksQuery = useAllBooks();
    const articlesQuery = useAllArticles();
    const updateHighlightMutation = useUpdateHighlight();

    const highlights = useMemo(() => unlinkedHighlightsQuery.data || [], [unlinkedHighlightsQuery.data]);
    const books = useMemo(() => booksQuery.data || [], [booksQuery.data]);
    const articles = useMemo(() => {
        const data = articlesQuery.data;
        return Array.isArray(data) ? data : (data || []);
    }, [articlesQuery.data]);

    const loading = unlinkedHighlightsQuery.isLoading || booksQuery.isLoading || articlesQuery.isLoading;
    const loadError = unlinkedHighlightsQuery.error || booksQuery.error || articlesQuery.error;
    const [dismissedError, setDismissedError] = useState(false);
    const error = loadError && !dismissedError ? 'Failed to load data. Please try again.' : null;

    // UI state
    const [sourceSearchQuery, setSourceSearchQuery] = useState('');
    const [selectedHighlights, setSelectedHighlights] = useState(new Set());
    const [linkDialogOpen, setLinkDialogOpen] = useState(false);
    const [mediaSearchQuery, setMediaSearchQuery] = useState('');
    const [linking, setLinking] = useState(false);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const [expandedSources, setExpandedSources] = useState(new Set());

    const refreshData = () => {
        setDismissedError(false);
        unlinkedHighlightsQuery.refetch();
        booksQuery.refetch();
        articlesQuery.refetch();
    };

    // Group highlights by source (title + author)
    const groupedHighlights = useMemo(() => {
        const groups = new Map();

        highlights.forEach(highlight => {
            const sourceKey = `${highlight.title || 'Unknown Source'}|||${highlight.author || ''}`;
            if (!groups.has(sourceKey)) {
                groups.set(sourceKey, {
                    title: highlight.title || 'Unknown Source',
                    author: highlight.author || null,
                    category: highlight.category,
                    highlights: []
                });
            }
            groups.get(sourceKey).highlights.push(highlight);
        });

        // Convert to array and sort by highlight count (most first)
        return Array.from(groups.values()).sort((a, b) => b.highlights.length - a.highlights.length);
    }, [highlights]);

    // Filter groups by search query
    const filteredGroups = useMemo(() => {
        if (!sourceSearchQuery.trim()) return groupedHighlights;

        const query = sourceSearchQuery.toLowerCase();
        return groupedHighlights.filter(group =>
            group.title.toLowerCase().includes(query) ||
            (group.author && group.author.toLowerCase().includes(query))
        );
    }, [groupedHighlights, sourceSearchQuery]);

    // Filter media for link dialog
    const filteredBooks = useMemo(() => {
        if (!mediaSearchQuery.trim()) return [];
        const query = mediaSearchQuery.toLowerCase();
        return books.filter(book =>
            book.title?.toLowerCase().includes(query) ||
            book.author?.toLowerCase().includes(query)
        ).slice(0, 10);
    }, [books, mediaSearchQuery]);

    const filteredArticles = useMemo(() => {
        if (!mediaSearchQuery.trim()) return [];
        const query = mediaSearchQuery.toLowerCase();
        return articles.filter(article =>
            article.title?.toLowerCase().includes(query) ||
            article.author?.toLowerCase().includes(query)
        ).slice(0, 10);
    }, [articles, mediaSearchQuery]);

    // Selection handlers
    const toggleHighlightSelection = (highlightId) => {
        setSelectedHighlights(prev => {
            const newSet = new Set(prev);
            if (newSet.has(highlightId)) {
                newSet.delete(highlightId);
            } else {
                newSet.add(highlightId);
            }
            return newSet;
        });
    };

    const selectAllInGroup = (groupHighlights) => {
        setSelectedHighlights(prev => {
            const newSet = new Set(prev);
            groupHighlights.forEach(h => newSet.add(h.id));
            return newSet;
        });
    };

    const deselectAllInGroup = (groupHighlights) => {
        setSelectedHighlights(prev => {
            const newSet = new Set(prev);
            groupHighlights.forEach(h => newSet.delete(h.id));
            return newSet;
        });
    };

    const isGroupFullySelected = (groupHighlights) => {
        return groupHighlights.every(h => selectedHighlights.has(h.id));
    };

    const isGroupPartiallySelected = (groupHighlights) => {
        const selectedCount = groupHighlights.filter(h => selectedHighlights.has(h.id)).length;
        return selectedCount > 0 && selectedCount < groupHighlights.length;
    };

    const clearAllSelections = () => {
        setSelectedHighlights(new Set());
    };

    // Link handler
    const handleLinkToMedia = async (mediaType, mediaId) => {
        if (selectedHighlights.size === 0) return;

        setLinking(true);
        try {
            const highlightsToLink = highlights.filter(h => selectedHighlights.has(h.id));
            let successCount = 0;
            let errorCount = 0;

            for (const highlight of highlightsToLink) {
                try {
                    const highlightData = {
                        text: highlight.text,
                        note: highlight.note,
                        tags: highlight.tags || [],
                        articleId: mediaType === 'article' ? mediaId : null,
                        bookId: mediaType === 'book' ? mediaId : null
                    };
                    await updateHighlightMutation.mutateAsync({ id: highlight.id, highlightData });
                    successCount++;
                } catch (err) {
                    console.error(`Failed to link highlight ${highlight.id}:`, err);
                    errorCount++;
                }
            }

            if (successCount > 0) {
                setSnackbar({
                    open: true,
                    message: `Successfully linked ${successCount} highlight${successCount !== 1 ? 's' : ''}${errorCount > 0 ? ` (${errorCount} failed)` : ''}`,
                    severity: errorCount > 0 ? 'warning' : 'success'
                });
                setSelectedHighlights(new Set());
                setLinkDialogOpen(false);
                setMediaSearchQuery('');
                // useUpdateHighlight's onSuccess already invalidates the highlight lists,
                // which refetches useUnlinkedHighlights automatically.
            } else {
                setSnackbar({
                    open: true,
                    message: 'Failed to link highlights. Please try again.',
                    severity: 'error'
                });
            }
        } catch (err) {
            console.error('Error linking highlights:', err);
            setSnackbar({
                open: true,
                message: 'An error occurred while linking highlights.',
                severity: 'error'
            });
        } finally {
            setLinking(false);
        }
    };

    // Toggle source expansion
    const toggleSourceExpansion = (sourceKey) => {
        setExpandedSources(prev => {
            const newSet = new Set(prev);
            if (newSet.has(sourceKey)) {
                newSet.delete(sourceKey);
            } else {
                newSet.add(sourceKey);
            }
            return newSet;
        });
    };

    // Truncate text helper
    const truncateText = (text, maxLength = 150) => {
        if (!text || text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    };

    if (loading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '50vh' }}>
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            {/* Header */}
            <Box sx={{ mb: 4 }}>
                <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
                    Bulk Link Highlights
                </Typography>
                <Typography variant="body1" color="text.secondary">
                    Link multiple highlights to books or articles at once. Highlights are grouped by their source.
                </Typography>
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 3 }} onClose={() => setDismissedError(true)}>{error}</Alert>
            )}

            {/* Stats & Actions Bar */}
            <Paper sx={{ p: 2, mb: 3, display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center', justifyContent: 'space-between' }}>
                <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
                    <Chip
                        label={`${highlights.length} unlinked highlight${highlights.length !== 1 ? 's' : ''}`}
                        variant="outlined"
                        sx={{ borderColor: '#fcfafa', color: '#fcfafa' }}
                    />
                    <Chip
                        label={`${filteredGroups.length} source${filteredGroups.length !== 1 ? 's' : ''}`}
                        variant="outlined"
                        sx={{ borderColor: '#fcfafa', color: '#fcfafa' }}
                    />
                    {selectedHighlights.size > 0 && (
                        <Chip
                            label={`${selectedHighlights.size} selected`}
                            color="success"
                            onDelete={clearAllSelections}
                        />
                    )}
                </Box>
                <Box sx={{ display: 'flex', gap: 1 }}>
                    <Button
                        variant="contained"
                        startIcon={<LinkIcon />}
                        disabled={selectedHighlights.size === 0}
                        onClick={() => setLinkDialogOpen(true)}
                    >
                        Link Selected ({selectedHighlights.size})
                    </Button>
                    <Button
                        variant="outlined"
                        onClick={refreshData}
                        sx={{ borderColor: '#fcfafa', color: '#fcfafa' }}
                    >
                        Refresh
                    </Button>
                </Box>
            </Paper>

            {/* Search Bar */}
            <TextField
                fullWidth
                placeholder="Search by source title or author..."
                value={sourceSearchQuery}
                onChange={(e) => setSourceSearchQuery(e.target.value)}
                InputProps={{
                    startAdornment: (
                        <InputAdornment position="start">
                            <SearchIcon />
                        </InputAdornment>
                    )
                }}
                sx={{ mb: 3 }}
            />

            {/* Grouped Highlights */}
            {filteredGroups.length === 0 ? (
                <Paper sx={{ p: 4, textAlign: 'center' }}>
                    <Typography variant="h6" color="text.secondary">
                        {sourceSearchQuery ? 'No sources match your search' : 'No unlinked highlights found'}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                        {sourceSearchQuery ? 'Try a different search term' : 'All highlights are linked to media items'}
                    </Typography>
                </Paper>
            ) : (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    {filteredGroups.map((group) => {
                        const sourceKey = `${group.title}|||${group.author || ''}`;
                        const isExpanded = expandedSources.has(sourceKey);
                        const allSelected = isGroupFullySelected(group.highlights);
                        const partialSelected = isGroupPartiallySelected(group.highlights);

                        return (
                            <Accordion
                                key={sourceKey}
                                expanded={isExpanded}
                                onChange={() => toggleSourceExpansion(sourceKey)}
                                sx={{
                                    '&:before': { display: 'none' },
                                    borderLeft: allSelected ? '4px solid #4caf50' : partialSelected ? '4px solid #ff9800' : '4px solid transparent'
                                }}
                            >
                                <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, width: '100%', pr: 2 }}>
                                        <Checkbox
                                            checked={allSelected}
                                            indeterminate={partialSelected}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                if (allSelected) {
                                                    deselectAllInGroup(group.highlights);
                                                } else {
                                                    selectAllInGroup(group.highlights);
                                                }
                                            }}
                                        />
                                        <Box sx={{ flex: 1 }}>
                                            <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                                                {group.title}
                                            </Typography>
                                            {group.author && (
                                                <Typography variant="body2" color="text.secondary">
                                                    by {group.author}
                                                </Typography>
                                            )}
                                        </Box>
                                        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                                            {group.category && (
                                                <Chip
                                                    label={group.category}
                                                    size="small"
                                                    sx={{ textTransform: 'capitalize' }}
                                                />
                                            )}
                                            <Chip
                                                label={`${group.highlights.length} highlight${group.highlights.length !== 1 ? 's' : ''}`}
                                                size="small"
                                                variant="outlined"
                                                sx={{ borderColor: '#fcfafa', color: '#fcfafa' }}
                                            />
                                        </Box>
                                    </Box>
                                </AccordionSummary>
                                <AccordionDetails>
                                    <Divider sx={{ mb: 2 }} />
                                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                                        {group.highlights.map((highlight) => (
                                            <Paper
                                                key={highlight.id}
                                                sx={{
                                                    p: 2,
                                                    display: 'flex',
                                                    alignItems: 'flex-start',
                                                    gap: 2,
                                                    bgcolor: selectedHighlights.has(highlight.id) ? 'rgba(76, 175, 80, 0.1)' : 'background.paper',
                                                    border: selectedHighlights.has(highlight.id) ? '1px solid rgba(76, 175, 80, 0.5)' : '1px solid rgba(255,255,255,0.1)',
                                                    cursor: 'pointer',
                                                    '&:hover': {
                                                        bgcolor: selectedHighlights.has(highlight.id) ? 'rgba(76, 175, 80, 0.15)' : 'rgba(255,255,255,0.05)'
                                                    }
                                                }}
                                                onClick={() => toggleHighlightSelection(highlight.id)}
                                            >
                                                <Checkbox
                                                    checked={selectedHighlights.has(highlight.id)}
                                                    onClick={(e) => e.stopPropagation()}
                                                    onChange={() => toggleHighlightSelection(highlight.id)}
                                                />
                                                <Box sx={{ flex: 1 }}>
                                                    <Typography
                                                        variant="body2"
                                                        sx={{
                                                            fontStyle: 'italic',
                                                            lineHeight: 1.6,
                                                            color: 'text.primary'
                                                        }}
                                                    >
                                                        &quot;{truncateText(highlight.text, 300)}&quot;
                                                    </Typography>
                                                    {highlight.note && (
                                                        <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                                                            Note: {truncateText(highlight.note, 100)}
                                                        </Typography>
                                                    )}
                                                    {highlight.highlightedAt && (
                                                        <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
                                                            {new Date(highlight.highlightedAt).toLocaleDateString()}
                                                        </Typography>
                                                    )}
                                                </Box>
                                            </Paper>
                                        ))}
                                    </Box>
                                </AccordionDetails>
                            </Accordion>
                        );
                    })}
                </Box>
            )}

            {/* Link Dialog */}
            <Dialog
                open={linkDialogOpen}
                onClose={() => !linking && setLinkDialogOpen(false)}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    Link {selectedHighlights.size} Highlight{selectedHighlights.size !== 1 ? 's' : ''} to Media
                </DialogTitle>
                <DialogContent>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        Search for a book or article to link the selected highlights to.
                    </Typography>
                    <TextField
                        fullWidth
                        placeholder="Search books or articles..."
                        value={mediaSearchQuery}
                        onChange={(e) => setMediaSearchQuery(e.target.value)}
                        // eslint-disable-next-line jsx-a11y/no-autofocus -- focuses search field on link-dialog open
                        autoFocus
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <SearchIcon />
                                </InputAdornment>
                            )
                        }}
                        sx={{ mb: 2 }}
                    />

                    {mediaSearchQuery.length > 0 && (
                        <Box sx={{ maxHeight: 300, overflow: 'auto' }}>
                            {filteredBooks.length > 0 && (
                                <Box sx={{ mb: 2 }}>
                                    <Typography variant="subtitle2" sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
                                        <BookIcon fontSize="small" /> Books
                                    </Typography>
                                    {filteredBooks.map((book) => (
                                        <Paper
                                            key={book.id}
                                            sx={{
                                                p: 1.5,
                                                mb: 1,
                                                cursor: 'pointer',
                                                '&:hover': { bgcolor: 'action.hover' }
                                            }}
                                            onClick={() => handleLinkToMedia('book', book.id)}
                                        >
                                            <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                                {book.title}
                                            </Typography>
                                            {book.author && (
                                                <Typography variant="caption" color="text.secondary">
                                                    by {book.author}
                                                </Typography>
                                            )}
                                        </Paper>
                                    ))}
                                </Box>
                            )}

                            {filteredArticles.length > 0 && (
                                <Box>
                                    <Typography variant="subtitle2" sx={{ mb: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
                                        <ArticleIcon fontSize="small" /> Articles
                                    </Typography>
                                    {filteredArticles.map((article) => (
                                        <Paper
                                            key={article.id}
                                            sx={{
                                                p: 1.5,
                                                mb: 1,
                                                cursor: 'pointer',
                                                '&:hover': { bgcolor: 'action.hover' }
                                            }}
                                            onClick={() => handleLinkToMedia('article', article.id)}
                                        >
                                            <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                                {article.title}
                                            </Typography>
                                            {article.author && (
                                                <Typography variant="caption" color="text.secondary">
                                                    by {article.author}
                                                </Typography>
                                            )}
                                        </Paper>
                                    ))}
                                </Box>
                            )}

                            {filteredBooks.length === 0 && filteredArticles.length === 0 && (
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                                    No matches found. Try a different search term.
                                </Typography>
                            )}
                        </Box>
                    )}

                    {mediaSearchQuery.length === 0 && (
                        <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                            Start typing to search for books or articles...
                        </Typography>
                    )}

                    {linking && (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
                            <CircularProgress size={24} />
                            <Typography variant="body2" sx={{ ml: 2 }}>
                                Linking highlights...
                            </Typography>
                        </Box>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setLinkDialogOpen(false)} disabled={linking} sx={{ color: '#fcfafa' }}>
                        Cancel
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Snackbar */}
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
        </Container>
    );
}
