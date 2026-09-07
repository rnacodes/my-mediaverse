import { Link as RouterLink } from 'react-router-dom';
import { Alert, AlertTitle, Box, Button, Typography } from '@mui/material';

/**
 * Media types that are not typed in by hand: each is created from its source, so this panel
 * sits above the Add Media form and points to the right importer. Keep the list in step with
 * the type selector in CommonFields — a type listed here is not offered there.
 */
const SOURCE_LINKS = [
  {
    label: 'Websites',
    description: 'Paste a URL, or import a bookmarks file from your browser.',
    to: '/import-website',
    action: 'Import a website',
  },
  {
    label: 'Articles & highlights',
    description: 'Synced from Readwise Reader.',
    to: '/import-media?section=readwise',
    action: 'Readwise sync',
  },
  {
    label: 'YouTube channels & playlists',
    description: 'Looked up from YouTube so episodes stay in sync.',
    to: '/import-media?section=youtube',
    action: 'YouTube import',
  },
];

function AddedFromSourcePanel() {
  return (
    <Alert severity="info" icon={false} sx={{ mb: 3, '& .MuiAlert-message': { width: '100%' } }} data-testid="added-from-source-panel">
      <AlertTitle sx={{ fontWeight: 'bold' }}>Some media is added from its source</AlertTitle>
      <Box component="ul" sx={{ listStyle: 'none', pl: 0, m: 0 }}>
        {SOURCE_LINKS.map((item) => (
          <Box
            component="li"
            key={item.to}
            sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, py: 0.75 }}
          >
            <Box>
              <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{item.label}</Typography>
              <Typography variant="body2" color="text.secondary">{item.description}</Typography>
            </Box>
            <Button component={RouterLink} to={item.to} size="small" variant="outlined" sx={{ whiteSpace: 'nowrap' }}>
              {item.action}
            </Button>
          </Box>
        ))}
      </Box>
      <Typography variant="body2" sx={{ mt: 1 }}>
        Books, movies, TV shows and podcasts can also be looked up on the{' '}
        <RouterLink to="/import-media">Import Media page</RouterLink> instead of typed in.
      </Typography>
    </Alert>
  );
}

export default AddedFromSourcePanel;
