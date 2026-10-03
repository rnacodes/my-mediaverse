import {
  Accordion, AccordionDetails, AccordionSummary, Alert, AlertTitle, Box, Button,
  List, ListItem, ListItemText, Paper, Typography,
} from '@mui/material';
import { ExpandMore } from '@mui/icons-material';
import StatTiles from '@/features/imports/pages/WebsiteImport/StatTiles';

const MAX_ERRORS_SHOWN = 10;

/**
 * What one sync or import run did, in the shape of the sync/import reporting contract:
 * a row of counts, the warnings, the per-item problems, and the fatal reason when the run
 * aborted. Each caller names its own title and which `*Count` fields to show.
 *
 * `stats` is a list of { label, key, color? }; `color` may be a function of the value.
 */
function ImportResultPanel({ result, title, failedTitle, stats, footer, onDismiss, testId = 'import-result' }) {
  if (!result) return null;

  const errors = result.errors ?? [];
  const warnings = result.warnings ?? [];
  const aborted = result.success === false;

  const tiles = stats.map(({ label, key, color }) => {
    const value = result[key] ?? 0;
    return {
      label,
      value,
      color: typeof color === 'function' ? color(value) : (color ?? 'text.secondary'),
    };
  });

  return (
    <Paper sx={{ p: 3, mb: 3, backgroundColor: 'background.paper' }} data-testid={testId}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">
          {aborted ? (failedTitle ?? `${title} failed`) : title}
        </Typography>
        {onDismiss && (
          <Button variant="text" onClick={onDismiss} sx={{ color: 'text.secondary' }}>
            Dismiss
          </Button>
        )}
      </Box>

      {aborted && (
        <Alert severity="error" sx={{ mb: 2 }}>
          <AlertTitle>Nothing was saved</AlertTitle>
          {result.errorMessage || 'The run stopped before anything was saved.'}
        </Alert>
      )}

      <StatTiles stats={tiles} />

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
        {typeof footer === 'function' ? footer(result) : footer}
        {result.reindexTriggered ? ' Search index updated.' : ''}
      </Typography>
    </Paper>
  );
}

export default ImportResultPanel;
