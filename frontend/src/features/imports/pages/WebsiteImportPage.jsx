import { useNavigate, useSearchParams } from 'react-router-dom';
import { Box, Button, Container, Tab, Tabs, Typography } from '@mui/material';
import { Language, ArrowBack } from '@mui/icons-material';
import SingleUrlImport from './WebsiteImport/SingleUrlImport';
import BookmarkFileImport from './WebsiteImport/BookmarkFileImport';
import UrlListImport from './WebsiteImport/UrlListImport';
import BookmarkletCard from './WebsiteImport/BookmarkletCard';
import { TABS_SX } from './WebsiteImport/importPageStyles';

// Tab keys double as the ?tab= value so the Import Media page and the bookmarklet can deep-link.
const TABS = [
  { key: 'url', label: 'Single URL' },
  { key: 'file', label: 'Bookmarks file' },
  { key: 'paste', label: 'Paste URLs' },
];

function WebsiteImportPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const requested = searchParams.get('tab');
  const activeTab = TABS.some((t) => t.key === requested) ? requested : 'url';

  const selectTab = (event, key) => {
    const next = new URLSearchParams(searchParams);
    next.set('tab', key);
    // The bookmarklet's url belongs to the single-URL tab only.
    if (key !== 'url') next.delete('url');
    setSearchParams(next, { replace: true });
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ mb: 3 }}>
        <Typography variant="h3" gutterBottom sx={{ color: '#ffffff', fontWeight: 700 }}>
          <Language sx={{ fontSize: 40, verticalAlign: 'middle', mr: 2, color: '#90caf9' }} />
          Import Website
        </Typography>
        <Typography variant="body1" sx={{ color: 'text.secondary' }}>
          Save one page, a whole browser export, or a pasted list. Titles, descriptions, images and RSS feeds are filled in for you.
        </Typography>
      </Box>

      <Tabs value={activeTab} onChange={selectTab} sx={TABS_SX} aria-label="Import method">
        {TABS.map((t) => (
          <Tab key={t.key} value={t.key} label={t.label} id={`website-import-tab-${t.key}`} />
        ))}
      </Tabs>

      {activeTab === 'url' && (
        <>
          <SingleUrlImport />
          <BookmarkletCard />
        </>
      )}
      {activeTab === 'file' && <BookmarkFileImport />}
      {activeTab === 'paste' && <UrlListImport />}

      <Box sx={{ mt: 3 }}>
        <Button
          variant="outlined"
          onClick={() => navigate('/import-media')}
          startIcon={<ArrowBack />}
          sx={{
            borderColor: 'rgba(255, 255, 255, 0.5)',
            color: 'text.primary',
            '&:hover': { borderColor: 'rgba(255, 255, 255, 0.8)', backgroundColor: 'rgba(255, 255, 255, 0.05)' },
          }}
        >
          Back to Import Options
        </Button>
      </Box>
    </Container>
  );
}

export default WebsiteImportPage;
