import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { TextField, Button, Box, Typography, CircularProgress, Alert, Chip, Grid, Paper } from '@mui/material';
import { Language, Download, Visibility, OpenInNew, RssFeed } from '@mui/icons-material';
import { useScrapeWebsitePreview, useImportWebsite } from '@/hooks/useWebsite';
import TagAutocomplete from './TagAutocomplete';
import { OUTLINED_BUTTON_SX, PRIMARY_BUTTON_SX, ACCENT_BUTTON_SX, PAPER_SX, extractErrorMessage } from './importPageStyles';

/**
 * Save one page: preview what the scraper sees, then import. A `?url=` query parameter
 * (the bookmarklet's hand-off) fills the field and runs the preview on arrival. When the
 * preview reports the page is already in the library, the import still works — the API
 * answers with the existing website — but the user is told first.
 */
function SingleUrlImport() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [url, setUrl] = useState(() => searchParams.get('url') ?? '');
  const [notes, setNotes] = useState('');
  const [topics, setTopics] = useState([]);
  const [genres, setGenres] = useState([]);
  const [titleOverride, setTitleOverride] = useState('');

  const [previewData, setPreviewData] = useState(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const scrapeMutation = useScrapeWebsitePreview();
  const importMutation = useImportWebsite();
  const isLoading = scrapeMutation.isPending;
  const isImporting = importMutation.isPending;
  const busy = isLoading || isImporting;

  const runPreview = (candidate) => {
    if (!candidate.trim()) {
      setError('Please enter a URL');
      return;
    }

    try {
      new URL(candidate);
    } catch {
      setError('Please enter a valid URL (e.g., https://example.com)');
      return;
    }

    setError('');
    setPreviewData(null);

    scrapeMutation.mutate(candidate.trim(), {
      onSuccess: (data) => setPreviewData(data),
      onError: (err) => {
        console.error('Preview error:', err);
        setError(extractErrorMessage(err, 'Failed to scrape website. Please check the URL and try again.'));
      },
    });
  };

  // Bookmarklet hand-off: preview the page the user was on. Runs on arrival and again if the
  // query changes while the tab is already mounted, but never twice for the same address.
  const handedOff = useRef(null);
  const fromQuery = searchParams.get('url');
  useEffect(() => {
    if (fromQuery && handedOff.current !== fromQuery) {
      handedOff.current = fromQuery;
      setUrl(fromQuery);
      runPreview(fromQuery);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fromQuery]);

  const handlePreview = () => runPreview(url);

  const handleImport = () => {
    if (!url.trim()) {
      setError('Please enter a URL');
      return;
    }

    setError('');
    setSuccess('');

    const websiteData = {
      url: url.trim(),
      notes: notes.trim() || null,
      topics: topics.length > 0 ? topics : null,
      genres: genres.length > 0 ? genres : null,
      titleOverride: titleOverride.trim() || null,
    };

    importMutation.mutate(websiteData, {
      onSuccess: (result) => {
        const alreadyThere = previewData?.existingWebsiteId && previewData.existingWebsiteId === result.id;
        setSuccess(alreadyThere
          ? `"${result.title}" was already in your library. Opening it...`
          : `Website "${result.title}" imported successfully! Redirecting...`);
        setTimeout(() => navigate(`/media/${result.id}`), 1500);
      },
      onError: (err) => {
        console.error('Import error:', err);
        setError(extractErrorMessage(err, 'Failed to import website. Please try again.'));
      },
    });
  };

  const handleKeyDown = (event) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      if (previewData) handleImport();
      else handlePreview();
    }
  };

  return (
    <>
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

      <Paper sx={PAPER_SX}>
        <Typography variant="h6" gutterBottom sx={{ color: 'text.primary', mb: 2 }}>
          Website URL
        </Typography>

        <TextField
          fullWidth
          label="URL"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="https://example.com"
          sx={{ mb: 2 }}
          disabled={busy}
        />

        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <Button
            variant="outlined"
            onClick={handlePreview}
            disabled={busy || !url.trim()}
            startIcon={isLoading ? <CircularProgress size={20} color="inherit" /> : <Visibility />}
            sx={OUTLINED_BUTTON_SX}
          >
            {isLoading ? 'Scraping...' : 'Preview'}
          </Button>

          <Button
            variant="contained"
            onClick={handleImport}
            disabled={busy || !url.trim()}
            startIcon={isImporting ? <CircularProgress size={20} color="inherit" /> : <Download />}
            sx={PRIMARY_BUTTON_SX}
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

      {previewData?.existingWebsiteId && (
        <Alert
          severity="warning"
          sx={{ mb: 3 }}
          action={(
            <Button color="inherit" size="small" onClick={() => navigate(`/media/${previewData.existingWebsiteId}`)}>
              Open it
            </Button>
          )}
        >
          This page is already in your library as <strong>{previewData.existingTitle || 'an existing website'}</strong>.
          Importing again will not create a duplicate.
        </Alert>
      )}

      {previewData && (
        <Paper sx={PAPER_SX}>
          <Typography variant="h6" gutterBottom sx={{ color: 'text.primary', mb: 3 }}>
            Preview
          </Typography>

          <Grid container spacing={3}>
            {previewData.imageUrl && (
              <Grid item xs={12} md={4}>
                <img
                  src={previewData.imageUrl}
                  alt={previewData.title || 'Website preview'}
                  style={{ width: '100%', maxHeight: 300, objectFit: 'cover', borderRadius: 8 }}
                  onError={(e) => { e.target.style.display = 'none'; }}
                />
              </Grid>
            )}
            <Grid item xs={12} md={previewData.imageUrl ? 8 : 12}>
              <Box>
                <Typography variant="h5" gutterBottom sx={{ color: 'text.primary' }}>
                  {previewData.title || 'Untitled'}
                </Typography>

                {previewData.domain && (
                  <Chip label={previewData.domain} size="small" icon={<Language />} sx={{ mb: 2 }} />
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

      <Paper sx={PAPER_SX}>
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
          disabled={busy}
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
          disabled={busy}
        />

        <TagAutocomplete kind="topic" label="Topics" value={topics} onChange={setTopics} disabled={busy} />
        <TagAutocomplete kind="genre" label="Genres" value={genres} onChange={setGenres} disabled={busy} />
      </Paper>

      <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
        <Button
          variant="contained"
          onClick={handleImport}
          disabled={busy || !url.trim()}
          startIcon={isImporting ? <CircularProgress size={20} color="inherit" /> : <Download />}
          size="large"
          sx={{ ...ACCENT_BUTTON_SX, px: 4 }}
        >
          {isImporting ? 'Importing...' : 'Import Website'}
        </Button>
      </Box>
    </>
  );
}

export default SingleUrlImport;
