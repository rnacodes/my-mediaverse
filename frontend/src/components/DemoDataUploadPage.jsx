import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Box, Typography, Button, Card, CardContent, TextField,
    Alert, CircularProgress, Divider, Chip, Tab, Tabs,
    FormControl, InputLabel, Select, MenuItem, Checkbox,
    FormControlLabel, Slider, IconButton, Paper, Collapse
} from '@mui/material';
import {
    ArrowBack, CloudUpload, Search as SearchIcon, CheckCircle,
    Error as ErrorIcon, Delete, ExpandMore, ExpandLess, Link as LinkIcon
} from '@mui/icons-material';
import { scrapeArticlePreview, createArticle } from '../api/articleService';
import { bulkCreateHighlights } from '../api/highlightService';

const whiteButtonSx = {
    backgroundColor: 'white',
    color: 'black',
    '&:hover': { backgroundColor: 'rgba(255, 255, 255, 0.9)' },
    '&.Mui-disabled': {
        backgroundColor: 'rgba(255, 255, 255, 0.5)',
        color: 'rgba(0, 0, 0, 0.5)'
    }
};

const tabSx = {
    '& .MuiTab-root': {
        color: 'rgba(255, 255, 255, 0.7)',
        '&.Mui-selected': { color: 'white' }
    },
    '& .MuiTabs-indicator': { backgroundColor: 'white' }
};

// ============================================
// Markdown Highlight Parser
// ============================================

