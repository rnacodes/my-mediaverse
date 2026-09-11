import { useState } from 'react';
import {
  Accordion, AccordionDetails, AccordionSummary, Alert, AlertTitle, Box, Button,
  CircularProgress, LinearProgress, List, ListItem, ListItemText, Paper, Typography,
} from '@mui/material';
import { AutoFixHigh, ExpandMore } from '@mui/icons-material';
import StatTiles from './StatTiles';
import { useEnrichmentRunner } from './useEnrichmentRunner';
import { OUTLINED_BUTTON_SX } from './importPageStyles';

const MAX_ERRORS_SHOWN = 10;

/**
 * What a bookmark-file or URL-list import did, plus the hand-off to enrichment: created rows
 * are stubs until the enrichment run fetches their titles, descriptions and images.
 */
function BulkImportResultPanel({ result, guard }) {
  const runner = useEnrichmentRunner();
  const [pendingAtStart] = useState(result.pendingEnrichmentCount ?? 0);

  if (!result) return null;

  const stats = [
    { label: 'Created', value: result.createdCount ?? 0, color: '#4caf50' },
    { label: 'Updated', value: result.updatedCount ?? 0, color: '#90caf9' },
    { label: 'Skipped', value: result.skippedCount ?? 0, color: 'text.secondary' },
    { label: 'Failed', value: result.failedCount ?? 0, color: result.failedCount ? '#f44336' : 'text.secondary' },
  ];
  if (result.nonWebLinkCount) {
    stats.push({ label: 'Not web links', value: result.nonWebLinkCount, color: 'text.secondary' });
  }

  const errors = result.errors ?? [];
  const pending = result.pendingEnrichmentCount ?? 0;
  const percent = runner.progress.total > 0 ? Math.round((runner.progress.done / runner.progress.total) * 100) : 0;

  // A run that stopped on an error can be picked up again: the server left the rest pending.
  const canRetry = runner.finished && !!runner.error;
  const enrichLabel = runner.running
    ? 'Enriching...'
    : canRetry
      ? 'Try again'
      : runner.finished
        ? 'Enrichment done'
        : `Enrich now (${pending} pending)`;

  const enrichButton = (
    <Button
      variant="outlined"
      onClick={() => runner.start(pending)}
      disabled={runner.running || pending === 0 || (runner.finished && !canRetry)}
      startIcon={runner.running ? <CircularProgress size={18} color="inherit" /> : <AutoFixHigh />}
      sx={OUTLINED_BUTTON_SX}
    >
      {enrichLabel}
    </Button>
  );

  return (
    <Paper sx={{ p: 3, backgroundColor: 'background.paper' }} data-testid="bulk-import-result">
      <Typography variant="h6" sx={{ mb: 2 }}>
        Import complete
      </Typography>

      <StatTiles stats={stats} />

      {result.warningMessage && (
        <Alert severity="warning" sx={{ mb: 2 }}>{result.warningMessage}</Alert>
      )}

      {errors.length > 0 && (
        <Accordion sx={{ mb: 2 }}>
          <AccordionSummary expandIcon={<ExpandMore />}>
            <Typography>Problems ({errors.length})</Typography>
          </AccordionSummary>
          <AccordionDetails>
            <List dense>
              {errors.slice(0, MAX_ERRORS_SHOWN).map((message) => (
                <ListItem key={message}>
                  <ListItemText primary={message} />
                </ListItem>
              ))}
            </List>
            {errors.length > MAX_ERRORS_SHOWN && (
              <Typography variant="body2" color="text.secondary">
                and {errors.length - MAX_ERRORS_SHOWN} more
              </Typography>
            )}
          </AccordionDetails>
        </Accordion>
      )}

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {result.foldersFound ? `${result.foldersFound} folder${result.foldersFound === 1 ? '' : 's'} found. ` : ''}
        {result.topicsCreatedCount ? `${result.topicsCreatedCount} new topic${result.topicsCreatedCount === 1 ? '' : 's'} created. ` : ''}
        {result.reindexTriggered ? 'Search index updated.' : ''}
      </Typography>

      <Box sx={{ borderTop: '1px solid rgba(255,255,255,0.12)', pt: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 'bold', mb: 0.5 }}>
          Fill in the details
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {pendingAtStart > 0
            ? `${pendingAtStart} website${pendingAtStart === 1 ? ' is' : 's are'} waiting for titles, descriptions, images and archive links. Enrichment fetches each page; it runs in pages of 50 so you can stop between them.`
            : 'Every website in the library already has its details.'}
        </Typography>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
          {guard ? guard(enrichButton) : enrichButton}
          {runner.running && (
            <Button variant="text" onClick={runner.cancel} sx={{ color: 'text.secondary' }}>
              Stop after this page
            </Button>
          )}
        </Box>

        {(runner.running || runner.finished) && runner.progress.total > 0 && (
          <Box sx={{ mt: 2 }}>
            <LinearProgress variant="determinate" value={percent} sx={{ mb: 1 }} />
            <Typography variant="body2" color="text.secondary">
              Enriched {runner.progress.done} of {runner.progress.total}
              {runner.totals.screenshotsRendered ? ` (${runner.totals.screenshotsRendered} screenshot${runner.totals.screenshotsRendered === 1 ? '' : 's'} rendered)` : ''}
            </Typography>
          </Box>
        )}

        {runner.finished && !runner.error && (
          <Alert severity={runner.totals.failed ? 'warning' : 'success'} sx={{ mt: 2 }}>
            <AlertTitle>Enrichment {runner.progress.done < runner.progress.total ? 'stopped' : 'finished'}</AlertTitle>
            {runner.totals.enriched} enriched, {runner.totals.unchanged} already complete, {runner.totals.skipped} unreachable, {runner.totals.failed} failed.
          </Alert>
        )}

        {runner.quotaReached && (
          <Alert severity="warning" sx={{ mt: 2 }}>
            The monthly screenshot budget ran out, so some websites were enriched without a screenshot. Rerun next month or switch the screenshot provider.
          </Alert>
        )}

        {runner.warning && !runner.quotaReached && (
          <Alert severity="info" sx={{ mt: 2 }}>{runner.warning}</Alert>
        )}

        {runner.error && (
          <Alert severity="error" sx={{ mt: 2 }}>{runner.error}</Alert>
        )}
      </Box>
    </Paper>
  );
}

export default BulkImportResultPanel;
