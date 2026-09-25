import {
  Accordion, AccordionDetails, AccordionSummary, Alert, AlertTitle, Box, Button,
  List, ListItem, ListItemText, Paper, Typography,
} from '@mui/material';
import { ExpandMore } from '@mui/icons-material';
import StatTiles from '@/features/imports/pages/WebsiteImport/StatTiles';

const MAX_ERRORS_SHOWN = 10;

/**
 * What one "Import episodes from TMDB" run did, in the shape of the sync/import
 * reporting contract: created / updated / skipped episodes, seasons that failed,
 * and the fatal reason when the run aborted.
 */
function TvEpisodeImportResultPanel({ result, onDismiss }) {
  if (!result) return null;

  const errors = result.errors ?? [];
  const warnings = result.warnings ?? [];
  const aborted = result.success === false;

  const stats = [
    { label: 'Created', value: result.createdCount ?? 0, color: '#4caf50' },
    { label: 'Updated', value: result.updatedCount ?? 0, color: '#90caf9' },
    { label: 'Skipped', value: result.skippedCount ?? 0, color: 'text.secondary' },
    { label: 'Failed seasons', value: result.failedCount ?? 0, color: result.failedCount ? '#f44336' : 'text.secondary' },
  ];

  return (
    <Paper sx={{ p: 3, mb: 3, backgroundColor: 'background.paper' }} data-testid="tv-episode-import-result">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">
          {aborted ? 'Episode import failed' : 'Episode import complete'}
        </Typography>
        {onDismiss && (
          <Button variant="text" onClick={onDismiss} sx={{ color: 'text.secondary' }}>
            Dismiss
          </Button>
        )}
      </Box>

      {aborted && (
        <Alert severity="error" sx={{ mb: 2 }}>
          <AlertTitle>Nothing was imported</AlertTitle>
          {result.errorMessage || 'The import stopped before any episodes were saved.'}
        </Alert>
      )}

      <StatTiles stats={stats} />

      {warnings.map((message) => (
        <Alert key={message} severity="warning" sx={{ mb: 2 }}>{message}</Alert>
      ))}

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

      <Typography variant="body2" color="text.secondary">
        {result.seasonsProcessed
          ? `${result.seasonsProcessed} season${result.seasonsProcessed === 1 ? '' : 's'} checked on TMDB. `
          : ''}
        {result.reindexTriggered ? 'Search index updated.' : ''}
      </Typography>
    </Paper>
  );
}

export default TvEpisodeImportResultPanel;