function parseHighlightMarkdown(text) {
    const lines = text.split('\n');
    let title = '';
    let author = '';
    let sourceUrl = '';
    const highlights = [];

    // Parse header: # Title by Author
    if (lines.length > 0 && lines[0].startsWith('#')) {
        const headerLine = lines[0].replace(/^#+\s*/, '').trim();
        const lastByIndex = headerLine.lastIndexOf(' by ');
        if (lastByIndex > 0) {
            title = headerLine.substring(0, lastByIndex).trim();
            author = headerLine.substring(lastByIndex + 4).trim();
        } else {
            title = headerLine;
        }
    }

    // Check for URL on line 2
    let startLine = 1;
    if (lines.length > 1 && lines[1].startsWith('URL:')) {
        sourceUrl = lines[1].replace(/^URL:\s*/, '').trim();
        startLine = 2;
    }

    // Parse highlights — separated by blank lines
    let currentHighlight = null;
    let collectingNote = false;

    for (let i = startLine; i < lines.length; i++) {
        const line = lines[i];
        const trimmed = line.trim();

        if (trimmed === '') {
            // Blank line — end of current note collection if active
            collectingNote = false;
            continue;
        }

        if (trimmed.startsWith('**Note:**')) {
            // Note line — attach to current highlight
            if (currentHighlight) {
                const noteText = trimmed.replace(/^\*\*Note:\*\*\s*/, '').trim();
                currentHighlight.note = noteText;
                collectingNote = true;
            }
            continue;
        }

        if (trimmed.startsWith('**Tags:**')) {
            // Tags line — attach to current highlight
            if (currentHighlight) {
                const tagsText = trimmed.replace(/^\*\*Tags:\*\*\s*/, '').trim();
                currentHighlight.tags = tagsText.split(',').map(t => t.trim().toLowerCase()).filter(Boolean);
            }
            collectingNote = false;
            continue;
        }

        if (collectingNote && currentHighlight) {
            // Continuation of a multi-line note
            currentHighlight.note = (currentHighlight.note || '') + '\n' + trimmed;
            continue;
        }

        // New highlight text
        currentHighlight = { text: trimmed, note: '', tags: [] };
        highlights.push(currentHighlight);
        collectingNote = false;
    }

    // Auto-detect category
    const category = sourceUrl ? 'articles' : 'books';

    return { title, author, sourceUrl, category, highlights };
}

// ============================================
// Article Upload Tab
// ============================================

function ArticleUploadTab() {
    const navigate = useNavigate();
    const [url, setUrl] = useState('');
    const [scraping, setScraping] = useState(false);
    const [scraped, setScraped] = useState(false);
    const [saving, setSaving] = useState(false);
    const [savedArticle, setSavedArticle] = useState(null);
    const [error, setError] = useState('');

    // Form fields
    const [title, setTitle] = useState('');
    const [articleAuthor, setArticleAuthor] = useState('');
    const [description, setDescription] = useState('');
    const [publication, setPublication] = useState('');
    const [thumbnail, setThumbnail] = useState('');
    const [status, setStatus] = useState('Uncharted');
    const [readingProgress, setReadingProgress] = useState(0);
    const [wordCount, setWordCount] = useState('');
    const [publicationDate, setPublicationDate] = useState('');
    const [topicsInput, setTopicsInput] = useState('');
    const [genresInput, setGenresInput] = useState('');
    const [isArchived, setIsArchived] = useState(false);
    const [isStarred, setIsStarred] = useState(false);
    const [notes, setNotes] = useState('');

    const handleScrape = async () => {
        if (!url.trim()) return;
        setScraping(true);
        setError('');
        try {
            const data = await scrapeArticlePreview(url.trim());
            setTitle(data.title || '');
            setArticleAuthor(data.author || '');
            setDescription(data.description || '');
            setPublication(data.publication || '');
            setThumbnail(data.imageUrl || '');
            setScraped(true);
        } catch (err) {
            const msg = err.response?.data?.error || err.response?.data?.details || err.message;
            setError(`Failed to scrape URL: ${msg}`);
        } finally {
            setScraping(false);
        }
    };

    const handleSave = async () => {
        if (!title.trim()) {
            setError('Title is required');
            return;
        }
        setSaving(true);
        setError('');
        try {
            const topics = topicsInput.split(',').map(t => t.trim().toLowerCase()).filter(Boolean);
            const genres = genresInput.split(',').map(g => g.trim().toLowerCase()).filter(Boolean);

            const articleData = {
                title: title.trim(),
                link: url.trim() || undefined,
                author: articleAuthor.trim() || undefined,
                description: description.trim() || undefined,
                publication: publication.trim() || undefined,
                thumbnail: thumbnail.trim() || undefined,
                status,
                readingProgress: readingProgress || undefined,
                wordCount: wordCount ? parseInt(wordCount, 10) : undefined,
                publicationDate: publicationDate || undefined,
                isArchived,
                isStarred,
                notes: notes.trim() || undefined,
                topics,
                genres,
                mediaType: 'Article'
            };

            const result = await createArticle(articleData);
            setSavedArticle(result);
        } catch (err) {
            const msg = err.response?.data?.error || err.response?.data?.details || err.message;
            setError(`Failed to save article: ${msg}`);
        } finally {
            setSaving(false);
        }
    };

    const handleReset = () => {
        setUrl('');
        setScraped(false);
        setSavedArticle(null);
        setError('');
        setTitle('');
        setArticleAuthor('');
        setDescription('');
        setPublication('');
        setThumbnail('');
        setStatus('Uncharted');
        setReadingProgress(0);
        setWordCount('');
        setPublicationDate('');
        setTopicsInput('');
        setGenresInput('');
        setIsArchived(false);
        setIsStarred(false);
        setNotes('');
    };

    if (savedArticle) {
        return (
            <Box>
                <Alert severity="success" sx={{ mb: 3 }}>
                    <Typography variant="body1">
                        Article "<strong>{savedArticle.title}</strong>" created successfully!
                    </Typography>
                </Alert>
                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
                    <Button variant="outlined" onClick={handleReset}>
                        Upload Another Article
                    </Button>
                    <Button
                        variant="contained"
                        startIcon={<LinkIcon />}
                        onClick={() => navigate(`/media/${savedArticle.id}`)}
                        sx={whiteButtonSx}
                    >
                        View Article
                    </Button>
                </Box>
            </Box>
        );
    }

    return (
        <Box>
            <Alert severity="info" sx={{ mb: 3 }}>
                <Typography variant="body2">
                    Paste an article URL to auto-fill metadata, then fill in any remaining fields to simulate a Readwise-imported article.
                </Typography>
            </Alert>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
                    {error}
                </Alert>
            )}

            {/* Step 1: URL Input */}
            <Box sx={{ display: 'flex', gap: 2, mb: 3, alignItems: 'flex-start' }}>
                <TextField
                    fullWidth
                    label="Article URL"
                    placeholder="https://example.com/article"
                    value={url}
                    onChange={(e) => setUrl(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleScrape()}
                />
                <Button
                    variant="contained"
                    onClick={handleScrape}
                    disabled={!url.trim() || scraping}
                    startIcon={scraping ? <CircularProgress size={20} color="inherit" /> : <SearchIcon />}
                    sx={{ ...whiteButtonSx, minWidth: 160, height: 56 }}
                >
                    {scraping ? 'Fetching...' : 'Fetch Metadata'}
                </Button>
            </Box>

            {/* Step 2: Edit Form (shown after scrape or user can fill manually) */}
            <Collapse in={scraped || title.length > 0}>
                <Divider sx={{ my: 2 }} />
                <Typography variant="h6" sx={{ mb: 2 }}>Article Details</Typography>

                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <TextField
                        fullWidth
                        label="Title"
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        required
                    />
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <TextField
                            fullWidth
                            label="Author"
                            value={articleAuthor}
                            onChange={(e) => setArticleAuthor(e.target.value)}
                        />
                        <TextField
                            fullWidth
                            label="Publication"
                            value={publication}
                            onChange={(e) => setPublication(e.target.value)}
                        />
                    </Box>
                    <TextField
                        fullWidth
                        label="Description"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        multiline
                        rows={3}
                    />
                    <TextField
                        fullWidth
                        label="Thumbnail URL"
                        value={thumbnail}
                        onChange={(e) => setThumbnail(e.target.value)}
                    />
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <FormControl fullWidth>
                            <InputLabel>Status</InputLabel>
                            <Select
                                value={status}
                                label="Status"
                                onChange={(e) => setStatus(e.target.value)}
                            >
                                <MenuItem value="Uncharted">Uncharted</MenuItem>
                                <MenuItem value="Exploring">Exploring</MenuItem>
                                <MenuItem value="Completed">Completed</MenuItem>
                                <MenuItem value="Wishlisted">Wishlisted</MenuItem>
                                <MenuItem value="Abandoned">Abandoned</MenuItem>
                            </Select>
                        </FormControl>
                        <TextField
                            fullWidth
                            label="Word Count"
                            type="number"
                            value={wordCount}
                            onChange={(e) => setWordCount(e.target.value)}
                        />
                        <TextField
                            fullWidth
                            label="Publication Date"
                            type="date"
                            value={publicationDate}
                            onChange={(e) => setPublicationDate(e.target.value)}
                            InputLabelProps={{ shrink: true }}
                        />
                    </Box>
                    <Box sx={{ px: 1 }}>
                        <Typography gutterBottom>Reading Progress: {readingProgress}%</Typography>
                        <Slider
                            value={readingProgress}
                            onChange={(e, val) => setReadingProgress(val)}
                            min={0}
                            max={100}
                            valueLabelDisplay="auto"
                        />
                    </Box>
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <TextField
                            fullWidth
                            label="Topics (comma-separated)"
                            value={topicsInput}
                            onChange={(e) => setTopicsInput(e.target.value)}
                            placeholder="e.g. productivity, technology"
                        />
                        <TextField
                            fullWidth
                            label="Genres (comma-separated)"
                            value={genresInput}
                            onChange={(e) => setGenresInput(e.target.value)}
                            placeholder="e.g. technology, science"
                        />
                    </Box>
                    <Box sx={{ display: 'flex', gap: 3 }}>
                        <FormControlLabel
                            control={<Checkbox checked={isArchived} onChange={(e) => setIsArchived(e.target.checked)} />}
                            label="Archived"
                        />
                        <FormControlLabel
                            control={<Checkbox checked={isStarred} onChange={(e) => setIsStarred(e.target.checked)} />}
                            label="Starred"
                        />
                    </Box>
                    <TextField
                        fullWidth
                        label="Notes"
                        value={notes}
                        onChange={(e) => setNotes(e.target.value)}
                        multiline
                        rows={2}
                    />

                    <Divider sx={{ my: 1 }} />

                    <Box sx={{ display: 'flex', justifyContent: 'center' }}>
                        <Button
                            variant="contained"
                            onClick={handleSave}
                            disabled={!title.trim() || saving}
                            startIcon={saving ? <CircularProgress size={20} color="inherit" /> : <CheckCircle />}
                            sx={whiteButtonSx}
                        >
                            {saving ? 'Saving...' : 'Save Article'}
                        </Button>
                    </Box>
                </Box>
            </Collapse>
        </Box>
    );
}

