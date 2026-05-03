import React from 'react';
import { Box, Typography, Button, IconButton, CircularProgress } from '@mui/material';
import { ArrowBack, Edit, Sync } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';

function MediaHeader({ title, mediaId, onReindex, reindexing }) {
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const isTablet = useMediaQuery(theme.breakpoints.down('md'));

  return (
    <Box sx={{
      display: 'flex',
      flexDirection: { xs: 'column', sm: 'row' },
      alignItems: { xs: 'flex-start', sm: 'center' },
      justifyContent: 'space-between',
      gap: { xs: 2, sm: 0 },
      mb: 3
    }}>
      <IconButton onClick={() => navigate(-1)}>
        <ArrowBack />
      </IconButton>

      <Typography
        variant="h3"
        component="h2"
        gutterBottom
        sx={{
          fontWeight: 'bold',
          fontSize: { xs: '1.75rem', sm: '2rem', md: '2.5rem' },
          textAlign: { xs: 'center', md: 'left' }
        }}
      >
        {title || 'Untitled Media'}
      </Typography>

      {!isMobile && (
        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
          {onReindex && (
            <Button
              onClick={onReindex}
              startIcon={reindexing ? <CircularProgress size={16} /> : <Sync />}
              variant="outlined"
              size={isTablet ? 'small' : 'medium'}
              disabled={reindexing}
            >
              {reindexing ? 'Reindexing...' : 'Reindex'}
            </Button>
          )}
          <Button
            onClick={() => navigate(`/media/${mediaId}/edit`)}
            startIcon={<Edit />}
            variant="contained"
            size={isTablet ? 'medium' : 'large'}
          >
            Edit Media
          </Button>
        </Box>
      )}
    </Box>
  );
}

export default React.memo(MediaHeader);
