import { useRef, useState } from 'react';
import {
  Alert, AlertTitle, Box, Button, Chip, CircularProgress, Paper, Table, TableBody, TableCell,
  TableHead, TableRow, Typography,
} from '@mui/material';
import { CloudUpload, Download, Visibility } from '@mui/icons-material';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_IMPORT_BLOCKED } from '@/features/demo/demoMessages';
import { usePreviewBookmarkFile, useImportBookmarkFile } from '@/hooks/useWebsite';
import BulkImportOptions from './BulkImportOptions';
import BulkImportResultPanel from './BulkImportResultPanel';
import StatTiles from './StatTiles';
import { OUTLINED_BUTTON_SX, PRIMARY_BUTTON_SX, PAPER_SX, DEFAULT_IMPORT_OPTIONS, extractErrorMessage } from './importPageStyles';

const INPUT_ID = 'bookmark-file-input';

const isBookmarkFile = (file) => /\.html?$/i.test(file.name);

/**
 * Import a browser or bookmark-manager export: pick or drop the file, preview what it holds,
 * choose how folders and tags map to topics, import. Created websites are stubs; the result
 * panel offers to enrich them straight away.
 * `compact` is for the Import Media page, where the same component sits inside an accordion
 * and the demo needs its buttons guarded.
 */
