import { useEffect, useRef } from 'react';
import { Box, Paper, Typography } from '@mui/material';
import { BookmarkAdd } from '@mui/icons-material';
import { buildBookmarklet } from './importPageStyles';

/**
 * A "Save to MyMediaVerse" link the user drags to the bookmarks bar. Clicking it on any page
 * opens this import page with that page's address filled in and previewed. A plain anchor,
 * not a button: browsers only let a real link be dragged into the bookmarks bar. The
 * javascript: href is set on the element directly because React refuses it as a prop.
 */
function BookmarkletCard() {
  const origin = typeof window !== 'undefined' ? window.location.origin : '';
  const linkRef = useRef(null);

  useEffect(() => {
    linkRef.current?.setAttribute('href', buildBookmarklet(origin));
  }, [origin]);

  return (
    <Paper sx={{ p: 3, mt: 3, backgroundColor: 'background.paper' }}>
      <Typography variant="h6" sx={{ mb: 1 }}>Save from your browser</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Drag this link to your bookmarks bar. On any page, click it to bring that page here, ready to save.
        In the demo, saving needs the unlock.
      </Typography>
      <Box
        component="a"
        ref={linkRef}
        draggable="true"
        onClick={(e) => e.preventDefault()}
        title="Drag me to your bookmarks bar"
        data-testid="bookmarklet-link"
        sx={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 1,
          px: 2,
          py: 1,
          borderRadius: 6,
          border: '1px solid #90caf9',
          color: '#90caf9',
          textDecoration: 'none',
          cursor: 'grab',
          fontWeight: 'bold',
          '&:hover': { backgroundColor: 'rgba(144, 202, 249, 0.08)' },
        }}
      >
        <BookmarkAdd fontSize="small" />
        Save to MyMediaVerse
      </Box>
    </Paper>
  );
}

export default BookmarkletCard;