// ============================================
// Highlight Upload Tab
// ============================================

function HighlightUploadTab() {
    const [file, setFile] = useState(null);
    const [parsed, setParsed] = useState(null);
    const [uploading, setUploading] = useState(false);
    const [uploadResult, setUploadResult] = useState(null);
    const [error, setError] = useState('');
    const [expandedHighlights, setExpandedHighlights] = useState(true);

    // Shared metadata (editable)
    const [title, setTitle] = useState('');
    const [author, setAuthor] = useState('');
    const [category, setCategory] = useState('books');
    const [sourceUrl, setSourceUrl] = useState('');
    const [highlightedAt, setHighlightedAt] = useState('');

    const handleFileUpload = (event) => {
        const uploadedFile = event.target.files[0];
        if (!uploadedFile) return;

        if (!uploadedFile.name.toLowerCase().endsWith('.md')) {
            setError('Please upload a markdown (.md) file.');
            return;
        }

        setFile(uploadedFile);
        setError('');

        const reader = new FileReader();
        reader.onload = (e) => {
            const content = e.target.result;
            const result = parseHighlightMarkdown(content);
            setParsed(result);
            setTitle(result.title);
            setAuthor(result.author);
            setCategory(result.category);
            setSourceUrl(result.sourceUrl);
        };
        reader.readAsText(uploadedFile);
    };

    const removeHighlight = (index) => {
        if (!parsed) return;
        const updated = { ...parsed, highlights: parsed.highlights.filter((_, i) => i !== index) };
        setParsed(updated);
    };

    const updateHighlightNote = (index, newNote) => {
        if (!parsed) return;
        const updated = {
            ...parsed,
            highlights: parsed.highlights.map((h, i) =>
                i === index ? { ...h, note: newNote } : h
            )
        };
        setParsed(updated);
    };

    const updateHighlightTags = (index, newTags) => {
        if (!parsed) return;
        const updated = {
            ...parsed,
            highlights: parsed.highlights.map((h, i) =>
                i === index ? { ...h, tags: newTags.split(',').map(t => t.trim().toLowerCase()).filter(Boolean) } : h
            )
        };
        setParsed(updated);
    };

    const handleUpload = async () => {
        if (!parsed || parsed.highlights.length === 0) return;
        setUploading(true);
        setError('');

        try {
            const highlightDtos = parsed.highlights.map(h => ({
                text: h.text,
                note: h.note || undefined,
                title: title.trim() || undefined,
                author: author.trim() || undefined,
                category: category || undefined,
                sourceUrl: sourceUrl.trim() || undefined,
                tags: h.tags.length > 0 ? h.tags : undefined,
                highlightedAt: highlightedAt || undefined
            }));

            const result = await bulkCreateHighlights(highlightDtos);
            setUploadResult(result);
        } catch (err) {
            const msg = err.response?.data?.error || err.response?.data?.details || err.message;
            setError(`Failed to upload highlights: ${msg}`);
        } finally {
            setUploading(false);
        }
    };

    const handleReset = () => {
        setFile(null);
        setParsed(null);
        setUploadResult(null);
        setError('');
        setTitle('');
        setAuthor('');
        setCategory('books');
        setSourceUrl('');
        setHighlightedAt('');
    };

    if (uploadResult) {
        return (
            <Box>
                <Alert severity="success" sx={{ mb: 3 }}>
                    <Typography variant="body1">
                        Successfully created <strong>{uploadResult.created}</strong> highlights
                        {uploadResult.linked > 0 && (
                            <> and auto-linked <strong>{uploadResult.linked}</strong> to existing media</>
                        )}!
                    </Typography>
                </Alert>

                {uploadResult.errors && uploadResult.errors.length > 0 && (
                    <Alert severity="warning" sx={{ mb: 3 }}>
                        <Typography variant="body2" gutterBottom><strong>Errors ({uploadResult.errors.length}):</strong></Typography>
                        {uploadResult.errors.map((err, i) => (
                            <Typography key={i} variant="body2">- {err}</Typography>
                        ))}
                    </Alert>
                )}

                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
                    <Button variant="outlined" onClick={handleReset}>
                        Upload Another File
                    </Button>
                </Box>
            </Box>
        );
    }

    return (
        <Box>
            <Alert severity="info" sx={{ mb: 3 }}>
                <Typography variant="body2">
                    Upload a markdown file with highlights. Expected format: <code># Title by Author</code> on the first line,
                    highlights separated by blank lines, with optional <code>**Note:**</code> and <code>**Tags:**</code> lines.
                </Typography>
            </Alert>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
                    {error}
                </Alert>
            )}

            {/* Step 1: File Upload */}
            <Box sx={{ display: 'flex', gap: 2, mb: 3, alignItems: 'center' }}>
                <Button
                    variant="contained"
                    component="label"
                    startIcon={<CloudUpload />}
                    sx={whiteButtonSx}
                >
                    Choose Markdown File
                    <input
                        type="file"
                        accept=".md"
                        hidden
                        onChange={handleFileUpload}
                    />
                </Button>
                {file && (
                    <Chip label={file.name} onDelete={() => handleReset()} />
                )}
            </Box>

            {/* Step 2: Preview & Edit */}
            {parsed && parsed.highlights.length > 0 && (
                <>
                    <Divider sx={{ my: 2 }} />
                    <Typography variant="h6" sx={{ mb: 2 }}>
                        Shared Metadata
                    </Typography>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mb: 3 }}>
                        <Box sx={{ display: 'flex', gap: 2 }}>
                            <TextField
                                fullWidth
                                label="Title"
                                value={title}
                                onChange={(e) => setTitle(e.target.value)}
                            />
                            <TextField
                                fullWidth
                                label="Author"
                                value={author}
                                onChange={(e) => setAuthor(e.target.value)}
                            />
                        </Box>
                        <Box sx={{ display: 'flex', gap: 2 }}>
                            <FormControl fullWidth>
                                <InputLabel>Category</InputLabel>
                                <Select
                                    value={category}
                                    label="Category"
                                    onChange={(e) => setCategory(e.target.value)}
                                >
                                    <MenuItem value="books">Books</MenuItem>
                                    <MenuItem value="articles">Articles</MenuItem>
                                    <MenuItem value="podcasts">Podcasts</MenuItem>
                                    <MenuItem value="tweets">Tweets</MenuItem>
                                </Select>
                            </FormControl>
                            <TextField
                                fullWidth
                                label="Source URL"
                                value={sourceUrl}
                                onChange={(e) => setSourceUrl(e.target.value)}
                            />
                            <TextField
                                fullWidth
                                label="Highlighted At"
                                type="date"
                                value={highlightedAt}
                                onChange={(e) => setHighlightedAt(e.target.value)}
                                InputLabelProps={{ shrink: true }}
                            />
                        </Box>
                    </Box>

                    <Divider sx={{ my: 2 }} />

                    {/* Highlights list */}
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
                        <Typography variant="h6">
                            Highlights ({parsed.highlights.length})
                        </Typography>
                        <IconButton onClick={() => setExpandedHighlights(!expandedHighlights)} size="small">
                            {expandedHighlights ? <ExpandLess /> : <ExpandMore />}
                        </IconButton>
                    </Box>

                    <Collapse in={expandedHighlights}>
                        <Paper
                            sx={{
                                maxHeight: 500,
                                overflow: 'auto',
                                p: 2,
                                mb: 3,
                                backgroundColor: 'rgba(255,255,255,0.05)'
                            }}
                        >
                            {parsed.highlights.map((h, index) => (
                                <Box
                                    key={index}
                                    sx={{
                                        mb: 2,
                                        p: 2,
                                        borderLeft: '3px solid rgba(255,255,255,0.3)',
                                        borderRadius: 1,
                                        backgroundColor: 'rgba(255,255,255,0.03)',
                                        '&:last-child': { mb: 0 }
                                    }}
                                >
                                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                                        <Typography
                                            variant="body2"
                                            sx={{
                                                fontStyle: 'italic',
                                                flex: 1,
                                                mr: 1,
                                                whiteSpace: 'pre-wrap'
                                            }}
                                        >
                                            "{h.text}"
                                        </Typography>
                                        <IconButton
                                            size="small"
                                            onClick={() => removeHighlight(index)}
                                            sx={{ color: 'rgba(255,255,255,0.5)', flexShrink: 0 }}
                                        >
                                            <Delete fontSize="small" />
                                        </IconButton>
                                    </Box>
                                    <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
                                        <TextField
                                            size="small"
                                            label="Note"
                                            value={h.note || ''}
                                            onChange={(e) => updateHighlightNote(index, e.target.value)}
                                            sx={{ flex: 2 }}
                                            multiline
                                        />
                                        <TextField
                                            size="small"
                                            label="Tags (comma-sep)"
                                            value={h.tags.join(', ')}
                                            onChange={(e) => updateHighlightTags(index, e.target.value)}
                                            sx={{ flex: 1 }}
                                        />
                                    </Box>
                                </Box>
                            ))}
                        </Paper>
                    </Collapse>

                    <Box sx={{ display: 'flex', justifyContent: 'center' }}>
                        <Button
                            variant="contained"
                            onClick={handleUpload}
                            disabled={uploading || parsed.highlights.length === 0}
                            startIcon={uploading ? <CircularProgress size={20} color="inherit" /> : <CloudUpload />}
                            sx={whiteButtonSx}
                        >
                            {uploading ? 'Uploading...' : `Upload ${parsed.highlights.length} Highlights`}
                        </Button>
                    </Box>
                </>
            )}

            {parsed && parsed.highlights.length === 0 && file && (
                <Alert severity="warning">
                    No highlights found in the file. Please check the markdown format.
                </Alert>
            )}
        </Box>
    );
}

// ============================================
// Main Page Component
// ============================================

function DemoDataUploadPage() {
    const [activeTab, setActiveTab] = useState(0);
    const navigate = useNavigate();

    return (
        <Box sx={{ p: 3, maxWidth: 1200, margin: '0 auto' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                <Button
                    startIcon={<ArrowBack />}
                    onClick={() => navigate(-1)}
                    sx={{ mr: 2 }}
                >
                    Back
                </Button>
                <Typography variant="h4">
                    Demo Data Upload
                </Typography>
            </Box>

            <Card sx={{ '&:hover': { boxShadow: 'none' }, transition: 'none' }}>
                <CardContent>
                    <Tabs
                        value={activeTab}
                        onChange={(e, newValue) => setActiveTab(newValue)}
                        sx={{ ...tabSx, mb: 3 }}
                    >
                        <Tab label="Upload Article" />
                        <Tab label="Upload Highlights" />
                    </Tabs>

                    {activeTab === 0 && <ArticleUploadTab />}
                    {activeTab === 1 && <HighlightUploadTab />}
                </CardContent>
            </Card>
        </Box>
    );
}

export default DemoDataUploadPage;