function BookmarkFileImport({ compact = false }) {
  const inputRef = useRef(null);
  const [file, setFile] = useState(null);
  const [dragging, setDragging] = useState(false);
  const [preview, setPreview] = useState(null);
  const [options, setOptions] = useState(DEFAULT_IMPORT_OPTIONS);
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');

  const previewMutation = usePreviewBookmarkFile();
  const importMutation = useImportBookmarkFile();
  const busy = previewMutation.isPending || importMutation.isPending;

  const accept = (candidate) => {
    if (!candidate) return;
    if (!isBookmarkFile(candidate)) {
      setError('Please choose a bookmarks file (.html). Browsers and bookmark managers export one from their bookmark manager.');
      setFile(null);
      return;
    }
    setError('');
    setPreview(null);
    setResult(null);
    setFile(candidate);
  };

  const handleFileSelect = (event) => accept(event.target.files?.[0]);

  const handleDrop = (event) => {
    event.preventDefault();
    setDragging(false);
    accept(event.dataTransfer?.files?.[0]);
  };

  const handlePreview = () => {
    setError('');
    previewMutation.mutate(file, {
      onSuccess: setPreview,
      onError: (err) => setError(extractErrorMessage(err, 'Could not read the bookmarks file.')),
    });
  };

  const handleImport = () => {
    setError('');
    importMutation.mutate({ file, options }, {
      onSuccess: (data) => {
        setResult(data);
        setPreview(null);
      },
      onError: (err) => setError(extractErrorMessage(err, 'The import failed.')),
    });
  };

  const reset = () => {
    setFile(null);
    setPreview(null);
    setResult(null);
    setError('');
    setOptions(DEFAULT_IMPORT_OPTIONS);
    if (inputRef.current) inputRef.current.value = '';
  };

  const guard = (button) => (compact ? <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>{button}</DemoWriteGuard> : button);

  const previewStats = preview && [
    { label: 'Found', value: preview.validCount, color: 'text.primary' },
    { label: 'New', value: preview.newCount, color: '#4caf50' },
    { label: 'Already in library', value: preview.alreadyInLibraryCount, color: '#90caf9' },
    { label: 'Repeated in file', value: preview.duplicateInFileCount, color: 'text.secondary' },
    ...(preview.nonWebLinkCount ? [{ label: 'Not web links', value: preview.nonWebLinkCount, color: 'text.secondary' }] : []),
    ...(preview.invalidCount ? [{ label: 'Unusable', value: preview.invalidCount, color: '#f44336' }] : []),
  ];

  return (
    <Box>
      {!compact && (
        <Alert severity="info" sx={{ mb: 3 }}>
          Export your bookmarks from Chrome, Firefox, Edge, Safari, Brave or Vivaldi, or from Raindrop, Pinboard or linkding.
          Any bookmarks <strong>.html</strong> file works. Folders and tags become topics, and every page is fetched afterwards
          for its title, description and image.
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>
      )}

      {!result && (
        <Paper sx={compact ? { p: 2, mb: 2 } : PAPER_SX}>
          <Box
            onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
            onDragLeave={() => setDragging(false)}
            onDrop={handleDrop}
            data-testid="bookmark-dropzone"
            sx={{
              border: '2px dashed',
              borderColor: dragging ? '#90caf9' : 'rgba(255,255,255,0.3)',
              borderRadius: 2,
              p: compact ? 2 : 4,
              textAlign: 'center',
              backgroundColor: dragging ? 'rgba(144, 202, 249, 0.08)' : 'transparent',
              transition: 'border-color 120ms, background-color 120ms',
            }}
          >
            <CloudUpload sx={{ fontSize: compact ? 32 : 48, color: '#90caf9', mb: 1 }} />
            <Typography variant="body1" sx={{ mb: 2 }}>
              Drop your bookmarks file here
            </Typography>
            <input
              ref={inputRef}
              id={INPUT_ID}
              type="file"
              accept=".html,.htm"
              onChange={handleFileSelect}
              style={{ display: 'none' }}
              disabled={busy}
            />
            <label htmlFor={INPUT_ID}>
              <Button variant="outlined" component="span" disabled={busy} sx={OUTLINED_BUTTON_SX}>
                Choose file
              </Button>
            </label>
          </Box>

          {file && (
            <Alert severity="info" sx={{ mt: 2 }}>
              <AlertTitle>File selected</AlertTitle>
              {file.name} ({(file.size / 1024).toFixed(1)} KB)
            </Alert>
          )}

          <Box sx={{ display: 'flex', gap: 2, mt: 2, flexWrap: 'wrap' }}>
            {guard(
              <Button
                variant="outlined"
                onClick={handlePreview}
                disabled={!file || busy}
                startIcon={previewMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <Visibility />}
                sx={OUTLINED_BUTTON_SX}
              >
                {previewMutation.isPending ? 'Reading...' : 'Preview'}
              </Button>,
            )}
            {guard(
              <Button
                variant="contained"
                onClick={handleImport}
                disabled={!file || busy}
                startIcon={importMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <Download />}
                sx={PRIMARY_BUTTON_SX}
              >
                {importMutation.isPending ? 'Importing...' : 'Import bookmarks'}
              </Button>,
            )}
            {file && (
              <Button variant="text" onClick={reset} disabled={busy} sx={{ color: 'text.secondary' }}>
                Reset
              </Button>
            )}
          </Box>
        </Paper>
      )}

      {preview && !result && (
        <Paper sx={compact ? { p: 2, mb: 2 } : PAPER_SX} data-testid="bookmark-preview">
          <Typography variant="h6" sx={{ mb: 2 }}>What the file holds</Typography>
          <StatTiles stats={previewStats} />

          {preview.folders?.length > 0 && (
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 1 }}>
                Folders (each becomes a topic when the switch below is on)
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {preview.folders.map((folder) => (
                  <Chip key={folder} label={folder} size="small" />
                ))}
              </Box>
            </Box>
          )}

          {preview.sample?.length > 0 && (
            <Box sx={{ mb: 2, overflowX: 'auto' }}>
              <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 1 }}>First entries</Typography>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Title</TableCell>
                    <TableCell>URL</TableCell>
                    <TableCell>Folder</TableCell>
                    <TableCell />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {preview.sample.map((row) => (
                    <TableRow key={row.url}>
                      <TableCell>{row.title || <em>untitled</em>}</TableCell>
                      <TableCell sx={{ maxWidth: 320, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{row.url}</TableCell>
                      <TableCell>{row.folderPath || ''}</TableCell>
                      <TableCell>{row.alreadyInLibrary && <Chip label="in library" size="small" />}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>
          )}

          <BulkImportOptions value={options} onChange={setOptions} disabled={busy} />
        </Paper>
      )}

      {result && (
        <>
          <BulkImportResultPanel result={result} guard={compact ? guard : undefined} />
          <Box sx={{ mt: 2 }}>
            <Button variant="text" onClick={reset} sx={{ color: 'text.secondary' }}>
              Import another file
            </Button>
          </Box>
        </>
      )}
    </Box>
  );
}

export default BookmarkFileImport;
