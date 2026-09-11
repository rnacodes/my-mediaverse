import { useState } from 'react';
import { Alert, Box, Button, CircularProgress, Paper, TextField, Typography } from '@mui/material';
import { Download, Visibility } from '@mui/icons-material';
import { usePreviewUrlList, useImportUrlList } from '@/hooks/useWebsite';
import BulkImportOptions from './BulkImportOptions';
import BulkImportResultPanel from './BulkImportResultPanel';
import StatTiles from './StatTiles';
import { OUTLINED_BUTTON_SX, PRIMARY_BUTTON_SX, PAPER_SX, DEFAULT_IMPORT_OPTIONS, extractErrorMessage } from './importPageStyles';

/**
 * Paste a list of links. One per line is the idea, but commas, bullets and bare domains are
 * accepted. Same preview, options and result flow as the bookmarks file.
 */
function UrlListImport() {
  const [text, setText] = useState('');
  const [preview, setPreview] = useState(null);
  const [options, setOptions] = useState(DEFAULT_IMPORT_OPTIONS);
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');

  const previewMutation = usePreviewUrlList();
  const importMutation = useImportUrlList();
  const busy = previewMutation.isPending || importMutation.isPending;

  const validate = () => {
    if (!text.trim()) {
      setError('Paste at least one link.');
      return false;
    }
    setError('');
    return true;
  };

  const handlePreview = () => {
    if (!validate()) return;
    previewMutation.mutate(text, {
      onSuccess: setPreview,
      onError: (err) => setError(extractErrorMessage(err, 'Could not read the list.')),
    });
  };

  const handleImport = () => {
    if (!validate()) return;
    importMutation.mutate({ urls: text, options }, {
      onSuccess: (data) => {
        setResult(data);
        setPreview(null);
      },
      onError: (err) => setError(extractErrorMessage(err, 'The import failed.')),
    });
  };

  const reset = () => {
    setText('');
    setPreview(null);
    setResult(null);
    setError('');
    setOptions(DEFAULT_IMPORT_OPTIONS);
  };

  const previewStats = preview && [
    { label: 'Found', value: preview.validCount, color: 'text.primary' },
    { label: 'New', value: preview.newCount, color: '#4caf50' },
    { label: 'Already in library', value: preview.alreadyInLibraryCount, color: '#90caf9' },
    { label: 'Repeated', value: preview.duplicateInFileCount, color: 'text.secondary' },
    ...(preview.nonWebLinkCount ? [{ label: 'Not links', value: preview.nonWebLinkCount, color: 'text.secondary' }] : []),
  ];

  return (
    <Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>
      )}

      {!result && (
        <Paper sx={PAPER_SX}>
          <Typography variant="h6" sx={{ mb: 1 }}>Links</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            One per line. Commas, bullets and bare domains like <code>example.com</code> are fine.
            Each page is fetched afterwards for its title, description and image.
          </Typography>
          <TextField
            fullWidth
            multiline
            minRows={6}
            label="Links"
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder={'https://example.com/article\nhttps://another.example/page'}
            disabled={busy}
            sx={{ mb: 2 }}
          />
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
            <Button
              variant="outlined"
              onClick={handlePreview}
              disabled={busy || !text.trim()}
              startIcon={previewMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <Visibility />}
              sx={OUTLINED_BUTTON_SX}
            >
              {previewMutation.isPending ? 'Reading...' : 'Preview'}
            </Button>
            <Button
              variant="contained"
              onClick={handleImport}
              disabled={busy || !text.trim()}
              startIcon={importMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <Download />}
              sx={PRIMARY_BUTTON_SX}
            >
              {importMutation.isPending ? 'Importing...' : 'Import links'}
            </Button>
            {text && (
              <Button variant="text" onClick={reset} disabled={busy} sx={{ color: 'text.secondary' }}>
                Clear
              </Button>
            )}
          </Box>
        </Paper>
      )}

      {preview && !result && (
        <Paper sx={PAPER_SX} data-testid="url-list-preview">
          <Typography variant="h6" sx={{ mb: 2 }}>What the list holds</Typography>
          <StatTiles stats={previewStats} />
          <BulkImportOptions value={options} onChange={setOptions} disabled={busy} showFolderOptions={false} />
        </Paper>
      )}

      {result && (
        <>
          <BulkImportResultPanel result={result} />
          <Box sx={{ mt: 2 }}>
            <Button variant="text" onClick={reset} sx={{ color: 'text.secondary' }}>
              Import another list
            </Button>
          </Box>
        </>
      )}
    </Box>
  );
}

export default UrlListImport;
