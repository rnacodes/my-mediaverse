import { Box, Grid, Typography } from '@mui/material';
import { AddCircleOutline, ImportExport, Topic, BookmarkAdd } from '@mui/icons-material';

// One tile in the quick-actions bar. The icon color (#695a8c) and hover behavior are
// shared across tiles.
const ActionTile = ({ icon: Icon, label, onClick, hoverBg }) => (
  <Grid item xs={6} sm={6} md={3} sx={{ textAlign: 'center' }}>
    <Box
      onClick={onClick}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        cursor: 'pointer',
        color: 'text.primary',
        p: { xs: 1.5, sm: 2 },
        minHeight: { xs: '100px', sm: '120px' },
        borderRadius: '12px',
        transition: 'all 0.2s ease',
        '&:hover': {
          transform: 'scale(1.05)',
          backgroundColor: hoverBg
        },
        '&:active': {
          transform: 'scale(0.98)'
        }
      }}
    >
      <Icon sx={{ fontSize: { xs: 50, sm: 60, md: 70 }, color: '#695a8c' }} />
      <Typography
        variant="h5"
        sx={{
          mt: 1,
          fontSize: { xs: '0.9rem', sm: '1.1rem', md: '1.25rem' },
          fontWeight: 'bold'
        }}
      >
        {label}
      </Typography>
    </Box>
  </Grid>
);

// The card of primary actions below the media-type grid.
const QuickActionsBar = ({
  onCreateMixlist,
  onImportMedia,
  onSearchByTopicOrGenre,
  onAddMedia,
}) => (
  <Box sx={{
      mt: { xs: 2, sm: 3, md: 4 },
      p: { xs: 2, sm: 3 },
      backgroundColor: 'background.paper',
      borderRadius: '16px',
      boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
  }}>
    <Grid container spacing={{ xs: 2, sm: 3 }} alignItems="center" justifyContent="center">
      <ActionTile icon={AddCircleOutline} label="Create a Mixlist" onClick={onCreateMixlist} hoverBg="rgba(105, 90, 140, 0.1)" />
      <ActionTile icon={ImportExport} label="Import Media" onClick={onImportMedia} hoverBg="rgba(105, 90, 140, 0.1)" />
      <ActionTile icon={Topic} label="Browse Topics/Genres" onClick={onSearchByTopicOrGenre} hoverBg="rgba(105, 90, 140, 0.1)" />
      <ActionTile icon={BookmarkAdd} label="Add Media" onClick={onAddMedia} hoverBg="rgba(105, 90, 140, 0.1)" />
    </Grid>
  </Box>
);

export default QuickActionsBar;
